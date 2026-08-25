using System.Diagnostics;

namespace QqPlaylist;

public partial class MainForm : Form
{
    private CancellationTokenSource? _cts;
    private PlaylistResult? _currentResult;
    private ProfileFetchResult? _currentProfile;
    private List<ListRow> _flatRows = new();
    private int _hoverIndex = -1;
    private int _activeTab = 0;          // 0=抓取结果  1=JSON 预览
    private string? _lastRawJson;        // 最近一次抓取返回的原始 JSON 字符串

    public MainForm()
    {
        InitializeComponent();
        EnableDoubleBuffered(lstMyPlaylists);
        EnableDoubleBuffered(lstPreviewSongs);
        TryAutoRestoreCookie();
        UpdatePreviewPaneEmpty();
        SetStatus("⏸ 待命 — 输入歌单 ID 或左侧列表点选喵~", C_DIM);
    }

    // ═══════════════════════════════════════
    //  Tab 1: 按歌单 ID 抓取
    // ═══════════════════════════════════════

    private async void OnFetchClick(object? sender, EventArgs e) => await FetchByIdAsync();
    private async void OnTxtPlaylistIdKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            await FetchByIdAsync();
        }
    }

    private async Task FetchByIdAsync()
    {
        var idOrUrl = txtPlaylistId.Text.Trim();
        if (string.IsNullOrEmpty(idOrUrl))
        {
            SetStatus("⚠ 先填个歌单 ID 或 URL 喵~", C_WARN);
            return;
        }

        if (_cts is { IsCancellationRequested: false })
        {
            SetStatus("⚠ 已经在抓了，别急喵~", C_WARN);
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var progress = new Progress<string>(msg => SetStatus(msg, C_DIM));

        SetInputBusy(true);
        SetStatus("⏳ 开始抓取…", C_DIM);

        try
        {
            var outcome = await PlaylistFetcher.FetchAsync(idOrUrl, progress, token);
            var result = outcome.Result;
            _currentResult = result;
            _lastRawJson = outcome.RawJson;

            var md = PlaylistFetcher.ToMarkdown(result);
            RenderOutputTab();
            UpdatePreviewPane(result);

            SetStatus($"✅ 成功抓取 {result.Songs.Count} 首 — {result.Name}", C_GOOD);
            if (chkAutoSave.Checked) TryAutoSave(result);
            UpdateSaveButtonsState();
        }
        catch (PlaylistFetchException ex)
        {
            SetStatus($"❌ {Truncate(ex.Message, 200)}", C_BAD);
            rtbOutput.Text = $"═══ 抓取失败 ═══\n\n{ex.Message}";
        }
        catch (OperationCanceledException)
        {
            SetStatus("⏹ 已取消", C_DIM);
        }
        catch (HttpRequestException ex)
        {
            SetStatus($"❌ 网络错误：{ex.Message}", C_BAD);
            rtbOutput.Text = $"═══ 网络错误 ═══\n\n{ex.Message}";
        }
        catch (Exception ex)
        {
            SetStatus($"❌ 未知错误：{ex.Message}", C_BAD);
            rtbOutput.Text = $"═══ 未知错误 ═══\n\n{ex}";
        }
        finally
        {
            SetInputBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void TryAutoSave(PlaylistResult result)
    {
        var path = (txtSavePath.Text ?? "").Trim();
        if (string.IsNullOrEmpty(path))
            path = DefaultSavePath(result.Id);

        try
        {
            File.WriteAllText(path, PlaylistFetcher.ToMarkdown(result), new System.Text.UTF8Encoding(true));
            SetStatus($"✅ 成功抓取 {result.Songs.Count} 首 — {result.Name}  |  💾 已保存：{path}", C_GOOD);
        }
        catch (Exception ex)
        {
            SetStatus($"⚠ 自动保存失败：{ex.Message}", C_WARN);
        }
    }

    // ═══════════════════════════════════════
    //  左侧：我的歌单列表（自定义绘制）
    // ═══════════════════════════════════════

    private void OnLstMyPlaylistsMeasureItem(object? sender, MeasureItemEventArgs e)
    {
        e.ItemHeight = lstMyPlaylists.Items[e.Index] is ListRow { IsSeparator: true } ? 28 : 58;
    }

    private void OnLstMyPlaylistsDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstMyPlaylists.Items.Count) return;
        if (lstMyPlaylists.Items[e.Index] is not ListRow row) return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bg = row.IsSeparator ? C_BG :
                 (e.State.HasFlag(DrawItemState.Selected) ? C_PANEL2 :
                  e.Index == _hoverIndex ? C_PANEL : C_BG);

        using (var br = new SolidBrush(bg))
            e.Graphics.FillRectangle(br, e.Bounds);

        if (row.IsSeparator)
        {
            // 分组标题 (── 我创建的 (65) ──)
            using var pen = new Pen(C_ACCENT, 2);
            e.Graphics.DrawLine(pen, e.Bounds.X + 8, e.Bounds.Y + 16, e.Bounds.X + 12, e.Bounds.Y + 16);
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var br = new SolidBrush(C_DIM);
            var size = e.Graphics.MeasureString(row.Display, font);
            e.Graphics.DrawString(row.Display, font, br, e.Bounds.X + 20, e.Bounds.Y + 8);
            return;
        }

        // Active border (选中)
        if (e.State.HasFlag(DrawItemState.Selected))
        {
            using var pen = new Pen(C_ACCENT2, 1);
            e.Graphics.DrawRectangle(pen, e.Bounds.X + 1, e.Bounds.Y + 1,
                e.Bounds.Width - 3, e.Bounds.Height - 3);
        }

        // Row 1: 图标 + 名字
        using (var f1 = new Font("Segoe UI", 10.5f, row.IsActive ? FontStyle.Bold : FontStyle.Regular))
        using (var br = new SolidBrush(C_TEXT))
        {
            e.Graphics.DrawString(row.Icon, new Font("Segoe UI Emoji", 10f),
                new SolidBrush(C_TEXT), e.Bounds.X + 12, e.Bounds.Y + 8);
            e.Graphics.DrawString(row.Display, font: f1, brush: br, x: e.Bounds.X + 34, y: e.Bounds.Y + 7);
        }

        // Row 2: badge + 播放数
        int badgeX = e.Bounds.X + 34;
        int badgeY = e.Bounds.Y + 32;
        var badgeText = $"{row.SongCount} 首";
        var badgeSize = e.Graphics.MeasureString(badgeText, F_MONO_XS);
        var badgeRect = new Rectangle(badgeX, badgeY, (int)badgeSize.Width + 12, 16);
        using (var br = new SolidBrush(C_PANEL2))
            e.Graphics.FillRectangle(br, badgeRect);
        using (var pen = new Pen(C_LINE))
            e.Graphics.DrawRectangle(pen, badgeRect);
        using (var br = new SolidBrush(C_TEXT2))
            e.Graphics.DrawString(badgeText, F_MONO_XS, br, badgeX + 6, badgeY + 2);

        if (!string.IsNullOrEmpty(row.PlayCount))
        {
            using var br = new SolidBrush(C_DIM);
            e.Graphics.DrawString(row.PlayCount, F_MONO_XS, br, badgeRect.Right + 8, badgeY + 2);
        }
    }

    private void OnLstMyPlaylistsSelectionChanged(object? sender, EventArgs e)
    {
        if (lstMyPlaylists.SelectedItem is ListRow { IsSeparator: false } row)
        {
            txtPlaylistId.Text = row.PlaylistId;
            lblRightCount.Text = "已选中";
            lblRightCount.ForeColor = C_GOOD;
            UpdatePreviewFromRow(row);
        }
    }

    private async void OnLstMyPlaylistsDoubleClick(object? sender, EventArgs e)
    {
        if (lstMyPlaylists.SelectedItem is ListRow { IsSeparator: false } row)
            await FetchByIdAsync();
    }

    // ═══════════════════════════════════════
    //  左侧：搜索过滤
    // ═══════════════════════════════════════

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        txtSearch.TextChanged += OnSearchTextChanged;
        lstMyPlaylists.MouseMove += OnLstMyPlaylistsMouseMove;
        lstMyPlaylists.MouseLeave += (s, _) => { _hoverIndex = -1; lstMyPlaylists.Invalidate(); };
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        if (_currentProfile is null) return;
        ApplyFilter();
    }

    private void OnLstMyPlaylistsMouseMove(object? sender, MouseEventArgs e)
    {
        var idx = lstMyPlaylists.IndexFromPoint(e.Location);
        if (idx == _hoverIndex) return;

        // 只重绘旧行+新行两小块，避免整列闪烁 (^ω^)～
        var prev = _hoverIndex;
        _hoverIndex = idx;
        if (prev >= 0 && prev < lstMyPlaylists.Items.Count)
        {
            var r = lstMyPlaylists.GetItemRectangle(prev);
            if (r.Height > 0) lstMyPlaylists.Invalidate(r);
        }
        if (idx >= 0 && idx < lstMyPlaylists.Items.Count)
        {
            var r = lstMyPlaylists.GetItemRectangle(idx);
            if (r.Height > 0) lstMyPlaylists.Invalidate(r);
        }
    }

    private void ApplyFilter()
    {
        if (_currentProfile is null) return;
        var q = txtSearch.Text.Trim();
        var rows = new List<ListRow>();
        int totalCreated = 0, totalCollected = 0;

        if (q.Length == 0)
        {
            if (_currentProfile.CreatedPlaylists.Count > 0)
            {
                rows.Add(new ListRow($"── 我创建的 ({_currentProfile.CreatedPlaylists.Count}) ──", ""));
                foreach (var p in _currentProfile.CreatedPlaylists)
                    rows.Add(ListRow.From(p));
                totalCreated = _currentProfile.CreatedPlaylists.Count;
            }
            if (_currentProfile.CollectedPlaylists.Count > 0)
            {
                rows.Add(new ListRow($"── 我收藏的 ({_currentProfile.CollectedPlaylists.Count}) ──", ""));
                foreach (var p in _currentProfile.CollectedPlaylists)
                    rows.Add(ListRow.From(p));
                totalCollected = _currentProfile.CollectedPlaylists.Count;
            }
        }
        else
        {
            var all = _currentProfile.CreatedPlaylists
                .Concat(_currentProfile.CollectedPlaylists)
                .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (all.Count > 0)
                rows.Add(new ListRow($"── 搜索结果 ({all.Count}) ──", ""));
            foreach (var p in all)
                rows.Add(ListRow.From(p));
        }

        lblLeftCount.Text = (totalCreated + totalCollected).ToString();

        _flatRows = rows;
        lstMyPlaylists.BeginUpdate();
        lstMyPlaylists.Items.Clear();
        lstMyPlaylists.Items.AddRange(rows.Cast<object>().ToArray());
        lstMyPlaylists.EndUpdate();
    }

    // ═══════════════════════════════════════
    //  左侧底部：Cookie + 加载
    // ═══════════════════════════════════════

    private async void OnLoadMyPlaylistsClick(object? sender, EventArgs e) => await LoadMyPlaylistsAsync();

    private async Task LoadMyPlaylistsAsync()
    {
        var uin = txtUin.Text.Trim();
        var cookie = txtCookie.Text.Trim();
        if (string.IsNullOrEmpty(uin)) { SetStatus("⚠ 先填 QQ 号喵~", C_WARN); return; }
        if (string.IsNullOrEmpty(cookie)) { SetStatus("⚠ Cookie 是空的喵~", C_WARN); return; }

        if (_cts is { IsCancellationRequested: false })
        {
            SetStatus("⚠ 已经在加载了，别急喵~", C_WARN);
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        SetLoadMyBusy(true);
        SetStatus("⏳ 调 QQ 音乐接口…", C_DIM);

        try
        {
            var profile = await ProfileFetcher.FetchAsync(uin, cookie, token);
            _currentProfile = profile;
            _lastRawJson = profile.RawJsonPreview;

            try { CookieStore.SaveCookie(uin, cookie); UpdateStoredHint(true); }
            catch (Exception ex) { SetStatus($"⚠ Cookie 本地保存失败：{ex.Message}", C_WARN); }

            ApplyFilter();

            var total = profile.CreatedPlaylists.Count + profile.CollectedPlaylists.Count;
            SetStatus(total > 0
                ? $"✅ {profile.Nickname ?? uin} — 创建 {profile.CreatedPlaylists.Count} / 收藏 {profile.CollectedPlaylists.Count} | JSON 已保存到：{profile.RawJsonPath}"
                : $"⚠ {profile.Nickname ?? uin} — 0 歌单 | JSON: {profile.RawJsonPath}",
                total > 0 ? C_GOOD : C_WARN);

            if (total == 0)
                SwitchOutputTab(1); // 切到 JSON 预览看诊断
        }
        catch (ProfileFetchException ex)
        {
            SetStatus($"❌ {Truncate(ex.Message, 200)}", C_BAD);
            if (_currentResult is not null) UpdatePreviewPane(_currentResult);
        }
        catch (HttpRequestException ex)
        {
            SetStatus($"❌ 网络错误：{ex.Message}", C_BAD);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ {ex}", C_BAD);
        }
        finally
        {
            SetLoadMyBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnClearStoredCookieClick(object? sender, EventArgs e)
    {
        var uin = txtUin.Text.Trim();
        if (string.IsNullOrEmpty(uin)) return;
        var ok = MessageBox.Show(
            $"确定要删除本机为 QQ {uin} 存储的 Cookie 吗？",
            "QqPlaylist", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (ok != DialogResult.Yes) return;
        CookieStore.DeleteCookie(uin);
        CookieStore.ClearLastUin();
        txtCookie.Clear();
        UpdateStoredHint(false);
        SetStatus("🗑 已清除本地 Cookie", C_DIM);
    }

    private void TryAutoRestoreCookie()
    {
        var lastUin = CookieStore.LoadLastUin();
        if (string.IsNullOrEmpty(lastUin)) return;
        txtUin.Text = lastUin;
        var cookie = CookieStore.LoadCookie(lastUin);
        if (cookie is not null)
        {
            txtCookie.Text = cookie;
            UpdateStoredHint(true);
        }
        else
        {
            UpdateStoredHint(false);
        }
    }

    private void UpdateStoredHint(bool stored)
    {
        if (stored)
        {
            lblCookieHint.Text = "✅ Cookie 已从本地读取";
            lblCookieHint.ForeColor = C_GOOD;
        }
        else
        {
            lblCookieHint.Text = "🆕 首次输入";
            lblCookieHint.ForeColor = C_DIM;
        }
    }

    // ═══════════════════════════════════════
    //  中间：输出 Tab
    // ═══════════════════════════════════════

    private void SwitchOutputTab(int idx)
    {
        _activeTab = idx;
        bool fetchActive = idx == 0;
        tabFetchResult.BackColor = fetchActive ? C_BG : C_BG2;
        lblTabFetchResult.ForeColor = fetchActive ? C_ACCENT : C_TEXT2;
        tabJsonPreview.BackColor = fetchActive ? C_BG2 : C_BG;
        lblTabJsonPreview.ForeColor = fetchActive ? C_TEXT2 : C_ACCENT;
        RenderOutputTab();
    }

    /// <summary>
    /// 根据当前激活的 tab 渲染 rtbOutput 内容 (^ω^)～
    /// </summary>
    private void RenderOutputTab()
    {
        // JSON tab 开启自动换行；Markdown tab 关掉（防止表格错位）
        rtbOutput.WordWrap = _activeTab == 1;

        if (_activeTab == 0)
        {
            // 抓取结果
            if (_currentResult is not null)
                rtbOutput.Text = PlaylistFetcher.ToMarkdown(_currentResult);
            else if (_currentProfile is not null)
            {
                var total = _currentProfile.CreatedPlaylists.Count + _currentProfile.CollectedPlaylists.Count;
                rtbOutput.Text =
                    $"═══ 我的歌单 ═══\n\n" +
                    $"👤 {(_currentProfile.Nickname ?? _currentProfile.Uin)}\n" +
                    $"📚 创建 {_currentProfile.CreatedPlaylists.Count} / 收藏 {_currentProfile.CollectedPlaylists.Count}（合计 {total}）\n\n" +
                    "👈 在左侧列表里点选具体歌单来查看详情喵~";
            }
            else
                rtbOutput.Text = "═══ 抓取结果 ═══\n\n还没抓取过内容，先在左侧输入歌单 ID 或者加载「我的歌单」喵~";
        }
        else
        {
            // JSON 预览
            if (!string.IsNullOrEmpty(_lastRawJson))
                rtbOutput.Text = _lastRawJson;
            else
                rtbOutput.Text = "═══ JSON 预览 ═══\n\n还没有任何原始响应，先抓一次歌单/加载「我的歌单」再看喵~";
        }
    }

    // ═══════════════════════════════════════
    //  右侧：实时预览
    // ═══════════════════════════════════════

    private void OnPreviewFetchClick(object? sender, EventArgs e) => _ = FetchByIdAsync();
    private void OnPreviewOpenJsonClick(object? sender, EventArgs e) => OnOpenRawJsonClick(sender, e);

    private void OnOpenRawJsonClick(object? sender, EventArgs e)
    {
        if (_currentProfile is null)
        {
            SetStatus("⚠ 还没加载过我的歌单喵~", C_WARN);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_currentProfile.RawJsonPath}\"")
            { UseShellExecute = true });
        }
        catch (Exception ex) { SetStatus($"❌ 打开文件失败：{ex.Message}", C_BAD); }
    }

    private void UpdatePreviewPaneEmpty()
    {
        lblPreviewName.Text = "— 选个歌单看看喵 —";
        lblPreviewName.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
        lblPreviewCreator.Text = "左侧双击抓取，或输入 ID 后点 ▶";
        stats[0].val.Text = "—";
        stats[1].val.Text = "—";
        stats[2].val.Text = "—";
        stats[3].val.Text = "—";
        lblCoverCountTag.Visible = false;
        lblCoverEmpty.Visible = true;
        picCover.Image = null;
        flpTags.Controls.Clear();
        lstPreviewSongs.Items.Clear();
        lblPreviewSongsTitle.Text = "歌曲预览 · —";
    }

    private void UpdatePreviewFromRow(ListRow row)
    {
        lblPreviewName.Text = row.Display;
        lblPreviewName.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        lblPreviewCreator.Text = $"双击列表项抓取全部歌曲";
        stats[0].val.Text = "—";
        stats[1].val.Text = "—";
        stats[2].val.Text = row.SongCount > 0 ? row.SongCount.ToString() : "—";
        stats[3].val.Text = row.PlayCount;
        lblCoverCountTag.Visible = false;
        lblCoverEmpty.Visible = true;
        picCover.Image = null;
        flpTags.Controls.Clear();
        lstPreviewSongs.Items.Clear();
        lblPreviewSongsTitle.Text = "歌曲预览 · Top 5";
    }

    private void UpdatePreviewPane(PlaylistResult result)
    {
        var totalSec = result.Songs.Sum(t => t.DurationSec);
        var th = totalSec / 3600;
        var tm = (totalSec % 3600) / 60;
        var avg = result.Songs.Count > 0 ? result.Songs.Average(t => t.DurationSec) : 0;

        lblPreviewName.Text = result.Name;
        lblPreviewName.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        var creatorLine = $"by {result.Creator}".Trim();
        if (result.CreateTime > 0)
        {
            try
            {
                creatorLine += " · " + DateTimeOffset.FromUnixTimeSeconds(result.CreateTime).LocalDateTime.ToString("yyyy-MM-dd");
            }
            catch { }
        }
        lblPreviewCreator.Text = creatorLine;

        stats[0].val.Text = th > 0 ? $"{th}小时{tm:D2}分" : $"{tm}分{totalSec % 60:D2}秒";
        stats[1].val.Text = $"{TimeSpan.FromSeconds((int)avg):m\\:ss}/首";
        stats[2].val.Text = result.Songs.Count.ToString();
        stats[3].val.Text = result.TotalCount > result.Songs.Count
            ? $"{result.TotalCount} (数据库)" : $"{result.TotalCount}";

        // Tags
        flpTags.Controls.Clear();
        foreach (var t in result.Tags.Split(" / ", StringSplitOptions.RemoveEmptyEntries))
            AddTag(t);

        // Cover
        if (!string.IsNullOrEmpty(result.CoverUrl))
        {
            lblCoverEmpty.Visible = false;
            lblCoverCountTag.Text = $"🎵 {result.Songs.Count} 首";
            lblCoverCountTag.Visible = true;
            _ = LoadCoverAsync(result.CoverUrl);
        }
        else
        {
            lblCoverEmpty.Visible = true;
            lblCoverCountTag.Visible = false;
        }

        // Top 5 songs
        lstPreviewSongs.BeginUpdate();
        lstPreviewSongs.Items.Clear();
        foreach (var s in result.Songs.Take(5))
            lstPreviewSongs.Items.Add(s);
        lstPreviewSongs.EndUpdate();
        lblPreviewSongsTitle.Text = $"歌曲预览 · Top {Math.Min(5, result.Songs.Count)}";
    }

    private async Task LoadCoverAsync(string url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            var bytes = await http.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            // marshal back to UI thread
            BeginInvoke(() =>
            {
                picCover.Image?.Dispose();
                picCover.Image = new Bitmap(img);
            });
        }
        catch
        {
            BeginInvoke(() => { lblCoverEmpty.Visible = true; lblCoverEmpty.Text = "🔍 封面加载失败"; });
        }
    }

    private void OnLstPreviewSongsDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstPreviewSongs.Items.Count) return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bg = new SolidBrush(e.Index == _hoverIndex ? C_PANEL : C_BG);
        e.Graphics.FillRectangle(bg, e.Bounds);

        if (lstPreviewSongs.Items[e.Index] is not Track t) return;

        // 序号
        using var brIdx = new SolidBrush(C_DIM);
        e.Graphics.DrawString((e.Index + 1).ToString(), F_MONO_XS, brIdx,
            e.Bounds.X + 4, e.Bounds.Y + 8);

        // 歌名 + 歌手
        var display = $"{t.Name} ";
        using (var brName = new SolidBrush(C_TEXT))
            e.Graphics.DrawString(display, F_NORMAL, brName, e.Bounds.X + 28, e.Bounds.Y + 7);

        using (var brArtist = new SolidBrush(C_TEXT2))
            e.Graphics.DrawString($"— {t.Artist}", F_MONO_XS, brArtist,
                e.Bounds.X + 28 + (int)e.Graphics.MeasureString(display, F_NORMAL).Width,
                e.Bounds.Y + 9);

        // 时长（右对齐）
        var dur = $"{t.DurationSec / 60}:{t.DurationSec % 60:D2}";
        var durSize = e.Graphics.MeasureString(dur, F_MONO_XS);
        using var brDur = new SolidBrush(C_TEXT2);
        e.Graphics.DrawString(dur, F_MONO_XS, brDur,
            e.Bounds.Right - durSize.Width - 8, e.Bounds.Y + 9);
    }

    // ═══════════════════════════════════════
    //  中间：保存 / 浏览 / 复制
    // ═══════════════════════════════════════

    private void OnAutoSaveChanged(object? sender, EventArgs e)
    {
        var onChk = chkAutoSave.Checked;
        txtSavePath.Enabled = onChk;
        btnBrowse.Enabled = onChk;
        if (onChk && string.IsNullOrWhiteSpace(txtSavePath.Text) && _currentResult is not null)
            txtSavePath.Text = DefaultSavePath(_currentResult.Id);
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Markdown 文件 (*.md)|*.md|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            DefaultExt = "md",
            FileName = _currentResult is null
                ? "playlist.md"
                : $"{SanitizeFileName(_currentResult.Name)}.md",
            InitialDirectory = GetInitialDir()
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtSavePath.Text = dlg.FileName;
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (_currentResult is null)
        {
            SetStatus("⚠ 还没抓取歌单呐~", C_WARN);
            return;
        }

        var path = (txtSavePath.Text ?? "").Trim();
        if (string.IsNullOrEmpty(path))
        {
            SetStatus("⚠ 路径是空的喵~", C_WARN);
            return;
        }

        try
        {
            if (!Path.HasExtension(path)) path += ".md";
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, PlaylistFetcher.ToMarkdown(_currentResult), new System.Text.UTF8Encoding(true));
            SetStatus($"💾 已保存到：{path}", C_GOOD);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ 保存失败：{ex.Message}", C_BAD);
        }
    }

    private void OnCopyClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(rtbOutput.Text))
        {
            SetStatus("⚠ 还没有内容可以复制喵~", C_WARN);
            return;
        }
        try
        {
            Clipboard.SetText(rtbOutput.Text);
            SetStatus($"📋 已复制到剪贴板 ({rtbOutput.Text.Length} 字符)", C_GOOD);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ 复制失败：{ex.Message}", C_BAD);
        }
    }

    // ═══════════════════════════════════════
    //  UI 辅助
    // ═══════════════════════════════════════

    private void SetInputBusy(bool busy)
    {
        btnFetch.Enabled = !busy;
        txtPlaylistId.Enabled = !busy;
        btnFetch.Text = busy ? "⏳ 抓取中…" : "▶ 抓取歌单";
    }

    private void SetLoadMyBusy(bool busy)
    {
        btnLoadMyPlaylists.Enabled = !busy;
        btnLoadMyPlaylists.Text = busy ? "⏳ 加载中…" : "💾 加载";
        txtUin.Enabled = !busy;
        txtCookie.Enabled = !busy;
    }

    private void SetStatus(string text, Color? color = null)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text, color));
            return;
        }
        lblStatus.Text = text;
        lblStatus.ForeColor = color ?? C_DIM;
        lblTopText.Text = text.Length > 60 ? text[..60] + "…" : text;
    }

    private void UpdateSaveButtonsState()
    {
        var hasResult = _currentResult is not null;
        var hasOutput = !string.IsNullOrEmpty(rtbOutput.Text);
        btnSave.Enabled = hasResult;
        btnCopy.Enabled = hasOutput;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        picCover.Image?.Dispose();
    }

    // ═══════════════════════════════════════
    //  工具
    // ═══════════════════════════════════════

    private sealed record ListRow(string Display, string PlaylistId)
    {
        public bool IsSeparator => string.IsNullOrEmpty(PlaylistId);
        public string Icon { get; init; } = "";
        public int SongCount { get; init; }
        public string PlayCount { get; init; } = "";
        public bool IsActive { get; init; }

        public static ListRow From(UserPlaylist p) => new(p.Name, p.Id)
        {
            Icon = "📁",
            SongCount = p.SongCount,
            PlayCount = ""
        };
    }

    private static string DefaultSavePath(string playlistId)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(dir, $"playlist_{SanitizeFileName(playlistId)}.md");
    }

    private string GetInitialDir()
    {
        var p = (txtSavePath.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(p) && Path.IsPathRooted(p))
            return Path.GetDirectoryName(p) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string SanitizeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(s.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrEmpty(clean) ? "playlist" : clean;
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    // ═══════════════════════════════════════
    //  自定义标题栏拖拽 + 双击最大化
    // ═══════════════════════════════════════
    private bool _draggingWindow;
    private Point _dragStartPoint;

    private void OnTitleBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _draggingWindow = true;
        _dragStartPoint = e.Location;
    }

    private void OnTitleBarMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingWindow) return;
        var p = pnlTitleBar.PointToScreen(e.Location);
        Location = new Point(p.X - _dragStartPoint.X, p.Y - _dragStartPoint.Y);
    }

    private void OnTitleBarMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _draggingWindow = false;
    }

    private void OnTitleBarMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    // ═══════════════════════════════════════
    //  通过反射打开 ListBox 的双缓冲，告别 OwnerDrawVariable 闪烁
    // ═══════════════════════════════════════
    private static void EnableDoubleBuffered(ListBox lb)
    {
        typeof(ListBox)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(lb, true);
    }
}