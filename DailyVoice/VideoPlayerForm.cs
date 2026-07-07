using System.Diagnostics;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放 (⁎⁍̴̛ᴗ⁍̴̛⁎)
///
/// 策略：优先用命令行播放器（全屏 + 播完退出），均不可用时降级系统播放器。
///   VLC:  vlc --fullscreen --play-and-exit --no-video-title-show <file>
///   ffplay: ffplay -fs -autoexit -noborder <file>
///   系统播放器: Process.Start (无全屏/自动关闭保证)
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly string _videoPath;

    public VideoPlayerForm(string videoPath)
    {
        _videoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

        // 隐藏自身，不显示任何窗口
        this.Text = "";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        this.Hide();
        Task.Run(() => PlayAndWait());
    }

    private void PlayAndWait()
    {
        Process? proc = null;

        try
        {
            // 优先 VLC — 最可靠的命令行全屏方案
            var vlcPath = FindVlc();
            if (vlcPath != null)
            {
                Debug.WriteLine($"[DailyVoice] VLC: {vlcPath}");
                proc = Process.Start(new ProcessStartInfo
                {
                    FileName = vlcPath,
                    Arguments = $"--fullscreen --play-and-exit --no-video-title-show --no-qt-fs-controller --video-on-top \"{_videoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }

            // 其次 ffplay
            if (proc == null)
            {
                var ffplayPath = FindInPath("ffplay");
                if (ffplayPath != null)
                {
                    Debug.WriteLine($"[DailyVoice] ffplay: {ffplayPath}");
                    proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = ffplayPath,
                        Arguments = $"-fs -autoexit -noborder \"{_videoPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }

            // 降级：系统默认播放器
            if (proc == null)
            {
                Debug.WriteLine("[DailyVoice] 降级系统播放器");
                proc = Process.Start(new ProcessStartInfo
                {
                    FileName = _videoPath,
                    UseShellExecute = true
                });
            }

            if (proc != null)
            {
                Debug.WriteLine($"[DailyVoice] 播放器已启动 (PID={proc.Id})，等待退出...");
                proc.WaitForExit();
                Debug.WriteLine($"[DailyVoice] 播放器已退出 (exit={proc.ExitCode})");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 播放异常: {ex.Message}");
        }

        // 清理并关闭
        this.BeginInvoke(() =>
        {
            if (!this.IsDisposed)
                this.Close();
        });
    }

    /// <summary>
    /// 查找 VLC 安装路径
    /// </summary>
    private static string? FindVlc()
    {
        // 常见安装路径
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC", "vlc.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "VideoLAN", "VLC", "vlc.exe"),
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        // PATH 环境变量中也找找
        return FindInPath("vlc");
    }

    /// <summary>
    /// 在 PATH 环境变量中查找可执行文件
    /// </summary>
    private static string? FindInPath(string exeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var exts = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? new[] { ".exe" };

        foreach (var dir in pathEnv.Split(';'))
        {
            foreach (var ext in exts)
            {
                var fullPath = Path.Combine(dir.Trim(), exeName + ext.Trim());
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }
        return null;
    }
}
