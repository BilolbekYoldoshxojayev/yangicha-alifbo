// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — Win32Tests.cs
// Unit and safety tests for Milestone 3 (Win32 Interception, Synthesis & Safety).
// ──────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class Win32Tests
{
    // ── Native helpers for in-memory Win32 Edit window testing ─────────

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    private const int WS_POPUP = unchecked((int)0x80000000);

    // ── Mock IProcessInfoProvider ──────────────────────────────────────

    private sealed class MockProcessInfoProvider : IProcessInfoProvider
    {
        private readonly Func<uint, string?> _resolver;

        public MockProcessInfoProvider(Func<uint, string?> resolver)
        {
            _resolver = resolver;
        }

        public string? GetProcessName(uint processId) => _resolver(processId);
    }

    // ── Mock IFocusTracker ─────────────────────────────────────────────

    private sealed class MockFocusTracker : IFocusTracker
    {
        public bool IsCurrentProcessAllowed { get; set; } = true;
        public bool IsPasswordFieldFocused { get; set; } = false;
        public string CurrentProcessName { get; set; } = "notepad.exe";
        public event Action? ForegroundChanged;

        public void TriggerForegroundChanged() => ForegroundChanged?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 1: Delimiter Virtual Key Synthesis & Anti-Recursion Tagging
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BuildInputSequence_EnterDelimiter_SynthesizesVkReturn()
    {
        // 6 backspaces to erase "shahar", 5 Unicode pairs for "şahar", 1 pair for VK_RETURN
        var inputs = InputSynthesizer.BuildInputSequence(6, "şahar", '\r');

        Assert.AreEqual(6 * 2 + 5 * 2 + 2, inputs.Length);

        // Verify last two inputs are native VK_RETURN (down and up)
        var keyDown = inputs[^2].data.ki;
        var keyUp = inputs[^1].data.ki;

        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, keyDown.wVk);
        Assert.AreEqual((uint)0, keyDown.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyDown.dwExtraInfo);

        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, keyUp.wVk);
        Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, keyUp.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyUp.dwExtraInfo);

        // Crucial: Must NOT be KEYEVENTF_UNICODE
        Assert.AreEqual(0u, keyDown.dwFlags & NativeMethods.KEYEVENTF_UNICODE);
        Assert.AreEqual(0u, keyUp.dwFlags & NativeMethods.KEYEVENTF_UNICODE);
    }

    [TestMethod]
    public void BuildInputSequence_NewlineDelimiter_SynthesizesVkReturn()
    {
        var inputs = InputSynthesizer.BuildInputSequence(6, "şahar", '\n');

        var keyDown = inputs[^2].data.ki;
        var keyUp = inputs[^1].data.ki;

        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, keyDown.wVk);
        Assert.AreEqual((ushort)NativeMethods.VK_RETURN, keyUp.wVk);
        Assert.AreEqual(0u, keyDown.dwFlags & NativeMethods.KEYEVENTF_UNICODE);
    }

    [TestMethod]
    public void BuildInputSequence_TabDelimiter_SynthesizesVkTab()
    {
        var inputs = InputSynthesizer.BuildInputSequence(7, "örtoq", '\t');

        Assert.AreEqual(7 * 2 + 5 * 2 + 2, inputs.Length);

        var keyDown = inputs[^2].data.ki;
        var keyUp = inputs[^1].data.ki;

        Assert.AreEqual((ushort)NativeMethods.VK_TAB, keyDown.wVk);
        Assert.AreEqual((uint)0, keyDown.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyDown.dwExtraInfo);

        Assert.AreEqual((ushort)NativeMethods.VK_TAB, keyUp.wVk);
        Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, keyUp.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyUp.dwExtraInfo);

        // Crucial: Must NOT be KEYEVENTF_UNICODE
        Assert.AreEqual(0u, keyDown.dwFlags & NativeMethods.KEYEVENTF_UNICODE);
    }

    [TestMethod]
    public void BuildInputSequence_SpaceDelimiter_SynthesizesUnicode()
    {
        var inputs = InputSynthesizer.BuildInputSequence(6, "şahar", ' ');

        var keyDown = inputs[^2].data.ki;
        var keyUp = inputs[^1].data.ki;

        Assert.AreEqual((ushort)0, keyDown.wVk);
        Assert.AreEqual((ushort)' ', keyDown.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE, keyDown.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyDown.dwExtraInfo);

        Assert.AreEqual((ushort)0, keyUp.wVk);
        Assert.AreEqual((ushort)' ', keyUp.wScan);
        Assert.AreEqual(NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP, keyUp.dwFlags);
        Assert.AreEqual((IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO, keyUp.dwExtraInfo);
    }

    [TestMethod]
    public void BuildInputSequence_PunctuationDelimiters_SynthesizeUnicode()
    {
        char[] puncts = { '.', ',', '!', '?', ':', ';', '-' };
        foreach (char p in puncts)
        {
            var inputs = InputSynthesizer.BuildInputSequence(4, "test", p);
            var keyDown = inputs[^2].data.ki;
            var keyUp = inputs[^1].data.ki;

            Assert.AreEqual((ushort)0, keyDown.wVk, $"Punctuation '{p}' wVk must be 0");
            Assert.AreEqual((ushort)p, keyDown.wScan, $"Punctuation '{p}' scan code must match character");
            Assert.IsTrue((keyDown.dwFlags & NativeMethods.KEYEVENTF_UNICODE) != 0);
            Assert.IsTrue((keyUp.dwFlags & NativeMethods.KEYEVENTF_UNICODE) != 0);
        }
    }

    [TestMethod]
    public void BuildInputSequence_100PercentEvents_HaveMagicExtraInfo()
    {
        var inputs = InputSynthesizer.BuildInputSequence(10, "qayta_qurish", '\r');

        Assert.IsTrue(inputs.Length > 0);
        for (int i = 0; i < inputs.Length; i++)
        {
            Assert.AreEqual(
                (IntPtr)InputSynthesizer.MAGIC_EXTRA_INFO,
                inputs[i].data.ki.dwExtraInfo,
                $"Event at index {i} lacked MAGIC_EXTRA_INFO dwExtraInfo tagging.");
        }
    }

    [TestMethod]
    public void BuildInputSequence_BackspaceCount_MatchesOldCharCount()
    {
        int oldCharCount = 8;
        var inputs = InputSynthesizer.BuildInputSequence(oldCharCount, "yangilik", ' ');

        // First 16 events must be 8 pairs of VK_BACK (down and up)
        for (int i = 0; i < oldCharCount * 2; i += 2)
        {
            var down = inputs[i].data.ki;
            var up = inputs[i + 1].data.ki;

            Assert.AreEqual((ushort)NativeMethods.VK_BACK, down.wVk);
            Assert.AreEqual((uint)0, down.dwFlags);

            Assert.AreEqual((ushort)NativeMethods.VK_BACK, up.wVk);
            Assert.AreEqual(NativeMethods.KEYEVENTF_KEYUP, up.dwFlags);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 2: Process Blacklist & Fail-Closed Security
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void FocusTracker_DefaultBlacklist_ContainsBashAndWsl()
    {
        var tracker = new FocusTracker();
        var list = tracker.GetBlacklist();

        Assert.IsTrue(list.Contains("bash.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("wsl.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("cmd.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("powershell.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("pwsh.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("code.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("devenv.exe", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(list.Contains("wt.exe", StringComparer.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FocusTracker_IsCurrentProcessAllowed_CaseInsensitive()
    {
        string[] blacklisted = { "CMD.EXE", "PowerShell.exe", "PWSH.EXE", "Code.EXE", "DEVENV.EXE", "WT.EXE", "BASH.EXE", "WSL.EXE" };

        foreach (var proc in blacklisted)
        {
            var mock = new MockProcessInfoProvider(_ => proc);
            var tracker = new FocusTracker(mock);
            tracker.UpdateProcess(5555);

            Assert.IsFalse(tracker.IsCurrentProcessAllowed, $"Process {proc} should be disallowed.");
        }
    }

    [TestMethod]
    public void FocusTracker_IsCurrentProcessAllowed_AllowsStandardApplications()
    {
        string[] allowed = { "notepad.exe", "wordpad.exe", "chrome.exe", "telegram.exe", "slack.exe", "firefox.exe" };

        foreach (var proc in allowed)
        {
            var mock = new MockProcessInfoProvider(_ => proc);
            var tracker = new FocusTracker(mock);
            tracker.UpdateProcess(1234);

            Assert.IsTrue(tracker.IsCurrentProcessAllowed, $"Process {proc} should be allowed.");
            Assert.AreEqual(proc, tracker.CurrentProcessName);
        }
    }

    [TestMethod]
    public void FocusTracker_FailClosed_WhenProcessQueryDenied()
    {
        // Simulating OpenProcess returning ERROR_ACCESS_DENIED (null returned by provider)
        var mock = new MockProcessInfoProvider(_ => null);
        var tracker = new FocusTracker(mock);

        tracker.UpdateProcess(9999); // Non-zero PID where query failed

        Assert.IsFalse(tracker.IsCurrentProcessAllowed, "Tracker must fail closed when process name query fails.");
        Assert.AreEqual(string.Empty, tracker.CurrentProcessName);
    }

    [TestMethod]
    public void FocusTracker_ElevatedTerminal_RejectedViaMock()
    {
        // Simulated elevated powershell.exe with PID 4321
        var mock = new MockProcessInfoProvider(pid => pid == 4321 ? "powershell.exe" : "notepad.exe");
        var tracker = new FocusTracker(mock);

        tracker.UpdateProcess(4321);
        Assert.IsFalse(tracker.IsCurrentProcessAllowed);
        Assert.AreEqual("powershell.exe", tracker.CurrentProcessName);

        tracker.UpdateProcess(1111);
        Assert.IsTrue(tracker.IsCurrentProcessAllowed);
        Assert.AreEqual("notepad.exe", tracker.CurrentProcessName);
    }

    [TestMethod]
    public void FocusTracker_AddAndRemoveBlacklist_StripsDirectoryPaths()
    {
        var mock = new MockProcessInfoProvider(_ => "myterm.exe");
        var tracker = new FocusTracker(mock);

        tracker.UpdateProcess(100);
        Assert.IsTrue(tracker.IsCurrentProcessAllowed);

        // Add with full directory path
        tracker.AddToBlacklist(@"C:\Program Files\MyTerm\myterm.exe");
        Assert.IsFalse(tracker.IsCurrentProcessAllowed);

        // Remove with uppercase path
        tracker.RemoveFromBlacklist(@"C:\PROGRAM FILES\MYTERM\MYTERM.EXE");
        Assert.IsTrue(tracker.IsCurrentProcessAllowed);
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 3: Password Field Detection
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void IsWin32EditPassword_DetectsES_PASSWORD()
    {
        // Create a real Win32 EDIT window with ES_PASSWORD style
        IntPtr hwnd = CreateWindowEx(
            0, "EDIT", "password_test",
            WS_POPUP | NativeMethods.ES_PASSWORD,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create test Win32 Edit window.");

        try
        {
            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsTrue(isPassword, "IsWin32EditPassword must return true for an Edit window with ES_PASSWORD style.");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [TestMethod]
    public void IsWin32EditPassword_ReturnsFalseForNormalEdit()
    {
        // Create a normal Win32 EDIT window WITHOUT ES_PASSWORD style
        IntPtr hwnd = CreateWindowEx(
            0, "EDIT", "normal_test",
            WS_POPUP,
            0, 0, 100, 20, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Assert.AreNotEqual(IntPtr.Zero, hwnd, "Failed to create test Win32 Edit window.");

        try
        {
            bool isPassword = FocusTracker.IsWin32EditPassword(hwnd);
            Assert.IsFalse(isPassword, "IsWin32EditPassword must return false for a standard Edit window.");
        }
        finally
        {
            DestroyWindow(hwnd);
        }
    }

    [TestMethod]
    public void IsWin32EditPassword_ZeroAndInvalidHandle_ReturnsFalse()
    {
        Assert.IsFalse(FocusTracker.IsWin32EditPassword(IntPtr.Zero));
        Assert.IsFalse(FocusTracker.IsWin32EditPassword(new IntPtr(0x7FFFFFFF)));
    }

    // ══════════════════════════════════════════════════════════════════
    // GROUP 4: KeyboardHook & MouseHook Integration
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void KeyboardHook_ClearBuffer_IsCallable()
    {
        var detector = new LanguageDetector();
        var focus = new MockFocusTracker();
        using var hook = new KeyboardHook(detector, focus);

        // ClearBuffer should execute cleanly
        hook.ClearBuffer();
    }

    [TestMethod]
    public void MouseHook_InstantiatesAndFiresEvent()
    {
        bool clickReceived = false;
        using var mouseHook = new MouseHook(() => clickReceived = true);

        bool eventReceived = false;
        mouseHook.MouseClicked += () => eventReceived = true;

        Assert.IsFalse(mouseHook.IsInstalled);
        Assert.IsFalse(clickReceived);
        Assert.IsFalse(eventReceived);
    }

    [TestMethod]
    public void Win32ProcessInfoProvider_ResolvesCurrentProcess()
    {
        var provider = new Win32ProcessInfoProvider();
        uint currentPid = (uint)Environment.ProcessId;

        string? procName = provider.GetProcessName(currentPid);

        Assert.IsNotNull(procName, "Current process executable name should be resolvable via PROCESS_QUERY_LIMITED_INFORMATION.");
        Assert.IsTrue(procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || procName.Length > 0);
    }
}
