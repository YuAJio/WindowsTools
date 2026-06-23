namespace DailyVoice;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // 时间设置
    private DateTimePicker dtpTime;
    private Label lblTime;

    // 音量
    private TrackBar tbVolume;
    private Label lblVolume;
    private Label lblVolumePercent;

    // 语音文件列表
    private ListBox lbFiles;
    private Button btnRefreshFiles;
    private Button btnPreviewFile;
    private Label lblFileCount;

    // 视频
    private DateTimePicker dtpVideoTime;
    private Label lblVideoFile;
    private Button btnBrowseVideo;
    private Button btnClearVideo;

    // 控制按钮
    private Button btnPlayNow;
    private Button btnStop;
    private Button btnOpenFolder;

    // 状态
    private Label lblStatus;

    // 开机自启
    private CheckBox chkAutoStart;

    // 保存
    private Button btnSave;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _scheduler?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "DailyVoice — 每日语音播放";
        this.Icon = IconHelper.LoadFromResource("DailyVoice.app.png");
        this.Size = new Size(420, 550);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── 时间设置 ──
        var gbTime = new GroupBox
        {
            Text = "⏰ 每日播放时间",
            Location = new Point(12, 12),
            Size = new Size(380, 50)
        };
        lblTime = new Label
        {
            Text = "每天",
            Location = new Point(14, 21),
            Size = new Size(35, 20)
        };
        dtpTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(52, 18),
            Size = new Size(90, 23)
        };
        gbTime.Controls.Add(lblTime);
        gbTime.Controls.Add(dtpTime);

        // ── 音量 ──
        var gbVolume = new GroupBox
        {
            Text = "🔊 音量",
            Location = new Point(12, 70),
            Size = new Size(380, 65)
        };
        lblVolume = new Label
        {
            Text = "🔈",
            Location = new Point(14, 24),
            Size = new Size(25, 20)
        };
        tbVolume = new TrackBar
        {
            Location = new Point(42, 20),
            Size = new Size(260, 45),
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            TickStyle = TickStyle.None
        };
        lblVolumePercent = new Label
        {
            Text = "80%",
            Location = new Point(308, 24),
            Size = new Size(55, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        tbVolume.ValueChanged += (s, e) =>
        {
            lblVolumePercent.Text = $"{tbVolume.Value}%";
        };
        gbVolume.Controls.Add(lblVolume);
        gbVolume.Controls.Add(tbVolume);
        gbVolume.Controls.Add(lblVolumePercent);

        // ── 语音文件列表 ──
        var gbFiles = new GroupBox
        {
            Text = "🎵 语音文件",
            Location = new Point(12, 143),
            Size = new Size(380, 165)
        };
        lbFiles = new ListBox
        {
            Location = new Point(14, 22),
            Size = new Size(230, 102),
            IntegralHeight = false
        };
        lblFileCount = new Label
        {
            Text = "0 个文件",
            Location = new Point(14, 130),
            Size = new Size(100, 20),
            ForeColor = Color.Gray
        };
        btnPreviewFile = new Button
        {
            Text = "▶ 试听",
            Location = new Point(255, 22),
            Size = new Size(110, 30)
        };
        btnPreviewFile.Click += OnPreviewFile;
        btnRefreshFiles = new Button
        {
            Text = "🔄 刷新列表",
            Location = new Point(255, 58),
            Size = new Size(110, 30)
        };
        btnRefreshFiles.Click += (s, e) => RefreshFileList();
        btnOpenFolder = new Button
        {
            Text = "📂 打开文件夹",
            Location = new Point(255, 94),
            Size = new Size(110, 30)
        };
        btnOpenFolder.Click += OnOpenFolder;
        gbFiles.Controls.Add(lbFiles);
        gbFiles.Controls.Add(lblFileCount);
        gbFiles.Controls.Add(btnPreviewFile);
        gbFiles.Controls.Add(btnRefreshFiles);
        gbFiles.Controls.Add(btnOpenFolder);

        // ── 视频播放 ──
        var gbVideo = new GroupBox
        {
            Text = "🎬 视频播放（独立定时）",
            Location = new Point(12, 316),
            Size = new Size(380, 80)
        };
        var lblVideoTime = new Label
        {
            Text = "每天",
            Location = new Point(14, 24),
            Size = new Size(35, 20)
        };
        dtpVideoTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = new Point(52, 21),
            Size = new Size(90, 23),
            Value = DateTime.Today.AddHours(14) // 默认 14:00
        };
        lblVideoFile = new Label
        {
            Text = "未选择视频",
            Location = new Point(14, 48),
            Size = new Size(160, 20),
            AutoEllipsis = true,
            ForeColor = Color.Gray
        };
        btnBrowseVideo = new Button
        {
            Text = "📁 选择",
            Location = new Point(185, 45),
            Size = new Size(75, 25)
        };
        btnBrowseVideo.Click += OnBrowseVideo;
        btnClearVideo = new Button
        {
            Text = "✕ 清除",
            Location = new Point(266, 45),
            Size = new Size(55, 25)
        };
        btnClearVideo.Click += OnClearVideo;
        gbVideo.Controls.Add(lblVideoTime);
        gbVideo.Controls.Add(dtpVideoTime);
        gbVideo.Controls.Add(lblVideoFile);
        gbVideo.Controls.Add(btnBrowseVideo);
        gbVideo.Controls.Add(btnClearVideo);

        // ── 控制 ──
        var gbControl = new GroupBox
        {
            Text = "🎮 控制",
            Location = new Point(12, 404),
            Size = new Size(380, 52)
        };
        btnPlayNow = new Button
        {
            Text = "▶ 立即播放",
            Location = new Point(14, 19),
            Size = new Size(110, 24)
        };
        btnPlayNow.Click += (s, e) => _scheduler?.PlayNow();
        btnStop = new Button
        {
            Text = "⏹ 停止",
            Location = new Point(132, 19),
            Size = new Size(80, 24)
        };
        btnStop.Click += (s, e) => _scheduler?.StopPlayback();
        gbControl.Controls.Add(btnPlayNow);
        gbControl.Controls.Add(btnStop);

        // ── 状态 ──
        lblStatus = new Label
        {
            Text = "⏸ 等待播放时间到达...",
            Location = new Point(12, 466),
            Size = new Size(260, 20),
            ForeColor = Color.DimGray
        };

        // ── 开机自启 ──
        chkAutoStart = new CheckBox
        {
            Text = "开机自启",
            Location = new Point(280, 466),
            Size = new Size(110, 20),
            CheckAlign = ContentAlignment.MiddleRight
        };

        // ── 保存按钮 ──
        btnSave = new Button
        {
            Text = "💾 保存设置",
            Location = new Point(295, 15),
            Size = new Size(100, 45)
        };
        btnSave.Click += OnSave;

        // ── 表单 ──
        this.Controls.Add(gbTime);
        this.Controls.Add(gbVolume);
        this.Controls.Add(gbFiles);
        this.Controls.Add(gbVideo);
        this.Controls.Add(gbControl);
        this.Controls.Add(lblStatus);
        this.Controls.Add(chkAutoStart);
        this.Controls.Add(btnSave);
        this.Resize += OnFormResize;
        this.FormClosing += OnFormClosing;
    }
}
