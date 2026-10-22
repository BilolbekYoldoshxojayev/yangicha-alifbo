// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — DashboardForm.cs
// Modern control center matching the visual identity of the website
// (Lapis #13205C, Deep Ink #0C1024, Turquoise #2FB3A6, Saffron #F0B23A).
// Features tabbed navigation, spacious cards, live sandbox, toggle switches,
// live statistics, and complete settings management.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.UI;

public sealed class DashboardForm : Form, IStateView
{
    private readonly TrayApplicationContext? _context;
    private readonly KeyboardHook _keyboardHook;
    private readonly LanguageDetector _detector;
    private readonly Action<bool>? _onStartupToggled;
    private readonly Func<bool>? _isStartupEnabled;
    private readonly Action? _openWhitelist;
    private readonly Action? _openBlacklist;
    private readonly Action? _onExit;

    // Palette constants from website
    private static readonly Color ColorInk = Color.FromArgb(12, 16, 36);          // #0C1024
    private static readonly Color ColorHeader = Color.FromArgb(17, 24, 60);       // #11183C
    private static readonly Color ColorCard = Color.FromArgb(21, 30, 68);         // #151E44
    private static readonly Color ColorCardBorder = Color.FromArgb(35, 50, 104);   // #233268
    private static readonly Color ColorTabActive = Color.FromArgb(28, 39, 90);     // #1C275A
    private static readonly Color ColorTurq = Color.FromArgb(47, 179, 166);       // #2FB3A6
    private static readonly Color ColorTurqHover = Color.FromArgb(56, 200, 185);  // #38C8B9
    private static readonly Color ColorSaffron = Color.FromArgb(240, 178, 58);    // #F0B23A
    private static readonly Color ColorEmerald = Color.FromArgb(16, 185, 129);    // #10B981
    private static readonly Color ColorTextPrimary = Color.FromArgb(255, 255, 255);
    private static readonly Color ColorTextBody = Color.FromArgb(238, 241, 236);
    private static readonly Color ColorTextMuted = Color.FromArgb(154, 165, 206);
    private static readonly Color ColorDangerBg = Color.FromArgb(59, 23, 36);
    private static readonly Color ColorDangerText = Color.FromArgb(255, 133, 154);

    // Header controls
    private Button _headerMasterBtn = null!;

    // Tab buttons & Pages
    private Button _tabPlaygroundBtn = null!;
    private Button _tabSettingsBtn = null!;
    private Button _tabStatsBtn = null!;
    private Button _tabGuideBtn = null!;

    private Panel _pagePlayground = null!;
    private Panel _pageSettings = null!;
    private Panel _pageStats = null!;
    private Panel _pageGuide = null!;

    // Playground controls
    private TextBox _inputBox = null!;
    private TextBox _outputBox = null!;
    private Button _copyBtn = null!;
    private Label _charCountLabel = null!;

    // Settings controls
    private Button _masterToggleBtn = null!;
    private Button _slangToggleBtn = null!;
    private Button _startupToggleBtn = null!;
    private RadioButton _strictRadio = null!;
    private RadioButton _aggressiveRadio = null!;

    // Stats controls
    private Label _statWordsConvertedLabel = null!;
    private System.Windows.Forms.Timer _statsTimer = null!;

    internal DashboardForm(
        TrayApplicationContext context,
        KeyboardHook keyboardHook,
        LanguageDetector detector)
        : this(keyboardHook, detector,
               context.SetStartup,
               context.GetIsStartupEnabled,
               context.OpenWhitelist,
               context.OpenBlacklist,
               context.ExitApplication)
    {
        _context = context;
    }

