using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Yoink;

public partial class MainForm : Form
{
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private readonly Config _config;
    private CancellationTokenSource? _downloadCts;
    private Process? _currentProcess;

    public MainForm()
    {
        InitializeComponent();

        _config = ConfigManager.Load();
        ApplyConfigToUi();

        // 默认输出目录
        if (string.IsNullOrEmpty(txtOutputDir.Text))
            txtOutputDir.Text = Path.Combine(BaseDir, "downloads");
    }

    // ═══════════════════════════════════════
    //  UI 操作
    // ═══════════════════════════════════════

    private void ApplyConfigToUi()
    {
        rbVideo.Checked = !_config.AudioOnly;
        rbAudio.Checked = _config.AudioOnly;
        txtOutputDir.Text = _config.OutputDir;

        var qIdx = _config.DefaultQuality switch
        {
            "1080" => 1, "720" => 2, "480" => 3, "360" => 4, _ => 0
        };
        cbQuality.SelectedIndex = qIdx >= 0 && qIdx < cbQuality.Items.Count ? qIdx : 0;
    }

    private void OnPaste(object? sender, EventArgs e)
    {
        try
        {
            var text = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(text))
                txtUrl.Text = text.Trim();
        }
        catch { /* clipboard failure */ }
    }

    private void OnBrowseOutput(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "选择下载保存目录",
            InitialDirectory = Directory.Exists(txtOutputDir.Text)
                ? txtOutputDir.Text : BaseDir
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            txtOutputDir.Text = dlg.SelectedPath;
    }

    // ═══════════════════════════════════════
    //  下载引擎
    // ═══════════════════════════════════════

    private async void OnDownload(object? sender, EventArgs e)
    {
        var url = txtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("请先粘贴一个视频链接喵~", "Yoink",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 查找 yt-dlp
        var ytDlpPath = ResolvePath(_config.YtDlpPath, "yt-dlp.exe");
        if (ytDlpPath == null)
        {
            MessageBox.Show("找不到 yt-dlp.exe！\n\n请在 C:\\Software\\tydlp\\ 放置 yt-dlp.exe\n或修改 config.json 指定路径。", "Yoink",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 查找 ffmpeg（音视频合并/提取需要）
        var ffmpegPath = ResolvePath(_config.FfmpegPath, "ffmpeg.exe");

        // 确保输出目录存在
        var outputDir = txtOutputDir.Text.Trim();
        if (string.IsNullOrEmpty(outputDir))
            outputDir = Path.Combine(BaseDir, "downloads");
        Directory.CreateDirectory(outputDir);

        // 构建命令行
        var isAudio = rbAudio.Checked;
        var qIdx = cbQuality.SelectedIndex;

        var outputTemplate = Path.Combine(outputDir, "%(title).200s.%(ext)s");

        // 用 ArgumentList 逐条传参，自动转义 & < > 等特殊字符
        var argList = new List<string>();

        // 格式选择 — 最佳画质时不传 -f，让 yt-dlp 自动选最优格式合并
        if (isAudio)
        {
            argList.AddRange(["-f", "bestaudio", "-x", "--audio-format", "mp3", "--audio-quality", "0"]);
        }
        else if (qIdx >= 1)
        {
            var height = qIdx switch { 1 => 1080, 2 => 720, 3 => 480, 4 => 360, _ => 1080 };
            argList.AddRange(["-f", $"bestvideo[height<={height}]+bestaudio/best[height<={height}]"]);
        }
        // qIdx == 0 (最佳): 不传 -f，yt-dlp 默认自动选最优

        // 视频模式：让 yt-dlp 合并最优视频+音频流为 mp4
        if (!isAudio)
            argList.AddRange(["--merge-output-format", "mp4"]);

        // 指定 ffmpeg 位置
        if (ffmpegPath != null)
            argList.AddRange(["--ffmpeg-location", Path.GetDirectoryName(ffmpegPath)!]);

        // 禁用 JS 运行时检测 — 避免 deno 架构不匹配弹窗
        argList.AddRange(["--js-runtimes", "none"]);

        argList.AddRange(["-o", outputTemplate, "--no-playlist", "--progress", "--newline", url]);

        Log($"▶ Yoink! {url}");
        Log($"⚙ yt-dlp: {ytDlpPath}");
        Log($"📂 输出: {outputDir}");
        Log($"🎬 模式: {(isAudio ? "仅音频" : $"视频 ({cbQuality.SelectedItem})")}");
        Log($"🔧 ffmpeg: {(ffmpegPath ?? "未找到")}");
        Log($"📋 参数: yt-dlp {string.Join(" ", argList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}");

        // 保存配置
        _config.AudioOnly = isAudio;
        _config.OutputDir = outputDir;
        _config.DefaultQuality = qIdx switch { 1 => "1080", 2 => "720", 3 => "480", 4 => "360", _ => "best" };
        ConfigManager.Save(_config);

        // UI 切换下载模式
        SetDownloading(true);
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        try
        {
            await Task.Run(() => RunDownload(ytDlpPath, argList, ffmpegPath, token), token);
        }
        catch (OperationCanceledException)
        {
            Log("⏹ 下载已取消");
        }
        catch (Exception ex)
        {
            Log($"❌ 下载出错: {ex.Message}");
        }
        finally
        {
            SetDownloading(false);
        }
    }

    private void RunDownload(string ytDlpPath, List<string> argList, string? ffmpegPath,
        CancellationToken token)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        foreach (var a in argList)
            psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        _currentProcess = proc;

        // 注册取消回调
        using var reg = token.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(true);
            }
            catch { /* process already exited */ }
        });

        // 异步读取输出
        var progressRegex = new Regex(@"\[download\]\s+(\d+\.?\d*)%");
        var etaRegex = new Regex(@"ETA\s+(\S+)");
        var speedRegex = new Regex(@"at\s+([\d.]+)(MiB|KiB|GiB)/s");

        proc.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            var line = e.Data;

            this.BeginInvoke(() => Log(line));

            // 解析进度
            var m = progressRegex.Match(line);
            if (m.Success && double.TryParse(m.Groups[1].Value, out var pct))
            {
                var eta = "";
                var em = etaRegex.Match(line);
                if (em.Success) eta = $" ETA {em.Groups[1].Value}";

                var speed = "";
                var sm = speedRegex.Match(line);
                if (sm.Success) speed = $" {sm.Groups[1].Value} {sm.Groups[2].Value}/s";

                this.BeginInvoke(() =>
                {
                    pbDownload.Value = (int)Math.Min(100, pct);
                    lblProgress.Text = $"📥 {pct:F1}%{eta}";
                    lblSpeed.Text = $"⚡{speed}";
                });
            }
            else if (line.Contains("[ExtractAudio]") || line.Contains("[Merger]"))
            {
                this.BeginInvoke(() => lblProgress.Text = $"🔧 {line.Trim()}");
            }
            else if (line.Contains("has already been downloaded"))
            {
                this.BeginInvoke(() =>
                {
                    lblProgress.Text = "✅ 文件已存在，跳过";
                    lblProgress.ForeColor = Color.DarkGreen;
                });
            }
        };

        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                this.BeginInvoke(() => Log($"⚠ {e.Data}"));
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        // 捕获退出码再跨线程回调 — proc 在 using 块结束前会被 Dispose
        int exitCode = proc.ExitCode;

        // 更新 UI 完成状态
        this.BeginInvoke(() =>
        {
            if (exitCode == 0)
            {
                pbDownload.Value = 100;
                lblProgress.Text = "✅ 下载完成！";
                lblProgress.ForeColor = Color.DarkGreen;
                lblSpeed.Text = "";
                Log("✅ 下载完成！");
            }
            else if (!token.IsCancellationRequested)
            {
                lblProgress.Text = $"❌ 错误 (exit={exitCode})";
                lblProgress.ForeColor = Color.DarkRed;
                Log($"❌ yt-dlp 退出码: {exitCode}");
            }
        });

        _currentProcess = null;
    }

    private void OnCancel(object? sender, EventArgs e)
    {
        _downloadCts?.Cancel();
        Log("⏹ 正在取消...");
    }

    // ═══════════════════════════════════════
    //  工具方法
    // ═══════════════════════════════════════

    /// <summary>
    /// 解析工具路径：优先配置值 → 程序目录 → 固定路径 → PATH
    /// </summary>
    private static string? ResolvePath(string? configuredPath, string exeName)
    {
        // 1. 配置的路径
        if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        // 2. 与 exe 同目录
        var local = Path.Combine(BaseDir, exeName);
        if (File.Exists(local)) return local;

        // 3. usecase/ 子目录（便携部署首选）
        local = Path.Combine(BaseDir, "usecase", exeName);
        if (File.Exists(local)) return local;

        // 4. tools/ 子目录（旧兼容）
        local = Path.Combine(BaseDir, "tools", exeName);
        if (File.Exists(local)) return local;

        // 5. 固定路径 C:\Software\tydlp\
        local = Path.Combine(@"C:\Software\tydlp", exeName);
        if (File.Exists(local)) return local;

        // 5. PATH 环境变量
        return FindInPath(exeName);
    }

    private static string? FindInPath(string exeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var exts = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? [".exe"];
        foreach (var dir in pathEnv.Split(';'))
            foreach (var ext in exts)
            {
                var full = Path.Combine(dir.Trim(), exeName);
                full = Path.ChangeExtension(full, ext.Trim());
                if (File.Exists(full)) return full;
            }
        return null;
    }

    private void SetDownloading(bool downloading)
    {
        btnDownload.Enabled = !downloading;
        btnCancel.Enabled = downloading;
        txtUrl.ReadOnly = downloading;
        cbQuality.Enabled = !downloading;
        rbVideo.Enabled = !downloading;
        rbAudio.Enabled = !downloading;

        if (!downloading)
        {
            pbDownload.Value = 0;
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        // 自动滚动到底部
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            var result = MessageBox.Show("下载还在进行中，确定要退出吗？",
                "Yoink", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        _downloadCts?.Cancel();
        ConfigManager.Save(_config);
    }
}
