using System.Diagnostics;

namespace ClaudeMaster;

partial class MainForm : Form
{
    private bool _isInstalling;
    private CancellationTokenSource? _installCts;

    public MainForm()
    {
        InitializeComponent();
        LoadConfigFromEnv();
    }

    // ═══════════════════════════════════════════
    // 环境检测
    // ═══════════════════════════════════════════

    private async void OnFormShown(object? sender, EventArgs e)
    {
        await DetectEnvironment();
    }

    private async Task DetectEnvironment()
    {
        // 检测 Node.js
        var nodeVersion = await RunAndCapture("node", "--version", 5000);
        if (nodeVersion != null)
        {
            _lblNodeStatus.Text = $"✅ 已安装 ({nodeVersion})";
            _lblNodeStatus.ForeColor = Color.Green;
            _lnkNodeInstall.Visible = false;
            _btnInstall.Enabled = true;
        }
        else
        {
            _lblNodeStatus.Text = "❌ 未安装";
            _lblNodeStatus.ForeColor = Color.Red;
            _lnkNodeInstall.Visible = true;
        }

        // 检测 Claude Code（多重策略）
        var claudeVersion = await DetectClaudeCode();
        if (claudeVersion != null)
        {
            _lblClaudeStatus.Text = $"✅ 已安装 ({claudeVersion})";
            _lblClaudeStatus.ForeColor = Color.Green;
        }
        else
        {
            _lblClaudeStatus.Text = "❌ 未安装";
            _lblClaudeStatus.ForeColor = Color.OrangeRed;
        }
    }

    /// <summary>
    /// 多重策略检测 Claude Code：--version → where → 环境变量 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    /// </summary>
    private async Task<string?> DetectClaudeCode()
    {
        // 策略 1: claude --version（走 cmd.exe /c，标准 PATH 解析）
        var version = await RunAndCapture("claude", "--version", 8000);
        if (version != null) return version;

        // 策略 2: where claude 确认是否在 PATH 中
        var whereResult = await RunAndCapture("where", "claude", 3000);
        if (whereResult != null)
        {
            // where 找到了但 --version 失败了？尝试用全路径再试一次
            var firstLine = whereResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (firstLine != null)
            {
                version = await RunAndCapture(firstLine, "--version", 5000);
                if (version != null) return version;
            }
        }

        // 策略 3: 检查 VS Code 扩展自带的环境变量 CLAUDE_CODE_EXECPATH
        var execPath = Environment.GetEnvironmentVariable("CLAUDE_CODE_EXECPATH");
        if (!string.IsNullOrEmpty(execPath) && File.Exists(execPath))
        {
            var result = await RunAndCapture(execPath, "--version", 5000);
            if (result != null) return result;
        }

        return null;
    }

    private void OnNodeInstallLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://nodejs.org/zh-cn/download/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开浏览器：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ═══════════════════════════════════════════
    // 安装 Claude Code
    // ═══════════════════════════════════════════

    private async void OnInstallClick(object? sender, EventArgs e)
    {
        if (_isInstalling) return;

        _isInstalling = true;
        _btnInstall.Enabled = false;
        _txtInstallLog.Clear();
        _installCts = new CancellationTokenSource();

        LogToInstallLog("开始安装 Claude Code...");
        LogToInstallLog("$ npm install -g @anthropic-ai/claude-code");
        LogToInstallLog("");

        try
        {
            await Task.Run(() => RunNpmInstall(_installCts.Token));
        }
        catch (OperationCanceledException)
        {
            LogToInstallLog("");
            LogToInstallLog("⚠ 安装已取消");
        }
        finally
        {
            _isInstalling = false;
            _btnInstall.Enabled = true;

            // 重新检测 Claude Code 版本
            LogToInstallLog("");
            var version = await DetectClaudeCode();
            if (version != null)
            {
                LogToInstallLog($"✅ Claude Code 安装成功！版本: {version}");
                _lblClaudeStatus.Text = $"✅ 已安装 ({version})";
                _lblClaudeStatus.ForeColor = Color.Green;
            }
            else
            {
                LogToInstallLog("❌ 安装后无法检测到 claude 命令，请检查 PATH 或重新打开终端");
            }
        }
    }

    private void RunNpmInstall(CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c npm install -g @anthropic-ai/claude-code",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = new Process { StartInfo = psi };

        var outputLock = new object();
        var exited = false;

        proc.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            if (ct.IsCancellationRequested && !exited) return;
            lock (outputLock)
                BeginInvoke(() => LogToInstallLog(e.Data));
        };

