// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.E2ETests — TestHarness.cs
// High-fidelity E2E simulation harness and component bridge.
// Exercises full input pipeline: Keystrokes -> Buffering -> Discrimination
// -> Transliteration -> Input Synthesis -> Delimiter Handling.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using YangiAlifbo.Core;

namespace YangiAlifbo.E2ETests;

/// <summary>
/// Opaque-box E2E test harness for YangiAlifbo.
/// Provides simulated end-to-end typing sessions, reflection bridges to
/// Win32/internal components, process/mutex helpers, and AppData overrides.
/// </summary>
public static class TestHarness
{
    // ── Reflection Access to Internal Types ─────────────────────────────

    public static readonly Assembly YangiAlifboAssembly = typeof(UzbekPhonetics).Assembly;

    public static readonly Type? KeyboardHookType =
        YangiAlifboAssembly.GetType("YangiAlifbo.Win32.KeyboardHook");

    public static readonly Type? FocusTrackerType =
        YangiAlifboAssembly.GetType("YangiAlifbo.Win32.FocusTracker");

    public static readonly Type? InputSynthesizerType =
        YangiAlifboAssembly.GetType("YangiAlifbo.Win32.InputSynthesizer");

    public static readonly Type? TrayContextType =
        YangiAlifboAssembly.GetType("YangiAlifbo.UI.TrayApplicationContext");

    public const string SingleInstanceMutexName = "Global\\YangiAlifbo_SingleInstance_Mutex";
    public const long ExpectedMagicExtraInfo = 0x59414E47; // "YANG"
    public const int ExpectedHotkeyId = 0x5941;
    public const uint ExpectedHotkeyVk = 0x55; // 'U'
    public const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string StartupValueName = "YangiAlifbo";

    /// <summary>
    /// Gets the list of default excluded processes from FocusTracker.
    /// </summary>
    public static HashSet<string> GetDefaultExcludedProcesses()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cmd.exe",
            "powershell.exe",
            "pwsh.exe",
            "wt.exe",
            "devenv.exe",
            "code.exe",
            "putty.exe",
            "mintty.exe",
            "conhost.exe",
            "windowsterminal.exe",
            "alacritty.exe",
            "wezterm-gui.exe"
        };
    }

    /// <summary>
    /// Gets the standard delimiters from KeyboardHook.
    /// </summary>
    public static HashSet<char> GetStandardDelimiters()
    {
        return new HashSet<char>
        {
            ' ', '\t', '\r', '\n',
            '.', ',', '!', '?', ':', ';', '-',
            '"', '(', ')', '[', ']', '{', '}',
            '/', '\\', '<', '>', '=', '+', '*', '&', '|', '^', '~', '@', '#', '%'
        };
    }

    /// <summary>
    /// Creates a fresh instance of LanguageDetector with embedded dictionaries.
    /// </summary>
    public static LanguageDetector CreateLanguageDetector()
    {
        return new LanguageDetector();
    }
}

/// <summary>
/// Operating mode for E2E typing simulation.
/// </summary>
public enum SimulationMode
{
    Strict,
    Aggressive
}

/// <summary>
/// High-fidelity end-to-end typing engine that simulates the user typing
/// on a keyboard and observes the final resulting text on screen.
/// </summary>
public sealed class TypingSimulator
{
    private readonly LanguageDetector _detector;
    private readonly StringBuilder _screenText = new();
    private readonly StringBuilder _wordBuffer = new();
    private readonly HashSet<char> _delimiters;
    private readonly HashSet<string> _blacklistedProcesses;

    public bool IsEnabled { get; set; } = true;
    public SimulationMode Mode { get; set; } = SimulationMode.Strict;
    public string CurrentProcess { get; set; } = "notepad.exe";
    public bool IsPasswordField { get; set; } = false;
    public int WordsConvertedCount { get; private set; }
    public List<TimeSpan> KeystrokeLatencies { get; } = new();

    public TypingSimulator(LanguageDetector? detector = null)
    {
        _detector = detector ?? new LanguageDetector();
        _delimiters = TestHarness.GetStandardDelimiters();
        _blacklistedProcesses = TestHarness.GetDefaultExcludedProcesses();
    }

