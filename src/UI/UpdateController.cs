// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — UpdateController.cs
// Schedules update checks (15 s after start, then every 6 hours), runs the
// download and install through Updater, and reports progress to the tray
// and the control center. Auto-update can be switched off; the choice is
// kept as a flag file next to startup_disabled.flag.
// ──────────────────────────────────────────────────────────────────────

using YangiAlifbo.Core;

namespace YangiAlifbo.UI;

public enum UpdateStatus { Idle, Checking, UpToDate, Available, Downloading, Installing, Failed }

public sealed class UpdateController : IDisposable
{
    private static readonly string DisabledFlag = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo", "autoupdate_disabled.flag");

    private readonly Updater _updater = new();
    private readonly SynchronizationContext _ui;
    private readonly Action<string, string> _notify;
    private readonly Action _exitForRestart;
    private readonly System.Windows.Forms.Timer _timer = new();
    private int _busy;

    public UpdateStatus Status { get; private set; } = UpdateStatus.Idle;
    public ReleaseInfo? Latest { get; private set; }
    public int Progress { get; private set; }
    public string? Error { get; private set; }
    public DateTime? LastChecked { get; private set; }

    /// <summary>Notes of the update that started this process, if any.</summary>
    public WhatsNew? JustUpdated { get; }

    public event Action? Changed;

    /// <param name="notify">Shows a tray balloon (title, text).</param>
    /// <param name="exitForRestart">Ends this process after the new exe was started.</param>
    public UpdateController(Action<string, string> notify, Action exitForRestart)
    {
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _notify = notify;
        _exitForRestart = exitForRestart;
        JustUpdated = Updater.TakeWhatsNew();
        Updater.CleanupOldExe();
    }

    public static Version CurrentVersion => Updater.CurrentVersion;
    public static bool CanSelfUpdate => Updater.CanSelfUpdate;

    public bool AutoUpdate
    {
        get => !File.Exists(DisabledFlag);
        set
        {
            try
            {
                if (value) File.Delete(DisabledFlag);
                else { Directory.CreateDirectory(Path.GetDirectoryName(DisabledFlag)!); File.WriteAllText(DisabledFlag, "disabled"); }
            }
            catch { }
            Raise();
        }
    }

    public void Start()
    {
        _timer.Interval = 15_000;
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds;
            if (AutoUpdate) await CheckAsync(install: true);
        };
        _timer.Start();
    }

    /// <summary>
    /// Looks for a newer release and, when <paramref name="install"/> is set and this
    /// build can replace itself, downloads, verifies and installs it.
    /// </summary>
    public async Task CheckAsync(bool install)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        try
        {
            Set(UpdateStatus.Checking);
            var latest = await _updater.GetLatestAsync().ConfigureAwait(false);
            LastChecked = DateTime.Now;
            if (latest == null) { Fail("Yangilanishni tekshirib bo‘lmadi. Internet aloqasini tekshiring."); return; }
            Latest = latest;

            if (latest.Version <= CurrentVersion) { Set(UpdateStatus.UpToDate); return; }
            Set(UpdateStatus.Available);
            if (!install || !CanSelfUpdate) return;

            Set(UpdateStatus.Downloading);
            var progress = new Progress<int>(p => { Progress = p; Raise(); });
            string file = await _updater.DownloadAsync(latest, progress).ConfigureAwait(false);

            Set(UpdateStatus.Installing);
            _ui.Post(_ =>
            {
                if (Updater.TryInstall(file, latest, out string error))
                {
                    _notify("YangiAlifbo yangilanmoqda", $"v{latest.Version} o‘rnatildi, dastur qayta ishga tushmoqda.");
                    _exitForRestart();
                }
                else
                {
                    Fail("Yangi versiyani o‘rnatib bo‘lmadi: " + error);
                }
            }, null);
        }
        catch (Exception ex)
        {
            Fail("Yangilash to‘xtadi: " + ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private void Set(UpdateStatus s) { Status = s; if (s != UpdateStatus.Failed) Error = null; Raise(); }
    private void Fail(string message) { Error = message; Status = UpdateStatus.Failed; Raise(); }
    private void Raise() => _ui.Post(_ => Changed?.Invoke(), null);

    public void Dispose()
    {
        _timer.Dispose();
        _updater.Dispose();
    }
}
