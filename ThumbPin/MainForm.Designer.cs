namespace ThumbPin;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    private Button btnCapture;
    private Button btnToggleForeground;
    private Button btnUnpinAll;
    private Label lblPinnedCount;
    private Label lblStatus;

    private NotifyIcon notifyIcon;
    private ContextMenuStrip trayMenu;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _hookProc = null;
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "ThumbPin — 窗口置顶";
        this.Icon = IconHelper.LoadFromResource("ThumbPin.app.png");
        this.Size = new Size(320, 220);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── 捕获按钮 ──
        btnCapture = new Button
        {
            Text = "🎯 捕获窗口并置顶",
            Location = new Point(20, 20),
            Size = new Size(260, 40),
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        btnCapture.Click += OnCapture;

        // ── 快速切换前台窗口 ──
        btnToggleForeground = new Button
        {
            Text = "📌 置顶/取消 当前窗口 (Ctrl+Shift+F7)",
            Location = new Point(20, 72),
            Size = new Size(260, 40)
        };
        btnToggleForeground.Click += (s, e) => ToggleForegroundWindow();

        // ── 全部取消 ──
        btnUnpinAll = new Button
        {
            Text = "🗑 取消全部置顶",
            Location = new Point(20, 124),
            Size = new Size(260, 30)
        };
        btnUnpinAll.Click += (s, e) => UnpinAll();

        // ── 状态 ──
        lblPinnedCount = new Label
        {
            Text = "已置顶: 0 个窗口",
            Location = new Point(20, 162),
            Size = new Size(260, 20),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblStatus = new Label
        {
            Text = "",
            Location = new Point(20, 185),
            Size = new Size(260, 20),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── 托盘菜单 ──
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, OnShowFromTray);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, OnExit);

        notifyIcon = new NotifyIcon
        {
            Text = "ThumbPin — 窗口置顶",
            Icon = IconHelper.LoadFromResource("ThumbPin.app.png"),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        notifyIcon.DoubleClick += OnShowFromTray;

        // ── 表单 ──
        this.Controls.Add(btnCapture);
        this.Controls.Add(btnToggleForeground);
        this.Controls.Add(btnUnpinAll);
        this.Controls.Add(lblPinnedCount);
        this.Controls.Add(lblStatus);
        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;
    }
}
