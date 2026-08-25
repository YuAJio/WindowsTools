using System.Diagnostics;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放 (⁎⁍̴̛ᴗ⁍̴̛⁎)
///
/// 排班模式：按顺序播放视频列表，播完全部后关闭。同一视频可重复排班实现"循环"。
/// 策略：优先用命令行播放器（全屏 + 播完退出），均不可用时降级系统播放器。
///   VLC:  vlc --fullscreen --play-and-exit --no-video-title-show <file>
///   ffplay: ffplay -fs -autoexit -noborder <file>
///   系统播放器: Process.Start (无全屏/自动关闭保证)
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly IReadOnlyList<string> _videoPaths;

    /// <summary>
    /// 每个排班视频即将开始播放时触发（参数 = 排班索引）。
    /// 从后台线程触发，订阅方需自行切换回 UI 线程。
    /// </summary>
    public event Action<int>? OnVideoStarted;

    public VideoPlayerForm(IReadOnlyList<string> videoPaths)
    {
        _videoPaths = videoPaths ?? throw new ArgumentNullException(nameof(videoPaths));

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
        try
        {
            for (var i = 0; i < _videoPaths.Count; i++)
            {
                var path = _videoPaths[i];

                // 文件被删/移动时跳过，保持索引与排班列表一致
                if (!File.Exists(path))
                {
                    Debug.WriteLine($"[DailyVoice] 排班第 {i + 1} 项文件不存在，跳过: {path}");
                    continue;
                }

                Debug.WriteLine($"[DailyVoice] 排班视频 {i + 1}/{_videoPaths.Count}: {Path.GetFileName(path)}");

                // 通知 UI 高亮当前项（后台线程）
                OnVideoStarted?.Invoke(i);

                var proc = LaunchPlayer(path);

                if (proc != null)
                {
                    Debug.WriteLine($"[DailyVoice] 播放器已启动 (PID={proc.Id})，等待退出...");
                    proc.WaitForExit();
                    Debug.WriteLine($"[DailyVoice] 播放器已退出 (exit={proc.ExitCode})");
                }
                else
                {
                    // 降级到系统播放器时拿不到 Process，跳出排班
                    Debug.WriteLine("[DailyVoice] 系统播放器无法追踪退出，跳出排班");
                    break;
                }
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
    /// 启动单个播放器进程（按优先级：VLC → ffplay → 系统默认）
    /// </summary>
    private static Process? LaunchPlayer(string videoPath)
    {
        // 优先 VLC — 最可靠的命令行全屏方案
        var vlcPath = FindVlc();
        if (vlcPath != null)
        {
            Debug.WriteLine($"[DailyVoice] VLC: {vlcPath}");
            return Process.Start(new ProcessStartInfo
            {
                FileName = vlcPath,
                Arguments = $"--fullscreen --play-and-exit --no-video-title-show --no-qt-fs-controller --video-on-top \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        // 其次 ffplay
        var ffplayPath = FindInPath("ffplay");
        if (ffplayPath != null)
        {
            Debug.WriteLine($"[DailyVoice] ffplay: {ffplayPath}");
            return Process.Start(new ProcessStartInfo
            {
                FileName = ffplayPath,
                Arguments = $"-fs -autoexit -noborder \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        // 降级：系统默认播放器（拿不到进程，跳出排班）
        Debug.WriteLine("[DailyVoice] 降级系统播放器");
        return Process.Start(new ProcessStartInfo
        {
            FileName = videoPath,
            UseShellExecute = true
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
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
            @"C:\Software\VLC\vlc.exe",
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
