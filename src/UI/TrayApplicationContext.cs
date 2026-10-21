// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — TrayApplicationContext.cs
// System tray application context: tray icon, context menu, global
// hotkey (Ctrl+Shift+U), and lifecycle management.
// ──────────────────────────────────────────────────────────────────────

using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.UI;

/// <summary>
/// Application context that lives in the system tray. Manages the
/// keyboard hook, focus tracker, language detector, context menu,
/// and global hotkey for toggling transliteration on/off.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    // ── Constants ──────────────────────────────────────────────────────

    private const int HOTKEY_ID = 0x5941; // Unique ID for our hotkey
    private const uint VK_U = 0x55;       // Virtual key code for 'U'

    private const string StartupRegistryKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "YangiAlifbo";

    // ── Components ─────────────────────────────────────────────────────

    private readonly NotifyIcon _trayIcon;
    private readonly LanguageDetector _detector;
    private readonly FocusTracker _focusTracker;
    private readonly KeyboardHook _keyboardHook;

    // ── Menu Items (need references for dynamic state) ─────────────────

    private readonly ToolStripMenuItem _enableItem;
    private readonly ToolStripMenuItem _strictModeItem;
    private readonly ToolStripMenuItem _aggressiveModeItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _chatSlangItem;

    // ── Hotkey Window ──────────────────────────────────────────────────
    private readonly HotkeyWindow _hotkeyWindow;

    // ── Icons ──────────────────────────────────────────────────────────
    private readonly Icon _iconActive;
    private readonly Icon _iconDisabled;

    // ── Constructor ────────────────────────────────────────────────────

    public TrayApplicationContext()
    {
        // ── Initialize core components ─────────────────────────────
        _detector = new LanguageDetector();
        _focusTracker = new FocusTracker();
        _keyboardHook = new KeyboardHook(_detector, _focusTracker);

        // ── Generate tray icons programmatically ───────────────────
        _iconActive = GenerateIcon(Color.FromArgb(34, 139, 34), "Ö"); // Forest Green
        _iconDisabled = GenerateIcon(Color.Gray, "Ö");

        // ── Build context menu ─────────────────────────────────────
        _enableItem = new ToolStripMenuItem("✅ Yoqilgan (Enabled)", null, OnToggleEnabled)
        {
            Checked = true
        };

        _strictModeItem = new ToolStripMenuItem("Qat'iy (Strict — Dictionary Verified)", null, OnSetStrictMode)
        {
            Checked = true
        };

        _aggressiveModeItem = new ToolStripMenuItem("Agressiv (Aggressive — Convert All)", null, OnSetAggressiveMode)
        {
            Checked = false
        };

        var modeMenu = new ToolStripMenuItem("Rejim (Mode)");
        modeMenu.DropDownItems.Add(_strictModeItem);
        modeMenu.DropDownItems.Add(_aggressiveModeItem);

        // ── Ensure Startup is enabled by default ───────────────────
        EnsureStartupDefault();

        _chatSlangItem = new ToolStripMenuItem("💬 Jonli til va chat qisqartmalari (Chat Slang Normalizer)", null, OnToggleChatSlang)
        {
            Checked = _keyboardHook.IsChatSlangNormalizerEnabled
        };

        _startupItem = new ToolStripMenuItem("Windows bilan ishga tushirish (Run on Startup)", null, OnToggleStartup)
        {
            Checked = IsStartupEnabled()
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_enableItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(modeMenu);
        contextMenu.Items.Add(_chatSlangItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Oq ro'yxatni tahrirlash (Edit Whitelist)...", null, OnEditWhitelist);
        contextMenu.Items.Add("Qora ro'yxatni tahrirlash (Edit Blacklist)...", null, OnEditBlacklist);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_startupItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add($"ℹ️ Haqida (About) — v1.2.0 | Lug'at: {_detector.UzbekLexiconCount:N0} so'z", null, OnAbout);
        contextMenu.Items.Add("❌ Chiqish (Exit)", null, OnExit);

        // ── Create tray icon ───────────────────────────────────────
        _trayIcon = new NotifyIcon
        {
            Icon = _iconActive,
            Text = "YangiAlifbo — O'zbek yozuvi konvertori\n(Ctrl+Shift+U yoqish/o'chirish)",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        // Double-click tray icon = toggle
        _trayIcon.DoubleClick += (_, _) => ToggleEnabled();

        // ── Install hooks and start ────────────────────────────────
        _focusTracker.Install();
        _keyboardHook.Install();

        // ── Register global hotkey (Ctrl+Shift+U) ──────────────────
        _hotkeyWindow = new HotkeyWindow(OnHotkeyPressed);
        NativeMethods.RegisterHotKey(
            _hotkeyWindow.Handle,
            HOTKEY_ID,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
            VK_U);
    }

    // ── Toggle Logic ───────────────────────────────────────────────────

    private void ToggleEnabled()
    {
        _keyboardHook.IsEnabled = !_keyboardHook.IsEnabled;
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool enabled = _keyboardHook.IsEnabled;
        _trayIcon.Icon = enabled ? _iconActive : _iconDisabled;
        _enableItem.Text = enabled ? "✅ Yoqilgan (Enabled)" : "⬜ O'chirilgan (Disabled)";
        _enableItem.Checked = enabled;
        _trayIcon.Text = enabled
            ? "YangiAlifbo — Faol ✓\n(Ctrl+Shift+U o'chirish)"
            : "YangiAlifbo — O'chirilgan ✗\n(Ctrl+Shift+U yoqish)";
    }

    // ── Event Handlers ─────────────────────────────────────────────────

    private void OnHotkeyPressed()
    {
        ToggleEnabled();
    }

    private void OnToggleEnabled(object? sender, EventArgs e)
    {
        ToggleEnabled();
    }

    private void OnSetStrictMode(object? sender, EventArgs e)
    {
        _keyboardHook.Mode = TransliterationMode.Strict;
        _strictModeItem.Checked = true;
        _aggressiveModeItem.Checked = false;
    }

    private void OnSetAggressiveMode(object? sender, EventArgs e)
    {
        _keyboardHook.Mode = TransliterationMode.Aggressive;
        _strictModeItem.Checked = false;
        _aggressiveModeItem.Checked = true;
    }

    private void OnEditWhitelist(object? sender, EventArgs e)
    {
        OpenFileInEditor(LanguageDetector.GetWhitelistPath());
    }

    private void OnEditBlacklist(object? sender, EventArgs e)
    {
        OpenFileInEditor(LanguageDetector.GetBlacklistPath());
    }

    private static readonly string StartupDisabledFlagPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YangiAlifbo",
        "startup_disabled.flag");

    private void OnToggleChatSlang(object? sender, EventArgs e)
    {
        _keyboardHook.IsChatSlangNormalizerEnabled = !_keyboardHook.IsChatSlangNormalizerEnabled;
        _chatSlangItem.Checked = _keyboardHook.IsChatSlangNormalizerEnabled;
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        if (IsStartupEnabled())
        {
            RemoveFromStartup();
            _startupItem.Checked = false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StartupDisabledFlagPath)!);
                File.WriteAllText(StartupDisabledFlagPath, "disabled");
            }
            catch { }
        }
        else
        {
            AddToStartup();
            _startupItem.Checked = true;
            try
            {
                if (File.Exists(StartupDisabledFlagPath))
                    File.Delete(StartupDisabledFlagPath);
            }
            catch { }
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "YangiAlifbo — O'zbek Yozuvi Konvertori\n" +
            "Versiya 1.2.0 (Telegram va Jonli Muloqot Nashri)\n\n" +
            "1995 lotin alifbosidan 2024 yangi alifboga\n" +
            "real-vaqtda konvertatsiya qiladi.\n\n" +
            $"O'zbek lug'ati: {_detector.UzbekLexiconCount:N0} so'z\n" +
            $"Ingliz filtri: {_detector.EnglishRejectCount:N0} so'z\n" +
            $"Bu sessiyada o'zgartirilgan: {_keyboardHook.WordsConverted:N0} so'z\n\n" +
            "Tezkor tugma: Ctrl+Shift+U\n\n" +
            "sh → ş  |  ch → ç  |  o' → ö  |  g' → ğ",
            "YangiAlifbo haqida",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        // Reload user lists in case they were edited
        _detector.ReloadUserLists();

        // Clean shutdown
        _trayIcon.Visible = false;
        ExitThread();
    }

    // ── Startup Registry Management ────────────────────────────────────

    private static void EnsureStartupDefault()
    {
        try
        {
            // If the user explicitly disabled startup via menu, respect their choice
            if (File.Exists(StartupDisabledFlagPath))
                return;

            if (!IsStartupEnabled())
            {
                AddToStartup();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to ensure startup default: {ex.Message}");
        }
    }

    private static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(StartupValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static void AddToStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            string exePath = Application.ExecutablePath;
            key?.SetValue(StartupValueName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup registration failed: {ex.Message}");
        }
    }

    private static void RemoveFromStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            key?.DeleteValue(StartupValueName, false);
        }
        catch
        {
            // Ignore errors removing startup entry
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    /// <summary>Opens a file in the default text editor.</summary>
    private static void OpenFileInEditor(string path)
    {
        try
        {
            // Ensure the file exists
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "# YangiAlifbo — So'zlar ro'yxati\n# Har bir qatorda bitta so'z yozing.\n\n");
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Faylni ochib bo'lmadi:\n{ex.Message}",
                "Xatolik",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Generates a simple icon with a colored circle and text label.
    /// Used to create the active (green) and disabled (grey) tray icons
    /// programmatically — no external .ico files needed.
    /// </summary>
    private static Icon GenerateIcon(Color backgroundColor, string text)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // Draw background circle
        using var brush = new SolidBrush(backgroundColor);
        graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);

        // Draw text
        using var font = new Font("Segoe UI", 16, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        var textSize = graphics.MeasureString(text, font);
        float x = (size - textSize.Width) / 2;
        float y = (size - textSize.Height) / 2;
        graphics.DrawString(text, font, textBrush, x, y);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    // ── Disposal ───────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unregister hotkey
            NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
            _hotkeyWindow.Dispose();

            // Unhook keyboard and focus tracking
            _keyboardHook.Dispose();
            _focusTracker.Dispose();

            // Dispose tray icon
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            // Dispose icons
            _iconActive.Dispose();
            _iconDisabled.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Invisible window that receives WM_HOTKEY messages for the global hotkey.
/// </summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private readonly Action _onHotkey;

    public HotkeyWindow(Action onHotkey)
    {
        _onHotkey = onHotkey;

        // Create an invisible window to receive hotkey messages
        var cp = new CreateParams
        {
            Caption = "YangiAlifbo_HotkeyWindow",
            Style = 0, // WS_OVERLAPPED but invisible
        };
        CreateHandle(cp);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            _onHotkey();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
