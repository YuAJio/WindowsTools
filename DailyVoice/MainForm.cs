using System.Diagnostics;
using Microsoft.Win32;

namespace DailyVoice;

public partial class MainForm : Form
{
    // PublishSingleFile 下 AppContext.BaseDirectory 指向临时目录，
    // 用 ProcessPath 取 exe 真实所在目录 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private readonly string _voiceDir = Path.Combine(BaseDir, "voice");
    private readonly string _videoDir = Path.Combine(BaseDir, "video");
    private readonly Scheduler _scheduler;
    private readonly AudioPlayer _audioPlayer;
    private Config _config;

    public MainForm()
    {
        InitializeComponent();

        // 文件夹迁移：旧版 video/ → voice/
        MigrateFolders();
        Directory.CreateDirectory(_voiceDir);
        Directory.CreateDirectory(_videoDir);

        // 加载配置
        _config = ConfigManager.Load();
        ApplyConfigToUi();

        // 播放器 + 调度器
        _audioPlayer = new AudioPlayer { Volume = _config.Volume / 100f };
        _scheduler = new Scheduler(_voiceDir, _audioPlayer, () => _config);
        _scheduler.OnPlayStarted += () => UpdateStatus("▶ 正在播放...", Color.Crimson);
        _scheduler.OnPlayFinished += () => UpdateStatus("⏸ 等待播放时间到达...", Color.DimGray);
        _scheduler.OnVideoStarted += OnPlaylistVideoStarted;

        // 初始加载文件列表
        RefreshFileList();

        // 开机自启状态
        chkAutoStart.Checked = IsAutoStartEnabled();
    }

    // ═══════════════════════════════════════
    //  文件夹迁移
    // ═══════════════════════════════════════

    /// <summary>
    /// 如果用户旧版把音频放在 video/ 目录，迁移到 voice/
    /// </summary>
    private void MigrateFolders()
    {
        if (Directory.Exists(_videoDir) && !Directory.Exists(_voiceDir))
        {
            try
            {
                Directory.Move(_videoDir, _voiceDir);
            }
            catch { /* 尽力而为，迁移失败不阻塞启动 */ }
        }
    }

    // ═══════════════════════════════════════
    //  UI 操作
    // ═══════════════════════════════════════

