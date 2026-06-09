using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ThumbPin;

/// <summary>
/// 被置顶窗口左上角的图钉标记 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 透明分层窗口 + 点击穿透，跟随目标窗口移动。
/// </summary>
internal class OverlayIcon : IDisposable
{
    private readonly IntPtr _targetHwnd;
    private readonly Form _overlayForm;
    private readonly System.Windows.Forms.Timer _followTimer;
    private readonly IntPtr _winEventHook;
    private readonly NativeMethods.WinEventDelegate _winEventProc;

    private const int SIZE = 24;
    private bool _disposed;

    public OverlayIcon(IntPtr targetHwnd)
    {
        _targetHwnd = targetHwnd;

        // 创建透明分层窗口
        _overlayForm = new Form
        {
            Size = new Size(SIZE, SIZE),
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            TopMost = true,
            StartPosition = FormStartPosition.Manual,
            BackColor = Color.Magenta,
            TransparencyKey = Color.Magenta,
            AllowTransparency = true,
        };

        // 取消窗口的 WS_EX_TOOLWINDOW，让其拥有 WS_EX_TRANSPARENT 穿透点击
        SetTransparentClickThrough(_overlayForm.Handle);

        // 用 GDI+ 画图钉图标
        _overlayForm.Paint += DrawThumbPin;
        PositionOverlay();

        _overlayForm.Show();

        // 窗口位置变化监听（SetWinEventHook）
        _winEventProc = WinEventCallback;
        _winEventHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            NativeMethods.EVENT_OBJECT_DESTROY,
            IntPtr.Zero,
            _winEventProc,
            (uint)Environment.ProcessId,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        // 备用定时器（200ms 兜底）
        _followTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _followTimer.Tick += (_, _) =>
        {
            if (!IsTargetAlive())
            {
                Dispose();
                return;
            }
            PositionOverlay();
        };
        _followTimer.Start();
    }

    private void WinEventCallback(IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == _targetHwnd && idObject == 0)
        {
            if (eventType == NativeMethods.EVENT_OBJECT_DESTROY)
            {
                Dispose();
            }
            else
            {
                PositionOverlay();
            }
        }
    }

    private void PositionOverlay()
    {
        if (_disposed) return;

        if (NativeMethods.GetWindowRect(_targetHwnd, out var rect))
        {
            // 放在标题栏左上角
            int x = rect.Left + 4;
            int y = rect.Top + 4;

            if (_overlayForm.Location.X != x || _overlayForm.Location.Y != y)
            {
                // 跨线程安全
                if (_overlayForm.InvokeRequired)
                    _overlayForm.Invoke(() => _overlayForm.Location = new Point(x, y));
                else
                    _overlayForm.Location = new Point(x, y);
            }
        }
    }

    private void DrawThumbPin(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(Color.FromArgb(220, 46, 46)); // 红图钉色
        using var pen = new Pen(Color.FromArgb(180, 30, 30), 1.5f);
        using var highlight = new SolidBrush(Color.FromArgb(100, 255, 255, 255));

        var rect = new RectangleF(3, 2, 16, 12);

        // 图钉主体（圆角矩形）
        FillRoundedRect(g, brush, rect, 3);

        // 高光斜线
        g.DrawLine(new Pen(highlight, 1.5f), 6, 4, 14, 9);

        // 尖端（倒三角）
        var tip = new PointF[]
        {
            new(rect.X + rect.Width / 2 - 3, rect.Bottom),
            new(rect.X + rect.Width / 2 + 3, rect.Bottom),
            new(rect.X + rect.Width / 2, rect.Bottom + 6)
        };
        g.FillPolygon(brush, tip);
        g.DrawPolygon(pen, tip);
    }

    private static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, int radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    private bool IsTargetAlive()
    {
        return IsWindow(_targetHwnd);
    }

    // ═══════════════════════════════════════
    //  点击穿透
    // ═══════════════════════════════════════

    private static void SetTransparentClickThrough(IntPtr hWnd)
    {
        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LAYERED = 0x00080000;
        const uint WS_EX_TRANSPARENT = 0x00000020;

        var exStyle = (uint)NativeMethods.GetWindowLong(hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
        SetWindowLong(hWnd, GWL_EXSTYLE, (int)exStyle);
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _followTimer.Stop();
        _followTimer.Dispose();

        if (_winEventHook != IntPtr.Zero)
            NativeMethods.UnhookWinEvent(_winEventHook);

        if (!_overlayForm.IsDisposed)
        {
            _overlayForm.Invoke(() =>
            {
                _overlayForm.Hide();
                _overlayForm.Dispose();
            });
        }
    }
}
