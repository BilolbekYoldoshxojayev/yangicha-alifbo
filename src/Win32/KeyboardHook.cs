// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — KeyboardHook.cs
// Global low-level keyboard hook (WH_KEYBOARD_LL) that captures
// keystrokes, maintains a word buffer, and triggers transliteration
// when a word delimiter is pressed.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using YangiAlifbo.Core;

namespace YangiAlifbo.Win32;

/// <summary>
/// Installs a global low-level keyboard hook. Accumulates typed
/// characters into a word buffer and, on word boundary delimiters,
/// decides whether to transliterate the buffered word.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    // ── Fields ─────────────────────────────────────────────────────────

    private IntPtr _hookId = IntPtr.Zero;

    /// <summary>
    /// Must be stored as an instance field to prevent GC from collecting
    /// the delegate while the hook is active.
    /// </summary>
    private readonly NativeMethods.LowLevelKeyboardProc _hookProc;

    /// <summary>Rolling word buffer — accumulates characters as the user types.</summary>
    private readonly StringBuilder _wordBuffer = new(64);

    /// <summary>Reference to the transliteration engine for Uzbek phonetic rules.</summary>
    private readonly UzbekPhonetics _phonetics = new();

    /// <summary>Reference to the language detector for Uzbek/foreign discrimination.</summary>
    private readonly LanguageDetector _detector;

    /// <summary>Focus tracker for process blacklisting and password field detection.</summary>
    private readonly IFocusTracker _focusTracker;

    /// <summary>Mouse hook to invalidate word buffer on mouse clicks.</summary>
    private readonly MouseHook? _mouseHook;

    /// <summary>Whether the hook is actively processing (user can toggle).</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Operating mode: Strict = dictionary verified, Aggressive = convert all.
    /// </summary>
    public TransliterationMode Mode { get; set; } = TransliterationMode.Strict;

    /// <summary>Whether chat normalizer (colloquial words, contractions, w->sh, unaccented vowels) is active.</summary>
    public bool IsChatSlangNormalizerEnabled { get; set; } = true;

    /// <summary>Running count of words converted this session (for statistics).</summary>
    public int WordsConverted { get; private set; }

    /// <summary>
    /// Set of delimiter characters that trigger word processing.
    /// </summary>
    private static readonly HashSet<char> Delimiters = new()
    {
        ' ', '\t', '\r', '\n',
        '.', ',', '!', '?', ':', ';', '-',
        '"', '(', ')', '[', ']', '{', '}',
        '/', '\\', '<', '>', '=', '+', '*', '&', '|', '^', '~', '@', '#', '%',
    };

    /// <summary>
    /// Virtual key codes that should clear the word buffer because they
    /// move the caret or disrupt sequential character entry.
    /// </summary>
    private static readonly HashSet<int> BufferClearKeys = new()
    {
        NativeMethods.VK_LEFT,
        NativeMethods.VK_RIGHT,
        NativeMethods.VK_UP,
        NativeMethods.VK_DOWN,
        NativeMethods.VK_HOME,
        NativeMethods.VK_END,
        NativeMethods.VK_PRIOR, // Page Up
        NativeMethods.VK_NEXT,  // Page Down
        NativeMethods.VK_ESCAPE,
        NativeMethods.VK_DELETE,
    };

    /// <summary>
    /// Apostrophe-like characters that Uzbek uses between letters.
    /// These are captured into the buffer as a canonical apostrophe.
    /// </summary>
    private static readonly HashSet<char> ApostropheChars = new()
    {
        '\'',     // U+0027  APOSTROPHE
        '\u2018', // U+2018  LEFT SINGLE QUOTATION MARK
        '\u2019', // U+2019  RIGHT SINGLE QUOTATION MARK
        '\u02BB', // U+02BB  MODIFIER LETTER TURNED COMMA
        '\u02BC', // U+02BC  MODIFIER LETTER APOSTROPHE
        '`',      // U+0060  GRAVE ACCENT
    };

    // Track whether we're currently injecting to avoid processing our own events
    private bool _isSelfInjecting;

    // ── Constructor ────────────────────────────────────────────────────

    public KeyboardHook(LanguageDetector detector, IFocusTracker focusTracker, MouseHook? mouseHook = null)
    {
        _detector = detector;
        _focusTracker = focusTracker;
        _mouseHook = mouseHook ?? new MouseHook(ClearBuffer);

        // Store the delegate as a field so GC doesn't collect it
        _hookProc = HookCallback;

        // Clear buffer whenever the user switches windows
        _focusTracker.ForegroundChanged += OnForegroundChanged;
    }

    // ── Public API ─────────────────────────────────────────────────────

    /// <summary>Installs the global low-level keyboard hook and mouse hook.</summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero) return; // Already installed

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);

        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to install keyboard hook. Error: {Marshal.GetLastWin32Error()}");
        }

        try
        {
            _mouseHook?.Install();
        }
        catch
        {
            // Defensive: if mouse hook installation fails, keyboard hook remains operational
        }
    }

    /// <summary>Uninstalls the global keyboard hook and mouse hook.</summary>
    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        _mouseHook?.Uninstall();
    }

    /// <summary>Clears the word buffer (e.g. on mouse click or focus change).</summary>
    public void ClearBuffer() => _wordBuffer.Clear();

    // ── Hook Callback ──────────────────────────────────────────────────

    /// <summary>
    /// The low-level keyboard hook callback. Processes every keystroke
    /// in the system. Must return quickly (&lt; 5ms).
    /// </summary>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // If nCode < 0, we must pass to next hook without processing
        if (nCode < 0)
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

        // ── Guard 1: Self-injection check ──────────────────────────
        // If this event was generated by our own SendInput, pass it through
        if (_isSelfInjecting || (long)hookStruct.dwExtraInfo == InputSynthesizer.MAGIC_EXTRA_INFO)
        {
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // Only process key-down events
        int msg = (int)wParam;
        if (msg != NativeMethods.WM_KEYDOWN && msg != NativeMethods.WM_SYSKEYDOWN)
        {
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Guard 2: Enabled check ────────────────────────────────
        if (!IsEnabled)
        {
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Guard 3: Process blacklist check ──────────────────────
        if (!_focusTracker.IsCurrentProcessAllowed)
        {
            _wordBuffer.Clear();
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Guard 4: Password field check ─────────────────────────
        if (_focusTracker.IsPasswordFieldFocused)
        {
            _wordBuffer.Clear();
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        int vk = (int)hookStruct.vkCode;

        // ── Handle shortcuts with Ctrl (Ctrl+V, Ctrl+X, Ctrl+Z, Ctrl+A) ──
        if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0 &&
            (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) == 0)
        {
            if (vk is 'V' or 'v' or 'X' or 'x' or 'Z' or 'z' or 'A' or 'a')
            {
                _wordBuffer.Clear();
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }
        }

        // ── Handle Backspace ──────────────────────────────────────
        if (vk == NativeMethods.VK_BACK)
        {
            if (_wordBuffer.Length > 0)
                _wordBuffer.Remove(_wordBuffer.Length - 1, 1);
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Handle navigation keys → clear buffer ─────────────────
        if (BufferClearKeys.Contains(vk))
        {
            _wordBuffer.Clear();
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Translate virtual key to character (Zero Allocation) ──
        char? typedChar = VirtualKeyToChar(hookStruct);

        if (typedChar == null)
        {
            // Modifier-only keys (Shift, Ctrl, Alt) — ignore
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        char ch = typedChar.Value;

        // ── Check if it's a hyphen in an ordinal number or compound word (e.g. 5-chi, 1-o'rin, kel-chi) ──
        if (ch == '-' && _wordBuffer.Length > 0 && char.IsLetterOrDigit(_wordBuffer[_wordBuffer.Length - 1]) && _wordBuffer[_wordBuffer.Length - 1] != '-')
        {
            _wordBuffer.Append('-');
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Check if it's a delimiter ─────────────────────────────
        if (IsDelimiter(ch, vk))
        {
            // Process the buffered word (if any)
            if (_wordBuffer.Length > 0)
            {
                string word = _wordBuffer.ToString();
                _wordBuffer.Clear();

                string? converted = TryConvertWord(word);
                if (converted != null && converted != word)
                {
                    // Suppress the delimiter keystroke — we'll send it ourselves
                    // after the replacement
                    _isSelfInjecting = true;
                    try
                    {
                        InputSynthesizer.ReplaceWord(word.Length, converted, ch);
                        WordsConverted++;
                    }
                    finally
                    {
                        _isSelfInjecting = false;
                    }

                    // Return 1 to suppress the original delimiter
                    return (IntPtr)1;
                }
            }
            else
            {
                _wordBuffer.Clear();
            }

            // Let the delimiter pass through normally
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Check if it's an apostrophe-like character ────────────
        if (ApostropheChars.Contains(ch))
        {
            // Apostrophes are part of Uzbek words (o', g', Is'hoq)
            _wordBuffer.Append('\'');
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Regular alphanumeric character → append to buffer ─────
        if (char.IsLetterOrDigit(ch))
        {
            _wordBuffer.Append(ch);
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Anything else → treat as word break, clear buffer ─────
        _wordBuffer.Clear();
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // ── Word Conversion Logic ──────────────────────────────────────────

    /// <summary>
    /// Attempts to convert a word from 1995 Uzbek Latin to the reformed alphabet.
    /// Returns the converted word, or null if no conversion should happen.
    /// </summary>
    private string? TryConvertWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return null;

        // Tier 1: Absolute protection for English and foreign words
        if (_detector.IsForeignOrReject(word))
            return null;

        string workWord = word;
        if (IsChatSlangNormalizerEnabled)
        {
            workWord = ChatNormalizer.Normalize(word);
        }

        // Quick check: does the workWord contain convertible patterns?
        if (!UzbekPhonetics.HasConvertiblePatterns(workWord))
        {
            // If the word was expanded/normalized (e.g. contraction like bilmadm -> bilmadim, or kirasz -> kirasiz),
            // and it changed from the original word:
            if (workWord != word && _detector.IsUzbek(workWord))
            {
                return workWord;
            }
            return null;
        }

        // In Aggressive mode, skip dictionary checks
        if (Mode == TransliterationMode.Aggressive)
        {
            return UzbekPhonetics.Convert(workWord);
        }

        // Strict mode: verify the word is Uzbek before converting
        if (_detector.IsUzbek(workWord))
        {
            return UzbekPhonetics.Convert(workWord);
        }

        return null;
    }

    // ── Helper Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Determines if a character/VK is a word delimiter.
    /// </summary>
    private static bool IsDelimiter(char ch, int vk)
    {
        if (vk == NativeMethods.VK_SPACE || vk == NativeMethods.VK_RETURN || vk == NativeMethods.VK_TAB)
            return true;

        return Delimiters.Contains(ch);
    }

    /// <summary>
    /// Converts a virtual key code + scan code to a Unicode character
    /// using stack-allocated buffers (zero heap allocations) and
    /// preserving dead-key states (wFlags = 4).
    /// </summary>
    private static char? VirtualKeyToChar(NativeMethods.KBDLLHOOKSTRUCT hookStruct)
    {
        int vk = (int)hookStruct.vkCode;

        // Skip modifier-only keys
        if (vk is NativeMethods.VK_SHIFT or NativeMethods.VK_CONTROL or NativeMethods.VK_MENU
            or 0xA0 or 0xA1  // VK_LSHIFT, VK_RSHIFT
            or 0xA2 or 0xA3  // VK_LCONTROL, VK_RCONTROL
            or 0xA4 or 0xA5  // VK_LMENU, VK_RMENU
            or 0x5B or 0x5C) // VK_LWIN, VK_RWIN
        {
            return null;
        }

        // Skip function keys and other non-character keys
        if (vk is >= 0x70 and <= 0x87) // F1-F24
            return null;

        // 1. Stack allocate 256 bytes for keyboard state (0 bytes on heap)
        Span<byte> keyboardState = stackalloc byte[256];
        NativeMethods.GetKeyboardState(ref keyboardState[0]);

        // 2. Synchronize real-time physical modifier states
        SynchronizeModifierState(keyboardState);

        // 3. If Ctrl is held (excluding Ctrl+Alt which is AltGr), don't translate
        bool ctrlDown = (keyboardState[NativeMethods.VK_CONTROL] & 0x80) != 0;
        bool altDown = (keyboardState[NativeMethods.VK_MENU] & 0x80) != 0;
        if (ctrlDown && !altDown)
            return null;

        // 4. Stack allocate 4 chars for ToUnicodeEx output (0 bytes on heap)
        Span<char> charBuffer = stackalloc char[4];

        uint threadId = NativeMethods.GetWindowThreadProcessId(
            NativeMethods.GetForegroundWindow(), out _);
        IntPtr layout = NativeMethods.GetKeyboardLayout(threadId);

        // 5. Translate virtual key with wFlags = 4 (DO_NOT_ALTER_KEY_STATE)
        const uint TO_UNICODE_DO_NOT_ALTER_STATE = 4;
        int result = NativeMethods.ToUnicodeEx(
            hookStruct.vkCode,
            hookStruct.scanCode,
            ref keyboardState[0],
            ref charBuffer[0],
            charBuffer.Length,
            TO_UNICODE_DO_NOT_ALTER_STATE,
            layout);

        if (result >= 1)
        {
            return charBuffer[0];
        }

        return null;
    }

    /// <summary>
    /// Synchronizes physical modifier key states into the keyboard state buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SynchronizeModifierState(Span<byte> keyboardState)
    {
        keyboardState[NativeMethods.VK_SHIFT] = (byte)((NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0 ? 0x80 : 0);
        keyboardState[NativeMethods.VK_CONTROL] = (byte)((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0 ? 0x80 : 0);
        keyboardState[NativeMethods.VK_MENU] = (byte)((NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0 ? 0x80 : 0);
        keyboardState[NativeMethods.VK_CAPITAL] = (byte)((NativeMethods.GetKeyState(NativeMethods.VK_CAPITAL) & 0x0001) != 0 ? 0x01 : 0);
    }

    private void OnForegroundChanged()
    {
        ClearBuffer();
    }

    // ── IDisposable ────────────────────────────────────────────────────

    public void Dispose()
    {
        _focusTracker.ForegroundChanged -= OnForegroundChanged;
        Uninstall();
        _mouseHook?.Dispose();
    }
}

/// <summary>
/// Transliteration mode determines how strictly words are verified.
/// </summary>
public enum TransliterationMode
{
    /// <summary>
    /// Only convert words confirmed to be Uzbek by the dictionary
    /// and morphological analyzer. Default and safest mode.
    /// </summary>
    Strict,

    /// <summary>
    /// Convert all words containing sh/ch/o'/g' patterns without
    /// dictionary verification. Use at your own risk.
    /// </summary>
    Aggressive
}
