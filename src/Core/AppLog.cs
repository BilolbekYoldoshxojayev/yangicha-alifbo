// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — AppLog.cs
// Appends a line to %APPDATA%\YangiAlifbo\launch.log (the same file
// Program.cs writes at start-up). Never throws.
// ──────────────────────────────────────────────────────────────────────

namespace YangiAlifbo.Core;

public static class AppLog
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo", "launch.log");

    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.AppendAllText(Path, $"[{DateTime.Now}] [pid {Environment.ProcessId}] {message}\n");
            }
        }
        catch { }
    }
}