    private void ApplyConfigToUi()
    {
        if (TimeSpan.TryParse(_config.PlayTime, out var ts))
            dtpTime.Value = DateTime.Today.Add(ts);

        tbVolume.Value = _config.Volume;
        lblVolumePercent.Text = $"{_config.Volume}%";

        // 视频时间
        if (!string.IsNullOrEmpty(_config.VideoPlayTime) &&
            TimeSpan.TryParse(_config.VideoPlayTime, out var videoTs))
            dtpVideoTime.Value = DateTime.Today.Add(videoTs);

        // 旧版单选视频 → 迁移到排班（迁移后清空旧字段，避免重复迁移）
        if (_config.VideoPlaylist.Count == 0 &&
            !string.IsNullOrEmpty(_config.VideoFile) && File.Exists(_config.VideoFile))
        {
            _config.VideoPlaylist.Add(_config.VideoFile);
            _config.VideoFile = null;
        }

        RefreshPlaylistUi();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _config = new Config
        {
            PlayTime = dtpTime.Value.ToString("HH:mm"),
            Volume = tbVolume.Value,
            VideoPlayTime = dtpVideoTime.Value.ToString("HH:mm"),
            VideoPlaylist = _config.VideoPlaylist // 排班列表实时维护在 _config 上
        };
        ConfigManager.Save(_config);
        _audioPlayer.Volume = _config.Volume / 100f;

        // 开机自启
        SetAutoStart(chkAutoStart.Checked);

        MessageBox.Show("设置已保存喵~ ✨", "DailyVoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnPreviewFile(object? sender, EventArgs e)
    {
        if (lbFiles.SelectedItem is not string file || string.IsNullOrWhiteSpace(file))
            return;

        var fullPath = Path.Combine(_voiceDir, file);
        if (File.Exists(fullPath))
            _scheduler.Preview(fullPath);
    }

    private void OnOpenFolder(object? sender, EventArgs e)
    {
        Process.Start("explorer.exe", _voiceDir);
    }

    private void OnAddVideo(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "选择视频文件（追加到排班）",
            Filter = "视频文件|*.mp4;*.mkv;*.avi;*.webm;*.mov|所有文件|*.*",
            InitialDirectory = Directory.Exists(_videoDir) ? _videoDir : BaseDir
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            // 单选：一次加一个，重复添加实现同一视频多次排班
            _config.VideoPlaylist.Add(dlg.FileName);
            RefreshPlaylistUi();
        }
    }

    private void OnRemoveVideo(object? sender, EventArgs e)
    {
        var idx = lbVideoPlaylist.SelectedIndex;
        if (idx < 0 || idx >= _config.VideoPlaylist.Count) return;

        _config.VideoPlaylist.RemoveAt(idx);
        RefreshPlaylistUi();
    }

    private void OnMoveUp(object? sender, EventArgs e)
    {
        var idx = lbVideoPlaylist.SelectedIndex;
        if (idx <= 0 || idx >= _config.VideoPlaylist.Count) return;

        (_config.VideoPlaylist[idx - 1], _config.VideoPlaylist[idx]) =
            (_config.VideoPlaylist[idx], _config.VideoPlaylist[idx - 1]);
        RefreshPlaylistUi(selectIndex: idx - 1);
    }

    private void OnMoveDown(object? sender, EventArgs e)
    {
        var idx = lbVideoPlaylist.SelectedIndex;
        if (idx < 0 || idx >= _config.VideoPlaylist.Count - 1) return;

        (_config.VideoPlaylist[idx + 1], _config.VideoPlaylist[idx]) =
            (_config.VideoPlaylist[idx], _config.VideoPlaylist[idx + 1]);
        RefreshPlaylistUi(selectIndex: idx + 1);
    }

    /// <summary>
    /// 排班列表 → ListBox（显示文件名，_config 里存完整路径）
    /// </summary>
    private void RefreshPlaylistUi(int? selectIndex = null)
    {
        lbVideoPlaylist.Items.Clear();
        foreach (var path in _config.VideoPlaylist)
            lbVideoPlaylist.Items.Add(Path.GetFileName(path));

        lblPlaylistCount.Text = $"{_config.VideoPlaylist.Count} 个视频";

        // 上移/下移后恢复选中跟随
        if (selectIndex.HasValue && selectIndex.Value >= 0 && selectIndex.Value < lbVideoPlaylist.Items.Count)
            lbVideoPlaylist.SelectedIndex = selectIndex.Value;
    }

    private void OnPlayVideo(object? sender, EventArgs e)
    {
        // 预检：至少有一个可播放文件；失效项由播放器内部跳过（保持索引与列表对齐）
        if (!_config.VideoPlaylist.Any(File.Exists))
        {
            MessageBox.Show("排班里没有可播放的视频文件喵~", "DailyVoice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // 播放排班快照：改排班只影响下一轮，当前轮绝不乱序
            using var videoForm = new VideoPlayerForm(_config.VideoPlaylist.ToList());
            videoForm.OnVideoStarted += OnPlaylistVideoStarted;
            videoForm.ShowDialog(); // 模态阻塞：播放期间 UI 锁定，排班不可被改动
            videoForm.OnVideoStarted -= OnPlaylistVideoStarted;

            // 播放结束，取消高亮
            lbVideoPlaylist.SelectedIndex = -1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DailyVoice 立即播放视频失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 排班播放进度 → 高亮当前项（事件从后台线程触发，切回 UI 线程）
    /// </summary>
    private void OnPlaylistVideoStarted(int index)
    {
        BeginInvoke(() =>
        {
            if (index < 0 || index >= lbVideoPlaylist.Items.Count) return;

            lbVideoPlaylist.SelectedIndex = index;
            // 让当前项保持在可见区域（顶部往下 2 行为准）
            lbVideoPlaylist.TopIndex = Math.Max(0, index - 2);
        });
    }

    /// <summary>
    /// OwnerDraw：模态播放期间 ListBox 被禁用，系统默认不绘制高亮，
    /// 这里按 SelectedIndex 强制绘制，当前项 = 蓝底白字 + "▶" 前缀
    /// </summary>
    private void OnPlaylistDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lbVideoPlaylist.Items.Count) return;

        var isCurrent = e.Index == lbVideoPlaylist.SelectedIndex;
        using var bg = new SolidBrush(isCurrent ? Color.SteelBlue : SystemColors.Window);
        e.Graphics.FillRectangle(bg, e.Bounds);

        var text = lbVideoPlaylist.Items[e.Index]?.ToString() ?? "";
        if (isCurrent) text = "▶ " + text;

        using var fg = new SolidBrush(isCurrent ? Color.White : SystemColors.ControlText);
        using var fmt = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            LineAlignment = StringAlignment.Center
        };
        e.Graphics.DrawString(text, e.Font ?? Font, fg, e.Bounds, fmt);
    }

    private void RefreshFileList()
    {
        var files = _scheduler.GetVoiceFiles()
            .Select(f => Path.GetFileName(f))
            .ToArray();

        lbFiles.Items.Clear();
        foreach (var f in files)
            lbFiles.Items.Add(f);

        lblFileCount.Text = $"{files.Length} 个文件";
    }

    private void UpdateStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(text, color));
            return;
        }
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    // ═══════════════════════════════════════
    //  开机自启
    // ═══════════════════════════════════════

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue("DailyVoice") != null;
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath!;
            key.SetValue("DailyVoice", $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue("DailyVoice", throwOnMissingValue: false);
        }
    }

    // ═══════════════════════════════════════
    //  窗口生命周期
    // ═══════════════════════════════════════

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
        {
            this.Hide();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;

        // 最小化到托盘而不是关闭
        this.Hide();
        e.Cancel = true;
    }
}
