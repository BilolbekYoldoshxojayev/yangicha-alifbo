// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — InputSynthesizer.cs
// Safely sends synthetic keystrokes via SendInput with a dwExtraInfo
// magic marker to prevent the keyboard hook from re-processing them.
// ──────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;

namespace YangiAlifbo.Win32;

/// <summary>
/// Sends synthetic keyboard input (backspaces, Unicode characters, and native delimiters)
/// using the Win32 SendInput API. All events are tagged with a magic
/// <c>dwExtraInfo</c> value so the keyboard hook can recognize and
/// pass them through without re-processing.
/// </summary>
public static class InputSynthesizer
{
    /// <summary>
    /// Magic marker in dwExtraInfo to identify our own synthetic events.
    /// ASCII for "YANG" = 0x59414E47
    /// </summary>
    public const long MAGIC_EXTRA_INFO = 0x59414E47;

    private static readonly int InputSize = Marshal.SizeOf<NativeMethods.INPUT>();

    /// <summary>
    /// Builds the exact sequence of INPUT structures needed to erase
    /// <paramref name="oldCharCount"/> characters, type <paramref name="newWord"/>,
    /// and type <paramref name="delimiter"/>. Pure function suitable for isolated unit testing.
    /// </summary>
    /// <param name="oldCharCount">Number of characters to erase via backspace.</param>
    /// <param name="newWord">The converted replacement word.</param>
    /// <param name="delimiter">The delimiter character that triggered conversion.</param>
    /// <returns>Array of Win32 INPUT structures ready for SendInput.</returns>
    public static NativeMethods.INPUT[] BuildInputSequence(int oldCharCount, string newWord, char delimiter)
    {
        var inputs = new List<NativeMethods.INPUT>(
            oldCharCount * 2 + newWord.Length * 2 + 2);

        // ── Step 1: Erase old word with Backspaces ─────────────────────
        for (int i = 0; i < oldCharCount; i++)
        {
            inputs.Add(MakeKeyDown(NativeMethods.VK_BACK));
            inputs.Add(MakeKeyUp(NativeMethods.VK_BACK));
        }

        // ── Step 2: Type the new word as Unicode ───────────────────────
        foreach (char c in newWord)
        {
            inputs.Add(MakeUnicodeDown(c));
            inputs.Add(MakeUnicodeUp(c));
        }

        // ── Step 3: Type the delimiter ─────────────────────────────────
        // Enter and Tab require native virtual keys for chat submit and form navigation
        if (delimiter is '\r' or '\n')
        {
            inputs.Add(MakeKeyDown(NativeMethods.VK_RETURN));
            inputs.Add(MakeKeyUp(NativeMethods.VK_RETURN));
        }
        else if (delimiter == '\t')
        {
            inputs.Add(MakeKeyDown(NativeMethods.VK_TAB));
            inputs.Add(MakeKeyUp(NativeMethods.VK_TAB));
        }
        else
        {
            inputs.Add(MakeUnicodeDown(delimiter));
            inputs.Add(MakeUnicodeUp(delimiter));
        }

        return inputs.ToArray();
    }

    /// <summary>
    /// Replaces the last <paramref name="oldCharCount"/> characters in the
    /// active application with <paramref name="newWord"/>, then sends the
    /// original <paramref name="delimiter"/> character.
    /// </summary>
    /// <param name="oldCharCount">
    /// Number of characters to erase (backspaces to send).
    /// </param>
    /// <param name="newWord">The replacement word (new Uzbek spelling).</param>
    /// <param name="delimiter">
    /// The word-boundary character that triggered the replacement (e.g. space).
    /// </param>
    public static void ReplaceWord(int oldCharCount, string newWord, char delimiter)
    {
        var inputArray = BuildInputSequence(oldCharCount, newWord, delimiter);
        NativeMethods.SendInput((uint)inputArray.Length, inputArray, InputSize);
    }

    /// <summary>Creates a KEY_DOWN event for a virtual key code.</summary>
    private static NativeMethods.INPUT MakeKeyDown(int vk)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            data = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = (IntPtr)MAGIC_EXTRA_INFO
                }
            }
        };
    }

    /// <summary>Creates a KEY_UP event for a virtual key code.</summary>
    private static NativeMethods.INPUT MakeKeyUp(int vk)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            data = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = 0,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = (IntPtr)MAGIC_EXTRA_INFO
                }
            }
        };
    }

    /// <summary>Creates a UNICODE KEY_DOWN event for a character.</summary>
    private static NativeMethods.INPUT MakeUnicodeDown(char c)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            data = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = (IntPtr)MAGIC_EXTRA_INFO
                }
            }
        };
    }

    /// <summary>Creates a UNICODE KEY_UP event for a character.</summary>
    private static NativeMethods.INPUT MakeUnicodeUp(char c)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            data = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = (IntPtr)MAGIC_EXTRA_INFO
                }
            }
        };
    }
}