    public DashboardForm(
        KeyboardHook keyboardHook,
        LanguageDetector detector,
        Action<bool>? onStartupToggled = null,
        Func<bool>? isStartupEnabled = null,
        Action? openWhitelist = null,
        Action? openBlacklist = null,
        Action? onExit = null)
    {
        _keyboardHook = keyboardHook;
        _detector = detector;
        _onStartupToggled = onStartupToggled;
        _isStartupEnabled = isStartupEnabled;
        _openWhitelist = openWhitelist;
        _openBlacklist = openBlacklist;
        _onExit = onExit;

        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "YangiAlifbo — Boshqaruv Paneli (Control Center)";
        ClientSize = new Size(980, 720);
        MinimumSize = new Size(880, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ColorInk;
        ForeColor = ColorTextBody;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        ShowIcon = true;
        DoubleBuffered = true;

        // Main layout container (Header, TabBar, PageContent, Footer)
        var root = new Panel { Dock = DockStyle.Fill, BackColor = ColorInk };

        // ── 1. Header (Always Visible) ──────────────────────────────
        var headerPanel = BuildHeaderPanel();

        // ── 2. Tab Bar ───────────────────────────────────────────────
        var tabBar = BuildTabBar();

        // ── 3. Footer (Always Visible) ───────────────────────────────
        var footerPanel = BuildFooterPanel();

        // ── 4. Content Area ──────────────────────────────────────────
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorInk,
            Padding = new Padding(18)
        };

        _pagePlayground = BuildPlaygroundPage();
        _pageSettings = BuildSettingsPage();
        _pageStats = BuildStatsPage();
        _pageGuide = BuildGuidePage();

        contentPanel.Controls.Add(_pagePlayground);
        contentPanel.Controls.Add(_pageSettings);
        contentPanel.Controls.Add(_pageStats);
        contentPanel.Controls.Add(_pageGuide);

        // Assemble root in correct dock order
        root.Controls.Add(contentPanel); // Fill added first so Top/Bottom dock properly
        root.Controls.Add(footerPanel);  // Bottom
        root.Controls.Add(tabBar);       // Top
        root.Controls.Add(headerPanel);  // Top

        Controls.Add(root);

        // Select initial tab
        SelectTab(0);

        // Update live preview & sync state
        UpdateLiveTranslation();
        NotifyStateChanged();

        // Stats timer for real-time tickers
        _statsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _statsTimer.Tick += (_, _) => UpdateStatsTicker();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _statsTimer.Start();
    }

