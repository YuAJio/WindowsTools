namespace ThumbPin;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

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
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "ThumbPin — 窗口置顶";
        this.Icon = IconHelper.LoadFromResource("ThumbPin.app.png");
        this.Size = new Size(300, 180);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── 说明 ──
        var lblHint = new Label
        {
            Text = "快捷键: Ctrl + Shift + F7",
            Location = new Point(20, 20),
            Size = new Size(250, 24),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── 全部取消 ──
        btnUnpinAll = new Button
        {
            Text = "🗑 取消全部置顶",
            Location = new Point(50, 60),
            Size = new Size(190, 32)
        };
        btnUnpinAll.Click += (s, e) => UnpinAll();

        // ── 状态 ──
        lblPinnedCount = new Label
        {
            Text = "已置顶: 0 个窗口",
            Location = new Point(20, 105),
            Size = new Size(250, 20),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblStatus = new Label
        {
            Text = "",
            Location = new Point(20, 128),
            Size = new Size(250, 20),
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
        this.Controls.Add(lblHint);
        this.Controls.Add(btnUnpinAll);
        this.Controls.Add(lblPinnedCount);
        this.Controls.Add(lblStatus);
        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;
    }
}
