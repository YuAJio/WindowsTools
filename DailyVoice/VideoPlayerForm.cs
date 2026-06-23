using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放窗体 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 优先使用 WebView2 + VirtualHostName 映射播放。
/// WebView2 失败时降级到系统默认播放器 (Process.Start)。
/// 播放结束/Escape 自动关闭。失败静默处理。
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _videoPath;
    private bool _playing;

    private const string VIRTUAL_HOST = "dailyvoice.video";

    public VideoPlayerForm(string videoPath)
    {
        _videoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

        this.Text = "DailyVoice Video";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.BackColor = Color.Black;
        this.KeyPreview = true;
        this.KeyDown += OnKeyDown;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        this.Controls.Add(_webView);

        this.Load += OnLoad;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[DailyVoice] VideoPlayerForm.OnLoad — 视频: {_videoPath}");
        Debug.WriteLine($"[DailyVoice] 文件存在: {File.Exists(_videoPath)}");

        try
        {
            Debug.WriteLine("[DailyVoice] 正在初始化 WebView2...");
            await _webView.EnsureCoreWebView2Async();
            Debug.WriteLine("[DailyVoice] WebView2 CoreWebView2 就绪");

            // 注册消息通道
            _webView.CoreWebView2.WebMessageReceived += OnWebMessage;

            // VirtualHostName 映射视频目录
            var videoDir = Path.GetDirectoryName(_videoPath)!;
            var videoName = Path.GetFileName(_videoPath);
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VIRTUAL_HOST, videoDir, CoreWebView2HostResourceAccessKind.Allow);
            Debug.WriteLine($"[DailyVoice] VirtualHost 映射: https://{VIRTUAL_HOST}/ → {videoDir}");

            // HTML5 视频页面
            var videoUrl = $"https://{VIRTUAL_HOST}/{Uri.EscapeDataString(videoName)}";
            Debug.WriteLine($"[DailyVoice] 视频 URL: {videoUrl}");

            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='margin:0;background:#000;overflow:hidden;display:flex;align-items:center;justify-content:center;width:100vw;height:100vh'>
<video id='vid' src='{videoUrl}' autoplay playsinline
    style='max-width:100vw;max-height:100vh;object-fit:contain'
    onended='window.chrome.webview.postMessage(""ended"")'
    onerror='window.chrome.webview.postMessage(""err:""+document.getElementById(""vid"").error?.code)'
    onloadeddata='window.chrome.webview.postMessage(""loaded"")'
    onplay='window.chrome.webview.postMessage(""play"")'>
</video>
<div id='log' style='position:fixed;bottom:10px;left:10px;color:#0f0;font-size:12px;font-family:monospace;z-index:99'></div>
<script>
(function() {{
    function log(msg) {{
        var el = document.getElementById('log');
        el.textContent += msg + '\n';
        window.chrome.webview.postMessage('log:' + msg);
    }}
    log('HTML ready');
    var v = document.getElementById('vid');
    log('video element: ' + (v ? 'found' : 'NOT found'));
    log('video src: ' + v.getAttribute('src'));
    v.play().then(function() {{
        log('play() resolved');
        try {{ v.requestFullscreen().then(function() {{ log('fullscreen ok'); }}).catch(function(e) {{ log('fullscreen denied: '+e.message); }}); }} catch(e) {{ log('fullscreen error: '+e); }}
    }}).catch(function(err) {{
        log('play() FAILED: ' + err.name + ' — ' + err.message);
        window.chrome.webview.postMessage('err:' + err.message);
    }});
}})();
</script>
</body></html>";

            _webView.CoreWebView2.NavigateToString(html);
            Debug.WriteLine("[DailyVoice] HTML 已注入 WebView2");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] WebView2 初始化失败: {ex.GetType().Name} — {ex.Message}");
            Debug.WriteLine("[DailyVoice] 降级到系统默认播放器...");
            FallbackToSystemPlayer();
        }
    }

    private void OnWebMessage(object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = e.TryGetWebMessageAsString();
        Debug.WriteLine($"[DailyVoice] WebView2 → {msg}");

        if (msg == "ended")
        {
            _playing = false;
            this.BeginInvoke(() => this.Close());
        }
        else if (msg != null && msg.StartsWith("err:"))
        {
            var err = msg[4..];
            Debug.WriteLine($"[DailyVoice] 视频播放错误 → 降级系统播放器: {err}");
            _playing = false;
            // WebView2 播不了，降级到系统播放器
            this.BeginInvoke(() =>
            {
                this.Hide();
                FallbackToSystemPlayer();
                this.Close();
            });
        }
    }

    /// <summary>
    /// 降级方案：用系统默认播放器打开视频
    /// </summary>
    private void FallbackToSystemPlayer()
    {
        try
        {
            Debug.WriteLine($"[DailyVoice] Fallback: Process.Start(\"{_videoPath}\")");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = _videoPath,
                UseShellExecute = true
            });
            // 不等待退出 — 用户自己关播放器
            Debug.WriteLine($"[DailyVoice] 系统播放器已启动 (PID={process?.Id})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 系统播放器也失败: {ex.Message}");
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Debug.WriteLine("[DailyVoice] 用户按 Escape 关闭视频窗");
            this.Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _webView.Dispose();
        }
        base.Dispose(disposing);
    }
}
