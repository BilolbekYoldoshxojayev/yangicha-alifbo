// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — ControlCenterForm.cs
// Control center rendered with WebView2 from embedded HTML (src/UI/Web),
// sharing the website's design. The page talks to this form through
// WebView2 messages; see app.js for the message types.
// Falls back to DashboardForm when the WebView2 runtime is missing.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.UI;

/// <summary>A window that reflects the tray state and can be told it changed.</summary>
public interface IStateView
{
    void NotifyStateChanged();
}

/// <summary>The settings and actions the control center drives (implemented by the tray).</summary>
public interface IControlCenterHost
{
    bool IsEnabled { get; }
    TransliterationMode Mode { get; }
    bool IsChatSlangNormalizerEnabled { get; }
    void ToggleEnabled();
    void SetMode(TransliterationMode mode);
    void SetChatSlang(bool enabled);
    bool GetIsStartupEnabled();
    void SetStartup(bool enabled);
    void OpenWhitelist();
    void OpenBlacklist();
    void ExitApplication();
}

public sealed class ControlCenterForm : Form, IStateView
{
    private const string Origin = "https://yangialifbo.app/";
    private const string RepoUrl = "https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo";
    private const string SiteUrl = "https://bilolbekyoldoshxojayev.github.io/yangicha-alifbo/";

    private static readonly Color Lapis = Color.FromArgb(19, 32, 92);
    private static readonly Assembly Asm = typeof(ControlCenterForm).Assembly;

    private readonly IControlCenterHost _context;
    private readonly KeyboardHook _keyboardHook;
    private readonly LanguageDetector _detector;
    private readonly WebView2 _web;
    private readonly System.Windows.Forms.Timer _ticker = new() { Interval = 1000 };
    private bool _ready;

    private static bool? _runtimeAvailable;

    /// <summary>True when the Edge WebView2 runtime is installed.</summary>
    public static bool IsRuntimeAvailable()
    {
        if (_runtimeAvailable is bool known) return known;
        try { _runtimeAvailable = !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString()); }
        catch { _runtimeAvailable = false; }
        return _runtimeAvailable.Value;
    }

    public ControlCenterForm(IControlCenterHost context, KeyboardHook keyboardHook, LanguageDetector detector, Icon? icon)
    {
        _context = context;
        _keyboardHook = keyboardHook;
        _detector = detector;

        Text = "YangiAlifbo";
        if (icon != null) Icon = icon;
        BackColor = Lapis;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        // Sizes are in 96-DPI units; scale them for the current monitor and keep them on screen.
        var area = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1280, 800);
        ClientSize = new Size(Math.Min(LogicalToDeviceUnits(1060), area.Width - 40),
                              Math.Min(LogicalToDeviceUnits(700), area.Height - 60));
        MinimumSize = new Size(LogicalToDeviceUnits(760), LogicalToDeviceUnits(560));

        _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Lapis };
        Controls.Add(_web);

        _ticker.Tick += (_, _) => { if (Visible) PostState(); };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Dark title bar and a lapis caption on Windows 11 (ignored where unsupported).
        int on = 1;
        DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int));
        int caption = 0x00461A0E; // #0E1A46 as COLORREF (0x00BBGGRR)
        DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int));
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YangiAlifbo", "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            await _web.EnsureCoreWebView2Async(env);

            var core = _web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;

            core.AddWebResourceRequestedFilter(Origin + "*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += OnResourceRequested;
            core.NavigationStarting += (_, a) => { if (!a.Uri.StartsWith(Origin, StringComparison.Ordinal)) a.Cancel = true; };
            core.NewWindowRequested += (_, a) => a.Handled = true;
            core.WebMessageReceived += OnMessage;
            core.Navigate(Origin + "index.html");
            _ticker.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Boshqaruv panelini ochib bo‘lmadi:\n" + ex.Message, "YangiAlifbo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ── Embedded UI files ──────────────────────────────────────────

    private void OnResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        string path = new Uri(e.Request.Uri).AbsolutePath.TrimStart('/');
        if (path.Length == 0) path = "index.html";
        Stream? stream = Asm.GetManifestResourceStream("ui/" + path.Replace('/', '\\'))
                         ?? Asm.GetManifestResourceStream("ui/" + path);
        var env = _web.CoreWebView2.Environment;
        e.Response = stream == null
            ? env.CreateWebResourceResponse(null, 404, "Not Found", "")
            : env.CreateWebResourceResponse(stream, 200, "OK", "Content-Type: " + ContentType(path));
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".woff2" => "font/woff2",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream"
    };

    // ── Messages from the page ─────────────────────────────────────

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var doc = JsonDocument.Parse(e.WebMessageAsJson);
        var msg = doc.RootElement;
        string type = msg.GetProperty("type").GetString() ?? "";

        switch (type)
        {
            case "ready":
                _ready = true;
                PostState();
                break;
            case "toggleEnabled":
                _context.ToggleEnabled();
                break;
            case "setSlang":
                _context.SetChatSlang(msg.GetProperty("value").GetBoolean());
                break;
            case "setStartup":
                _context.SetStartup(msg.GetProperty("value").GetBoolean());
                break;
            case "setMode":
                _context.SetMode(msg.GetProperty("value").GetString() == "Aggressive"
                    ? TransliterationMode.Aggressive : TransliterationMode.Strict);
                break;
            case "convert":
                string text = msg.GetProperty("text").GetString() ?? "";
                string result = TextConverter.Convert(text, _detector, _context.Mode, _context.IsChatSlangNormalizerEnabled, out int changed);
                Post(new { type = "converted", id = msg.GetProperty("id").GetInt32(), text = result, changed });
                break;
            case "copy":
                try { Clipboard.SetText(msg.GetProperty("text").GetString() ?? ""); } catch { }
                break;
            case "open":
                Open(msg.GetProperty("target").GetString());
                break;
            case "reload":
                _detector.ReloadUserLists();
                break;
            case "hide":
                Hide();
                break;
            case "exit":
                if (MessageBox.Show(this, "YangiAlifbo’dan chiqasizmi? Konvertatsiya to‘xtaydi.", "YangiAlifbo",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    _context.ExitApplication();
                break;
        }
    }

    private void Open(string? target)
    {
        switch (target)
        {
            case "whitelist": _context.OpenWhitelist(); break;
            case "blacklist": _context.OpenBlacklist(); break;
            case "github": Launch(RepoUrl); break;
            case "site": Launch(SiteUrl); break;
            case "logs":
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo");
                Directory.CreateDirectory(dir);
                Launch(dir);
                break;
        }
    }

    private static void Launch(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch { }
    }

    // ── State to the page ──────────────────────────────────────────

    public void NotifyStateChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(NotifyStateChanged)); return; }
        PostState();
    }

    private void PostState()
    {
        if (!_ready) return;
        var v = Asm.GetName().Version;
        Post(new
        {
            type = "state",
            version = v == null ? "" : $"{v.Major}.{v.Minor}.{v.Build}",
            enabled = _context.IsEnabled,
            slang = _context.IsChatSlangNormalizerEnabled,
            startup = _context.GetIsStartupEnabled(),
            mode = _context.Mode.ToString(),
            words = _keyboardHook.WordsConverted,
            lexicon = _detector.UzbekLexiconCount,
            reject = _detector.EnglishRejectCount,
            memoryMb = Environment.WorkingSet / (1024 * 1024)
        });
    }

    private void Post(object payload)
    {
        if (_web.CoreWebView2 == null) return;
        _web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(payload));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // The window hides to the tray; the app keeps running.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _ticker.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ticker.Dispose();
            _web.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
