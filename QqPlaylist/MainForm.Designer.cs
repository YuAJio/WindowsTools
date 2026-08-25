namespace QqPlaylist;

#nullable enable

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // ─── Theme ───
    private static readonly Color C_BG       = Color.FromArgb(27, 29, 34);    // #1b1d22
    private static readonly Color C_BG2      = Color.FromArgb(34, 37, 43);    // #22252b
    private static readonly Color C_PANEL    = Color.FromArgb(39, 42, 49);    // #272a31
    private static readonly Color C_PANEL2   = Color.FromArgb(46, 50, 58);    // #2e323a
    private static readonly Color C_LINE     = Color.FromArgb(58, 63, 72);    // #3a3f48
    private static readonly Color C_LINE2    = Color.FromArgb(68, 74, 85);    // #444a55
    private static readonly Color C_TEXT     = Color.FromArgb(230, 233, 239);// #e6e9ef
    private static readonly Color C_TEXT2    = Color.FromArgb(155, 163, 180);// #9ba3b4
    private static readonly Color C_DIM      = Color.FromArgb(110, 117, 133);// #6e7585
    private static readonly Color C_ACCENT   = Color.FromArgb(92, 200, 255);  // #5cc8ff
    private static readonly Color C_ACCENT2  = Color.FromArgb(124, 158, 255); // #7c9eff
    private static readonly Color C_GOOD     = Color.FromArgb(93, 211, 158);  // #5dd39e
    private static readonly Color C_BAD      = Color.FromArgb(255, 118, 118);// #ff7676
    private static readonly Color C_WARN     = Color.FromArgb(255, 180, 84);  // #ffb454
    private static readonly Color C_PINK     = Color.FromArgb(255, 141, 180);// #ff8db4

    // ─── Fonts ───
    private static readonly Font F_TITLE = new("Segoe UI", 9.5f, FontStyle.Bold);
    private static readonly Font F_TITLE_VER = new("Cascadia Code", 9f, FontStyle.Regular);
    private static readonly Font F_HEAD = new("Segoe UI", 9.5f, FontStyle.Bold);
    private static readonly Font F_NORMAL = new("Segoe UI", 9f);
    private static readonly Font F_MONO = new("Cascadia Code", 10f);
    private static readonly Font F_MONO_SM = new("Cascadia Code", 9f);
    private static readonly Font F_MONO_XS = new("Cascadia Code", 8.5f);
    private static readonly Font F_LABEL = new("Segoe UI", 8.5f, FontStyle.Bold);
    private static readonly Font F_PILL = new("Cascadia Code", 9f);
    private static readonly Font F_DIM = new("Segoe UI", 8.5f);

    // ════════ Title bar ════════
    private Panel pnlTitleBar = null!;
    private Label lblTitleIcon = null!;
    private Label lblTitleText = null!;
    private Label lblVersion = null!;
    private Panel pnlWinBtns = null!;
    private Label btnWinMin = null!, btnWinMax = null!, btnWinClose = null!;

    // ════════ Top status bar ════════
    private Panel pnlTopBar = null!;
    private Panel pnlLed = null!;
    private Label lblTopText = null!;
    private Panel pnlPills = null!;
    private Label pillStatus = null!, pillUin = null!, pillJson = null!;

    // ════════ Status bar (bottom) ════════
    private Panel pnlStatus = null!;
    private Label lblStatus = null!;

    // ════════ Main 3-pane container ════════
    private TableLayoutPanel tlpMain = null!;

    // ─── LEFT pane ───
    private Panel pnlLeft = null!;
    private Panel pnlLeftHead = null!;
    private Label lblLeftIcon = null!, lblLeftTitle = null!, lblLeftCount = null!;
    private Panel pnlSearch = null!;
    private Label lblSearchIcon = null!;
    private TextBox txtSearch = null!;
    private ListBox lstMyPlaylists = null!;
    private Panel pnlLoadMy = null!;
    private Panel pnlLoadMyHead = null!;
    private Label lblLoadMyArrow = null!, lblLoadMyText = null!;
    private Panel pnlLoadMyBody = null!;
    private Label lblCookieHint = null!;
    private TextBox txtUin = null!;
    private TextBox txtCookie = null!;
    private Panel pnlLoadMyBtns = null!;
    private Button btnLoadMyPlaylists = null!;
    private Button btnClearStoredCookie = null!;

    // ─── CENTER pane ───
    private Panel pnlCenter = null!;
    private Panel pnlInputArea = null!;
    private TableLayoutPanel tlpInputRow = null!;
    private Label lblIdLabel = null!;
    private TextBox txtPlaylistId = null!;
    private Button btnFetch = null!;
    private Panel pnlSaveBar = null!;
    private CheckBox chkAutoSave = null!;
    private TextBox txtSavePath = null!;
    private Button btnBrowse = null!;
    private Button btnSave = null!;
    private Button btnCopy = null!;
    private Panel pnlOutput = null!;
    private Panel pnlOutputTabs = null!;
    private Panel tabFetchResult = null!;
    private Label lblTabFetchResult = null!;
    private Label lblTabFetchClose = null!;
    private Panel tabJsonPreview = null!;
    private Label lblTabJsonPreview = null!;
    private Label lblTabJsonClose = null!;
    private RichTextBox rtbOutput = null!;

    // ─── RIGHT pane (preview) ───
    private Panel pnlRight = null!;
    private Panel pnlRightHead = null!;
    private Label lblRightIcon = null!, lblRightTitle = null!, lblRightCount = null!;
    private Panel pnlPreviewCover = null!;
    private PictureBox picCover = null!;
    private Label lblCoverCountTag = null!;
    private Label lblCoverEmpty = null!;
    private Label lblPreviewName = null!;
    private Label lblPreviewCreator = null!;
    private TableLayoutPanel tlpStats = null!;
    private (Panel panel, Label lbl, Label val)[] stats = null!;
    private FlowLayoutPanel flpTags = null!;
    private Panel pnlPreviewSongs = null!;
    private Label lblPreviewSongsTitle = null!;
    private ListBox lstPreviewSongs = null!;
    private Panel pnlPreviewFoot = null!;
    private Button btnPreviewFetch = null!;
    private Button btnPreviewOpenJson = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            F_TITLE.Dispose(); F_TITLE_VER.Dispose(); F_HEAD.Dispose(); F_NORMAL.Dispose();
            F_MONO.Dispose(); F_MONO_SM.Dispose(); F_MONO_XS.Dispose();
            F_LABEL.Dispose(); F_PILL.Dispose(); F_DIM.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        // ════════ Form ════════
        Text = "QqPlaylist";
        Icon = IconHelper.LoadFromResource("QqPlaylist.app.png");
        BackColor = C_BG;
        ForeColor = C_TEXT;
        Size = new Size(1340, 840);
        MinimumSize = new Size(1140, 700);
        FormBorderStyle = FormBorderStyle.None;  // 去掉 Windows 标题栏，只留我们自定义的
        StartPosition = FormStartPosition.CenterScreen;
        Font = F_NORMAL;

        // ════════ Title bar ════════
        pnlTitleBar = MakePanel(C_BG2, new Size(0, 36), Dock: DockStyle.Top);
        pnlTitleBar.Paint += (s, e) =>
        {
            using var lg = new System.Drawing.Drawing2D.LinearGradientBrush(
                pnlTitleBar.ClientRectangle, Color.FromArgb(43, 46, 53), Color.FromArgb(31, 34, 40), 90f);
            e.Graphics.FillRectangle(lg, pnlTitleBar.ClientRectangle);
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
        };

        lblTitleIcon = new Label
        {
            Text = "🎵", Font = new Font("Segoe UI Emoji", 12f),
            ForeColor = C_TEXT, AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(14, 9)
        };

        lblTitleText = new Label
        {
            Text = "QqPlaylist", Font = F_TITLE,
            ForeColor = C_TEXT, AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(38, 10)
        };

        lblVersion = new Label
        {
            Text = "v2.0", Font = F_TITLE_VER,
            ForeColor = C_DIM, AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(122, 11)
        };

        // Move version to after title text dynamically (it's just visual, leave at fixed offset)

        pnlWinBtns = MakePanel(Color.Transparent, new Size(96, 36), Dock: DockStyle.Right);
        btnWinMin = MakeWinBtn("─", C_DIM, new Point(8, 6));
        btnWinMax = MakeWinBtn("□", C_DIM, new Point(40, 6));
        btnWinClose = MakeWinBtn("×", C_BAD, new Point(72, 6));
        btnWinMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
        btnWinMax.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        btnWinClose.Click += (s, e) => Close();
        pnlWinBtns.Controls.AddRange(new Control[] { btnWinMin, btnWinMax, btnWinClose });

        pnlTitleBar.Controls.AddRange(new Control[] {
            lblTitleIcon, lblTitleText, lblVersion, pnlWinBtns
        });

        // 标题栏拖动支持（因为 FormBorderStyle.None 没法拖）
        pnlTitleBar.MouseDown += OnTitleBarMouseDown;
        pnlTitleBar.MouseMove += OnTitleBarMouseMove;
        pnlTitleBar.MouseUp += OnTitleBarMouseUp;
        pnlTitleBar.MouseDoubleClick += OnTitleBarMouseDoubleClick;
        // 让标题栏子控件（图标/文字/版本）也能拖动
        foreach (var c in new Control[] { lblTitleIcon, lblTitleText, lblVersion })
        {
            c.MouseDown += OnTitleBarMouseDown;
            c.MouseMove += OnTitleBarMouseMove;
            c.MouseUp += OnTitleBarMouseUp;
            c.MouseDoubleClick += OnTitleBarMouseDoubleClick;
        }

        // ════════ Bottom status bar ════════
        pnlStatus = MakePanel(C_BG2, new Size(0, 30), Dock: DockStyle.Bottom);
        pnlStatus.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, 0, pnlStatus.Width, 0);
        };
        lblStatus = new Label
        {
            Text = "⏸ 待命", Font = F_MONO_SM, ForeColor = C_TEXT2,
            BackColor = Color.Transparent, AutoSize = false, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0)
        };
        pnlStatus.Controls.Add(lblStatus);

        // ════════ Top status bar ════════
        pnlTopBar = MakePanel(C_BG2, new Size(0, 50), Dock: DockStyle.Top);
        pnlTopBar.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
        };

        pnlLed = new Panel
        {
            Size = new Size(10, 10), BackColor = C_GOOD,
            Location = new Point(16, 20)
        };
        pnlLed.Paint += (s, e) =>
        {
            using var br = new SolidBrush(pnlLed.BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(br, 0, 0, pnlLed.Width - 1, pnlLed.Height - 1);
            // glow
            using var glow = new SolidBrush(Color.FromArgb(80, pnlLed.BackColor));
            e.Graphics.FillEllipse(glow, -4, -4, pnlLed.Width + 7, pnlLed.Height + 7);
        };

        lblTopText = new Label
        {
            Text = "The Greatest Hits · 166 首 · ⏱ 9小时23分 · 已选中",
            Font = F_NORMAL, ForeColor = C_TEXT2,
            BackColor = Color.Transparent, AutoSize = true, Location = new Point(38, 17)
        };

        pnlPills = MakePanel(Color.Transparent, new Size(0, 50), Dock: DockStyle.Right);
        pillStatus = MakePill("● 抓取成功", C_GOOD, Color.FromArgb(26, 42, 35), Color.FromArgb(44, 74, 60));
        pillUin = MakePill("QQ 1145661286", C_TEXT2, C_PANEL, C_LINE);
        pillJson = MakePill("📂 JSON 已保存", C_TEXT2, C_PANEL, C_LINE);
        pnlPills.Controls.AddRange(new Control[] { pillJson, pillUin, pillStatus });
        // Right-aligned
        pnlPills.Resize += (s, e) =>
        {
            int x = pnlPills.Width - 14;
            foreach (var p in new[] { pillJson, pillUin, pillStatus })
            {
                p.Location = new Point(x - p.Width, 15);
                x -= p.Width + 8;
            }
        };

        pnlTopBar.Controls.AddRange(new Control[] { pnlLed, lblTopText, pnlPills });

        // ════════ Main 3-pane container ════════
        tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = C_BG,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tlpMain.Controls.Add(BuildLeftPane(), 0, 0);
        tlpMain.Controls.Add(BuildCenterPane(), 1, 0);
        tlpMain.Controls.Add(BuildRightPane(), 2, 0);

        // ════════ Form composition ════════
        Controls.Add(tlpMain);
        Controls.Add(pnlTopBar);
        Controls.Add(pnlTitleBar);
        Controls.Add(pnlStatus);

        FormClosing += OnFormClosing;
        ResumeLayout(false);
    }

    // ════════ 无边框窗口的边缘拖拽缩放 ════════
    private const int WM_NCHITTEST = 0x0084;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
        {
            var pos = PointToClient(Cursor.Position);
            int grip = 6; // 边缘拖拽宽度
            if (pos.X <= grip && pos.Y <= grip) { m.Result = (IntPtr)HTTOPLEFT; return; }
            if (pos.X >= Width - grip && pos.Y <= grip) { m.Result = (IntPtr)HTTOPRIGHT; return; }
            if (pos.X <= grip && pos.Y >= Height - grip) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
            if (pos.X >= Width - grip && pos.Y >= Height - grip) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
            if (pos.X <= grip) { m.Result = (IntPtr)HTLEFT; return; }
            if (pos.X >= Width - grip) { m.Result = (IntPtr)HTRIGHT; return; }
            if (pos.Y <= grip) { m.Result = (IntPtr)HTTOP; return; }
            if (pos.Y >= Height - grip) { m.Result = (IntPtr)HTBOTTOM; return; }
        }
        base.WndProc(ref m);
    }

    // ════════════════ LEFT PANE ════════════════
    private Panel BuildLeftPane()
    {
        pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(0) };
        pnlLeft.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, pnlLeft.Width - 1, 0, pnlLeft.Width - 1, pnlLeft.Height);
        };

        // Header
        pnlLeftHead = MakePanel(C_PANEL, new Size(0, 44), Dock: DockStyle.Top);
        pnlLeftHead.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlLeftHead.Height - 1, pnlLeftHead.Width, pnlLeftHead.Height - 1);
        };
        lblLeftIcon = new Label
        {
            Text = "📋", Font = new Font("Segoe UI Emoji", 11f), ForeColor = C_TEXT,
            AutoSize = true, BackColor = Color.Transparent, Location = new Point(14, 13)
        };
        lblLeftTitle = new Label
        {
            Text = "我的歌单", Font = F_HEAD, ForeColor = C_TEXT,
            AutoSize = true, BackColor = Color.Transparent, Location = new Point(38, 13)
        };
        lblLeftCount = MakePill("72", C_TEXT2, C_PANEL2, C_LINE);
        lblLeftCount.Padding = new Padding(10, 3, 10, 3);
        lblLeftCount.AutoSize = true;
        pnlLeftHead.Controls.AddRange(new Control[] { lblLeftIcon, lblLeftTitle, lblLeftCount });
        pnlLeftHead.Resize += (s, e) => { lblLeftCount.Location = new Point(pnlLeftHead.Width - lblLeftCount.Width - 12, 12); };

        // Search
        pnlSearch = new Panel
        {
            BackColor = C_PANEL2, Height = 36,
            Location = new Point(10, 52), Padding = new Padding(12, 8, 12, 8)
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var br = new SolidBrush(C_PANEL2);
            using var p = new Pen(C_LINE);
            e.Graphics.FillRectangle(br, pnlSearch.ClientRectangle);
            e.Graphics.DrawRectangle(p, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };
        pnlSearch.Width = 270 - 20;
        pnlSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        lblSearchIcon = new Label
        {
            Text = "🔍", Font = new Font("Segoe UI Emoji", 11f),
            ForeColor = C_DIM, AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(12, 9)
        };
        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None, BackColor = C_PANEL2,
            ForeColor = C_TEXT, Font = F_NORMAL,
            Location = new Point(34, 10), Width = pnlSearch.Width - 44,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        txtSearch.PlaceholderText = "搜索歌单名…";
        pnlSearch.Controls.AddRange(new Control[] { lblSearchIcon, txtSearch });

        // ListBox (owner-drawn)
        lstMyPlaylists = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = C_BG,
            ForeColor = C_TEXT,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawVariable,
            Font = F_NORMAL,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        lstMyPlaylists.MeasureItem += OnLstMyPlaylistsMeasureItem;
        lstMyPlaylists.DrawItem += OnLstMyPlaylistsDrawItem;
        lstMyPlaylists.SelectedIndexChanged += OnLstMyPlaylistsSelectionChanged;
        lstMyPlaylists.DoubleClick += OnLstMyPlaylistsDoubleClick;
        lstMyPlaylists.ItemHeight = 58;

        // LoadMy (collapsible)
        pnlLoadMy = MakePanel(C_PANEL, new Size(0, 36), Dock: DockStyle.Bottom);
        pnlLoadMy.Padding = new Padding(10, 0, 10, 8);
        pnlLoadMy.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE2);
            e.Graphics.DrawLine(p, 8, 0, pnlLoadMy.Width - 8, 0);
        };

        pnlLoadMyHead = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent, Cursor = Cursors.Hand };
        lblLoadMyArrow = new Label
        {
            Text = "▶", Font = new Font("Segoe UI Symbol", 9f), ForeColor = C_DIM,
            AutoSize = true, BackColor = Color.Transparent, Location = new Point(2, 11)
        };
        lblLoadMyText = new Label
        {
            Text = "⚙ 加载 / 刷新我的歌单", Font = F_NORMAL,
            ForeColor = C_TEXT2, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 11)
        };
        pnlLoadMyHead.Controls.AddRange(new Control[] { lblLoadMyArrow, lblLoadMyText });
        pnlLoadMyHead.Click += ToggleLoadMy;
        lblLoadMyArrow.Click += ToggleLoadMy;
        lblLoadMyText.Click += ToggleLoadMy;

        pnlLoadMyBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false, Padding = new Padding(0, 10, 0, 0) };
        lblCookieHint = new Label
        {
            Text = "✅ Cookie 已从本地读取", Font = F_MONO_XS,
            ForeColor = C_GOOD, AutoSize = true, BackColor = Color.Transparent,
            Location = new Point(0, 0)
        };
        txtUin = MakeTextInput("QQ 号", "1145661286", new Point(0, 26));
        txtUin.Height = 24;
        txtCookie = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle, BackColor = C_BG, ForeColor = C_TEXT,
            Font = F_MONO_XS, Multiline = true, ScrollBars = ScrollBars.Vertical,
            Location = new Point(0, 60), Height = 120, Width = pnlLoadMy.Width - 20,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            PlaceholderText = "uin=...; skey=...; ..."
        };
        pnlLoadMyBtns = new Panel
        {
                Height = 34, BackColor = Color.Transparent,
                Location = new Point(0, 188), Width = pnlLoadMy.Width - 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        btnLoadMyPlaylists = MakeBtn("💾 加载", primary: true, new Point(0, 2));
        btnLoadMyPlaylists.Width = pnlLoadMyBtns.Width - 56;
        btnLoadMyPlaylists.Height = 30;
        btnLoadMyPlaylists.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnClearStoredCookie = MakeBtn("🗑", primary: false, new Point(pnlLoadMyBtns.Width - 50, 2));
        btnClearStoredCookie.Width = 50;
        btnClearStoredCookie.Height = 30;
        btnClearStoredCookie.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnLoadMyPlaylists.Click += OnLoadMyPlaylistsClick;
        btnClearStoredCookie.Click += OnClearStoredCookieClick;

        pnlLoadMyBtns.Controls.AddRange(new Control[] { btnLoadMyPlaylists, btnClearStoredCookie });
        pnlLoadMyBody.Controls.AddRange(new Control[] { lblCookieHint, txtUin, txtCookie, pnlLoadMyBtns });
        pnlLoadMyBody.Resize += (s, e) =>
        {
            txtCookie.Width = pnlLoadMyBody.Width;
            pnlLoadMyBtns.Width = pnlLoadMyBody.Width;
            btnLoadMyPlaylists.Width = pnlLoadMyBtns.Width - 56;
            btnClearStoredCookie.Location = new Point(pnlLoadMyBtns.Width - 50, 2);
        };

        pnlLoadMy.Controls.Add(pnlLoadMyBody);
        pnlLoadMy.Controls.Add(pnlLoadMyHead);

        pnlLeft.Controls.Add(lstMyPlaylists);
        pnlLeft.Controls.Add(pnlSearch);
        pnlLeft.Controls.Add(pnlLeftHead);
        pnlLeft.Controls.Add(pnlLoadMy);

        return pnlLeft;
    }

    private bool _loadMyExpanded;
    private void ToggleLoadMy(object? s, EventArgs e)
    {
        _loadMyExpanded = !_loadMyExpanded;
        pnlLoadMyBody.Visible = _loadMyExpanded;
        pnlLoadMy.Height = _loadMyExpanded ? 260 : 36;
        lblLoadMyArrow.Text = _loadMyExpanded ? "▼" : "▶";
    }

    // ════════════════ CENTER PANE ════════════════
    private Panel BuildCenterPane()
    {
        pnlCenter = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };

        // Input area
        pnlInputArea = new Panel
        {
            Dock = DockStyle.Top, BackColor = C_BG2, Padding = new Padding(16, 14, 16, 12),
            Height = 100
        };
        pnlInputArea.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlInputArea.Height - 1, pnlInputArea.Width, pnlInputArea.Height - 1);
        };

        tlpInputRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 42, ColumnCount = 3, RowCount = 1,
            BackColor = C_BG2
        };
        tlpInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tlpInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tlpInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        tlpInputRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        lblIdLabel = new Label
        {
            Text = "🎵 歌单 ID 或 URL", Font = F_LABEL, ForeColor = C_TEXT2,
            AutoSize = false, Dock = DockStyle.Fill, BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft, Margin = new Padding(0, 0, 8, 4)
        };
        txtPlaylistId = MakeTextInput("https://y.qq.com/n/ryqq_v2/playlist/9768457679 或 9768457679", "9768457679", Point.Empty);
        txtPlaylistId.Dock = DockStyle.Fill;
        txtPlaylistId.Margin = new Padding(0, 18, 10, 0);
        txtPlaylistId.Font = F_MONO;
        txtPlaylistId.KeyDown += OnTxtPlaylistIdKeyDown;

        btnFetch = MakeBtn("▶ 抓取歌单", primary: true, Point.Empty);
        btnFetch.Dock = DockStyle.Fill;
        btnFetch.Margin = new Padding(0, 14, 0, 0);
        btnFetch.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        btnFetch.Height = 36;
        btnFetch.Click += OnFetchClick;

        tlpInputRow.Controls.Add(lblIdLabel, 0, 0);
        tlpInputRow.Controls.Add(txtPlaylistId, 1, 0);
        tlpInputRow.Controls.Add(btnFetch, 2, 0);

        // Save bar
        pnlSaveBar = new Panel
        {
            Dock = DockStyle.Top, Height = 38, BackColor = C_BG2,
            Padding = new Padding(0, 8, 0, 0)
        };
        pnlSaveBar.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 16, 14, pnlSaveBar.Width - 16, 14);
        };

        chkAutoSave = new CheckBox
        {
            Text = "解析后自动保存", Font = F_NORMAL, ForeColor = C_TEXT2,
            AutoSize = true, BackColor = Color.Transparent, Checked = true,
            Location = new Point(0, 18)
        };
        chkAutoSave.CheckedChanged += OnAutoSaveChanged;

        txtSavePath = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle, BackColor = C_BG, ForeColor = C_TEXT2,
            Font = F_MONO_XS, Location = new Point(150, 16), Width = 400, Height = 26,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = @"C:\Users\Smile4KKR\Documents\playlist_The Greatest Hits.md"
        };
        txtSavePath.Enabled = false;

        btnBrowse = MakeBtn("📂 浏览…", primary: false, new Point(560, 14));
        btnBrowse.Size = new Size(80, 28);
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.Enabled = false;
        btnBrowse.Click += OnBrowseClick;

        btnSave = MakeBtn("💾 保存", primary: false, new Point(646, 14));
        btnSave.Size = new Size(70, 28);
        btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSave.Enabled = false;
        btnSave.Click += OnSaveClick;

        btnCopy = MakeBtn("📋 复制", primary: false, new Point(722, 14));
        btnCopy.Size = new Size(70, 28);
        btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.Enabled = false;
        btnCopy.Click += OnCopyClick;

        pnlSaveBar.Controls.AddRange(new Control[] { chkAutoSave, txtSavePath, btnBrowse, btnSave, btnCopy });
        pnlSaveBar.Resize += (s, e) =>
        {
            int right = pnlSaveBar.Width - 16;
            btnCopy.Location = new Point(right - btnCopy.Width, 14);
            btnSave.Location = new Point(btnCopy.Location.X - btnSave.Width - 6, 14);
            btnBrowse.Location = new Point(btnSave.Location.X - btnBrowse.Width - 6, 14);
            txtSavePath.Width = btnBrowse.Location.X - txtSavePath.Location.X - 8;
        };

        pnlInputArea.Controls.Add(pnlSaveBar);
        pnlInputArea.Controls.Add(tlpInputRow);

        // Output
        pnlOutput = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 22, 26), Padding = new Padding(0) };

        pnlOutputTabs = new Panel
        {
            Dock = DockStyle.Top, Height = 40, BackColor = C_BG2, Padding = new Padding(8, 0, 8, 0)
        };
        pnlOutputTabs.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlOutputTabs.Height - 1, pnlOutputTabs.Width, pnlOutputTabs.Height - 1);
        };

        tabFetchResult = MakeTab(true, new Point(8, 0), "📄 抓取结果", out lblTabFetchResult, out lblTabFetchClose);
        tabJsonPreview = MakeTab(false, new Point(170, 0), "📋 JSON 预览", out lblTabJsonPreview, out lblTabJsonClose);
        tabFetchResult.Click += (s, e) => SwitchOutputTab(0);
        tabJsonPreview.Click += (s, e) => SwitchOutputTab(1);
        lblTabFetchClose.Click += (s, e) => SwitchOutputTab(0);
        lblTabJsonClose.Click += (s, e) => SwitchOutputTab(1);

        pnlOutputTabs.Controls.AddRange(new Control[] { tabFetchResult, tabJsonPreview });

        rtbOutput = new RichTextBox
        {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 22, 26),
            ForeColor = Color.FromArgb(207, 211, 218), Font = F_MONO,
            BorderStyle = BorderStyle.None, ReadOnly = true,
            WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both
        };

        pnlOutput.Controls.Add(rtbOutput);
        pnlOutput.Controls.Add(pnlOutputTabs);

        pnlCenter.Controls.Add(pnlOutput);
        pnlCenter.Controls.Add(pnlInputArea);

        return pnlCenter;
    }

    // SwitchOutputTab moved to MainForm.cs (avoid duplicate definition)

    // ════════════════ RIGHT PANE ════════════════
    private Panel BuildRightPane()
    {
        pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(0) };
        pnlRight.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, 0, 0, pnlRight.Height);
        };

        // Header
        pnlRightHead = MakePanel(C_PANEL, new Size(0, 44), Dock: DockStyle.Top);
        pnlRightHead.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, pnlRightHead.Height - 1, pnlRightHead.Width, pnlRightHead.Height - 1);
        };
        lblRightIcon = new Label
        {
            Text = "👁", Font = new Font("Segoe UI Emoji", 11f), ForeColor = C_TEXT,
            AutoSize = true, BackColor = Color.Transparent, Location = new Point(14, 13)
        };
        lblRightTitle = new Label
        {
            Text = "实时预览", Font = F_HEAD, ForeColor = C_TEXT,
            AutoSize = true, BackColor = Color.Transparent, Location = new Point(38, 13)
        };
        lblRightCount = MakePill("已选中", C_TEXT2, C_PANEL2, C_LINE);
        lblRightCount.Padding = new Padding(10, 3, 10, 3);
        lblRightCount.AutoSize = true;
        pnlRightHead.Controls.AddRange(new Control[] { lblRightIcon, lblRightTitle, lblRightCount });
        pnlRightHead.Resize += (s, e) => { lblRightCount.Location = new Point(pnlRightHead.Width - lblRightCount.Width - 12, 12); };

        // Body container
        var pnlRightBody = new Panel
        {
            Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(0)
        };

        // Cover
        pnlPreviewCover = new Panel
        {
            BackColor = C_PANEL, Location = new Point(16, 16), Size = new Size(328, 328)
        };
        pnlPreviewCover.Paint += (s, e) =>
        {
            using var br = new SolidBrush(C_PANEL);
            e.Graphics.FillRectangle(br, pnlPreviewCover.ClientRectangle);
            // Shadow effect (simple)
            using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            e.Graphics.FillRectangle(shadow, 4, pnlPreviewCover.Height - 8, pnlPreviewCover.Width, 8);
        };
        picCover = new PictureBox
        {
            Dock = DockStyle.Fill, BackColor = C_PANEL, SizeMode = PictureBoxSizeMode.Zoom
        };
        picCover.LoadCompleted += (s, e) => { lblCoverEmpty.Visible = e.Error is not null || picCover.Image is null; };
        lblCoverCountTag = new Label
        {
            Text = "🎵 166 首", Font = F_MONO_SM, ForeColor = Color.White,
            BackColor = Color.FromArgb(192, 0, 0, 0), AutoSize = true,
            Padding = new Padding(10, 5, 10, 5), Visible = false,
            Location = new Point(248, 292)
        };
        lblCoverEmpty = new Label
        {
            Text = "🔍 选中歌单后显示封面", Font = F_NORMAL, ForeColor = C_DIM,
            AutoSize = false, Dock = DockStyle.Fill, BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter, Visible = true
        };
        pnlPreviewCover.Controls.Add(picCover);
        pnlPreviewCover.Controls.Add(lblCoverEmpty);
        pnlPreviewCover.Controls.Add(lblCoverCountTag);
        lblCoverCountTag.BringToFront();
        lblCoverEmpty.BringToFront();

        // Meta
        var pnlMeta = new Panel
        {
            BackColor = C_BG, Location = new Point(16, 358), Size = new Size(328, 196)
        };
        lblPreviewName = new Label
        {
            Text = "The Greatest Hits", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = C_TEXT, BackColor = Color.Transparent, AutoSize = false, Width = 328, Height = 26,
            Location = new Point(0, 0), TextAlign = ContentAlignment.MiddleLeft
        };
        lblPreviewCreator = new Label
        {
            Text = "by Kosmos,Cosmos · 2020-03-15", Font = F_NORMAL,
            ForeColor = C_TEXT2, BackColor = Color.Transparent, AutoSize = false, Width = 328, Height = 20,
            Location = new Point(0, 30)
        };

        // Stats 2x2 grid
        tlpStats = new TableLayoutPanel
        {
            Location = new Point(0, 60), ColumnCount = 2, RowCount = 2,
            Width = 328, Height = 100, BackColor = C_BG
        };
        tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tlpStats.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tlpStats.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        stats = new (Panel, Label, Label)[4];
        string[] labels = { "总时长", "平均时长", "歌曲数", "总播放" };
        string[] values = { "9小时23分", "3:25/首", "166", "38 次" };
        for (int i = 0; i < 4; i++)
        {
            var panel = new Panel { BackColor = C_PANEL, Margin = new Padding(0, 0, 3, 3), Dock = DockStyle.Fill };
            panel.Paint += (s, e) =>
            {
                using var br = new SolidBrush(C_PANEL);
                using var p = new Pen(C_LINE);
                e.Graphics.FillRectangle(br, panel.ClientRectangle);
                e.Graphics.DrawRectangle(p, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            var lbl = new Label
            {
                Text = labels[i].ToUpper(), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_DIM, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 8)
            };
            var val = new Label
            {
                Text = values[i], Font = new Font("Cascadia Code", 10.5f, FontStyle.Bold),
                ForeColor = C_TEXT, BackColor = Color.Transparent, AutoSize = true, Location = new Point(10, 24)
            };
            panel.Controls.AddRange(new Control[] { lbl, val });
            stats[i] = (panel, lbl, val);
        }
        for (int i = 0; i < 4; i++)
            tlpStats.Controls.Add(stats[i].panel, i % 2, i / 2);

        // Tags
        flpTags = new FlowLayoutPanel
        {
            Location = new Point(0, 166), Width = 328, Height = 30,
            BackColor = C_BG, AutoScroll = false
        };
        AddTag("英语");
        AddTag("流行");
        AddTag("摇滚");

        pnlMeta.Controls.AddRange(new Control[] { lblPreviewName, lblPreviewCreator, tlpStats, flpTags });

        // Songs
        pnlPreviewSongs = new Panel
        {
            Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(16, 0, 16, 0),
            Visible = true
        };
        lblPreviewSongsTitle = new Label
        {
            Text = "歌曲预览 · Top 5", Font = F_LABEL, ForeColor = C_DIM,
            BackColor = Color.Transparent, AutoSize = true, Location = new Point(0, 10), Height = 20
        };
        lstPreviewSongs = new ListBox
        {
            Location = new Point(0, 36), Width = 328, Height = 160,
            BackColor = C_BG, ForeColor = C_TEXT, BorderStyle = BorderStyle.None,
            IntegralHeight = false, DrawMode = DrawMode.OwnerDrawVariable,
            Font = F_NORMAL, ItemHeight = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        lstPreviewSongs.MeasureItem += (s, e) => e.ItemHeight = 32;
        lstPreviewSongs.DrawItem += OnLstPreviewSongsDrawItem;

        pnlPreviewSongs.Controls.AddRange(new Control[] { lblPreviewSongsTitle, lstPreviewSongs });

        // Foot
        pnlPreviewFoot = new Panel
        {
            Dock = DockStyle.Bottom, Height = 60, BackColor = C_BG,
            Padding = new Padding(16, 10, 16, 12)
        };
        pnlPreviewFoot.Paint += (s, e) =>
        {
            using var p = new Pen(C_LINE);
            e.Graphics.DrawLine(p, 0, 0, pnlPreviewFoot.Width, 0);
        };
        btnPreviewFetch = MakeBtn("▶ 抓取完整歌单", primary: true, new Point(0, 6));
        btnPreviewFetch.Size = new Size(200, 36);
        btnPreviewOpenJson = MakeBtn("📂 JSON", primary: false, new Point(208, 6));
        btnPreviewOpenJson.Size = new Size(120, 36);
        btnPreviewFetch.Click += OnPreviewFetchClick;
        btnPreviewOpenJson.Click += OnPreviewOpenJsonClick;
        pnlPreviewFoot.Controls.AddRange(new Control[] { btnPreviewFetch, btnPreviewOpenJson });

        pnlRightBody.Controls.Add(pnlPreviewSongs);
        pnlRightBody.Controls.Add(pnlMeta);
        pnlRightBody.Controls.Add(pnlPreviewCover);
        pnlRightBody.Controls.Add(pnlPreviewFoot);

        pnlRight.Controls.Add(pnlRightBody);
        pnlRight.Controls.Add(pnlRightHead);

        return pnlRight;
    }

    private void AddTag(string text)
    {
        var tag = new Label
        {
            Text = text, Font = F_MONO_XS, ForeColor = C_PINK,
            BackColor = Color.FromArgb(42, 37, 53),
            AutoSize = true, Padding = new Padding(9, 4, 9, 4),
            Margin = new Padding(0, 0, 6, 0)
        };
        tag.Paint += (s, e) =>
        {
            using var br = new SolidBrush(Color.FromArgb(42, 37, 53));
            using var p = new Pen(Color.FromArgb(58, 49, 69));
            var r = tag.ClientRectangle;
            e.Graphics.FillRectangle(br, r);
            e.Graphics.DrawRectangle(p, 0, 0, r.Width - 1, r.Height - 1);
        };
        flpTags.Controls.Add(tag);
    }

    // ════════════════ Helpers ════════════════
    private static Panel MakePanel(Color back, Size size, DockStyle Dock = DockStyle.None)
        => new Panel { BackColor = back, Size = size, Dock = Dock };

    private Label MakeWinBtn(string text, Color color, Point loc) => new()
    {
        Text = text, Font = new Font("Cascadia Code", 12f),
        ForeColor = color, BackColor = C_BG2,
        AutoSize = false, Size = new Size(24, 24),
        TextAlign = ContentAlignment.MiddleCenter,
        Location = loc, Cursor = Cursors.Hand
    };

    private Label MakePill(string text, Color fg, Color bg, Color border)
    {
        var l = new Label
        {
            Text = text, Font = F_PILL, ForeColor = fg,
            BackColor = bg, AutoSize = true,
            Padding = new Padding(12, 5, 12, 5)
        };
        l.Paint += (s, e) =>
        {
            using var br = new SolidBrush(bg);
            using var p = new Pen(border);
            var r = l.ClientRectangle;
            e.Graphics.FillRectangle(br, r);
            e.Graphics.DrawRectangle(p, 0, 0, r.Width - 1, r.Height - 1);
        };
        return l;
    }

    private TextBox MakeTextInput(string placeholder, string initial, Point loc)
    {
        var t = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle, BackColor = C_BG, ForeColor = C_TEXT,
            Font = F_MONO_XS, Location = loc, Height = 24,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        t.PlaceholderText = placeholder;
        t.Text = initial;
        return t;
    }

    private Button MakeBtn(string text, bool primary, Point loc)
    {
        var bg = primary ? Color.FromArgb(74, 143, 208) : C_PANEL;
        var border = primary ? Color.FromArgb(58, 124, 190) : C_LINE;
        var fg = primary ? Color.White : C_TEXT;
        var b = new Button
        {
            Text = text, Font = F_NORMAL,
            BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat,
            Location = loc, Cursor = Cursors.Hand, AutoSize = false
        };
        b.FlatAppearance.BorderColor = border;
        b.FlatAppearance.BorderSize = 1;
        var origBg = bg;
        b.MouseEnter += (s, e) =>
            b.BackColor = primary ? Color.FromArgb(84, 153, 218) : C_PANEL2;
        b.MouseLeave += (s, e) => b.BackColor = origBg;
        return b;
    }

    private Panel MakeTab(bool active, Point loc, string title, out Label titleLabel, out Label closeLabel)
    {
        var p = new Panel
        {
            BackColor = active ? C_BG : C_BG2,
            Location = loc, Size = new Size(160, 40), Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        var lbl = new Label
        {
            Text = title, Font = new Font("Segoe UI", 9.5f),
            ForeColor = active ? C_ACCENT : C_TEXT2, BackColor = Color.Transparent,
            AutoSize = true, Location = new Point(16, 12)
        };
        var close = new Label
        {
            Text = "×", Font = new Font("Cascadia Code", 11f),
            ForeColor = C_DIM, BackColor = Color.Transparent, AutoSize = true,
            Location = new Point(132, 11), Cursor = Cursors.Hand
        };
        close.MouseEnter += (s, e) => close.ForeColor = C_TEXT;
        close.MouseLeave += (s, e) => close.ForeColor = C_DIM;

        titleLabel = lbl;
        closeLabel = close;
        p.Controls.AddRange(new Control[] { lbl, close });
        p.Paint += (s, e) =>
        {
            using var br = new SolidBrush(p.BackColor);
            using var pPen = active ? new Pen(C_ACCENT, 2) : new Pen(C_LINE);
            e.Graphics.FillRectangle(br, p.ClientRectangle);
            if (active)
                e.Graphics.DrawLine(pPen, 0, p.Height - 1, p.Width, p.Height - 1);
        };
        // 让标题 Label 的点击也能转发到 Panel（WinForms 不会自动冒泡）
        var onClick = typeof(Control).GetMethod("OnClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        lbl.Click += (s, e) => onClick?.Invoke(p, new object[] { EventArgs.Empty });
        return p;
    }
}