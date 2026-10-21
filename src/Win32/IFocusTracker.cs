// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — IFocusTracker.cs
// Interface contract for window focus, process exclusion, and
// password field detection.
// ──────────────────────────────────────────────────────────────────────

namespace YangiAlifbo.Win32;

/// <summary>
/// Interface contract for tracking the foreground window and determining
/// process permissions and password field state.
/// </summary>
public interface IFocusTracker
{
    /// <summary>
    /// Gets whether the current foreground process is permitted for transliteration.
    /// Fails closed (false) if process identity is inaccessible.
    /// </summary>
    bool IsCurrentProcessAllowed { get; }

    /// <summary>
    /// Gets whether the currently focused control is a password field.
    /// Fast O(1) non-blocking check for low-level hook callbacks.
    /// </summary>
    bool IsPasswordFieldFocused { get; }

    /// <summary>
    /// Gets the executable name of the current foreground process.
    /// </summary>
    string CurrentProcessName { get; }

    /// <summary>
    /// Raised when the foreground window changes.
    /// </summary>
    event Action? ForegroundChanged;
}
