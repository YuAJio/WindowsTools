using System.Diagnostics;
using System.Windows.Forms;

namespace DailyVoice;

/// <summary>
/// 全屏视频播放窗体 — AxHost WMP 嵌入方案 (⁎⁍̴̛ᴗ⁍̴̛⁎)
///
/// 架构：AxHost 内嵌 Windows Media Player ActiveX
///   - 无边框最大化窗体 = 全屏
///   - WMP 控件 Dock.Fill 填满
///   - uiMode = "none" 隐藏播放控件
///   - 轮询 playState → MediaEnded → 自动关闭
///
/// 零外部依赖（WMP 是 Windows 系统组件）
/// </summary>
internal sealed class VideoPlayerForm : Form
{
    private readonly string _videoPath;
    private readonly System.Windows.Forms.Timer _monitor;
    private WmpHost? _wmpHost;

    public VideoPlayerForm(string videoPath)
    {
        _videoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

        this.Text = "";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.BackColor = Color.Black;
        this.KeyPreview = true;
        this.KeyDown += OnKeyDown;
        this.Load += OnLoad;
        this.FormClosing += OnFormClosing;

        _monitor = new System.Windows.Forms.Timer { Interval = 500 };
        _monitor.Tick += OnMonitorTick;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[DailyVoice] VideoPlayerForm.OnLoad — {_videoPath}");

        try
        {
            // 创建 WMP ActiveX 宿主
            _wmpHost = new WmpHost();
            ((System.ComponentModel.ISupportInitialize)_wmpHost).BeginInit();
            _wmpHost.Dock = DockStyle.Fill;
            this.Controls.Add(_wmpHost);
            ((System.ComponentModel.ISupportInitialize)_wmpHost).EndInit();

            // 获取 WMP 对象并配置
            dynamic player = _wmpHost.Player;
            player.uiMode = "none";
            player.enableContextMenu = false;
            player.stretchToFit = true;
            player.settings.volume = 100;
            player.URL = _videoPath;

            Debug.WriteLine("[DailyVoice] WMP ActiveX 已嵌入，开始播放...");
            _monitor.Start();
            player.Ctlcontrols.play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] WMP 初始化失败: {ex.Message}");
            CloseWithFallback();
        }
    }

    private void OnMonitorTick(object? sender, EventArgs e)
    {
        try
        {
            if (_wmpHost == null) return;

            dynamic player = _wmpHost.Player;
            int state = player.playState;

            // state 3 = WMPPlayState.wmppsPlaying
            if (state == 3)
            {
                Debug.WriteLine("[DailyVoice] WMP 播放中");
            }
            // state 8 = WMPPlayState.wmppsMediaEnded
            // state 1 = WMPPlayState.wmppsStopped
            if (state == 8 || state == 1)
            {
                Debug.WriteLine($"[DailyVoice] WMP 播放结束 (state={state})");
                _monitor.Stop();
                this.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyVoice] 播放监控异常: {ex.Message}");
            _monitor.Stop();
            CloseWithFallback();
        }
    }

    private void CloseWithFallback()
    {
        try
        {
            Debug.WriteLine("[DailyVoice] 降级到系统播放器");
            Process.Start(new ProcessStartInfo
            {
                FileName = _videoPath,
                UseShellExecute = true
            });
        }
        catch { /* last resort */ }
        this.Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
            this.Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _monitor.Stop();
        _monitor.Dispose();
    }
}

/// <summary>
/// AxHost 子类 — 包装 Windows Media Player ActiveX 控件
/// CLSID: {6BF52A50-394A-11d3-B153-00C04F79FAA6}
/// </summary>
internal sealed class WmpHost : AxHost
{
    private const string WMP_CLSID = "6BF52A50-394A-11d3-B153-00C04F79FAA6";

    public WmpHost() : base(WMP_CLSID) { }

    /// <summary>
    /// 获取底层 WMP COM 对象 (dynamic 便于调用属性和方法)
    /// </summary>
    public dynamic Player => GetOcx();
}