    // ──────────────────────────────────────────────────────────────────
    // HEADER BUILDER
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = ColorHeader,
            Padding = new Padding(20, 10, 20, 10)
        };

        // Custom bottom border paint
        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
        };

        // Left: Branding flow
        var brandFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 600,
            Height = 54,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var logoLabel = new Label
        {
            Text = "Ö",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorTurq,
            Size = new Size(42, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 4, 12, 0)
        };

        var titlesPanel = new Panel
        {
            Size = new Size(520, 50),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = "YangiAlifbo",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            AutoSize = true,
            Location = new Point(0, 0)
        };

        var versionBadge = new Label
        {
            Text = " v1.5.0 ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = ColorTurq,
            ForeColor = ColorInk,
            AutoSize = true,
            Location = new Point(160, 6),
            Padding = new Padding(4, 2, 4, 2)
        };

        var subtitleLabel = new Label
        {
            Text = "1995 ➔ 2024 Yangi o'zbek yozuvi konvertori va jonli muloqot normalizatori",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorTextMuted,
            AutoSize = true,
            Location = new Point(1, 26)
        };

        titlesPanel.Controls.AddRange(new Control[] { titleLabel, versionBadge, subtitleLabel });
        brandFlow.Controls.Add(logoLabel);
        brandFlow.Controls.Add(titlesPanel);

        // Right: Master Power Toggle Button
        _headerMasterBtn = new Button
        {
            Dock = DockStyle.Right,
            Width = 220,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 4)
        };
        _headerMasterBtn.FlatAppearance.BorderSize = 0;
        _headerMasterBtn.Click += (_, _) => DoToggleEnabled();

        panel.Controls.Add(brandFlow);
        panel.Controls.Add(_headerMasterBtn);

        return panel;
    }

    // ──────────────────────────────────────────────────────────────────
    // TAB BAR BUILDER
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildTabBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(15, 22, 56),
            Padding = new Padding(20, 0, 20, 0)
        };

        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
        };

        var tabFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _tabPlaygroundBtn = CreateTabButton("✍️  Jonli Sinash (Playground)", 0);
        _tabSettingsBtn = CreateTabButton("⚙️  Barcha Sozlamalar (Settings)", 1);
        _tabStatsBtn = CreateTabButton("📊  Statistika & Lug'at (Stats)", 2);
        _tabGuideBtn = CreateTabButton("ℹ️  Qoidalar & Yordam (Guide)", 3);

        tabFlow.Controls.AddRange(new Control[] {
            _tabPlaygroundBtn,
            _tabSettingsBtn,
            _tabStatsBtn,
            _tabGuideBtn
        });

        panel.Controls.Add(tabFlow);
        return panel;
    }

    private Button CreateTabButton(string text, int tabIndex)
    {
        var btn = new Button
        {
            Text = text,
            Height = 47,
            AutoSize = true,
            Padding = new Padding(16, 0, 16, 0),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.Transparent,
            ForeColor = ColorTextMuted,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => SelectTab(tabIndex);
        return btn;
    }

    private void SelectTab(int index)
    {
        var tabs = new[] { _tabPlaygroundBtn, _tabSettingsBtn, _tabStatsBtn, _tabGuideBtn };
        var pages = new[] { _pagePlayground, _pageSettings, _pageStats, _pageGuide };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = (i == index);
            tabs[i].BackColor = active ? ColorTabActive : Color.Transparent;
            tabs[i].ForeColor = active ? ColorTurq : ColorTextMuted;
            pages[i].Visible = active;
            if (active)
            {
                pages[i].Dock = DockStyle.Fill;
                pages[i].BringToFront();
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // TAB 1: PLAYGROUND PAGE
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildPlaygroundPage()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = ColorInk };

        // Top Hint
        var hintLabel = new Label
        {
            Text = "💡 Klaviatura orqali yozganda dastur orqa fonda istalgan ilovada (Telegram, Word, Brauzer) avtomatik ishlaydi. Quyidagi maydonda esa yangi qoidalarni jonli sinab ko'rishingiz mumkin:",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(0, 4, 0, 0)
        };

        // Main 2-column Grid
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 10)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // ── Left Card: Input ───────────────────────────────────────
        var leftCard = CreateCardContainer();
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // Title
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Input
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Bottom action

        var leftTitle = new Label
        {
            Text = "Eski imloda yozing (1995 lotin yoki chat qisqartmalari):",
            ForeColor = ColorSaffron,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _inputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            BackColor = ColorHeader,
            ForeColor = ColorTextPrimary,
            Font = new Font("Segoe UI", 11.5f),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            Text = "o'zbekcha gaplashamz, yaxwi boldi, oshani korishamz!"
        };
        _inputBox.TextChanged += (_, _) => UpdateLiveTranslation();

        var leftBottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var clearBtn = new Button
        {
            Text = "✕ Tozalash",
            Dock = DockStyle.Left,
            Width = 95,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(32, 44, 88),
            ForeColor = ColorTextMuted,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        clearBtn.FlatAppearance.BorderSize = 0;
        clearBtn.Click += (_, _) => _inputBox.Clear();

        _charCountLabel = new Label
        {
            Text = "Belgilar: 0  |  So'zlar: 0",
            Dock = DockStyle.Right,
            AutoSize = true,
            ForeColor = ColorTextMuted,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 6, 0, 0)
        };
        leftBottom.Controls.Add(clearBtn);
        leftBottom.Controls.Add(_charCountLabel);

        leftLayout.Controls.Add(leftTitle, 0, 0);
        leftLayout.Controls.Add(_inputBox, 0, 1);
        leftLayout.Controls.Add(leftBottom, 0, 2);
        leftCard.Controls.Add(leftLayout);

        // ── Right Card: Output ──────────────────────────────────────
        var rightCard = CreateCardContainer();
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var rightHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var rightTitle = new Label
        {
            Text = "2024 Yangi alifboda natija:",
            ForeColor = ColorTurq,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        _copyBtn = new Button
        {
            Text = "📋 Nusxalash (Copy)",
            Dock = DockStyle.Right,
            Width = 140,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorTurq,
            ForeColor = ColorInk,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _copyBtn.FlatAppearance.BorderSize = 0;
        _copyBtn.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_outputBox.Text))
            {
                Clipboard.SetText(_outputBox.Text);
                _copyBtn.Text = "✓ Nusxalandi!";
                _copyBtn.BackColor = ColorEmerald;
                _copyBtn.ForeColor = Color.White;
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) =>
                {
                    _copyBtn.Text = "📋 Nusxalash (Copy)";
                    _copyBtn.BackColor = ColorTurq;
                    _copyBtn.ForeColor = ColorInk;
                    t.Stop();
                    t.Dispose();
                };
                t.Start();
            }
        };
        rightHeader.Controls.Add(rightTitle);
        rightHeader.Controls.Add(_copyBtn);

        _outputBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            BackColor = ColorCard,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        var rightBottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var speedLabel = new Label
        {
            Text = "⚡ O'girish tezligi: < 0.02 ms (O(1))",
            Dock = DockStyle.Left,
            AutoSize = true,
            ForeColor = ColorTurq,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        var sampleBtn = new Button
        {
            Text = "🔄 Namuna matn",
            Dock = DockStyle.Right,
            Width = 110,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(32, 44, 88),
            ForeColor = ColorTextMuted,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        sampleBtn.FlatAppearance.BorderSize = 0;
        sampleBtn.Click += (_, _) =>
        {
            _inputBox.Text = "o'zbekcha gaplashamz, yaxwi boldi, oshani korishamz!";
        };
        rightBottom.Controls.Add(speedLabel);
        rightBottom.Controls.Add(sampleBtn);

        rightLayout.Controls.Add(rightHeader, 0, 0);
        rightLayout.Controls.Add(_outputBox, 0, 1);
        rightLayout.Controls.Add(rightBottom, 0, 2);
        rightCard.Controls.Add(rightLayout);

        grid.Controls.Add(leftCard, 0, 0);
        grid.Controls.Add(rightCard, 1, 0);

        // Bottom Rule Pills Banner
        var pillsBanner = BuildPillsBanner();

        page.Controls.Add(grid);
        page.Controls.Add(pillsBanner);
        page.Controls.Add(hintLabel);

        return page;
    }

    private Panel BuildPillsBanner()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = ColorHeader,
            Padding = new Padding(12, 6, 12, 6)
        };

        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var chips = new[]
        {
            ("sh ➔ ş", ColorTurq),
            ("ch ➔ ç", ColorTurq),
            ("o' ➔ ö", ColorTurq),
            ("g' ➔ ğ", ColorTurq),
            ("korishamz ➔ körişamiz", ColorSaffron),
            ("yaxwi ➔ yaxşi", ColorSaffron)
        };

        foreach (var (text, color) in chips)
        {
            var lbl = new Label
            {
                Text = $"  {text}  ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = color,
                BackColor = ColorCard,
                AutoSize = true,
                Margin = new Padding(0, 2, 10, 0),
                Padding = new Padding(4, 2, 4, 2)
            };
            flow.Controls.Add(lbl);
        }

        panel.Controls.Add(flow);
        return panel;
    }

    // ──────────────────────────────────────────────────────────────────
    // TAB 2: ALL SETTINGS PAGE
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildSettingsPage()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorInk,
            AutoScroll = true,
            Padding = new Padding(0, 0, 10, 0)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 920,
            BackColor = ColorInk
        };

        // ── Card 1: Master Power ──────────────────────────────────
        _masterToggleBtn = CreateStateToggleButton();
        _masterToggleBtn.Click += (_, _) => DoToggleEnabled();

        var cardMaster = CreateSettingRowCard(
            "⚡ Tizim Faolligi (Master Conversion)",
            "Klaviatura orqali yozilayotgan barcha o'zbekcha matnlarni yangi alifboga avtomatik o'girish.\nTezkor tugma: Ctrl + Shift + U (istalgan ilovada ishlaydi)",
            _masterToggleBtn);

        // ── Card 2: Chat Slang Normalizer ─────────────────────────
        _slangToggleBtn = CreateStateToggleButton();
        _slangToggleBtn.Click += (_, _) =>
        {
            bool next = !_keyboardHook.IsChatSlangNormalizerEnabled;
            if (_context != null) next = !_context.IsChatSlangNormalizerEnabled;
            DoSetChatSlang(next);
            UpdateLiveTranslation();
        };

        var cardSlang = CreateSettingRowCard(
            "💬 Jonli Til va Chat Qisqartmalari (Chat Slang Normalizer)",
            "Telegram, Instagram va muloqotdagi so'zlashuv qisqartmalarini to'g'ri yangi alifboga o'giradi.\nNamunalar: korishamz ➔ körişamiz, yaxwi ➔ yaxşi, bovotdi ➔ bölmoqda, tawavor ➔ taşavorgin",
            _slangToggleBtn);

        // ── Card 3: Conversion Mode (Strict vs Aggressive) ─────────
        var cardMode = CreateModeSelectionCard();

        // ── Card 4: Run on Startup ────────────────────────────────
        _startupToggleBtn = CreateStateToggleButton();
        _startupToggleBtn.Click += (_, _) =>
        {
            DoSetStartup(!DoGetStartupEnabled());
        };

        var cardStartup = CreateSettingRowCard(
            "🚀 Windows Bilan Birga Ishga Tushirish (Startup)",
            "Kompyuter yoqilganda dastur orqa fonda avtomatik ravishda ishga tushadi va tizim patnisiga joylashadi.\nO'chirilganda dasturni har safar qo'lda ochish lozim bo'ladi.",
            _startupToggleBtn);

        // ── Card 5: Whitelist & Blacklist ─────────────────────────
        var cardLists = CreateListsCard();

        flow.Controls.AddRange(new Control[] {
            cardMaster,
            cardSlang,
            cardMode,
            cardStartup,
            cardLists
        });

        page.Controls.Add(flow);
        return page;
    }

    private Panel CreateSettingRowCard(string title, string description, Control actionControl)
    {
        var card = CreateCardContainer();
        card.Height = 100;
        card.Width = 910;
        card.Margin = new Padding(0, 0, 0, 14);

        var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };

        var textPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            Dock = DockStyle.Top,
            Height = 26
        };

        var descLbl = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill
        };

        textPanel.Controls.Add(descLbl);
        textPanel.Controls.Add(titleLbl);

        actionControl.Dock = DockStyle.Right;

        inner.Controls.Add(textPanel);
        inner.Controls.Add(actionControl);
        card.Controls.Add(inner);

        return card;
    }

    private Panel CreateModeSelectionCard()
    {
        var card = CreateCardContainer();
        card.Height = 160;
        card.Width = 910;
        card.Margin = new Padding(0, 0, 0, 14);

        var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };

        var titleLbl = new Label
        {
            Text = "🎯 Konversiya Qat'iyligi (Rejim)",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            Dock = DockStyle.Top,
            Height = 26
        };

        var descLbl = new Label
        {
            Text = "O'zbek so'zlarini aniqlash va o'girish qoidalarini tanlang:",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Top,
            Height = 22
        };

        var optionsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 0, 0) };

        _strictRadio = new RadioButton
        {
            Text = "Qat'iy rejim (Strict — Tavsiya etiladi)\n948 000+ so'zli rasmiy o'zbek lug'ati bo'yicha tekshiriladi. Dasturlash va inglizcha so'zlar (chat, post, function, true, test) hech qachon buzilmaydi.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorTurq,
            Location = new Point(0, 4),
            Size = new Size(860, 46),
            Cursor = Cursors.Hand
        };
        _strictRadio.CheckedChanged += (_, _) =>
        {
            if (_strictRadio.Checked)
            {
                DoSetMode(TransliterationMode.Strict);
                UpdateLiveTranslation();
            }
        };

        _aggressiveRadio = new RadioButton
        {
            Text = "Agressiv rejim (Aggressive)\nBarcha mos harf birikmalari lug'atda bo'lish-bo'lmasligidan qat'i nazar to'liq o'giriladi.",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorSaffron,
            Location = new Point(0, 52),
            Size = new Size(860, 40),
            Cursor = Cursors.Hand
        };
        _aggressiveRadio.CheckedChanged += (_, _) =>
        {
            if (_aggressiveRadio.Checked)
            {
                DoSetMode(TransliterationMode.Aggressive);
                UpdateLiveTranslation();
            }
        };

        optionsPanel.Controls.Add(_strictRadio);
        optionsPanel.Controls.Add(_aggressiveRadio);

        inner.Controls.Add(optionsPanel);
        inner.Controls.Add(descLbl);
        inner.Controls.Add(titleLbl);
        card.Controls.Add(inner);

        return card;
    }

    private Panel CreateListsCard()
    {
        var card = CreateCardContainer();
        card.Height = 110;
        card.Width = 910;
        card.Margin = new Padding(0, 0, 0, 14);

        var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };

        var textPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Text = "📚 Shaxsiy Lug'atlar va Maxsus Ro'yxatlar",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            Dock = DockStyle.Top,
            Height = 26
        };

        var descLbl = new Label
        {
            Text = "O'zingizning maxsus atamalaringiz yoki hech qachon o'zgarmasligi kerak bo'lgan so'zlarni boshqaring.\nFayl ochilganda har bir qatorda bittadan so'z kiritiladi.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill
        };

        textPanel.Controls.Add(descLbl);
        textPanel.Controls.Add(titleLbl);

        var btnsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 360,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        var whiteBtn = new Button
        {
            Text = "📝 Oq ro'yxat (Whitelist)",
            Width = 170,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 42, 94),
            ForeColor = ColorTurq,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        whiteBtn.FlatAppearance.BorderColor = ColorTurq;
        whiteBtn.Click += (_, _) => DoOpenWhitelist();

        var blackBtn = new Button
        {
            Text = "🛡️ Qora ro'yxat (Blacklist)",
            Width = 170,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 42, 94),
            ForeColor = ColorSaffron,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        blackBtn.FlatAppearance.BorderColor = ColorSaffron;
        blackBtn.Click += (_, _) => DoOpenBlacklist();

        btnsPanel.Controls.Add(whiteBtn);
        btnsPanel.Controls.Add(blackBtn);

        inner.Controls.Add(textPanel);
        inner.Controls.Add(btnsPanel);
        card.Controls.Add(inner);

        return card;
    }

    private Button CreateStateToggleButton()
    {
        var btn = new Button
        {
            Width = 150,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 12, 0, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // ──────────────────────────────────────────────────────────────────
    // TAB 3: STATS & LEXICON PAGE
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildStatsPage()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorInk,
            AutoScroll = true,
            Padding = new Padding(0, 0, 10, 0)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 920,
            BackColor = ColorInk
        };

        // Top 3 Badges
        var badgesGrid = new TableLayoutPanel
        {
            Width = 910,
            Height = 110,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        badgesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        badgesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        badgesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        var card1 = CreateMetricBadge("947 967", "Rasmiy O'zbek Lug'ati So'zlari", ColorTurq);
        var card2 = CreateMetricBadge("10 057", "Xorijiy / Inglizcha Himoyalangan So'zlar", ColorSaffron);
        var card3 = CreateMetricBadge("< 0.05 ms", "O'rtacha So'z Tahlili Tezligi (O(1))", ColorEmerald);

        badgesGrid.Controls.Add(card1, 0, 0);
        badgesGrid.Controls.Add(card2, 1, 0);
        badgesGrid.Controls.Add(card3, 2, 0);

        // Session Tracker Card
        var sessionCard = CreateCardContainer();
        sessionCard.Width = 910;
        sessionCard.Height = 120;
        sessionCard.Margin = new Padding(0, 0, 0, 16);

        var sessionInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var sessionTitle = new Label
        {
            Text = "📈 Joriy Sessiya Metrikayi",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };

        _statWordsConvertedLabel = new Label
        {
            Text = "Yuklanmoqda...",
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill
        };

        sessionInner.Controls.Add(_statWordsConvertedLabel);
        sessionInner.Controls.Add(sessionTitle);
        sessionCard.Controls.Add(sessionInner);

        // Action Buttons Card
        var actionsCard = CreateCardContainer();
        actionsCard.Width = 910;
        actionsCard.Height = 100;
        actionsCard.Margin = new Padding(0, 0, 0, 16);

        var actionsInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var actionsTitle = new Label
        {
            Text = "🌐 Qo'shimcha Havolalar va Amallar",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColorTextPrimary,
            Dock = DockStyle.Top,
            Height = 26
        };

        var actionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };

        var githubBtn = new Button
        {
            Text = "🌐 GitHub Rasmiy Sahifasi",
            Width = 200,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 42, 94),
            ForeColor = ColorTurq,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 6, 12, 0)
        };
        githubBtn.FlatAppearance.BorderColor = ColorTurq;
        githubBtn.Click += (_, _) =>
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

        var logsBtn = new Button
        {
            Text = "📁 Jurnallar Jildini Ochish (Logs)",
            Width = 240,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 42, 94),
            ForeColor = ColorTextBody,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 6, 12, 0)
        };
        logsBtn.FlatAppearance.BorderColor = ColorCardBorder;
        logsBtn.Click += (_, _) =>
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YangiAlifbo");
                Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", dir);
            }
            catch { }
        };

        var reloadBtn = new Button
        {
            Text = "🔄 Lug'atlarni Qayta Yuklash",
            Width = 220,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 42, 94),
            ForeColor = ColorTextBody,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 6, 0, 0)
        };
        reloadBtn.FlatAppearance.BorderColor = ColorCardBorder;
        reloadBtn.Click += (_, _) =>
        {
            _detector.ReloadUserLists();
            MessageBox.Show("Shaxsiy lug'atlar (Oq va Qora ro'yxat) muvaffaqiyatli qayta yuklandi!", "YangiAlifbo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        actionsFlow.Controls.AddRange(new Control[] { githubBtn, logsBtn, reloadBtn });
        actionsInner.Controls.Add(actionsFlow);
        actionsInner.Controls.Add(actionsTitle);
        actionsCard.Controls.Add(actionsInner);

        flow.Controls.AddRange(new Control[] { badgesGrid, sessionCard, actionsCard });
        page.Controls.Add(flow);

        return page;
    }

    private Panel CreateMetricBadge(string value, string title, Color accent)
    {
        var card = CreateCardContainer();
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(4);

        var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };

        var valLbl = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = accent,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        inner.Controls.Add(titleLbl);
        inner.Controls.Add(valLbl);
        card.Controls.Add(inner);

        return card;
    }

    // ──────────────────────────────────────────────────────────────────
    // TAB 4: GUIDE & HELP PAGE
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildGuidePage()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorInk,
            AutoScroll = true,
            Padding = new Padding(0, 0, 10, 0)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 920,
            BackColor = ColorInk
        };

        var cardRules = CreateCardContainer();
        cardRules.Width = 910;
        cardRules.Height = 220;
        cardRules.Margin = new Padding(0, 0, 0, 16);

        var rulesInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var rulesTitle = new Label
        {
            Text = "📖 2024 Yangi O'zbek Alifbosi Harflari Jadvali",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColorTurq,
            Dock = DockStyle.Top,
            Height = 30
        };

        var rulesBody = new Label
        {
            Text = "•  sh ➔ ş   (Masalan: shahar ➔ şahar, ishchi ➔ işçi, yaxshi ➔ yaxşi)\n" +
                   "•  ch ➔ ç   (Masalan: choy ➔ çoy, chiroyli ➔ çiroyli, uch ➔ uç)\n" +
                   "•  o' ➔ ö   (Masalan: o'zbek ➔ özbek, ko'r ➔ kör, bo'ldi ➔ böldi)\n" +
                   "•  g' ➔ ğ   (Masalan: g'alaba ➔ ğalaba, tog' ➔ toğ, bog' ➔ boğ)\n\n" +
                   "Dastur so'z tugaganda (Bo'sh joy, nuqta, vergul yoki Enter bosilganda) so'zni tahlil qilib, darhol almashtiradi.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorTextBody,
            Dock = DockStyle.Fill
        };

        rulesInner.Controls.Add(rulesBody);
        rulesInner.Controls.Add(rulesTitle);
        cardRules.Controls.Add(rulesInner);

        var cardSlangGuide = CreateCardContainer();
        cardSlangGuide.Width = 910;
        cardSlangGuide.Height = 180;
        cardSlangGuide.Margin = new Padding(0, 0, 0, 16);

        var slangInner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var slangTitle = new Label
        {
            Text = "💬 Jonli Chat va So'zlashuv Qisqartmalari",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColorSaffron,
            Dock = DockStyle.Top,
            Height = 30
        };

        var slangBody = new Label
        {
            Text = "Foydalanuvchilar chatlarda tez yozish uchun so'zlarni qisqartirib ishlatadilar:\n" +
                   "•  korishamz ➔ körişamiz   |   gaplashamz ➔ gaplaşamiz\n" +
                   "•  yaxwi ➔ yaxşi   |   xoliman ➔ xohlayman   |   tawavor ➔ taşavorgin\n" +
                   "•  bovotdi ➔ bölmoqda   |   bovuradi ➔ bölaveradi   |   aytvorin ➔ aytib yuboring\n\n" +
                   "«Jonli til normalizatori» bu kabi shakllarni avtomatik tanib, to'g'ri yangi alifboga o'girib beradi.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorTextBody,
            Dock = DockStyle.Fill
        };

        slangInner.Controls.Add(slangBody);
        slangInner.Controls.Add(slangTitle);
        cardSlangGuide.Controls.Add(slangInner);

        flow.Controls.AddRange(new Control[] { cardRules, cardSlangGuide });
        page.Controls.Add(flow);

        return page;
    }

    // ──────────────────────────────────────────────────────────────────
    // FOOTER (Always Visible)
    // ──────────────────────────────────────────────────────────────────

    private Panel BuildFooterPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = ColorHeader,
            Padding = new Padding(20, 10, 20, 10)
        };

        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
        };

        var hint = new Label
        {
            Text = "💡 Maslahat: Tray ikonkaga 1 marta bosib bu oynani ochishingiz mumkin. Oyna yopilsa ham dastur orqa fonda ishlayveradi.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        var btnsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 380,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };

        var hideBtn = new Button
        {
            Text = "✕ Patnisga Yashirish",
            Width = 160,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorTurq,
            ForeColor = ColorInk,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(10, 0, 0, 0)
        };
        hideBtn.FlatAppearance.BorderSize = 0;
        hideBtn.Click += (_, _) => Hide();

        var exitBtn = new Button
        {
            Text = "🚪 Dasturdan Chiqish",
            Width = 160,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorDangerBg,
            ForeColor = ColorDangerText,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        exitBtn.FlatAppearance.BorderColor = Color.FromArgb(139, 38, 62);
        exitBtn.Click += (_, _) =>
        {
            var res = MessageBox.Show(
                "Rostdan ham YangiAlifbo dasturidan butunlay chiqmoqchimisiz?\n(Chiqilsa, avtomatik o'girish to'xtatiladi)",
                "YangiAlifbo — Chiqish",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                DoExitApplication();
            }
        };

        btnsFlow.Controls.Add(hideBtn);
        btnsFlow.Controls.Add(exitBtn);

        panel.Controls.Add(hint);
        panel.Controls.Add(btnsFlow);

        return panel;
    }

    // ──────────────────────────────────────────────────────────────────
    // HELPERS & SYNC LOGIC
    // ──────────────────────────────────────────────────────────────────

    private void DoToggleEnabled()
    {
        if (_context != null)
        {
            _context.ToggleEnabled();
        }
        else
        {
            _keyboardHook.IsEnabled = !_keyboardHook.IsEnabled;
            NotifyStateChanged();
        }
    }

    private void DoSetChatSlang(bool enabled)
    {
        if (_context != null)
        {
            _context.SetChatSlang(enabled);
        }
        else
        {
            _keyboardHook.IsChatSlangNormalizerEnabled = enabled;
            NotifyStateChanged();
        }
    }

    private void DoSetMode(TransliterationMode mode)
    {
        if (_context != null)
        {
            _context.SetMode(mode);
        }
        else
        {
            _keyboardHook.Mode = mode;
            NotifyStateChanged();
        }
    }

    private bool DoGetStartupEnabled()
    {
        if (_context != null) return _context.GetIsStartupEnabled();
        return _isStartupEnabled?.Invoke() ?? false;
    }

    private void DoSetStartup(bool enabled)
    {
        if (_context != null) _context.SetStartup(enabled);
        else _onStartupToggled?.Invoke(enabled);
        NotifyStateChanged();
    }

    private void DoOpenWhitelist()
    {
        if (_context != null) _context.OpenWhitelist();
        else _openWhitelist?.Invoke();
    }

    private void DoOpenBlacklist()
    {
        if (_context != null) _context.OpenBlacklist();
        else _openBlacklist?.Invoke();
    }

    private void DoExitApplication()
    {
        if (_context != null) _context.ExitApplication();
        else _onExit?.Invoke();
    }

    private static Panel CreateCardContainer()
    {
        var panel = new Panel
        {
            BackColor = ColorCard,
            Padding = new Padding(1)
        };

        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorCardBorder, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        return panel;
    }

    public void NotifyStateChanged()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(NotifyStateChanged));
            return;
        }

        bool isEnabled = _context != null ? _context.IsEnabled : _keyboardHook.IsEnabled;

        // Header Master Button
        _headerMasterBtn.Text = isEnabled ? "● FAOL (ACTIVE)" : "○ TO'XTATILGAN (OFF)";
        _headerMasterBtn.BackColor = isEnabled ? ColorEmerald : Color.FromArgb(45, 55, 88);
        _headerMasterBtn.ForeColor = Color.White;

        // Settings Page Master Toggle Button
        _masterToggleBtn.Text = isEnabled ? "YOQILGAN ✓" : "O'CHIRILGAN ✕";
        _masterToggleBtn.BackColor = isEnabled ? ColorEmerald : Color.FromArgb(45, 55, 88);
        _masterToggleBtn.ForeColor = Color.White;

        // Slang Toggle Button
        bool slangOn = _context != null ? _context.IsChatSlangNormalizerEnabled : _keyboardHook.IsChatSlangNormalizerEnabled;
        _slangToggleBtn.Text = slangOn ? "YOQILGAN ✓" : "O'CHIRILGAN ✕";
        _slangToggleBtn.BackColor = slangOn ? ColorTurq : Color.FromArgb(45, 55, 88);
        _slangToggleBtn.ForeColor = slangOn ? ColorInk : Color.White;

        // Startup Toggle Button
        bool startupOn = DoGetStartupEnabled();
        _startupToggleBtn.Text = startupOn ? "YOQILGAN ✓" : "O'CHIRILGAN ✕";
        _startupToggleBtn.BackColor = startupOn ? ColorTurq : Color.FromArgb(45, 55, 88);
        _startupToggleBtn.ForeColor = startupOn ? ColorInk : Color.White;

        // Conversion Mode Radios
        if (_strictRadio != null && _aggressiveRadio != null)
        {
            var currentMode = _context != null ? _context.Mode : _keyboardHook.Mode;
            bool isStrict = currentMode == TransliterationMode.Strict;
            _strictRadio.Checked = isStrict;
            _aggressiveRadio.Checked = !isStrict;
        }

        UpdateStatsTicker();
    }

    private void UpdateLiveTranslation()
    {
        string input = _inputBox.Text;
        if (string.IsNullOrEmpty(input))
        {
            _outputBox.Text = string.Empty;
            _charCountLabel.Text = "Belgilar: 0  |  So'zlar: 0";
            return;
        }

        int wordsCount = input.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        _charCountLabel.Text = $"Belgilar: {input.Length}  |  So'zlar: {wordsCount}";

        bool slangEnabled = _context != null ? _context.IsChatSlangNormalizerEnabled : _keyboardHook.IsChatSlangNormalizerEnabled;
        var mode = _context != null ? _context.Mode : _keyboardHook.Mode;
        _outputBox.Text = TextConverter.Convert(input, _detector, mode, slangEnabled, out _);
    }

    private void UpdateStatsTicker()
    {
        if (_statWordsConvertedLabel != null)
        {
            _statWordsConvertedLabel.Text =
                $"• Bu sessiyada klaviatura orqali o'zgartirilgan so'zlar: {_keyboardHook.WordsConverted:N0} ta\n" +
                $"• Tezkor tugma holati: Ctrl + Shift + U (Klaviatura kancasi: {(_keyboardHook.IsEnabled ? "Faol ✓" : "To'xtatilgan ✗")})\n" +
                $"• Xotira ishlatilishi: ~{Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)} MB";
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statsTimer?.Stop();
            _statsTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Intercept close button to hide into tray instead of terminating
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _statsTimer.Stop();
        _statsTimer.Dispose();
        base.OnFormClosing(e);
    }
}
