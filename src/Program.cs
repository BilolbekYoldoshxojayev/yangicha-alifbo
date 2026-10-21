// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — Program.cs
// Entry point: single-instance mutex, legacy instance cleanup,
// and starts the system tray / desktop dashboard application.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using YangiAlifbo.UI;

namespace YangiAlifbo;

internal static class Program
{
    /// <summary>
    /// Global mutex name ensures only one instance can run at a time.
    /// </summary>
    private const string MutexName = "Global\\YangiAlifbo_SingleInstance_Mutex";

    [STAThread]
    static void Main(string[] args)
    {
        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo");
        Directory.CreateDirectory(logDir);
        string logFile = Path.Combine(logDir, "launch.log");

        try
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] Starting YangiAlifbo v1.3.0...\n");

            // ── Clean up any legacy or duplicate running instances ─────
            // Ensures new versions cleanly replace old versions without collision.
            KillLegacyInstances(logFile);

            // ── Single-instance guard ──────────────────────────────────
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Wait briefly for the terminating legacy instance to release the mutex
                if (!mutex.WaitOne(TimeSpan.FromSeconds(3), false))
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Mutex busy after legacy cleanup. Exiting.\n");
                    return;
                }
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

    /// <summary>
    /// Terminates any previously running instances of YangiAlifbo so that
    /// old and new versions never collide, and the newly launched version takes over.
    /// </summary>
    private static void KillLegacyInstances(string logFile)
    {
        try
        {
            int currentPid = Environment.ProcessId;
            var processes = Process.GetProcessesByName("YangiAlifbo");

            foreach (var p in processes)
            {
                if (p.Id != currentPid)
                {
                    try
                    {
                        File.AppendAllText(logFile, $"[{DateTime.Now}] Terminating previous YangiAlifbo instance (PID: {p.Id})...\n");
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(logFile, $"[{DateTime.Now}] Could not terminate instance {p.Id}: {ex.Message}\n");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] Legacy process scan error: {ex.Message}\n");
        }
    }
}
