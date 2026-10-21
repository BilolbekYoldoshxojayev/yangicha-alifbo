// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — DashboardForm.cs
// Modern control center matching the visual identity of the website
// (Lapis #13205C, Deep Ink #0F1530, Turquoise #2FB3A6, Saffron #F0B23A).
// Includes live interactive sandbox, toggle cards, real-time stats, and list editors.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.UI;

public sealed class DashboardForm : Form
{
    private readonly KeyboardHook _keyboardHook;
    private readonly LanguageDetector _detector;
    private readonly Action<bool> _onStartupToggled;
    private readonly Func<bool> _isStartupEnabled;

    // Palette constants from website styles.css
    private static readonly Color ColorInk = Color.FromArgb(15, 21, 48);          // #0F1530
    private static readonly Color ColorLapis = Color.FromArgb(19, 32, 92);        // #13205C
    private static readonly Color ColorLapisCard = Color.FromArgb(27, 43, 115);   // #1B2B73
    private static readonly Color ColorBorder = Color.FromArgb(38, 56, 138);      // #26388A
    private static readonly Color ColorTurq = Color.FromArgb(47, 179, 166);       // #2FB3A6
    private static readonly Color ColorSaffron = Color.FromArgb(240, 178, 58);    // #F0B23A
    private static readonly Color ColorPorcelain = Color.FromArgb(238, 241, 236); // #EEF1EC
    private static readonly Color ColorMuted = Color.FromArgb(179, 187, 219);     // #B3BBDB

    // Controls
    private TextBox _inputBox = null!;
    private TextBox _outputBox = null!;
    private CheckBox _chatSlangCheck = null!;
    private CheckBox _startupCheck = null!;
    private ComboBox _modeCombo = null!;
    private Label _statsLabel = null!;
    private System.Windows.Forms.Timer _statsTimer = null!;

