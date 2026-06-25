namespace Yoink;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // URL
    private TextBox txtUrl;
    private Button btnPaste;

    // 模式
    private RadioButton rbVideo;
    private RadioButton rbAudio;

    // 质量 + 输出 + Cookie
    private ComboBox cbQuality;
    private TextBox txtOutputDir;
    private Button btnBrowseDir;
    private ComboBox cbCookie;

    // 操作
    private Button btnDownload;
    private Button btnCancel;

    // 进度
    private ProgressBar pbDownload;
    private Label lblProgress;
    private Label lblSpeed;

    // 日志
    private TextBox txtLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "Yoink — 媒体下载器";
        this.Size = new Size(500, 545);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── URL 输入 ──
        var gbUrl = new GroupBox
        {
            Text = "🔗 视频 / 音频链接",
            Location = new Point(12, 12),
            Size = new Size(460, 58)
        };
        txtUrl = new TextBox
        {
            Location = new Point(14, 22),
            Size = new Size(340, 23)
        };
        btnPaste = new Button
        {
            Text = "📋 粘贴",
            Location = new Point(360, 21),
            Size = new Size(85, 25)
        };
        btnPaste.Click += OnPaste;
        gbUrl.Controls.Add(txtUrl);
        gbUrl.Controls.Add(btnPaste);

        // ── 下载选项 ──
        var gbOptions = new GroupBox
        {
            Text = "📥 下载选项",
            Location = new Point(12, 78),
            Size = new Size(460, 105)
        };
        rbVideo = new RadioButton
        {
            Text = "🎬 视频+音频",
            Location = new Point(14, 22),
            Size = new Size(110, 20),
            Checked = true
        };
        rbAudio = new RadioButton
        {
            Text = "🎵 仅音频",
            Location = new Point(130, 22),
            Size = new Size(90, 20)
        };
        var lblQuality = new Label
        {
            Text = "质量:",
            Location = new Point(14, 50),
            Size = new Size(42, 20)
        };
        cbQuality = new ComboBox
        {
            Location = new Point(56, 47),
            Size = new Size(90, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbQuality.Items.AddRange(["最佳", "1080p", "720p", "480p", "360p"]);
        cbQuality.SelectedIndex = 0;
        var lblOutDir = new Label
        {
            Text = "输出:",
            Location = new Point(158, 50),
            Size = new Size(42, 20)
        };
        txtOutputDir = new TextBox
        {
            Location = new Point(200, 47),
            Size = new Size(160, 23),
            Text = "downloads"
        };
        btnBrowseDir = new Button
        {
            Text = "📂",
            Location = new Point(366, 46),
            Size = new Size(28, 25)
        };
        btnBrowseDir.Click += OnBrowseOutput;
        var lblCookie = new Label
        {
            Text = "🍪",
            Location = new Point(14, 74),
            Size = new Size(28, 20)
        };
        cbCookie = new ComboBox
        {
            Location = new Point(42, 72),
            Size = new Size(80, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cbCookie.Items.AddRange(["cookies.txt", "无", "Edge", "Chrome", "Firefox"]);
        cbCookie.SelectedIndex = 0;
        gbOptions.Controls.Add(rbVideo);
        gbOptions.Controls.Add(rbAudio);
        gbOptions.Controls.Add(lblQuality);
        gbOptions.Controls.Add(cbQuality);
        gbOptions.Controls.Add(lblOutDir);
        gbOptions.Controls.Add(txtOutputDir);
        gbOptions.Controls.Add(btnBrowseDir);
        gbOptions.Controls.Add(lblCookie);
        gbOptions.Controls.Add(cbCookie);

        // ── 操作按钮 ──
        var gbAction = new GroupBox
        {
            Text = "🎮 操作",
            Location = new Point(12, 191),
            Size = new Size(460, 52)
        };
        btnDownload = new Button
        {
            Text = "⬇ Yoink!",
            Location = new Point(14, 19),
            Size = new Size(130, 26),
            BackColor = Color.DarkSlateBlue,
            ForeColor = Color.White
        };
        btnDownload.Click += OnDownload;
        btnCancel = new Button
        {
            Text = "⏹ 取消",
            Location = new Point(156, 19),
            Size = new Size(80, 26),
            Enabled = false
        };
        btnCancel.Click += OnCancel;
        gbAction.Controls.Add(btnDownload);
        gbAction.Controls.Add(btnCancel);

        // ── 进度 ──
        var gbProgress = new GroupBox
        {
            Text = "📊 进度",
            Location = new Point(12, 251),
            Size = new Size(460, 70)
        };
        pbDownload = new ProgressBar
        {
            Location = new Point(14, 22),
            Size = new Size(430, 14),
            Style = ProgressBarStyle.Continuous
        };
        lblProgress = new Label
        {
            Text = "等待下载...",
            Location = new Point(14, 42),
            Size = new Size(280, 20),
            ForeColor = Color.Gray
        };
        lblSpeed = new Label
        {
            Text = "",
            Location = new Point(300, 42),
            Size = new Size(144, 20),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.Gray
        };
        gbProgress.Controls.Add(pbDownload);
        gbProgress.Controls.Add(lblProgress);
        gbProgress.Controls.Add(lblSpeed);

        // ── 日志 ──
        var gbLog = new GroupBox
        {
            Text = "📋 日志",
            Location = new Point(12, 329),
            Size = new Size(460, 170)
        };
        txtLog = new TextBox
        {
            Location = new Point(14, 22),
            Size = new Size(430, 138),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 8.25f)
        };
        gbLog.Controls.Add(txtLog);

        // ── 表单 ──
        this.Controls.Add(gbUrl);
        this.Controls.Add(gbOptions);
        this.Controls.Add(gbAction);
        this.Controls.Add(gbProgress);
        this.Controls.Add(gbLog);
        this.FormClosing += OnFormClosing;
    }
}
