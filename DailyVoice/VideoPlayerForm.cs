using System.Diagnostics;
using System.Net;
using System.Text;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放窗体 — 终极方案 (⁎⁍̴̛ᴗ⁍̴̛⁎)
///
/// 架构：本地 HTTP 服务器 + Edge 浏览器 Kiosk 模式
///   1. HttpListener 监听 localhost 随机端口
///   2. 服务 HTML 页面（含 &lt;video&gt; 标签）+ 视频文件流
///   3. Edge --kiosk 全屏打开页面
///   4. JS 在视频结束时 fetch /done → C# 收到信号 → 关闭 Edge + 自身
///
/// 优势：零外部依赖（Edge 是 Win11 内置）、全屏可靠、自动关闭精准
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly string _videoPath;
    private HttpListener? _listener;
    private Process? _edgeProcess;
    private bool _closing;

    public VideoPlayerForm(string videoPath)
    {
        _videoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

        this.Text = "";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.BackColor = Color.Black;
        this.Load += OnLoad;
        this.FormClosing += OnFormClosing;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        // 隐藏自身后启动 HTTP 服务
        this.Hide();
        Task.Run(() => StartServer());
    }

    private void StartServer()
    {
        try
        {
            // 随机端口避免冲突
            _listener = new HttpListener();
            var port = FindFreePort();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            Debug.WriteLine($"[DailyVoice] HTTP 服务器启动: http://localhost:{port}/");

            // 启动 Edge Kiosk 模式
            var url = $"http://localhost:{port}/";
            var userDataDir = Path.Combine(Path.GetTempPath(), "dailyvoice_edge");
            // 清理旧的 user data 避免 Edge 报错
            try { if (Directory.Exists(userDataDir)) Directory.Delete(userDataDir, true); }
            catch { /* 可能被占用，忽略 */ }

            try
            {
                _edgeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "msedge",
                    Arguments = $"--kiosk \"{url}\" --user-data-dir=\"{userDataDir}\" --no-first-run --edge-kiosk-mode-fullscreen --no-error-dialogs",
                    UseShellExecute = false,
                    CreateNoWindow = false
                });
                Debug.WriteLine($"[DailyVoice] Edge Kiosk 已启动 (PID={_edgeProcess?.Id})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DailyVoice] Edge 启动失败: {ex.Message}，尝试默认浏览器");
                try
                {
                    _edgeProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { /* 最后兜底 */ }
            }

            // 处理 HTTP 请求
            while (!_closing && _listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    Task.Run(() => HandleRequest(ctx));
                }
                catch (HttpListenerException)
                {
                    // listener 被关闭
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Debug.WriteLine("[DailyVoice] HTTP 服务器退出");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 服务器异常: {ex.Message}");
            CleanupAndClose();
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url!.AbsolutePath;
            Debug.WriteLine($"[DailyVoice] HTTP 请求: {path}");

            switch (path)
            {
                case "/":
                    ServeHtml(ctx);
                    break;
                case "/video":
                    ServeVideo(ctx);
                    break;
                case "/done":
                    ServeDone(ctx);
                    break;
                default:
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 请求处理异常: {ex.Message}");
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// 服务 HTML 播放页面 — 黑色背景 + 居中视频 + JS 完成信号
    /// </summary>
    private void ServeHtml(HttpListenerContext ctx)
    {
        var html = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>DailyVoice Video</title>
<style>
* { margin:0; padding:0; }
body { background:#000; display:flex; align-items:center; justify-content:center; width:100vw; height:100vh; overflow:hidden; }
video { max-width:100vw; max-height:100vh; object-fit:contain; outline:none; }
</style></head>
<body>
<video id='v' src='/video' autoplay playsinline></video>
<script>
(function() {
    var v = document.getElementById('v');
    function log(m) { console.log('[DV] ' + m); }
    log('video element ready');
    v.onloadeddata = function() { log('loadeddata, duration=' + v.duration); };
    v.onplay = function() { log('playing'); try { v.requestFullscreen().then(function(){ log('fullscreen ok'); }).catch(function(e){ log('fullscreen: '+e.message); }); } catch(ex) { log('fullscreen error: '+ex); } };
    v.onended = function() { log('ended → signaling server'); fetch('/done').then(function(){ log('done signal sent'); }).catch(function(e){ log('done signal FAILED: '+e.message); }); };
    v.onerror = function() { log('ERROR code=' + (v.error? v.error.code : '?')); };
    v.play().then(function() { log('play() ok'); }).catch(function(e) { log('play() FAILED: ' + e.message); });
})();
</script>
</body></html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = buffer.Length;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.Close();
    }

    /// <summary>
    /// 流式传输视频文件
    /// </summary>
    private void ServeVideo(HttpListenerContext ctx)
    {
        try
        {
            var fileInfo = new FileInfo(_videoPath);
            if (!fileInfo.Exists)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            // 根据扩展名设置 MIME 类型
            var ext = Path.GetExtension(_videoPath).ToLowerInvariant();
            var mime = ext switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                _ => "video/mp4"
            };

            ctx.Response.ContentType = mime;
            ctx.Response.ContentLength64 = fileInfo.Length;

            using var fs = File.OpenRead(_videoPath);
            fs.CopyTo(ctx.Response.OutputStream);
            ctx.Response.Close();

            Debug.WriteLine($"[DailyVoice] 视频已流完 ({fileInfo.Length} bytes)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 视频流异常: {ex.Message}");
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// JS 发来播放完成信号 → 清理并关闭
    /// </summary>
    private void ServeDone(HttpListenerContext ctx)
    {
        Debug.WriteLine("[DailyVoice] 收到视频完成信号！");
        ctx.Response.StatusCode = 200;
        ctx.Response.Close();

        // 在主线程上清理
        this.BeginInvoke(() => CleanupAndClose());
    }

    private void CleanupAndClose()
    {
        if (_closing) return;
        _closing = true;

        Debug.WriteLine("[DailyVoice] 清理资源...");

        // 关闭 Edge
        try
        {
            if (_edgeProcess != null && !_edgeProcess.HasExited)
            {
                _edgeProcess.Kill();
                _edgeProcess.Dispose();
            }
        }
        catch { /* ignore */ }

        // 停止 HTTP 服务器
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch { /* ignore */ }

        // 关闭窗体
        try
        {
            if (!this.IsDisposed)
                this.Close();
        }
        catch { /* ignore */ }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        CleanupAndClose();
    }

    /// <summary>
    /// 找一个空闲端口
    /// </summary>
    private static int FindFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