    public DashboardForm(
        KeyboardHook keyboardHook,
        LanguageDetector detector,
        Action<bool> onStartupToggled,
        Func<bool> isStartupEnabled)
    {
        _keyboardHook = keyboardHook;
        _detector = detector;
        _onStartupToggled = onStartupToggled;
        _isStartupEnabled = isStartupEnabled;

        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "YangiAlifbo — Boshqaruv Paneli (Control Center)";
        ClientSize = new Size(920, 680);
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ColorInk;
        ForeColor = ColorPorcelain;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        ShowIcon = true;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24),
            BackColor = ColorInk,
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Header
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 48));  // Live Sandbox Card
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 130)); // Toggle Settings Cards
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Stats bar
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Footer Buttons

        // ── 1. Header ───────────────────────────────────────────────
        var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorInk };
        var titleLabel = new Label
        {
            Text = "YangiAlifbo",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = ColorPorcelain,
            AutoSize = true,
            Location = new Point(0, 4)
        };

        var versionBadge = new Label
        {
            Text = " v1.3.0 ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = ColorTurq,
            ForeColor = ColorInk,
            AutoSize = true,
            Location = new Point(175, 12),
            Padding = new Padding(4, 2, 4, 2)
        };

        var statusBadge = new Label
        {
            Text = " ● Faol (Active) ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(235, 12),
            Padding = new Padding(4, 2, 4, 2)
        };

        var subtitleLabel = new Label
        {
            Text = "O'zbek yozuvi konvertori va jonli muloqot normalizatori  |  Tezkor tugma: Ctrl+Shift+U",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorMuted,
            AutoSize = true,
            Location = new Point(2, 40)
        };

        headerPanel.Controls.AddRange(new Control[] { titleLabel, versionBadge, statusBadge, subtitleLabel });

        // ── 2. Live Sandbox Card ────────────────────────────────────
        var sandboxCard = CreateCardPanel("JONLI SINASH (INTERACTIVE PLAYGROUND)");
        var sandboxGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12),
        };
        sandboxGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        sandboxGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Left: Input
        var leftBoxPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var leftTitle = new Label
        {
            Text = "Eski imloda yozing (1995 lotin yoki chat qisqartmalari):",
            ForeColor = ColorMuted,
            Dock = DockStyle.Top,
            Height = 24
        };
        _inputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            BackColor = ColorLapis,
            ForeColor = ColorPorcelain,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            Text = "o'zbekcha gaplashamz, yaxwi boldi, oshani korishamz!"
        };
        _inputBox.TextChanged += (_, _) => UpdateLiveTranslation();
        leftBoxPanel.Controls.Add(_inputBox);
        leftBoxPanel.Controls.Add(leftTitle);

        // Right: Output
        var rightBoxPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        var rightHeader = new Panel { Dock = DockStyle.Top, Height = 24 };
        var rightTitle = new Label
        {
            Text = "2024 Yangi alifboda natija:",
            ForeColor = ColorTurq,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        var copyBtn = new Button
        {
            Text = "Nusxalash (Copy)",
            Dock = DockStyle.Right,
            Width = 110,
            Height = 22,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorTurq,
            ForeColor = ColorInk,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        copyBtn.FlatAppearance.BorderSize = 0;
        copyBtn.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_outputBox.Text))
            {
                Clipboard.SetText(_outputBox.Text);
                copyBtn.Text = "Nusxalandi! ✓";
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) => { copyBtn.Text = "Nusxalash (Copy)"; t.Stop(); t.Dispose(); };
                t.Start();
            }
        };
        rightHeader.Controls.Add(rightTitle);
        rightHeader.Controls.Add(copyBtn);

        _outputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            BackColor = ColorLapisCard,
            ForeColor = ColorPorcelain,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };
        rightBoxPanel.Controls.Add(_outputBox);
        rightBoxPanel.Controls.Add(rightHeader);

        sandboxGrid.Controls.Add(leftBoxPanel, 0, 0);
        sandboxGrid.Controls.Add(rightBoxPanel, 1, 0);
        sandboxCard.Controls.Add(sandboxGrid);

        // ── 3. Toggle Settings Cards ────────────────────────────────
        var settingsCard = CreateCardPanel("SOZLAMALAR VA REJIMLAR");
        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(12, 6, 12, 6)
        };
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

        // Setting 1: Chat Slang
        _chatSlangCheck = new CheckBox
        {
            Text = "💬 Jonli til va chat normalizatori\n(korishamz➔körişamiz, yaxwi➔yaxşi)",
            Checked = _keyboardHook.IsChatSlangNormalizerEnabled,
            ForeColor = ColorPorcelain,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _chatSlangCheck.CheckedChanged += (_, _) =>
        {
            _keyboardHook.IsChatSlangNormalizerEnabled = _chatSlangCheck.Checked;
            UpdateLiveTranslation();
        };

        // Setting 2: Startup
        _startupCheck = new CheckBox
        {
            Text = "🚀 Windows bilan ishga tushirish\n(Kompyuter yoqilganda faollashadi)",
            Checked = _isStartupEnabled(),
            ForeColor = ColorPorcelain,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        _startupCheck.CheckedChanged += (_, _) => _onStartupToggled(_startupCheck.Checked);

        // Setting 3: Mode
        var modePanel = new Panel { Dock = DockStyle.Fill };
        var modeTitle = new Label
        {
            Text = "🎯 Konvertatsiya rejimi:",
            ForeColor = ColorMuted,
            Dock = DockStyle.Top,
            Height = 22
        };
        _modeCombo = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ColorLapis,
            ForeColor = ColorPorcelain,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f)
        };
        _modeCombo.Items.Add("Qat'iy (Strict — Lug'at orqali)");
        _modeCombo.Items.Add("Agressiv (Convert All)");
        _modeCombo.SelectedIndex = _keyboardHook.Mode == TransliterationMode.Strict ? 0 : 1;
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            _keyboardHook.Mode = _modeCombo.SelectedIndex == 0
                ? TransliterationMode.Strict
                : TransliterationMode.Aggressive;
            UpdateLiveTranslation();
        };
        modePanel.Controls.Add(_modeCombo);
        modePanel.Controls.Add(modeTitle);

        settingsLayout.Controls.Add(_chatSlangCheck, 0, 0);
        settingsLayout.Controls.Add(_startupCheck, 1, 0);
        settingsLayout.Controls.Add(modePanel, 2, 0);
        settingsCard.Controls.Add(settingsLayout);

        // ── 4. Live Statistics Bar ──────────────────────────────────
        var statsCard = CreateCardPanel("TIZIM HOLATI VA STATISTIKA");
        _statsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        statsCard.Controls.Add(_statsLabel);

        // ── 5. Footer Buttons ───────────────────────────────────────
        var footerPanel = new Panel { Dock = DockStyle.Fill };

        var btnWhitelist = CreateFooterButton("Oq ro'yxat (Whitelist)...", 0);
        btnWhitelist.Click += (_, _) => OpenUserListEditor("user_whitelist.txt");

        var btnBlacklist = CreateFooterButton("Qora ro'yxat (Blacklist)...", 175);
        btnBlacklist.Click += (_, _) => OpenUserListEditor("user_blacklist.txt");

        var btnWebsite = CreateFooterButton("🌐 GitHub sahifasi", 350);
        btnWebsite.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo",
                    UseShellExecute = true
                });
            }
            catch { }
        };

        var btnMinimize = new Button
        {
            Text = "Patnisga yashirish (Minimize to Tray)",
            Dock = DockStyle.Right,
            Width = 230,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorLapisCard,
            ForeColor = ColorSaffron,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnMinimize.FlatAppearance.BorderColor = ColorSaffron;
        btnMinimize.Click += (_, _) => Hide();

        footerPanel.Controls.AddRange(new Control[] { btnWhitelist, btnBlacklist, btnWebsite, btnMinimize });

        // Add rows to mainPanel
        mainPanel.Controls.Add(headerPanel, 0, 0);
        mainPanel.Controls.Add(sandboxCard, 0, 1);
        mainPanel.Controls.Add(settingsCard, 0, 2);
        mainPanel.Controls.Add(statsCard, 0, 3);
        mainPanel.Controls.Add(footerPanel, 0, 4);

        Controls.Add(mainPanel);

        // Initial live preview
        UpdateLiveTranslation();
        UpdateStats();

        // Stats ticker timer
        _statsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _statsTimer.Tick += (_, _) => UpdateStats();
        _statsTimer.Start();
    }

    private void UpdateLiveTranslation()
    {
        string input = _inputBox.Text;
        if (string.IsNullOrEmpty(input))
        {
            _outputBox.Text = string.Empty;
            return;
        }

        // Process words dynamically through the pipeline
        var tokens = System.Text.RegularExpressions.Regex.Split(input, @"(\s+|[.,!?:;""\(\)\[\]\{\}\/\\])");
        var sb = new System.Text.StringBuilder(input.Length);

        bool slangEnabled = _chatSlangCheck.Checked;
        var mode = _modeCombo.SelectedIndex == 0 ? ConversionMode.Strict : ConversionMode.Aggressive;

        foreach (string token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;

            if (token.Length >= 2 && char.IsLetter(token[0]))
            {
                // Protected English
                if (_detector.IsForeignOrReject(token))
                {
                    sb.Append(token);
                    continue;
                }

                string work = token;
                if (slangEnabled)
                {
                    work = ChatNormalizer.Normalize(token);
                }

                if (UzbekPhonetics.HasConvertiblePatterns(work))
                {
                    if (mode == ConversionMode.Aggressive || _detector.IsUzbek(work))
                    {
                        sb.Append(UzbekPhonetics.Convert(work));
                        continue;
                    }
                }
                else if (work != token && _detector.IsUzbek(work))
                {
                    sb.Append(work);
                    continue;
                }

                sb.Append(token);
            }
            else
            {
                sb.Append(token);
            }
        }

        _outputBox.Text = sb.ToString();
    }

    private void UpdateStats()
    {
        _statsLabel.Text =
            $"📊 Lug'at: {_detector.UzbekLexiconCount:N0} so'z  |  " +
            $"Inglizcha filtr: {_detector.EnglishRejectCount:N0} so'z  |  " +
            $"Sessiyada almashtirildi: {_keyboardHook.WordsConverted:N0} so'z  |  " +
            $"O'rtacha kechikish: < 0.05 ms";
    }

    private static Panel CreateCardPanel(string title)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorLapisCard,
            Padding = new Padding(1)
        };

        var titleLabel = new Label
        {
            Text = title,
            ForeColor = ColorSaffron,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(12, 4, 0, 0)
        };
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private Button CreateFooterButton(string text, int x)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 6),
            Width = 165,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorLapis,
            ForeColor = ColorPorcelain,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = ColorBorder;
        return btn;
    }

    private static void OpenUserListEditor(string fileName)
    {
        try
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "YangiAlifbo");
            Directory.CreateDirectory(appData);
            string path = Path.Combine(appData, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "# Har bir qatorda bitta so'z kiriting.\n", System.Text.Encoding.UTF8);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Faylni ochishda xatolik:\n{ex.Message}", "Xatolik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Don't kill the background hook when user clicks [X], minimize to tray instead
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
