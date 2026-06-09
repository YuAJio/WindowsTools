namespace Klick;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // 连点类型
    private RadioButton rbMouse;
    private RadioButton rbKeyboard;

    // 键盘捕获
    private Button btnCaptureKey;
    private Label lblTargetKey;

    // 鼠标按键选择
    private ComboBox cmbMouseButton;

    // 间隔
    private NumericUpDown nudInterval;
    private Label lblInterval;

    // 状态
    private Label lblStatus;
    private Button btnMinimizeToTray;

    // 热键提示
    private Label lblHotkeyHint;

    // 托盘
    private NotifyIcon notifyIcon;
    private ContextMenuStrip trayMenu;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            _hookProc = null; // 防止 GC 回收委托
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "Klick - 连点器";
        this.Size = new Size(360, 310);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // --- 连点类型 ---
        var gbType = new GroupBox
        {
            Text = "连点类型",
            Location = new Point(12, 12),
            Size = new Size(320, 50)
        };
        rbMouse = new RadioButton
        {
            Text = "鼠标连点",
            Location = new Point(14, 20),
            Size = new Size(90, 20),
            Checked = true
        };
        rbKeyboard = new RadioButton
        {
            Text = "键盘连发",
            Location = new Point(120, 20),
            Size = new Size(90, 20)
        };
        rbMouse.CheckedChanged += OnTypeChanged;
        rbKeyboard.CheckedChanged += OnTypeChanged;
        gbType.Controls.Add(rbMouse);
        gbType.Controls.Add(rbKeyboard);

        // --- 鼠标按键选择 ---
        var gbMouse = new GroupBox
        {
            Text = "鼠标按键",
            Location = new Point(12, 70),
            Size = new Size(320, 52)
        };
        cmbMouseButton = new ComboBox
        {
            Location = new Point(14, 20),
            Size = new Size(130, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbMouseButton.Items.AddRange(["左键", "右键", "中键"]);
        cmbMouseButton.SelectedIndex = 0;
        gbMouse.Controls.Add(cmbMouseButton);

        // --- 键盘目标 ---
        var gbKeyboard = new GroupBox
        {
            Text = "目标按键（捕获模式）",
            Location = new Point(12, 70),
            Size = new Size(320, 52),
            Visible = false
        };
        btnCaptureKey = new Button
        {
            Text = "🎯 点击捕获按键",
            Location = new Point(14, 18),
            Size = new Size(130, 24)
        };
        btnCaptureKey.Click += OnCaptureKeyClick;
        lblTargetKey = new Label
        {
            Text = "未设置",
            Location = new Point(154, 21),
            Size = new Size(150, 20),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };
        gbKeyboard.Controls.Add(btnCaptureKey);
        gbKeyboard.Controls.Add(lblTargetKey);

        // --- 间隔 ---
        var gbInterval = new GroupBox
        {
            Text = "连点间隔",
            Location = new Point(12, 130),
            Size = new Size(320, 52)
        };
        lblInterval = new Label
        {
            Text = "间隔 (ms):",
            Location = new Point(14, 22),
            Size = new Size(65, 20)
        };
        nudInterval = new NumericUpDown
        {
            Location = new Point(80, 19),
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 10000,
            Value = 50
        };
        gbInterval.Controls.Add(lblInterval);
        gbInterval.Controls.Add(nudInterval);

        // --- 状态 ---
        lblStatus = new Label
        {
            Text = "⏸ 已停止",
            Location = new Point(12, 195),
            Size = new Size(320, 24),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // --- 最小化按钮 ---
        btnMinimizeToTray = new Button
        {
            Text = "最小化到托盘",
            Location = new Point(12, 228),
            Size = new Size(140, 30)
        };
        btnMinimizeToTray.Click += OnMinimizeToTray;

        // --- 热键提示 ---
        lblHotkeyHint = new Label
        {
            Text = "F8 启动  |  F9 停止",
            Location = new Point(170, 233),
            Size = new Size(160, 20),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleRight
        };

        // --- 托盘菜单 ---
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, OnShowFromTray);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, OnExit);

        notifyIcon = new NotifyIcon
        {
            Text = "Klick - 连点器",
            ContextMenuStrip = trayMenu,
            Visible = false
        };
        // 用系统内置图标
        notifyIcon.Icon = SystemIcons.Application;
        notifyIcon.DoubleClick += OnShowFromTray;

        // --- 表单 ---
        this.Controls.Add(gbType);
        this.Controls.Add(gbMouse);
        this.Controls.Add(gbKeyboard);
        this.Controls.Add(gbInterval);
        this.Controls.Add(lblStatus);
        this.Controls.Add(btnMinimizeToTray);
        this.Controls.Add(lblHotkeyHint);
        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;

        // 缓存控件的 field，方便切换显示用
        _gbMouse = gbMouse;
        _gbKeyboard = gbKeyboard;
    }

    // 缓存动态切换引用
    private GroupBox _gbMouse;
    private GroupBox _gbKeyboard;
}
