// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — NativeMethods.cs
// All Win32 P/Invoke signatures and structs in a single file.
// ──────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;

namespace YangiAlifbo.Win32;

/// <summary>
/// Win32 P/Invoke declarations for keyboard hooks, input synthesis,
/// focus tracking, hotkey registration, and process queries.
/// </summary>
public static class NativeMethods
{
    // ── Delegates ──────────────────────────────────────────────────────

    /// <summary>Low-level keyboard hook callback delegate.</summary>
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>Low-level mouse hook callback delegate.</summary>
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>WinEvent hook callback delegate for focus tracking.</summary>
    public delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    // ── Constants ──────────────────────────────────────────────────────

    // Hook types
    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL    = 14;

    // Key messages
    public const int WM_KEYDOWN    = 0x0100;
    public const int WM_KEYUP      = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP   = 0x0105;

    // Mouse messages
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP   = 0x0202;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP   = 0x0205;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MBUTTONUP   = 0x0208;
    public const int WM_XBUTTONDOWN = 0x020B;
    public const int WM_XBUTTONUP   = 0x020C;
    public const int WM_MOUSEWHEEL  = 0x020A;

    // Hotkey message
    public const int WM_HOTKEY = 0x0312;

    // Virtual key codes
    public const int VK_BACK      = 0x08;
    public const int VK_TAB       = 0x09;
    public const int VK_RETURN    = 0x0D;
    public const int VK_SHIFT     = 0x10;
    public const int VK_CONTROL   = 0x11;
    public const int VK_MENU      = 0x12; // Alt
    public const int VK_CAPITAL   = 0x14; // CapsLock
    public const int VK_ESCAPE    = 0x1B;
    public const int VK_SPACE     = 0x20;
    public const int VK_PRIOR     = 0x21; // Page Up
    public const int VK_NEXT      = 0x22; // Page Down
    public const int VK_END       = 0x23;
    public const int VK_HOME      = 0x24;
    public const int VK_LEFT      = 0x25;
    public const int VK_UP        = 0x26;
    public const int VK_RIGHT     = 0x27;
    public const int VK_DOWN      = 0x28;
    public const int VK_DELETE    = 0x2E;

    // Keyboard input flags
    public const uint INPUT_KEYBOARD     = 1;
    public const uint KEYEVENTF_KEYUP    = 0x0002;
    public const uint KEYEVENTF_UNICODE  = 0x0004;

    // WinEvent constants
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint EVENT_OBJECT_FOCUS      = 0x8005;
    public const uint WINEVENT_OUTOFCONTEXT   = 0x0000;

    // Win32 Edit / Password inspection constants
    public const int GWL_STYLE = -16;
    public const int ES_PASSWORD = 0x0020;
    public const uint EM_GETPASSWORDCHAR = 0x00D2;
    public const uint SMTO_ABORTIFHUNG = 0x0002;
    public const uint UIA_IsPasswordPropertyId = 30019;

    // Process name flags
    public const uint PROCESS_NAME_WIN32 = 0;

    // Hotkey modifiers
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT   = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;

    // KBDLLHOOKSTRUCT flags
    public const uint LLKHF_INJECTED = 0x00000010;

    // ── Structs ────────────────────────────────────────────────────────

    /// <summary>Point structure with x and y coordinates.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    /// <summary>Data structure received by the low-level mouse hook.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>Rectangle definition for window geometry.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    /// <summary>GUI thread info containing active and focused window handles.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    /// <summary>Data structure received by the low-level keyboard hook.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>Input event container for SendInput.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUTUNION data;
    }

    /// <summary>Union of input types (mouse, keyboard, hardware).</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    /// <summary>Mouse input event for SendInput.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    /// <summary>Keyboard input event for SendInput.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>Hardware input event for SendInput.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // ── Hook Functions ─────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(
        int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(
        int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(
        IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    // ── Input Synthesis ────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // ── Keyboard State ─────────────────────────────────────────────────

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetKeyboardState(ref byte lpKeyState);

    [DllImport("user32.dll")]
    public static extern int ToUnicodeEx(
        uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff,
        int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(
        uint wVirtKey, uint wScanCode, ref byte lpKeyState,
        ref char pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern short GetAsyncKeyState(int vKey);

    // ── Window / Focus ─────────────────────────────────────────────────

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    /// <summary>
    /// Architecture-safe retrieval of window style or long pointer values.
    /// </summary>
    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        return GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    // ── Module Handle ──────────────────────────────────────────────────

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ── Hotkey ─────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── Process Query ──────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageNameW(
        IntPtr hProcess, uint dwFlags,
        [Out] System.Text.StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint GetModuleFileNameEx(
        IntPtr hProcess, IntPtr hModule,
        [Out] System.Text.StringBuilder lpBaseName, uint nSize);

    // Process access rights
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
}
