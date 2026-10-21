// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — Program.cs
// Entry point: single-instance mutex, starts the system tray application.
// ──────────────────────────────────────────────────────────────────────

using YangiAlifbo.UI;

namespace YangiAlifbo;

internal static class Program
{
    /// <summary>
    /// Global mutex name ensures only one instance can run at a time.
    /// </summary>
    private const string MutexName = "Global\\YangiAlifbo_SingleInstance_Mutex";

    [STAThread]
    static void Main()
    {
        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo");
        Directory.CreateDirectory(logDir);
        string logFile = Path.Combine(logDir, "launch.log");

        try
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] Starting YangiAlifbo...\n");

            // ── Single-instance guard ──────────────────────────────────────
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                File.AppendAllText(logFile, $"[{DateTime.Now}] Another instance mutex already exists. Exiting.\n");
                return;
            }

            File.AppendAllText(logFile, $"[{DateTime.Now}] Mutex acquired. Initializing WinForms...\n");
            ApplicationConfiguration.Initialize();

            File.AppendAllText(logFile, $"[{DateTime.Now}] Creating TrayApplicationContext...\n");
            using var appContext = new TrayApplicationContext();

            File.AppendAllText(logFile, $"[{DateTime.Now}] Entering Application.Run...\n");
            Application.Run(appContext);

            File.AppendAllText(logFile, $"[{DateTime.Now}] Application.Run exited cleanly.\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] FATAL ERROR:\n{ex}\n");
            MessageBox.Show($"Xatolik yuz berdi:\n{ex.Message}", "YangiAlifbo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
