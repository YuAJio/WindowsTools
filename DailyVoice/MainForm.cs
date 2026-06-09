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
    private readonly Scheduler _scheduler;
    private readonly AudioPlayer _audioPlayer;
    private Config _config;

    public MainForm()
    {
        InitializeComponent();

        Directory.CreateDirectory(_voiceDir);

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
    //  UI 操作
    // ═══════════════════════════════════════

    private void ApplyConfigToUi()
    {
        if (TimeSpan.TryParse(_config.PlayTime, out var ts))
            dtpTime.Value = DateTime.Today.Add(ts);

        tbVolume.Value = _config.Volume;
        lblVolumePercent.Text = $"{_config.Volume}%";
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _config = new Config
        {
            PlayTime = dtpTime.Value.ToString("HH:mm"),
            Volume = tbVolume.Value
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
