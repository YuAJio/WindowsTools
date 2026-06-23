using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放窗体 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 使用 Windows Media Player COM 引擎 — 原生全屏 + 播放完成自动关闭。
/// WMP 不可用时降级到系统默认播放器。
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly string _videoPath;

    public VideoPlayerForm(string videoPath)
    {
        _videoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

        // 不需要 WebView2 了 — WMP COM 自建全屏窗口
        this.Text = "DailyVoice Video";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Minimized; // 最小化，让 WMP 全屏占据
        this.ShowInTaskbar = false;
        this.BackColor = Color.Black;
        this.KeyPreview = true;
        this.KeyDown += OnKeyDown;
        this.Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[DailyVoice] 开始播放视频: {_videoPath}");
        Debug.WriteLine($"[DailyVoice] 文件存在: {File.Exists(_videoPath)}");

        // 异步播放，不阻塞 Load 事件
        Task.Run(() => PlayWithWmp());
    }

    private void PlayWithWmp()
    {
        try
        {
            var wmpType = Type.GetTypeFromProgID("WMPlayer.OCX");
            if (wmpType == null)
            {
                Debug.WriteLine("[DailyVoice] WMP 不可用，降级系统播放器");
                FallbackToSystemPlayer();
                return;
            }

            dynamic wmp = Activator.CreateInstance(wmpType)!;
            Debug.WriteLine("[DailyVoice] WMP COM 实例已创建");

            wmp.settings.volume = 100;
            wmp.URL = _videoPath;
            wmp.fullScreen = true;
            Debug.WriteLine("[DailyVoice] WMP 全屏模式已设置，等待播放...");

            // 等待 WMP 开始播放
            var started = DateTime.Now;
            while ((DateTime.Now - started).TotalSeconds < 10)
            {
                Thread.Sleep(300);
                try
                {
                    int state = wmp.playState;
                    Debug.WriteLine($"[DailyVoice] WMP 状态: {state}");
                    if (state == 3) // Playing
                        break;
                    if (state == 1 || state == 8) // Stopped or MediaEnded before playing
                    {
                        Debug.WriteLine("[DailyVoice] WMP 播放提前结束");
                        Marshal.ReleaseComObject(wmp);
                        CloseForm();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DailyVoice] WMP 状态检查异常: {ex.Message}");
                    break;
                }
            }

            // 等待播放完成
            while (true)
            {
                Thread.Sleep(500);
                try
                {
                    int state = wmp.playState;
                    if (state == 1 || state == 8) // Stopped or MediaEnded
                    {
                        Debug.WriteLine($"[DailyVoice] WMP 播放完毕 (state={state})");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DailyVoice] WMP 播放监控异常: {ex.Message}");
                    break;
                }
            }

            try { Marshal.ReleaseComObject(wmp); }
            catch { /* best effort */ }

            CloseForm();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] WMP COM 失败: {ex.Message}");
            FallbackToSystemPlayer();
        }
    }

    private void FallbackToSystemPlayer()
    {
        try
        {
            Debug.WriteLine($"[DailyVoice] Fallback: Process.Start 系统播放器");
            Process.Start(new ProcessStartInfo
            {
                FileName = _videoPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 系统播放器也失败: {ex.Message}");
        }
        CloseForm();
    }

    private void CloseForm()
    {
        if (this.InvokeRequired)
            this.BeginInvoke(() => this.Close());
        else
            this.Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Debug.WriteLine("[DailyVoice] 用户按 Escape 关闭");
            this.Close();
        }
    }
}
