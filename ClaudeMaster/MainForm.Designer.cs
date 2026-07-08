namespace ClaudeMaster;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // ── Header ──
    private Label _lblTitle;

    // ── Status 环境检测 ──
    private GroupBox _gbStatus;
    private Label _lblNodeStatus;
    private LinkLabel _lnkNodeInstall;
    private Label _lblClaudeStatus;

    // ── Install 安装 ──
    private GroupBox _gbInstall;
    private Button _btnInstall;
    private TextBox _txtInstallLog;

    // ── Config API 配置 ──
    private GroupBox _gbConfig;
    private TextBox _txtBaseUrl;
    private TextBox _txtApiToken;
    private Button _btnSave;
    private Button _btnTest;

    // ── Tray 系统托盘 ──
    private NotifyIcon _notifyIcon = null!;
    private ContextMenuStrip _trayMenu = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _installCts?.Cancel();
            _installCts?.Dispose();
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Icon = IconHelper.LoadFromResource("ClaudeMaster.app.png");
        this.Text = "ClaudeMaster — Claude Code 环境配置器";
        this.Size = new Size(460, 520);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ═══════════════════════════════════════
        // Header
        // ═══════════════════════════════════════
        _lblTitle = new Label
        {
            Text = "🃏 ClaudeMaster",
            Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
            Location = new Point(14, 10),
            Size = new Size(420, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ═══════════════════════════════════════
        // Section 1: 环境检测
        // ═══════════════════════════════════════
        _gbStatus = new GroupBox
        {
            Text = "🔍 环境检测",
            Location = new Point(12, 50),
            Size = new Size(420, 90)
        };

        var lblNodeLabel = new Label
        {
            Text = "Node.js：",
            Location = new Point(14, 24),
            Size = new Size(72, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        _lblNodeStatus = new Label
        {
            Text = "检测中...",
            Location = new Point(90, 24),
            Size = new Size(200, 20),
            ForeColor = Color.Gray
        };
        _lnkNodeInstall = new LinkLabel
        {
            Text = "下载安装",
            Location = new Point(290, 24),
            Size = new Size(110, 20),
            Visible = false,
            LinkColor = Color.DodgerBlue
        };
        _lnkNodeInstall.LinkClicked += OnNodeInstallLinkClicked;

        var lblClaudeLabel = new Label
        {
            Text = "Claude Code：",
            Location = new Point(14, 52),
            Size = new Size(72, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        _lblClaudeStatus = new Label
        {
            Text = "检测中...",
            Location = new Point(90, 52),
            Size = new Size(310, 20),
            ForeColor = Color.Gray
        };

        _gbStatus.Controls.AddRange(new Control[]
        {
            lblNodeLabel, _lblNodeStatus, _lnkNodeInstall,
            lblClaudeLabel, _lblClaudeStatus
        });

        // ═══════════════════════════════════════
        // Section 2: 安装 Claude Code
        // ═══════════════════════════════════════
        _gbInstall = new GroupBox
        {
            Text = "📦 安装 Claude Code",
            Location = new Point(12, 148),
            Size = new Size(420, 155)
        };

        _btnInstall = new Button
        {
            Text = "⬇ 安装 / 更新 Claude Code",
            Location = new Point(14, 22),
            Size = new Size(210, 30),
            Enabled = false,
            FlatStyle = FlatStyle.System
        };
        _btnInstall.Click += OnInstallClick;

        _txtInstallLog = new TextBox
        {
            Location = new Point(14, 58),
            Size = new Size(392, 85),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 8.25f),
            WordWrap = true
        };

        _gbInstall.Controls.Add(_btnInstall);
        _gbInstall.Controls.Add(_txtInstallLog);

        // ═══════════════════════════════════════
        // Section 3: API 配置
        // ═══════════════════════════════════════
        _gbConfig = new GroupBox
        {
            Text = "⚙ API 配置",
            Location = new Point(12, 311),
            Size = new Size(420, 120)
        };

        var lblBaseUrl = new Label
        {
            Text = "Base URL：",
            Location = new Point(14, 26),
            Size = new Size(70, 23),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtBaseUrl = new TextBox
        {
            Location = new Point(88, 24),
            Size = new Size(318, 23),
            PlaceholderText = "https://api.anthropic.com"
        };

        var lblApiKey = new Label
        {
            Text = "API Key：",
            Location = new Point(14, 54),
            Size = new Size(70, 23),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtApiToken = new TextBox
        {
            Location = new Point(88, 52),
            Size = new Size(318, 23),
            UseSystemPasswordChar = true,
            PlaceholderText = "sk-..."
        };

        _btnSave = new Button
        {
            Text = "💾 保存配置",
            Location = new Point(88, 82),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.System
        };
        _btnSave.Click += OnSaveClick;

        _btnTest = new Button
        {
            Text = "🔍 检测配置",
            Location = new Point(226, 82),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.System
        };
        _btnTest.Click += OnTestClick;

        _gbConfig.Controls.AddRange(new Control[]
        {
            lblBaseUrl, _txtBaseUrl,
            lblApiKey, _txtApiToken,
            _btnSave, _btnTest
        });

        // ═══════════════════════════════════════
        // System Tray 系统托盘
        // ═══════════════════════════════════════
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("显示窗口", null, OnShowFromTray);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("退出", null, OnExit);

        _notifyIcon = new NotifyIcon
        {
            Text = "ClaudeMaster — Claude Code 环境配置器",
            Icon = IconHelper.LoadFromResource("ClaudeMaster.app.png"),
            ContextMenuStrip = _trayMenu,
            Visible = false
        };
        _notifyIcon.DoubleClick += OnShowFromTray;

        // ═══════════════════════════════════════
        // Add to Form
        // ═══════════════════════════════════════
        this.Controls.AddRange(new Control[]
        {
            _lblTitle,
            _gbStatus,
            _gbInstall,
            _gbConfig
        });

        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;
        this.Shown += OnFormShown;
    }
}
