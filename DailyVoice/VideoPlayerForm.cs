using System.Diagnostics;
using Microsoft.Web.WebView2.WinForms;

namespace DailyVoice;

/// <summary>
/// 全屏无边框视频播放窗体 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 使用 WebView2 + HTML5 video 播放本地视频文件。
/// 播放结束自动关闭，Escape 键手动关闭。
/// 失败静默关闭，不弹错误框。
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _videoPath;

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

            // HTML5 全屏视频页面
            var escapedPath = _videoPath.Replace("\\", "\\\\").Replace("'", "\\'");
            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='margin:0;background:#000;overflow:hidden;display:flex;align-items:center;justify-content:center;width:100vw;height:100vh'>
<video id='vid' src='file:///{escapedPath}' autoplay
    style='max-width:100vw;max-height:100vh;object-fit:contain'
    onended='window.chrome.webview.postMessage(""video-ended"")'
    onerror='window.chrome.webview.postMessage(""video-error:""+(document.getElementById(""vid"").error?.code||""unknown""))'>
</video>
<script>
(function() {{
    var v = document.getElementById('vid');
    v.play().catch(function(err) {{
        window.chrome.webview.postMessage('video-error:' + err.message);
    }});
    try {{ v.requestFullscreen(); }} catch(e) {{}}
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
        Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = e.TryGetWebMessageAsString();
        if (msg == "video-ended")
        {
            // WebMessageReceived 在非 UI 线程触发，用 BeginInvoke 回 UI 线程关闭
            this.BeginInvoke(() => this.Close());
        }
        else if (msg != null && msg.StartsWith("video-error:"))
        {
            // 静默关闭，不弹框
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