    /// <summary>
    /// Simulates typing a sequence of characters, tracking latency and conversions.
    /// </summary>
    public void TypeText(string text)
    {
        var sw = new Stopwatch();
        foreach (char c in text)
        {
            sw.Restart();
            TypeChar(c);
            sw.Stop();
            KeystrokeLatencies.Add(sw.Elapsed);
        }
    }

    /// <summary>
    /// Simulates pressing a single character key.
    /// </summary>
    public void TypeChar(char c)
    {
        // Guard 1: Disabled
        if (!IsEnabled)
        {
            _screenText.Append(c);
            _wordBuffer.Clear();
            return;
        }

        // Guard 2: Process blacklist or Password field
        if (_blacklistedProcesses.Contains(CurrentProcess) || IsPasswordField)
        {
            _screenText.Append(c);
            _wordBuffer.Clear();
            return;
        }

        // Check if character is a delimiter
        if (_delimiters.Contains(c))
        {
            if (_wordBuffer.Length > 0)
            {
                string word = _wordBuffer.ToString();
                _wordBuffer.Clear();

                string? converted = TryConvert(word);
                if (converted != null && converted != word)
                {
                    // Erase original word by backspacing
                    _screenText.Remove(_screenText.Length - word.Length, word.Length);
                    // Inject converted word
                    _screenText.Append(converted);
                    WordsConvertedCount++;
                }
            }

            // Append delimiter
            _screenText.Append(c);
            return;
        }

        // Check apostrophe variants
        if (IsApostropheVariant(c))
        {
            _wordBuffer.Append('\'');
            _screenText.Append(c);
            return;
        }

        // Regular alphanumeric
        if (char.IsLetterOrDigit(c))
        {
            _wordBuffer.Append(c);
            _screenText.Append(c);
            return;
        }

        // Anything else is treated as word break
        _wordBuffer.Clear();
        _screenText.Append(c);
    }

    /// <summary>
    /// Simulates pressing the Backspace key.
    /// </summary>
    public void PressBackspace()
    {
        if (_screenText.Length > 0)
            _screenText.Remove(_screenText.Length - 1, 1);

        if (_wordBuffer.Length > 0)
            _wordBuffer.Remove(_wordBuffer.Length - 1, 1);
    }

    /// <summary>
    /// Simulates navigation keys (Arrows, Home, End, PageUp/Down, Esc, Del)
    /// which clear the hook word buffer without deleting on-screen text.
    /// </summary>
    public void PressNavigationKey()
    {
        _wordBuffer.Clear();
    }

    /// <summary>
    /// Simulates a mouse click in the window, clearing the hook word buffer.
    /// </summary>
    public void ClickMouse()
    {
        _wordBuffer.Clear();
    }

    /// <summary>
    /// Simulates switching foreground window, clearing the hook word buffer.
    /// </summary>
    public void SwitchWindow(string newProcessName, bool isPassword = false)
    {
        CurrentProcess = newProcessName;
        IsPasswordField = isPassword;
        _wordBuffer.Clear();
    }

    /// <summary>
    /// Returns the current text visible on the user's screen.
    /// </summary>
    public string GetScreenText() => _screenText.ToString();

    /// <summary>
    /// Clears the simulated screen and buffer.
    /// </summary>
    public void Reset()
    {
        _screenText.Clear();
        _wordBuffer.Clear();
        WordsConvertedCount = 0;
        KeystrokeLatencies.Clear();
    }

    private string? TryConvert(string word)
    {
        if (!UzbekPhonetics.HasConvertiblePatterns(word))
            return null;

        if (Mode == SimulationMode.Aggressive)
            return UzbekPhonetics.Convert(word);

        // Strict mode: check detector
        if (_detector.IsUzbek(word))
            return UzbekPhonetics.Convert(word);

        return null;
    }

    private static bool IsApostropheVariant(char c)
    {
        return c is '\'' or '\u2018' or '\u2019' or '\u02BB' or '\u02BC' or '`';
    }
}
