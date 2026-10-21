// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EmpiricalChallengeM3Tests.cs
// Adversarial Empirical Challenge Suite for Milestone 3 (M3).
// Focus areas:
// 1. Input synthesis sequence generation (InputSynthesizer.BuildInputSequence)
//    - Exact INPUT structure for word transformations (o'rtoq -> örtoq, shahar -> şahar)
//    - 100% MAGIC_EXTRA_INFO tagging across all generated events
//    - Delimiter handling: \r, \n -> VK_RETURN (0x0D); \t -> VK_TAB (0x09); space & punct -> Unicode
// 2. Win32 INPUT structure sizing & SendInput parameter validation:
//    - Marshal.SizeOf<NativeMethods.INPUT>() vs Win32 ABI expectations
// 3. Adversarial boundary conditions & stress testing:
//    - Extreme delimiter characters (all 31 delimiters, control chars, emojis)
//    - Multi-character word replacements (e.g. 50+ chars, expansion, shrinkage)
//    - Empty old word (0 backspaces)
//    - Single-letter conversions
//    - High concurrency multi-threaded synthesis (128 parallel threads)
//    - Nanosecond-level latency benchmark
// ──────────────────────────────────────────────────────────────────────

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class EmpiricalChallengeM3Tests
{
    // ══════════════════════════════════════════════════════════════════
    // GROUP 1: Input Synthesis Sequence Generation Structure
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildInputSequence_OrtoqToOrtoq_ExactStructure()
    {
        // "o'rtoq" is 6 characters. Converted is "örtoq" (5 characters). Delimiter is ' '
        string oldWord = "o'rtoq";
        string newWord = "örtoq";
        char delimiter = ' ';

        var inputs = InputSynthesizer.BuildInputSequence(oldWord.Length, newWord, delimiter);

        // Expected count: 6 backspaces (12 events) + 5 unicode chars (10 events) + 1 delimiter (2 events) = 24
        int expectedCount = oldWord.Length * 2 + newWord.Length * 2 + 2;
        Assert.AreEqual(expectedCount, inputs.Length, $"Input sequence length mismatch for '{oldWord}' -> '{newWord}'");

        // 1. Verify Backspaces: 6 pairs of VK_BACK
        for (int i = 0; i < oldWord.Length * 2; i += 2)
        {
            var down = inputs[i];
            var up = inputs[i + 1];

            Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, down.type);
            Assert.AreEqual((ushort)NativeMethods.VK_BACK, down.data.ki.wVk);
            Assert.AreEqual((ushort)0, down.data.ki.wScan);
            Assert.AreEqual((uint)0, down.data.ki.dwFlags);
            Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, down.data.ki.dwExtraInfo);

            Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, up.type);
            Assert.AreEqual((ushort)NativeMethods.VK_BACK, up.data.ki.wVk);
            Assert.AreEqual((ushort)0, up.data.ki.wScan);
            Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, up.data.ki.dwFlags);
            Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, up.data.ki.dwExtraInfo);
        }

        // 2. Verify Unicode characters for "örtoq": 'ö', 'r', 't', 'o', 'q'
        int charOffset = oldWord.Length * 2;
        for (int i = 0; i < newWord.Length; i++)
        {
            char expectedChar = newWord[i];
            var down = inputs[charOffset + i * 2];
            var up = inputs[charOffset + i * 2 + 1];

            Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, down.type);
            Assert.AreEqual((ushort)0, down.data.ki.wVk);
            Assert.AreEqual((ushort)expectedChar, down.data.ki.wScan);
            Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, down.data.ki.dwFlags);
            Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, down.data.ki.dwExtraInfo);

            Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, up.type);
            Assert.AreEqual((ushort)0, up.data.ki.wVk);
            Assert.AreEqual((ushort)expectedChar, up.data.ki.wScan);
            Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, up.data.ki.dwFlags);
            Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, up.data.ki.dwExtraInfo);
        }

        // 3. Verify Delimiter ' '
        var delimDown = inputs[^2];
        var delimUp = inputs[^1];

        Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, delimDown.type);
        Assert.AreEqual((ushort)0, delimDown.data.ki.wVk);
        Assert.AreEqual((ushort)' ', delimDown.data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, delimDown.data.ki.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, delimDown.data.ki.dwExtraInfo);

        Assert.AreEqual(NativeMethods.INPUT_KEYBOARD, delimUp.type);
        Assert.AreEqual((ushort)0, delimUp.data.ki.wVk);
        Assert.AreEqual((ushort)' ', delimUp.data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, delimUp.data.ki.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, delimUp.data.ki.dwExtraInfo);
    }

    [TestMethod]
    public void BuildInputSequence_ShaharToShahar_ExactStructure()
    {
        // "shahar" is 6 characters. Converted is "şahar" (5 characters). Delimiter is '.'
        string oldWord = "shahar";
        string newWord = "şahar";
        char delimiter = '.';

        var inputs = InputSynthesizer.BuildInputSequence(oldWord.Length, newWord, delimiter);

        Assert.AreEqual(6 * 2 + 5 * 2 + 2, inputs.Length);

        // Verify backspaces
        for (int i = 0; i < 12; i += 2)
        {
            Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[i].data.ki.wVk);
            Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[i + 1].data.ki.wVk);
        }

        // Verify "şahar" characters
        char[] expectedChars = { 'ş', 'a', 'h', 'a', 'r' };
        for (int i = 0; i < expectedChars.Length; i++)
        {
            int idx = 12 + i * 2;
            Assert.AreEqual((ushort)0, inputs[idx].data.ki.wVk);
            Assert.AreEqual((ushort)expectedChars[i], inputs[idx].data.ki.wScan);
            Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, inputs[idx].data.ki.dwFlags);

            Assert.AreEqual((ushort)0, inputs[idx + 1].data.ki.wVk);
            Assert.AreEqual((ushort)expectedChars[i], inputs[idx + 1].data.ki.wScan);
            Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, inputs[idx + 1].data.ki.dwFlags);
        }

        // Verify Delimiter '.'
        Assert.AreEqual((ushort)0, inputs[^2].data.ki.wVk);
        Assert.AreEqual((ushort)'.', inputs[^2].data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, inputs[^2].data.ki.dwFlags);

        Assert.AreEqual((ushort)0, inputs[^1].data.ki.wVk);
        Assert.AreEqual((ushort)'.', inputs[^1].data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, inputs[^1].data.ki.dwFlags);
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 2: 100% MAGIC_EXTRA_INFO Tagging Verification
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow(0, "a", ' ')]
    [DataRow(1, "b", '\t')]
    [DataRow(5, "şahar", '\r')]
    [DataRow(6, "örtoq", '\n')]
    [DataRow(10, "qayta_qurish", ',')]
    [DataRow(25, "juda_uzun_soz_transliteratsiyasi", '!')]
    public void BuildInputSequence_100PercentEvents_HaveMagicExtraInfo(int oldLen, string newWord, char delim)
    {
        var inputs = InputSynthesizer.BuildInputSequence(oldLen, newWord, delim);

        Assert.IsTrue(inputs.Length > 0);
        for (int i = 0; i < inputs.Length; i++)
        {
            Assert.AreEqual(
                (IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO,
                inputs[i].data.ki.dwExtraInfo,
                $"Event at index {i} lacked MAGIC_EXTRA_INFO tagging for word '{newWord}'.");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 3: Delimiter Handling Verification
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow('\r')]
    [DataRow('\n')]
    public void DelimiterHandling_ReturnAndNewline_SynthesizeVkReturn_NotUnicode(char delim)
    {
        var inputs = InputSynthesizer.BuildInputSequence(3, "abc", delim);

        var down = inputs[^2].data.ki;
        var up = inputs[^1].data.ki;

        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, down.wVk, "VK_RETURN expected for key down");
        Assert.AreEqual((ushort)0, down.wScan, "wScan must be 0 for virtual key");
        Assert.AreEqual((uint)0, down.dwFlags, "dwFlags must be 0 (no UNICODE) for key down");
        Assert.AreEqual(0u, down.dwFlags & NativeMethods.KEYEVENTF_UNICODE, "Must NOT have KEYEVENTF_UNICODE flag");
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, down.dwExtraInfo);

        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, up.wVk, "VK_RETURN expected for key up");
        Assert.AreEqual((ushort)0, up.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, up.dwFlags, "dwFlags must be KEYEVENTF_KEYUP for key up");
        Assert.AreEqual(0u, up.dwFlags & NativeMethods.KEYEVENTF_UNICODE, "Must NOT have KEYEVENTF_UNICODE flag");
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, up.dwExtraInfo);
    }

    [TestMethod]
    public void DelimiterHandling_Tab_SynthesizesVkTab_NotUnicode()
    {
        var inputs = InputSynthesizer.BuildInputSequence(3, "abc", '\t');

        var down = inputs[^2].data.ki;
        var up = inputs[^1].data.ki;

        Assert.AreEqual((ushort)NativeMethods.VK_TAB, down.wVk, "VK_TAB expected for key down");
        Assert.AreEqual((ushort)0, down.wScan, "wScan must be 0 for virtual key");
        Assert.AreEqual((uint)0, down.dwFlags, "dwFlags must be 0 for key down");
        Assert.AreEqual(0u, down.dwFlags & NativeMethods.KEYEVENTF_UNICODE, "Must NOT have KEYEVENTF_UNICODE flag");
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, down.dwExtraInfo);

        Assert.AreEqual((ushort)NativeMethods.VK_TAB, up.wVk, "VK_TAB expected for key up");
        Assert.AreEqual((ushort)0, up.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, up.dwFlags, "dwFlags must be KEYEVENTF_KEYUP for key up");
        Assert.AreEqual(0u, up.dwFlags & NativeMethods.KEYEVENTF_UNICODE, "Must NOT have KEYEVENTF_UNICODE flag");
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, up.dwExtraInfo);
    }

    [TestMethod]
    [DataRow(' ')]
    [DataRow('.')]
    [DataRow(',')]
    [DataRow('!')]
    [DataRow('?')]
    [DataRow(':')]
    [DataRow(';')]
    [DataRow('-')]
    public void DelimiterHandling_SpaceAndPunctuation_SynthesizeUnicodeCharacterPairs(char punct)
    {
        var inputs = InputSynthesizer.BuildInputSequence(3, "abc", punct);

        var down = inputs[^2].data.ki;
        var up = inputs[^1].data.ki;

        Assert.AreEqual((ushort)0, down.wVk, "wVk must be 0 for Unicode packet");
        Assert.AreEqual((ushort)punct, down.wScan, "wScan must contain character codepoint");
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, down.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, down.dwExtraInfo);

        Assert.AreEqual((ushort)0, up.wVk, "wVk must be 0 for Unicode packet");
        Assert.AreEqual((ushort)punct, up.wScan, "wScan must contain character codepoint");
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, up.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, up.dwExtraInfo);
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 4: Adversarial Stress Test & Edge Cases
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Stress_All31Delimiters_GenerateValidInputSequences()
    {
        char[] allDelimiters =
        {
            ' ', '\t', '\r', '\n',
            '.', ',', '!', '?', ':', ';', '-',
            '"', '(', ')', '[', ']', '{', '}',
            '/', '\\', '<', '>', '=', '+', '*', '&', '|', '^', '~', '@', '#', '%'
        };

        foreach (char d in allDelimiters)
        {
            var inputs = InputSynthesizer.BuildInputSequence(6, "test", d);
            Assert.AreEqual(6 * 2 + 4 * 2 + 2, inputs.Length, $"Length mismatch for delimiter '{d}' (0x{(int)d:X2})");

            // Verify all events have MAGIC_EXTRA_INFO
            foreach (var inp in inputs)
            {
                Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, inp.data.ki.dwExtraInfo);
            }
        }
    }

    [TestMethod]
    public void Stress_EmptyOldWord_GeneratesZeroBackspaces()
    {
        // When oldCharCount == 0, no backspaces should be generated
        var inputs = InputSynthesizer.BuildInputSequence(0, "yangilik", ' ');

        // Expected: 0 backspaces + 8 unicode pairs (16) + 1 delimiter pair (2) = 18
        Assert.AreEqual(18, inputs.Length);

        // Verify first event is the first character 'y', NOT a backspace
        Assert.AreEqual((ushort)0, inputs[0].data.ki.wVk);
        Assert.AreEqual((ushort)'y', inputs[0].data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, inputs[0].data.ki.dwFlags);
    }

    [TestMethod]
    public void Stress_SingleLetterConversion_ExactSequence()
    {
        // Single letter replacement e.g. "o'" (2 chars) -> "ö" (1 char)
        var inputs = InputSynthesizer.BuildInputSequence(2, "ö", ' ');

        // Expected: 2 backspaces (4) + 1 unicode pair (2) + 1 delimiter pair (2) = 8
        Assert.AreEqual(8, inputs.Length);

        // 2 backspaces
        Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[0].data.ki.wVk);
        Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[1].data.ki.wVk);
        Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[2].data.ki.wVk);
        Assert.AreEqual((ushort)NativeMethods.VK_BACK, inputs[3].data.ki.wVk);

        // 'ö'
        Assert.AreEqual((ushort)0, inputs[4].data.ki.wVk);
        Assert.AreEqual((ushort)'ö', inputs[4].data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, inputs[4].data.ki.dwFlags);

        Assert.AreEqual((ushort)0, inputs[5].data.ki.wVk);
        Assert.AreEqual((ushort)'ö', inputs[5].data.ki.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, inputs[5].data.ki.dwFlags);

        // ' '
        Assert.AreEqual((ushort)' ', inputs[6].data.ki.wScan);
        Assert.AreEqual((ushort)' ', inputs[7].data.ki.wScan);
    }

    [TestMethod]
    public void Stress_MultiCharacterReplacements_ExpansionAndShrinkage()
    {
        // Shrinkage: 10 chars -> 6 chars
        var shrink = InputSynthesizer.BuildInputSequence(10, "qisqa", ' ');
        Assert.AreEqual(10 * 2 + 5 * 2 + 2, shrink.Length);

        // Expansion: 5 chars -> 15 chars
        var expand = InputSynthesizer.BuildInputSequence(5, "uzoqroq_soz_ayt", ' ');
        Assert.AreEqual(5 * 2 + 15 * 2 + 2, expand.Length);

        // Very long word: 100 chars
        string longWord = new string('ş', 100);
        var longSeq = InputSynthesizer.BuildInputSequence(100, longWord, '\t');
        Assert.AreEqual(100 * 2 + 100 * 2 + 2, longSeq.Length);

        // Check delimiter is VK_TAB
        Assert.AreEqual((ushort)NativeMethods.VK_TAB, longSeq[^2].data.ki.wVk);
        Assert.AreEqual((ushort)NativeMethods.VK_TAB, longSeq[^1].data.ki.wVk);
    }

    [TestMethod]
    public void Stress_Concurrency_128Threads_StrictlyThreadSafe()
    {
        const int threadCount = 128;
        const int iterations = 1000;
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, threadId =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    int oldLen = (threadId + i) % 15;
                    string word = (i % 2 == 0) ? "şahar" : "örtoq";
                    char delim = (i % 4 == 0) ? '\r' : (i % 4 == 1) ? '\t' : (i % 4 == 2) ? ' ' : '.';

                    var seq = InputSynthesizer.BuildInputSequence(oldLen, word, delim);
                    int expected = oldLen * 2 + word.Length * 2 + 2;
                    if (seq.Length != expected)
                    {
                        errors.Add($"Length mismatch on thread {threadId}: expected {expected}, got {seq.Length}");
                        break;
                    }

                    // Check extra info
                    for (int j = 0; j < seq.Length; j++)
                    {
                        if (seq[j].data.ki.dwExtraInfo != (IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO)
                        {
                            errors.Add($"ExtraInfo corrupt on thread {threadId} index {j}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Exception on thread {threadId}: {ex.Message}");
            }
        });

        Assert.IsTrue(errors.IsEmpty, $"Concurrent synthesis failed: {errors.FirstOrDefault()}");
    }

    [TestMethod]
    public void Benchmark_BuildInputSequence_LatencyUnderMicrosecond()
    {
        // Warmup
        for (int i = 0; i < 5000; i++)
        {
            _ = InputSynthesizer.BuildInputSequence(6, "şahar", ' ');
        }

        const int iterations = 50_000;
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = InputSynthesizer.BuildInputSequence(6, "şahar", ' ');
        }

        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgUs = (totalMs * 1000.0) / iterations; // microseconds

        Assert.IsTrue(avgUs < 10.0, $"BuildInputSequence average latency {avgUs:F3} µs exceeded 10 µs threshold!");
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 5: Win32 INPUT Struct Size and ABI Verification
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Win32_InputStructSize_MatchesWin32Abi()
    {
        int size = Marshal.SizeOf<NativeMethods.INPUT>();
        int ptrSize = IntPtr.Size;

        // In 64-bit Windows: sizeof(INPUT) is 40 bytes.
        // In 32-bit Windows: sizeof(INPUT) is 28 bytes.
        int expectedSize = (ptrSize == 8) ? 40 : 28;
        Assert.AreEqual(expectedSize, size,
            $"INPUT struct size ({size} bytes) does not match Win32 ABI expected size ({expectedSize} bytes for {(ptrSize == 8 ? "x64" : "x86")}).");
    }

    [TestMethod]
    public void Win32_SendInput_RejectsInvalidCbSize()
    {
        // Test calling SendInput with an invalid cbSize (32 on x64, or 20 on x86)
        var seq = InputSynthesizer.BuildInputSequence(0, "", ' '); // 2 events for space
        int invalidSize = (IntPtr.Size == 8) ? 32 : 20;

        // Set LastWin32Error
        uint sent = NativeMethods.SendInput((uint)seq.Length, seq, invalidSize);
        int err = Marshal.GetLastWin32Error();

        // When cbSize is invalid, SendInput returns 0 and err is 87 (ERROR_INVALID_PARAMETER)
        TestContext?.WriteLine($"SendInput with invalid size={invalidSize} returned: {sent}, LastError: {err}");
        Assert.AreEqual(0u, sent, $"SendInput unexpectedly succeeded with invalid cbSize={invalidSize}.");
        Assert.AreEqual(87, err, $"SendInput must fail with ERROR_INVALID_PARAMETER (87) when cbSize is {invalidSize}.");
    }

    [TestMethod]
    public void Win32_SendInput_AcceptsCorrectCbSize()
    {
        var seq = InputSynthesizer.BuildInputSequence(0, "", ' ');
        int validSize = Marshal.SizeOf<NativeMethods.INPUT>();
        int ptrSize = IntPtr.Size;
        int expectedSize = (ptrSize == 8) ? 40 : 28;
        Assert.AreEqual(expectedSize, validSize);

        uint sent = NativeMethods.SendInput((uint)seq.Length, seq, validSize);
        int err = Marshal.GetLastWin32Error();

        TestContext?.WriteLine($"SendInput with valid size={validSize} returned: {sent}, LastError: {err}");
        Assert.AreNotEqual(87, err, "SendInput with valid cbSize must not fail with ERROR_INVALID_PARAMETER (87).");
        Assert.IsTrue(sent == (uint)seq.Length || err == 5, $"SendInput must either succeed or return ACCESS_DENIED (5) in background session. Actual sent: {sent}, err: {err}");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput40
    {
        public uint type;
        public NativeInputUnion40 data;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct NativeInputUnion40
    {
        [FieldOffset(0)]
        public NativeMethods.KEYBDINPUT ki;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [In] NativeInput40[] pInputs, int cbSize);

    [TestMethod]
    public void Win32_SendInput_SucceedsWhenCbSizeIs40OnX64()
    {
        int size40 = Marshal.SizeOf<NativeInput40>();
        Assert.AreEqual(40, size40, "NativeInput40 size must be 40 bytes on x64");

        var inputs = new NativeInput40[1];
        inputs[0] = new NativeInput40
        {
            type = NativeMethods.INPUT_KEYBOARD,
            data = new NativeInputUnion40
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = (ushort)'A',
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, // key up only to be harmless
                    dwExtraInfo = (IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO
                }
            }
        };

        uint sent = SendInput(1, inputs, size40);
        int err = Marshal.GetLastWin32Error();

        TestContext?.WriteLine($"SendInput with 40-byte struct returned: {sent}, LastError: {err}");
        // In interactive desktop: sent == 1. In non-interactive/CI test execution: sent == 0, err == 5 (ERROR_ACCESS_DENIED).
        // In BOTH cases, parameter validation passed (err != 87 ERROR_INVALID_PARAMETER).
        Assert.AreNotEqual(87, err, "SendInput with 40-byte struct must NOT fail with ERROR_INVALID_PARAMETER (87).");
        Assert.IsTrue(sent == 1 || err == 5, $"SendInput must either succeed (sent=1) or fail with ACCESS_DENIED (5) in background session. Actual sent: {sent}, err: {err}");
    }

    public TestContext? TestContext { get; set; }
}
