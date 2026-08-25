namespace MoodyBlues;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    private ListBox lbRecords;
    private Button btnDelete;
    private Button btnPlay;
    private Button btnStop;
    private Button btnOpenEditor;
    private CheckBox chkTrackCursor;
    private Label lblStatus;
    private Label lblHotkeyHint;
    private Label lblRecordCount;

    private NotifyIcon notifyIcon;
    private ContextMenuStrip trayMenu;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _recordEngine?.Dispose();
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "MoodyBlues — 操作录制与重播";
        this.Icon = IconHelper.LoadFromResource("MoodyBlues.app.png");
        this.Size = new Size(420, 450);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── 热键提示 ──
        lblHotkeyHint = new Label
        {
            Text = "F4 开始录制  |  F5 停止录制  |  F6 播放选中",
            Location = new Point(12, 12),
            Size = new Size(380, 22),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── 录制选项 ──
        chkTrackCursor = new CheckBox
        {
            Text = "📍 录制时追踪鼠标坐标（播放时还原光标位置，游戏内建议关闭）",
            Location = new Point(12, 40),
            Size = new Size(380, 22),
            Checked = true,
            Font = new Font("Segoe UI", 8)
        };

        // ── 状态 ──
        lblStatus = new Label
        {
            Text = "⏸ 等待操作...",
            Location = new Point(12, 66),
            Size = new Size(380, 22),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── 录制列表 ──
        var gbList = new GroupBox
        {
            Text = "📼 录制记录",
            Location = new Point(12, 96),
            Size = new Size(380, 240)
        };
        lbRecords = new ListBox
        {
            Location = new Point(14, 22),
            Size = new Size(350, 155),
            IntegralHeight = false
        };
        lblRecordCount = new Label
        {
            Text = "0 条记录",
            Location = new Point(14, 184),
            Size = new Size(100, 20),
            ForeColor = Color.Gray
        };

        // ── 按钮 ──
        btnPlay = new Button
        {
            Text = "▶ 播放选中",
            Location = new Point(130, 208),
            Size = new Size(110, 28),
            Enabled = false
        };
        btnPlay.Click += (s, e) => PlaySelected();

        btnStop = new Button
        {
            Text = "⏹ 停止播放",
            Location = new Point(248, 208),
            Size = new Size(110, 28),
            Enabled = false
        };
        btnStop.Click += (s, e) =>
        {
            _playbackEngine.Stop();
            btnStop.Enabled = false;
        };

        btnDelete = new Button
        {
            Text = "🗑 删除选中",
            Location = new Point(12, 208),
            Size = new Size(110, 28),
            Enabled = false
        };
        btnDelete.Click += (s, e) => DeleteSelected();

        gbList.Controls.Add(lbRecords);
        gbList.Controls.Add(lblRecordCount);
        gbList.Controls.Add(btnPlay);
        gbList.Controls.Add(btnStop);
        gbList.Controls.Add(btnDelete);

        lbRecords.SelectedIndexChanged += (s, e) =>
        {
            btnPlay.Enabled = lbRecords.SelectedItem != null;
            btnDelete.Enabled = lbRecords.SelectedItem != null;
        };

        // ── 预设编辑器入口（独立区域） ──
        var lblDivider = new Label
        {
            Text = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            Location = new Point(12, 344),
            Size = new Size(380, 18),
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnOpenEditor = new Button
        {
            Text = "✏ 打开预设编辑器 — 手动编排按键与鼠标序列",
            Location = new Point(12, 368),
            Size = new Size(380, 34),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            Enabled = true
        };
        btnOpenEditor.Click += (s, e) =>
        {
            using var editor = new PresetEditorForm();
            editor.ShowDialog(this);
        };

        // ── 托盘 ──
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, OnShowFromTray);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, OnExit);

        notifyIcon = new NotifyIcon
        {
            Text = "MoodyBlues — 操作录制与重播",
            Icon = IconHelper.LoadFromResource("MoodyBlues.app.png"),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        notifyIcon.DoubleClick += OnShowFromTray;

        // ── 表单 ──
        this.Controls.Add(lblHotkeyHint);
        this.Controls.Add(chkTrackCursor);
        this.Controls.Add(lblStatus);
        this.Controls.Add(gbList);
        this.Controls.Add(lblDivider);
        this.Controls.Add(btnOpenEditor);
        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;
    }
}
