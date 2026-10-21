// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — IProcessInfoProvider.cs
// Abstraction and Win32 P/Invoke implementation for resolving process names.
// ──────────────────────────────────────────────────────────────────────

using System.Text;

namespace YangiAlifbo.Win32;

/// <summary>
/// Abstraction for querying process executable names by process ID.
/// </summary>
public interface IProcessInfoProvider
{
    /// <summary>
    /// Returns the executable filename (e.g. "cmd.exe", "notepad.exe") for the given PID,
    /// or null if the process cannot be queried or access is denied.
    /// </summary>
    string? GetProcessName(uint processId);
}

/// <summary>
/// Default production implementation using Win32 OpenProcess with PROCESS_QUERY_LIMITED_INFORMATION
/// and QueryFullProcessImageNameW.
/// </summary>
public sealed class Win32ProcessInfoProvider : IProcessInfoProvider
{
    public string? GetProcessName(uint processId)
    {
        if (processId == 0) return null;

        IntPtr hProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);

        if (hProcess == IntPtr.Zero) return null;

        try
        {
            var sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;

            if (NativeMethods.QueryFullProcessImageNameW(hProcess, NativeMethods.PROCESS_NAME_WIN32, sb, ref size))
            {
                return Path.GetFileName(sb.ToString());
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
