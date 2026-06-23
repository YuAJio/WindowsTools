using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DailyVoice;

/// <summary>
/// 全屏无边框视频播放窗体 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 使用 WebView2 + HTML5 video + VirtualHostName 映射播放本地视频文件。
/// 播放结束自动关闭，Escape 键手动关闭。
/// 失败静默关闭，不弹错误框。
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _videoPath;

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
        try
        {
            await _webView.EnsureCoreWebView2Async();

            // 注册 JS → C# 消息通道
            _webView.CoreWebView2.WebMessageReceived += OnWebMessage;

            // 用 VirtualHostName 映射视频目录，绕开 file:/// 安全限制
            var videoDir = Path.GetDirectoryName(_videoPath)!;
            var videoName = Path.GetFileName(_videoPath);
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VIRTUAL_HOST, videoDir, CoreWebView2HostResourceAccessKind.Allow);

            // HTML5 全屏视频页面 — 通过虚拟主机加载本地文件
            var videoUrl = $"https://{VIRTUAL_HOST}/{Uri.EscapeDataString(videoName)}";
            Debug.WriteLine($"DailyVoice 播放视频: {videoUrl}");

            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='margin:0;background:#000;overflow:hidden;display:flex;align-items:center;justify-content:center;width:100vw;height:100vh'>
<video id='vid' src='{videoUrl}' autoplay playsinline
    style='max-width:100vw;max-height:100vh;object-fit:contain'
    onended='window.chrome.webview.postMessage(""video-ended"")'
    onerror='window.chrome.webview.postMessage(""video-error:""+(document.getElementById(""vid"").error?.code||""unknown""))'
    onloadeddata='window.chrome.webview.postMessage(""video-loaded"")'>
</video>
<script>
(function() {{
    var v = document.getElementById('vid');
    v.play().then(function() {{
        window.chrome.webview.postMessage('video-playing');
        try {{ v.requestFullscreen(); }} catch(e) {{}}
    }}).catch(function(err) {{
        window.chrome.webview.postMessage('video-error:' + err.message);
    }});
}})();
</script>
</body></html>";

            _webView.CoreWebView2.NavigateToString(html);
        }
        catch (Exception ex)
        {
            // WebView2 初始化失败 → 静默关闭
            Debug.WriteLine($"DailyVoice WebView2 初始化失败: {ex.Message}");
            this.BeginInvoke(() => this.Close());
        }
    }

    private void OnWebMessage(object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = e.TryGetWebMessageAsString();
        Debug.WriteLine($"DailyVoice WebView2 message: {msg}");

        if (msg == "video-ended")
        {
            this.BeginInvoke(() => this.Close());
        }
        else if (msg != null && msg.StartsWith("video-error:"))
        {
            Debug.WriteLine($"DailyVoice 视频播放错误: {msg}");
            this.BeginInvoke(() => this.Close());
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
            this.Close();
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
