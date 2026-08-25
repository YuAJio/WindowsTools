namespace MoodyBlues;

partial class PresetEditorForm
{
    private System.ComponentModel.IContainer components = null!;

    private ComboBox cmbPreset;
    private Button btnNew;
    private Button btnSave;
    private Button btnDeletePreset;
    private TextBox txtPresetName;
    private DataGridView dgvSteps;
    private Button btnCapture;
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Button btnDeleteStep;
    private Button btnPlay;
    private Button btnStop;
    private Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "MoodyBlues — ✏ 预设编辑器";
        this.Icon = IconHelper.LoadFromResource("MoodyBlues.app.png");
        this.Size = new Size(680, 480);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // ── 顶行：预设选择 + 操作 ──
        var lblPreset = new Label
        {
            Text = "预设：",
            Location = new Point(12, 16),
            Size = new Size(40, 24),
            TextAlign = ContentAlignment.MiddleRight
        };
        cmbPreset = new ComboBox
        {
            Location = new Point(56, 16),
            Size = new Size(210, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        btnNew = new Button
        {
            Text = "＋ 新建",
            Location = new Point(272, 14),
            Size = new Size(75, 28)
        };
        btnSave = new Button
        {
            Text = "💾 保存",
            Location = new Point(352, 14),
            Size = new Size(75, 28),
            Enabled = false
        };
        btnDeletePreset = new Button
        {
            Text = "🗑 删除",
            Location = new Point(432, 14),
            Size = new Size(75, 28),
            Enabled = false
        };

        // ── 名称行 ──
        var lblName = new Label
        {
            Text = "名称：",
            Location = new Point(12, 50),
            Size = new Size(40, 24),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtPresetName = new TextBox
        {
            Location = new Point(56, 50),
            Size = new Size(220, 24)
        };

        // ── 步骤表格 ──
        var lblSteps = new Label
        {
            Text = "🎮 步骤序列：",
            Location = new Point(12, 82),
            Size = new Size(200, 22),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        dgvSteps = new DataGridView
        {
            Location = new Point(12, 106),
            Size = new Size(640, 280),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        dgvSteps.Columns.Add("colNum", "#");
        dgvSteps.Columns["colNum"].FillWeight = 30;
        dgvSteps.Columns["colNum"].ReadOnly = true;
        dgvSteps.Columns.Add("colKey", "按键");
        dgvSteps.Columns["colKey"].FillWeight = 60;
        dgvSteps.Columns["colKey"].ReadOnly = true;
        dgvSteps.Columns.Add("colType", "类型");
        dgvSteps.Columns["colType"].FillWeight = 40;
        dgvSteps.Columns["colType"].ReadOnly = true;
        dgvSteps.Columns.Add("colHold", "按下(ms)");
        dgvSteps.Columns["colHold"].FillWeight = 50;
        dgvSteps.Columns.Add("colGap", "间隔(ms)");
        dgvSteps.Columns["colGap"].FillWeight = 50;
        dgvSteps.Columns.Add("colCoord", "坐标");
        dgvSteps.Columns["colCoord"].FillWeight = 50;
        dgvSteps.Columns["colCoord"].ReadOnly = true;
        dgvSteps.CellValueChanged += OnDirtyCheck;

        // ── 步骤操作 ──
        btnCapture = new Button
        {
            Text = "🎯 捕获输入",
            Location = new Point(12, 394),
            Size = new Size(100, 30),
            Enabled = false
        };
        btnMoveUp = new Button
        {
            Text = "▲ 上移",
            Location = new Point(118, 394),
            Size = new Size(70, 30),
            Enabled = false
        };
        btnMoveDown = new Button
        {
            Text = "▼ 下移",
            Location = new Point(192, 394),
            Size = new Size(70, 30),
            Enabled = false
        };
        btnDeleteStep = new Button
        {
            Text = "✕ 删除",
            Location = new Point(266, 394),
            Size = new Size(70, 30),
            Enabled = false
        };
        btnPlay = new Button
        {
            Text = "▶ 播放",
            Location = new Point(500, 394),
            Size = new Size(72, 30),
            BackColor = Color.FromArgb(70, 130, 70),
            ForeColor = Color.White,
            Enabled = false
        };
        btnStop = new Button
        {
            Text = "⏹ 停止",
            Location = new Point(578, 394),
            Size = new Size(72, 30),
            Enabled = false
        };

        lblStatus = new Label
        {
            Text = "⏸ 选择一个预设或新建",
            Location = new Point(12, 426),
            Size = new Size(640, 22),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // ── 事件 ──
        cmbPreset.SelectedIndexChanged += OnPresetSelected;
        btnNew.Click += OnNewPreset;
        btnSave.Click += OnSavePreset;
        btnDeletePreset.Click += OnDeletePreset;
        btnCapture.Click += OnCaptureInput;
        btnMoveUp.Click += OnMoveUp;
        btnMoveDown.Click += OnMoveDown;
        btnDeleteStep.Click += OnDeleteStep;
        btnPlay.Click += OnPlayPreset;
        btnStop.Click += OnStopPlayback;

        // ── 组装 ──
        this.Controls.Add(lblPreset);
        this.Controls.Add(cmbPreset);
        this.Controls.Add(btnNew);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnDeletePreset);
        this.Controls.Add(lblName);
        this.Controls.Add(txtPresetName);
        this.Controls.Add(lblSteps);
        this.Controls.Add(dgvSteps);
        this.Controls.Add(btnCapture);
        this.Controls.Add(btnMoveUp);
        this.Controls.Add(btnMoveDown);
        this.Controls.Add(btnDeleteStep);
        this.Controls.Add(btnPlay);
        this.Controls.Add(btnStop);
        this.Controls.Add(lblStatus);
    }
}
