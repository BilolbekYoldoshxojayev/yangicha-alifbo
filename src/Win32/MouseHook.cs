// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — MouseHook.cs
// Global low-level mouse hook (WH_MOUSE_LL) that detects mouse clicks
// and triggers buffer invalidation to prevent caret desynchronization.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YangiAlifbo.Win32;

/// <summary>
/// Installs a global low-level mouse hook (WH_MOUSE_LL) to detect mouse clicks.
/// Triggers buffer invalidation when the user repositions the caret via mouse.
/// </summary>
public sealed class MouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _hookProc;
    private readonly Action? _onMouseClick;
    private bool _disposed;

    /// <summary>
    /// Raised whenever any mouse click (left, right, middle, or X-button) occurs.
    /// </summary>
    public event Action? MouseClicked;

    /// <summary>
    /// Indicates whether the mouse hook is currently installed.
    /// </summary>
    public bool IsInstalled => _hookId != IntPtr.Zero;

    /// <summary>
    /// Initializes a new instance of <see cref="MouseHook"/>.
    /// </summary>
    /// <param name="onMouseClick">Optional action to invoke when a mouse button is pressed.</param>
    public MouseHook(Action? onMouseClick = null)
    {
        _onMouseClick = onMouseClick;
        // Keep reference to delegate to prevent GC collection
        _hookProc = HookCallback;
    }

    /// <summary>Installs the global low-level mouse hook.</summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);

        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to install mouse hook. Error: {Marshal.GetLastWin32Error()}");
        }
    }

    /// <summary>Uninstalls the global low-level mouse hook.</summary>
    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Low-level mouse hook procedure.
    /// Executes in &lt; 100ns and NEVER suppresses mouse events.
    /// </summary>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg is NativeMethods.WM_LBUTTONDOWN
                    or NativeMethods.WM_RBUTTONDOWN
                    or NativeMethods.WM_MBUTTONDOWN
                    or NativeMethods.WM_XBUTTONDOWN)
            {
                try
                {
                    _onMouseClick?.Invoke();
                    MouseClicked?.Invoke();
                }
                catch
                {
                    // Defensive guard: never let exceptions escape native hook callback
                }
            }
        }

        // Always call CallNextHookEx — mouse clicks must NEVER be swallowed
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}
