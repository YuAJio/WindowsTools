namespace ThumbPin;

public partial class MainForm : Form
{
    // 已被置顶的窗口句柄 → 对应的图钉标记
    private readonly Dictionary<IntPtr, OverlayIcon> _pinned = [];

    private const int HOTKEY_PIN = 1;

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
            UpdateStatus("已取消置顶", Color.DimGray);
        }
        else
        {
            // 置顶
            NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);
            var overlay = new OverlayIcon(hWnd);
            _pinned[hWnd] = overlay;
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
        UnpinAll();
        NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_PIN);
        notifyIcon.Visible = false;
        this.FormClosing -= OnFormClosing;
        Application.Exit();
    }
}
