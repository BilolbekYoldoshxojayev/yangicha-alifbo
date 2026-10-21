// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — FocusTracker.cs
// Monitors active window changes and determines whether the current
// focused application is in the process blacklist or has a password
// field focused.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YangiAlifbo.Win32;

/// <summary>
/// Tracks the foreground window and exposes whether keystrokes should
/// be intercepted (i.e., the current process is not blacklisted and
/// the focused control is not a password field).
/// </summary>
public sealed class FocusTracker : IFocusTracker, IDisposable
{
    /// <summary>
    /// Raised when the foreground window changes. The hook consumer
    /// should clear its word buffer on this event.
    /// </summary>
    public event Action? ForegroundChanged;

    /// <summary>
    /// Processes where transliteration is disabled (terminals, IDEs, etc.).
    /// Stored as lowercase filenames without path.
    /// </summary>
    private readonly HashSet<string> _processBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd.exe",
        "powershell.exe",
        "pwsh.exe",
        "wt.exe",           // Windows Terminal
        "devenv.exe",       // Visual Studio
        "code.exe",         // VS Code
        "bash.exe",         // Git Bash / WSL
        "wsl.exe",          // WSL launcher
        "putty.exe",
        "mintty.exe",
        "conhost.exe",
        "windowsterminal.exe",
        "alacritty.exe",
        "wezterm-gui.exe",
    };

    private readonly object _blacklistLock = new();
    private readonly IProcessInfoProvider _processInfoProvider;
    private IntPtr _winEventHook;
    private NativeMethods.WinEventDelegate? _winEventProc;
    private string _currentProcessName = string.Empty;
    private bool _isAccessDenied;
    private volatile bool _isPasswordFieldFocused;
    private bool _disposed;

    // UI Automation COM references
    private IUIAutomation? _uiAutomation;
    private IUIAutomationFocusChangedEventHandler? _uiaFocusHandler;

    /// <summary>
    /// Gets whether the current foreground process is safe for
    /// transliteration (not in the blacklist and not access denied).
    /// </summary>
    public bool IsCurrentProcessAllowed
    {
        get
        {
            if (_isAccessDenied) return false;
            lock (_blacklistLock)
            {
                return !_processBlacklist.Contains(_currentProcessName);
            }
        }
    }

    /// <summary>
    /// Gets whether the currently focused control is a password field.
    /// Fast O(1) non-blocking check for low-level hook callbacks.
    /// </summary>
    public bool IsPasswordFieldFocused
    {
        get => _isPasswordFieldFocused;
        internal set => _isPasswordFieldFocused = value;
    }

    /// <summary>
    /// Gets the current foreground process name.
    /// </summary>
    public string CurrentProcessName => _currentProcessName;

    /// <summary>
    /// Initializes a new instance of <see cref="FocusTracker"/>.
    /// </summary>
    /// <param name="processInfoProvider">Optional process info provider for testing.</param>
    public FocusTracker(IProcessInfoProvider? processInfoProvider = null)
    {
        _processInfoProvider = processInfoProvider ?? new Win32ProcessInfoProvider();
    }

    /// <summary>Installs the WinEvent hook and UI Automation focus tracker.</summary>
    public void Install()
    {
        // Store delegate reference to prevent GC collection
        _winEventProc = OnWinEvent;
        _winEventHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventProc,
            0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        // Try initializing UI Automation focus change event handler
        TryInitializeUiAutomation();

        // Initialize with current foreground window
        UpdateCurrentState();
    }

    /// <summary>Uninstalls hooks and event handlers.</summary>
    public void Uninstall()
    {
        if (_winEventHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }

        TryTeardownUiAutomation();
    }

    /// <summary>
    /// Adds a process name to the blacklist at runtime.
    /// </summary>
    public void AddToBlacklist(string processFileName)
    {
        if (!string.IsNullOrWhiteSpace(processFileName))
        {
            lock (_blacklistLock)
            {
                _processBlacklist.Add(Path.GetFileName(processFileName));
            }
        }
    }

    /// <summary>
    /// Removes a process name from the blacklist at runtime.
    /// </summary>
    public void RemoveFromBlacklist(string processFileName)
    {
        if (!string.IsNullOrWhiteSpace(processFileName))
        {
            lock (_blacklistLock)
            {
                _processBlacklist.Remove(Path.GetFileName(processFileName));
            }
        }
    }

    /// <summary>Returns a copy of the current blacklist for display.</summary>
    public IReadOnlyCollection<string> GetBlacklist()
    {
        lock (_blacklistLock)
        {
            return _processBlacklist.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if a Win32 window has the ES_PASSWORD style or dynamic password char set.
    /// </summary>
    public static bool IsWin32EditPassword(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;

        try
        {
            long style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE).ToInt64();
            if ((style & NativeMethods.ES_PASSWORD) != 0)
            {
                return true;
            }

            NativeMethods.SendMessageTimeout(
                hwnd,
                NativeMethods.EM_GETPASSWORDCHAR,
                IntPtr.Zero,
                IntPtr.Zero,
                NativeMethods.SMTO_ABORTIFHUNG,
                20,
                out IntPtr result);

            if (result != IntPtr.Zero)
            {
                return true;
            }
        }
        catch
        {
            // Defensive guard on invalid window handles
        }

        return false;
    }

    /// <summary>
    /// Updates the process name and access-denied state for a given process ID.
    /// Internal for deterministic unit testing.
    /// </summary>
    internal void UpdateProcess(uint processId)
    {
        if (processId == 0)
        {
            _currentProcessName = string.Empty;
            _isAccessDenied = false;
            return;
        }

        try
        {
            string? resolved = _processInfoProvider.GetProcessName(processId);
            if (resolved == null)
            {
                // Fail-closed: access denied or query failed
                _currentProcessName = string.Empty;
                _isAccessDenied = true;
            }
            else
            {
                _currentProcessName = resolved;
                _isAccessDenied = false;
            }
        }
        catch
        {
            // Fail-closed on any exception (e.g. Win32Exception(5) Access Denied)
            _currentProcessName = string.Empty;
            _isAccessDenied = true;
        }
    }

    private void OnWinEvent(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        UpdateCurrentState();
        ForegroundChanged?.Invoke();
    }

    /// <summary>
    /// Queries the foreground window's process name and password state.
    /// </summary>
    private void UpdateCurrentState()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                _currentProcessName = string.Empty;
                _isAccessDenied = true;
                _isPasswordFieldFocused = false;
                return;
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            UpdateProcess(processId);

            // Win32 Edit check via GUITHREADINFO
            NativeMethods.GUITHREADINFO gui = new();
            gui.cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>();
            if (NativeMethods.GetGUIThreadInfo(0, ref gui))
            {
                if (gui.hwndFocus != IntPtr.Zero && IsWin32EditPassword(gui.hwndFocus))
                {
                    _isPasswordFieldFocused = true;
                    return;
                }
            }

            if (IsWin32EditPassword(hwnd))
            {
                _isPasswordFieldFocused = true;
                return;
            }

            // Normal field (unless UIA has marked it as password)
            _isPasswordFieldFocused = false;
        }
        catch
        {
            _currentProcessName = string.Empty;
            _isAccessDenied = true;
            _isPasswordFieldFocused = false;
        }
    }

    private void TryInitializeUiAutomation()
    {
        try
        {
            _uiAutomation = new CUIAutomation() as IUIAutomation;
            if (_uiAutomation != null)
            {
                _uiaFocusHandler = new UiaFocusHandler(this);
                _uiAutomation.AddFocusChangedEventHandler(IntPtr.Zero, _uiaFocusHandler);
            }
        }
        catch
        {
            // In headless/CI environments or when COM fails, gracefully fall back to Win32 checks
            _uiAutomation = null;
            _uiaFocusHandler = null;
        }
    }

    private void TryTeardownUiAutomation()
    {
        try
        {
            if (_uiAutomation != null && _uiaFocusHandler != null)
            {
                _uiAutomation.RemoveFocusChangedEventHandler(_uiaFocusHandler);
            }
        }
        catch
        {
            // Ignore teardown errors
        }
        finally
        {
            _uiAutomation = null;
            _uiaFocusHandler = null;
        }
    }

    private sealed class UiaFocusHandler : IUIAutomationFocusChangedEventHandler
    {
        private readonly FocusTracker _owner;

        public UiaFocusHandler(FocusTracker owner)
        {
            _owner = owner;
        }

        public int HandleFocusChangedEvent(IUIAutomationElement sender)
        {
            try
            {
                if (sender != null)
                {
                    int hr = sender.GetCurrentPropertyValue(
                        (int)NativeMethods.UIA_IsPasswordPropertyId,
                        out object isPasswordVal);

                    if (hr == 0 && isPasswordVal is bool isPassword)
                    {
                        _owner._isPasswordFieldFocused = isPassword;
                        return 0;
                    }
                }
            }
            catch
            {
                // Defensive: ignore COM marshaling errors
            }

            return 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}
