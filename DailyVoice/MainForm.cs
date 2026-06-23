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

        // 视频文件
        if (!string.IsNullOrEmpty(_config.VideoFile) && File.Exists(_config.VideoFile))
        {
            lblVideoFile.Text = Path.GetFileName(_config.VideoFile);
            lblVideoFile.ForeColor = SystemColors.ControlText;
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _config = new Config
        {
            PlayTime = dtpTime.Value.ToString("HH:mm"),
            Volume = tbVolume.Value,
            VideoFile = _config.VideoFile,
            VideoPlayTime = dtpVideoTime.Value.ToString("HH:mm")
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

    private void OnBrowseVideo(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "选择视频文件",
            Filter = "视频文件|*.mp4;*.mkv;*.avi;*.webm;*.mov|所有文件|*.*",
            InitialDirectory = Directory.Exists(_videoDir) ? _videoDir : BaseDir
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _config.VideoFile = dlg.FileName;
            lblVideoFile.Text = Path.GetFileName(dlg.FileName);
            lblVideoFile.ForeColor = SystemColors.ControlText;
        }
    }

    private void OnClearVideo(object? sender, EventArgs e)
    {
        _config.VideoFile = null;
        lblVideoFile.Text = "未选择视频";
        lblVideoFile.ForeColor = Color.Gray;
    }

    private void OnPlayVideo(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_config.VideoFile) || !File.Exists(_config.VideoFile))
        {
            MessageBox.Show("请先选择一个视频文件喵~", "DailyVoice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            using var videoForm = new VideoPlayerForm(_config.VideoFile);
            videoForm.ShowDialog();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DailyVoice 立即播放视频失败: {ex.Message}");
        }
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