        proc.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            if (ct.IsCancellationRequested && !exited) return;
            lock (outputLock)
                BeginInvoke(() => LogToInstallLog(e.Data));
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // 等待完成或取消
        while (!proc.WaitForExit(200))
        {
            if (ct.IsCancellationRequested)
            {
                exited = true;
                proc.Kill(entireProcessTree: true);
                ct.ThrowIfCancellationRequested();
            }
        }

        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            BeginInvoke(() => LogToInstallLog($""));
            BeginInvoke(() => LogToInstallLog($"❌ npm 安装失败 (exit code: {proc.ExitCode})"));
        }
    }

    // ═══════════════════════════════════════════
    // API 配置
    // ═══════════════════════════════════════════

    private void LoadConfigFromEnv()
    {
        var baseUrl = Environment.GetEnvironmentVariable(
            "ANTHROPIC_BASE_URL", EnvironmentVariableTarget.User);
        var apiKey = Environment.GetEnvironmentVariable(
            "ANTHROPIC_API_KEY", EnvironmentVariableTarget.User);

        _txtBaseUrl.Text = !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl
            : "https://api.anthropic.com";

        _txtApiToken.Text = apiKey ?? "";
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        var baseUrl = _txtBaseUrl.Text.Trim();
        var apiKey = _txtApiToken.Text;

        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://api.anthropic.com";

        Environment.SetEnvironmentVariable(
            "ANTHROPIC_BASE_URL", baseUrl, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(
            "ANTHROPIC_API_KEY", apiKey, EnvironmentVariableTarget.User);

        LogToInstallLog("══════════════════════════════");
        LogToInstallLog("💾 配置已保存到用户环境变量");
        LogToInstallLog($"   ANTHROPIC_BASE_URL = {baseUrl}");
        LogToInstallLog($"   ANTHROPIC_API_KEY  = {(string.IsNullOrEmpty(apiKey) ? "(空)" : new string('*', Math.Min(apiKey.Length, 12)))}");
        LogToInstallLog("══════════════════════════════");
        LogToInstallLog("💡 提示：请重新启动终端 / IDE 使环境变量生效");

        MessageBox.Show(
            "配置已保存！\n\n请重新启动终端或 IDE 使环境变量生效。\n不需要重启电脑喵~",
            "ClaudeMaster",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async void OnTestClick(object? sender, EventArgs e)
    {
        _btnTest.Enabled = false;
        _txtInstallLog.Clear();

        LogToInstallLog("🔍 正在检测当前配置...");
        LogToInstallLog("");

        // 先显示当前配置
        var baseUrl = Environment.GetEnvironmentVariable(
            "ANTHROPIC_BASE_URL", EnvironmentVariableTarget.User);
        var apiKey = Environment.GetEnvironmentVariable(
            "ANTHROPIC_API_KEY", EnvironmentVariableTarget.User);

        LogToInstallLog($"  ANTHROPIC_BASE_URL = {baseUrl ?? "(未设置)"}");
        LogToInstallLog($"  ANTHROPIC_API_KEY  = {(string.IsNullOrEmpty(apiKey) ? "(未设置)" : "***已设置***")}");
        LogToInstallLog("");

        // 验证 Claude Code CLI
        var version = await DetectClaudeCode();
        if (version != null)
        {
            LogToInstallLog($"✅ Claude Code CLI 就绪 (v{version})");

            // 验证 API 连通性：检查 claude 是否有 --api-key 之类的参数
            LogToInstallLog("");
            LogToInstallLog("💡 完整的 API 连通性验证请在终端运行：");
            LogToInstallLog("   claude --version");
            LogToInstallLog("   或直接启动 claude 对话测试");
        }
        else
        {
            LogToInstallLog("❌ 未检测到 Claude Code CLI");
            LogToInstallLog("   请先点击「安装 / 更新 Claude Code」按钮");
        }

        _btnTest.Enabled = true;
    }

    // ═══════════════════════════════════════════
    // 通用工具方法
    // ═══════════════════════════════════════════

    /// <summary>
    /// 运行命令并捕获 stdout。Windows 下统一走 cmd.exe /c 确保 PATH/bat 正确解析 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    /// </summary>
    private async Task<string?> RunAndCapture(string fileName, string args, int timeoutMs)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Windows 下 claude/npm 等是 .cmd 批处理脚本，
                // UseShellExecute=false 直接调 CreateProcess 可能找不到或执行失败，
                // 统一走 cmd.exe /c 确保和终端行为一致
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {fileName} {args}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                var output = new System.Text.StringBuilder();
                proc.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };

                proc.BeginOutputReadLine();

                if (!proc.WaitForExit(timeoutMs))
                {
                    proc.Kill();
                    return null;
                }

                return proc.ExitCode == 0
                    ? output.ToString().Trim()
                    : null;
            }
            catch
            {
                return null;
            }
        });
    }

    private void LogToInstallLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => LogToInstallLog(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _txtInstallLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        _txtInstallLog.SelectionStart = _txtInstallLog.TextLength;
        _txtInstallLog.ScrollToCaret();
    }

    // ═══════════════════════════════════════════
    // 系统托盘 & 窗口生命周期
    // ═══════════════════════════════════════════

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            _notifyIcon.Visible = true;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;

        // 关闭 = 最小化到托盘
        e.Cancel = true;
        Hide();
        _notifyIcon.Visible = true;
    }

    private void OnShowFromTray(object? sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        _notifyIcon.Visible = false;
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _installCts?.Cancel();
        _notifyIcon.Visible = false;
        FormClosing -= OnFormClosing;
        Application.Exit();
    }
}
