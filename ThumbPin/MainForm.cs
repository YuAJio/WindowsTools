using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ThumbPin;

public partial class MainForm : Form
{
    // 已被置顶的窗口句柄 → 对应的图钉标记
    private readonly Dictionary<IntPtr, OverlayIcon> _pinned = [];

    // 热键 ID
    private const int HOTKEY_PIN = 1;

    // 鼠标钩子（捕获模式）
    private IntPtr _mouseHook = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _hookProc;

    public MainForm()
    {
        InitializeComponent();
        RegisterHotKey();
    }

    // ═══════════════════════════════════════
    //  核心：置顶切换
    // ═══════════════════════════════════════

    private void TogglePin(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        if (NativeMethods.IsTopMost(hWnd))
        {
            // 取消置顶
            NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_NOTOPMOST,
                0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);
            if (_pinned.TryGetValue(hWnd, out var overlay))
            {
                overlay.Dispose();
                _pinned.Remove(hWnd);
            }
            UpdateStatus($"已取消置顶", Color.DimGray);
        }
        else
        {
            // 置顶
            NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);
            var overlay = new OverlayIcon(hWnd);
            _pinned.Add(hWnd, overlay);
            var title = GetWindowTitle(hWnd);
            UpdateStatus($"✅ 已置顶: {(string.IsNullOrEmpty(title) ? "未知窗口" : title)}", Color.DarkGreen);
        }

        UpdatePinnedCount();
    }

    private void ToggleForegroundWindow()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero || hWnd == this.Handle) return;
        TogglePin(hWnd);
    }

    private void UnpinAll()
    {
        foreach (var (hWnd, overlay) in _pinned.ToArray())
        {
            if (NativeMethods.IsTopMost(hWnd))
            {
                NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_NOTOPMOST,
                    0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);
            }
            overlay.Dispose();
        }
        _pinned.Clear();
        UpdateStatus("已取消全部置顶", Color.DimGray);
        UpdatePinnedCount();
    }

    // ═══════════════════════════════════════
    //  捕获模式 — 低级鼠标钩子
    // ═══════════════════════════════════════

    private void OnCapture(object? sender, EventArgs e)
    {
        if (_mouseHook != IntPtr.Zero) return;

        btnCapture.Text = "⏳ 请点击目标窗口...";
        btnCapture.Enabled = false;
        this.Cursor = Cursors.Cross;

        _hookProc = MouseHookCallback;

        // 低级钩子 hMod 传 IntPtr.Zero 也可以，用当前进程模块
        _mouseHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            IntPtr.Zero,
            0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // WM_LBUTTONDOWN = 0x0201, WM_RBUTTONDOWN = 0x0204
        var msg = (int)wParam;
        if (nCode >= 0 && msg == 0x0201)
        {
            var ms = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            var hWnd = NativeMethods.WindowFromPoint(ms.pt.x, ms.pt.y);

            if (hWnd != this.Handle && hWnd != IntPtr.Zero)
            {
                TogglePin(hWnd);
            }

            // 恢复 UI
            BeginInvoke(() =>
            {
                btnCapture.Text = "🎯 捕获窗口并置顶";
                btnCapture.Enabled = true;
                this.Cursor = Cursors.Default;
            });

            ReleaseMouseHook();
            return (IntPtr)1; // 吃掉点击
        }

        // 右键取消捕获
        if (nCode >= 0 && msg == 0x0204)
        {
            BeginInvoke(() =>
            {
                btnCapture.Text = "🎯 捕获窗口并置顶";
                btnCapture.Enabled = true;
                this.Cursor = Cursors.Default;
                UpdateStatus("已取消", Color.DimGray);
            });
            ReleaseMouseHook();
            return (IntPtr)1;
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void ReleaseMouseHook()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
        _hookProc = null;
    }

    // ═══════════════════════════════════════
    //  全局热键 Ctrl+Shift+F7
    // ═══════════════════════════════════════

    private void RegisterHotKey()
    {
        if (!NativeMethods.RegisterHotKey(this.Handle, HOTKEY_PIN,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_F7))
        {
            MessageBox.Show("Ctrl+Shift+F7 热键注册失败（可能被其他程序占用）",
                "ThumbPin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            if (m.WParam.ToInt32() == HOTKEY_PIN)
                ToggleForegroundWindow();
        }
        base.WndProc(ref m);
    }

    // ═══════════════════════════════════════
    //  UI 辅助
    // ═══════════════════════════════════════

    private void UpdateStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(text, color));
            return;
        }
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    private void UpdatePinnedCount()
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdatePinnedCount());
            return;
        }
        lblPinnedCount.Text = $"已置顶: {_pinned.Count} 个窗口";
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var len = NativeMethods.GetWindowTextLength(hWnd);
        if (len == 0) return "";
        var sb = new System.Text.StringBuilder(len + 1);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // ═══════════════════════════════════════
    //  窗口生命周期
    // ═══════════════════════════════════════

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
            this.Hide();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        this.Hide();
        e.Cancel = true;
    }

    private void OnShowFromTray(object? sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        // 退出前取消所有置顶
        UnpinAll();
        ReleaseMouseHook();
        NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_PIN);
        notifyIcon.Visible = false;
        this.FormClosing -= OnFormClosing;
        Application.Exit();
    }

    // 关闭托盘再退出
    public void CleanExit()
    {
        OnExit(this, EventArgs.Empty);
    }
}
