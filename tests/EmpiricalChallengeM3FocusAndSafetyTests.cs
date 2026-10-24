// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EmpiricalChallengeM3FocusAndSafetyTests.cs
// Adversarial Empirical Challenge Suite for Milestone 3 (M3):
// FocusTracker Process Exclusion, Password Field Safety, Fail-Closed Security,
// and 100,000-iteration Performance Latency Benchmarks.
// ──────────────────────────────────────────────────────────────────────

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class EmpiricalChallengeM3FocusAndSafetyTests
{
    // ── Native Win32 P/Invokes for in-memory window creation ──────────

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int WS_POPUP = unchecked((int)0x80000000);
    private const uint EM_SETPASSWORDCHAR = 0x00CC;

    // ── Mock Implementations ──────────────────────────────────────────

    private sealed class MockProcessInfoProvider : IProcessInfoProvider
    {
        private readonly Func<uint, string?> _resolver;

        public MockProcessInfoProvider(Func<uint, string?> resolver)
        {
            _resolver = resolver;
        }

        public string? GetProcessName(uint processId) => _resolver(processId);
    }

    private sealed class MockFocusTracker : IFocusTracker
    {
        public bool IsCurrentProcessAllowed { get; set; } = true;
        public bool IsPasswordFieldFocused { get; set; } = false;
        public string CurrentProcessName { get; set; } = "notepad.exe";
        public event Action? ForegroundChanged;

        public void TriggerForegroundChanged() => ForegroundChanged?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 1: Process Exclusion & Casing Insensitivity
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ProcessExclusion_BlacklistCasingVariations_AllDisallowed()
    {
        string[] blacklistedProcessesWithVariations =
        {
            // cmd
            "cmd.exe", "CMD.EXE", "Cmd.Exe", "cMd.eXe",
            // powershell
            "powershell.exe", "POWERSHELL.EXE", "PowerShell.EXE", "PowerShell.exe",
            // pwsh
            "pwsh.exe", "PWSH.EXE", "Pwsh.Exe",
            // code
            "code.exe", "CODE.EXE", "Code.Exe", "CoDe.ExE",
            // devenv
            "devenv.exe", "DEVENV.EXE", "Devenv.Exe",
            // wt
            "wt.exe", "WT.EXE", "Wt.Exe",
            // bash
            "bash.exe", "BASH.EXE", "Bash.Exe",
            // wsl
            "wsl.exe", "WSL.EXE", "Wsl.Exe",
            // putty
            "putty.exe", "PUTTY.EXE", "PuTTY.exe",
            // mintty
            "mintty.exe", "MINTTY.EXE",
            // conhost
            "conhost.exe", "CONHOST.EXE",
            // windowsterminal
            "windowsterminal.exe", "WINDOWSTERMINAL.EXE", "WindowsTerminal.exe",
            // alacritty
            "alacritty.exe", "ALACRITTY.EXE", "Alacritty.exe",
            // wezterm-gui
            "wezterm-gui.exe", "WEZTERM-GUI.EXE", "WezTerm-Gui.exe"
        };

        foreach (var procName in blacklistedProcessesWithVariations)
        {
            var mock = new MockProcessInfoProvider(_ => procName);
            using var tracker = new FocusTracker(mock);
            tracker.UpdateProcess(4096);

            Assert.IsFalse(tracker.IsCurrentProcessAllowed,
                $"Security Violation: Blacklisted process '{procName}' must be disallowed regardless of casing!");
            Assert.AreEqual(procName, tracker.CurrentProcessName);
        }
    }

    [TestMethod]
    public void ProcessExclusion_UnblacklistedStandardApplications_Allowed()
    {
        string[] allowedProcesses =
        {
            "notepad.exe", "NOTEPAD.EXE",
            "winword.exe", "WINWORD.EXE",
            "telegram.exe", "Telegram.exe",
            "chrome.exe", "CHROME.EXE",
            "slack.exe", "SLACK.EXE",
            "msedge.exe", "excel.exe", "vlc.exe"
        };

        foreach (var procName in allowedProcesses)
        {
            var mock = new MockProcessInfoProvider(_ => procName);
            using var tracker = new FocusTracker(mock);
            tracker.UpdateProcess(2048);

            Assert.IsTrue(tracker.IsCurrentProcessAllowed,
                $"Usability Defect: Normal application '{procName}' should be allowed!");
            Assert.AreEqual(procName, tracker.CurrentProcessName);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 2: Fail-Closed Security Challenges
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ProcessExclusion_FailClosed_NullProviderReturnsFalse()
    {
        // Simulates OpenProcess or QueryFullProcessImageName returning access denied (null)
        var mock = new MockProcessInfoProvider(_ => null);
        using var tracker = new FocusTracker(mock);

        tracker.UpdateProcess(8888);

        Assert.IsFalse(tracker.IsCurrentProcessAllowed,
            "Security Violation: When process resolution returns null, FocusTracker must FAIL-CLOSED!");
        Assert.AreEqual(string.Empty, tracker.CurrentProcessName);
    }

    [TestMethod]
    public void ProcessExclusion_FailClosed_Win32ExceptionAccessDenied_MustReturnFalseNotThrowOrFailOpen()
    {
        // Simulates an access-denied Win32Exception(5) (ERROR_ACCESS_DENIED)
        var mock = new MockProcessInfoProvider(_ => throw new Win32Exception(5, "Access is denied"));
        using var tracker = new FocusTracker(mock);

        try
        {
            tracker.UpdateProcess(9999);
        }
        catch (Win32Exception)
        {
            // If UpdateProcess does not catch the exception internally,
            // check whether IsCurrentProcessAllowed is fail-closed
            Assert.IsFalse(tracker.IsCurrentProcessAllowed,
                "Security Vulnerability: FocusTracker failed open when Win32Exception(5) was thrown!");
            Assert.Fail("Unhandled Exception: FocusTracker.UpdateProcess must catch Win32Exception and fail closed rather than throwing!");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception type: {ex}");
        }

        Assert.IsFalse(tracker.IsCurrentProcessAllowed,
            "Security Violation: Access denied exception must enforce fail-closed (IsCurrentProcessAllowed == false)!");
    }

    [TestMethod]
    public void ProcessExclusion_FailClosed_UpdateCurrentState_MustNotFailOpenOnException()
    {
        var mock = new MockProcessInfoProvider(_ => throw new Win32Exception(5, "Access is denied"));
        using var tracker = new FocusTracker(mock);

        var updateMethod = typeof(FocusTracker).GetMethod("UpdateCurrentState", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(updateMethod);

        updateMethod.Invoke(tracker, null);

        Assert.IsFalse(tracker.IsCurrentProcessAllowed,
            "Security Vulnerability: FocusTracker.UpdateCurrentState caught exception and failed open!");
    }

    [TestMethod]
    public void ProcessExclusion_ZeroProcessId_HandledGracefully()
    {
        var mock = new MockProcessInfoProvider(_ => "notepad.exe");
        using var tracker = new FocusTracker(mock);

        tracker.UpdateProcess(0);

        Assert.AreEqual(string.Empty, tracker.CurrentProcessName);
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 3: Password Field Safety
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PasswordSafety_Win32Edit_ES_PASSWORD_Detected()
    {
        IntPtr hwnd = CreateWindowEx(
            0, "EDIT", "pw_test",
            WS_POPUP | NativeMethods.ES_PASSWORD,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create Win32 Edit window.");

        try
        {
            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsTrue(isPassword, "Edit control with ES_PASSWORD style (0x0020) must be detected as password field!");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [TestMethod]
    public void PasswordSafety_Win32Edit_EM_SETPASSWORDCHAR_Asterisk_Detected()
    {
        // Create an Edit control WITHOUT ES_PASSWORD style
        IntPtr hwnd = CreateWindowEx(
            0, "EDIT", "pw_dynamic_test",
            WS_POPUP,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create Win32 Edit window.");

        try
        {
            // Before setting password char -> should be false
            Assert.IsFalse(FocusTracker.IsWin32EditPassword(hwnd),
                "Precondition failed: Normal Edit without password char should not be detected as password.");

            // Dynamically set PasswordChar = '*' (0x002A) via EM_SETPASSWORDCHAR
            SendMessage(hwnd, EM_SETPASSWORDCHAR, (IntPtr)'*', IntPtr.Zero);

            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsTrue(isPassword,
                "Edit control with dynamic EM_SETPASSWORDCHAR ('*') must be detected as password field via EM_GETPASSWORDCHAR!");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [TestMethod]
    public void PasswordSafety_NormalEdit_ReturnsFalse()
    {
        IntPtr hwnd = CreateWindowEx(
            0, "EDIT", "normal_test",
            WS_POPUP,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create Win32 Edit window.");

        try
        {
            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsFalse(isPassword, "Standard Edit control must not be detected as a password field.");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [TestMethod]
    public void PasswordSafety_NonEditWindow_ReturnsFalse()
    {
        IntPtr hwnd = CreateWindowEx(
            0, "STATIC", "label_test",
            WS_POPUP,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create Win32 Static window.");

        try
        {
            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsFalse(isPassword, "Static text window must not be detected as a password field.");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 4: KeyboardHook Integration & Bypass on Password / Blacklist
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void KeyboardHook_WhenPasswordFieldFocused_BypassesTransliterationAndClearsBuffer()
    {
        using var detector = new LanguageDetector();
        var mockFocus = new MockFocusTracker
        {
            IsCurrentProcessAllowed = true,
            IsPasswordFieldFocused = true // PASSWORD FOCUSED!
        };

        using var hook = new KeyboardHook(detector, mockFocus);

        // Access internal _wordBuffer via reflection to simulate a partially typed word
        var wordBufferField = typeof(KeyboardHook).GetField("_wordBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(wordBufferField, "KeyboardHook must contain _wordBuffer field.");

        var wordBuffer = (StringBuilder)wordBufferField.GetValue(hook)!;
        wordBuffer.Append("shahar");
        Assert.AreEqual("shahar", wordBuffer.ToString());

        // Invoke HookCallback via reflection with a keystroke
        var hookCallbackMethod = typeof(KeyboardHook).GetMethod("HookCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hookCallbackMethod, "KeyboardHook must contain HookCallback method.");

        var hookStruct = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)NativeMethods.VK_SPACE,
            scanCode = 0x39,
            flags = 0,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        IntPtr lParam = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.KBDLLHOOKSTRUCT>());
        try
        {
            Marshal.StructureToPtr(hookStruct, lParam, false);

            var result = (IntPtr)hookCallbackMethod.Invoke(hook, new object[] { 0, (IntPtr)NativeMethods.WM_KEYDOWN, lParam })!;

            // Result must NOT be 1 (keystroke must not be suppressed)
            Assert.AreNotEqual((IntPtr)1, result, "Keystroke in password field must not be suppressed!");

            // Word buffer must be cleared immediately
            Assert.AreEqual(0, wordBuffer.Length, "Word buffer must be cleared when a password field is focused!");

            // Words converted count must remain 0
            Assert.AreEqual(0, hook.WordsConverted, "No transliteration must occur in a password field!");
        }
        finally
        {
            Marshal.FreeHGlobal(lParam);
        }
    }

    [TestMethod]
    public void KeyboardHook_WhenProcessDisallowed_BypassesTransliterationAndClearsBuffer()
    {
        using var detector = new LanguageDetector();
        var mockFocus = new MockFocusTracker
        {
            IsCurrentProcessAllowed = false, // TERMINAL / BLACKLISTED!
            IsPasswordFieldFocused = false
        };

        using var hook = new KeyboardHook(detector, mockFocus);

        var wordBufferField = typeof(KeyboardHook).GetField("_wordBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(wordBufferField);

        var wordBuffer = (StringBuilder)wordBufferField.GetValue(hook)!;
        wordBuffer.Append("o'rtoq");
        Assert.AreEqual("o'rtoq", wordBuffer.ToString());

        var hookCallbackMethod = typeof(KeyboardHook).GetMethod("HookCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(hookCallbackMethod);

        var hookStruct = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)NativeMethods.VK_SPACE,
            scanCode = 0x39,
            flags = 0,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        IntPtr lParam = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.KBDLLHOOKSTRUCT>());
        try
        {
            Marshal.StructureToPtr(hookStruct, lParam, false);

            var result = (IntPtr)hookCallbackMethod.Invoke(hook, new object[] { 0, (IntPtr)NativeMethods.WM_KEYDOWN, lParam })!;

            Assert.AreNotEqual((IntPtr)1, result, "Keystroke in disallowed process must not be suppressed!");
            Assert.AreEqual(0, wordBuffer.Length, "Word buffer must be cleared in disallowed process!");
            Assert.AreEqual(0, hook.WordsConverted, "No transliteration must occur in disallowed process!");
        }
        finally
        {
            Marshal.FreeHGlobal(lParam);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 5: Performance & Latency Benchmarks (100,000 iterations)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Benchmark_100k_BuildInputSequence_LatencyFarBelow1ms()
    {
        const int iterations = 100_000;
        var words = new (int oldLen, string newWord, char delim)[]
        {
            (6, "şahar", ' '),
            (6, "örtoq", '\r'),
            (7, "ğalaba", '\t'),
            (8, "çoyxona", '.'),
            (14, "o'zbekistonlik", '!')
        };

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            var item = words[i % words.Length];
            InputSynthesizer.BuildInputSequence(item.oldLen, item.newWord, item.delim);
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var item = words[i % words.Length];
            var seq = InputSynthesizer.BuildInputSequence(item.oldLen, item.newWord, item.delim);
            if (seq.Length == 0) Assert.Fail();
        }
        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgUsPerOp = (totalMs * 1000.0) / iterations;
        double avgMsPerOp = totalMs / iterations;

        Console.WriteLine($"[Benchmark] 100,000 BuildInputSequence: {totalMs:F2} ms total, {avgUsPerOp:F3} µs/op ({avgMsPerOp:F6} ms/op)");

        // Requirement: Latency must be far below 1ms (< 0.05ms target, requirement < 5ms)
        Assert.IsTrue(avgMsPerOp < 1.0,
            $"Average BuildInputSequence latency must be far below 1ms! Actual: {avgMsPerOp:F4} ms ({avgUsPerOp:F2} µs)");
        Assert.IsTrue(avgUsPerOp < 50.0,
            $"Average BuildInputSequence latency should be < 50 µs! Actual: {avgUsPerOp:F2} µs");
    }

    [TestMethod]
    public void Benchmark_100k_FocusTracker_O1_CachedReads()
    {
        const int iterations = 100_000;
        var mock = new MockProcessInfoProvider(_ => "notepad.exe");
        using var tracker = new FocusTracker(mock);
        tracker.UpdateProcess(1234);

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            _ = tracker.IsCurrentProcessAllowed;
            _ = tracker.IsPasswordFieldFocused;
        }

        var sw = Stopwatch.StartNew();
        bool allowed = true;
        bool isPw = false;
        for (int i = 0; i < iterations; i++)
        {
            allowed &= tracker.IsCurrentProcessAllowed;
            isPw |= tracker.IsPasswordFieldFocused;
        }
        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgNsPerRead = (totalMs * 1_000_000.0) / (iterations * 2);

        Console.WriteLine($"[Benchmark] 200,000 FocusTracker property reads: {totalMs:F2} ms total, {avgNsPerRead:F2} ns/read");

        Assert.IsTrue(allowed, "Precondition failed: notepad should be allowed.");
        Assert.IsFalse(isPw, "Precondition failed: notepad should not be password.");

        // O(1) in-memory cached read must be < 1000 ns (< 1 µs)
        Assert.IsTrue(avgNsPerRead < 1000.0,
            $"FocusTracker cached reads must be < 1000 ns! Actual: {avgNsPerRead:F2} ns");
    }
}
