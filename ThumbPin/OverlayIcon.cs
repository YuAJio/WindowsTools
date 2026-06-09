using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ThumbPin;

/// <summary>
/// 被置顶窗口左上角的图钉标记 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 纯 Win32 透明分层窗口 + 点击穿透 + 跟随目标窗口。
/// </summary>
internal class OverlayIcon : IDisposable
{
    private readonly IntPtr _targetHwnd;
    private readonly IntPtr _overlayHwnd;
    private readonly System.Windows.Forms.Timer _followTimer;
    private readonly IntPtr _winEventHook;
    private readonly NativeMethods.WinEventDelegate _winEventProc;
    private readonly uint _targetThreadId;
    private Bitmap? _bitmap;

    private const int SIZE = 24;
    private bool _disposed;

    public OverlayIcon(IntPtr targetHwnd)
    {
        _targetHwnd = targetHwnd;
        _targetThreadId = NativeMethods.GetWindowThreadProcessId(targetHwnd, out _);

        // 注册窗口类
        var className = "ThumbPinOverlay_" + Environment.ProcessId;
        var wc = new NativeMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = WndProc,
            hInstance = Marshal.GetHINSTANCE(typeof(OverlayIcon).Module),
            lpszClassName = className
        };
        NativeMethods.RegisterClassEx(ref wc);

        // 创建透明分层窗口
        const uint WS_EX_LAYERED = 0x00080000;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_EX_TRANSPARENT = 0x00000020;
        const uint WS_EX_TOPMOST = 0x00000008;
        const uint WS_POPUP = 0x80000000;

        _overlayHwnd = NativeMethods.CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_TOPMOST,
            className, "", WS_POPUP,
            0, 0, SIZE, SIZE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        // 绘制图钉图标到 bitmap
        _bitmap = RenderThumbPinIcon(SIZE);
        SetLayeredBitmap(_bitmap);

        PositionOverlay();
        NativeMethods.ShowWindow(_overlayHwnd, 4); // SW_SHOWNOACTIVATE

        // WinEvent 监听目标窗口位置变化
        _winEventProc = WinEventCallback;
        _winEventHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
            NativeMethods.EVENT_OBJECT_DESTROY,
            IntPtr.Zero,
            _winEventProc,
            0, _targetThreadId, // 监听目标线程
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        // 备用定时器
        _followTimer = new System.Windows.Forms.Timer { Interval = 300 };
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

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void SetLayeredBitmap(Bitmap bmp)
    {
        var screenDC = NativeMethods.GetDC(IntPtr.Zero);
        var memDC = NativeMethods.CreateCompatibleDC(screenDC);
        var hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
        var oldBitmap = NativeMethods.SelectObject(memDC, hBitmap);

        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = 0, // AC_SRC_OVER
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = 1 // AC_SRC_ALPHA
        };

        var ptSrc = new NativeMethods.POINT();
        var ptDest = new NativeMethods.POINT();
        var size = new NativeMethods.SIZE { cx = SIZE, cy = SIZE };

        NativeMethods.UpdateLayeredWindow(_overlayHwnd, screenDC, ref ptDest, ref size,
            memDC, ref ptSrc, 0, ref blend, 2); // ULW_ALPHA

        NativeMethods.SelectObject(memDC, oldBitmap);
        NativeMethods.DeleteDC(memDC);
        NativeMethods.ReleaseDC(IntPtr.Zero, screenDC);
        NativeMethods.DeleteObject(hBitmap);
    }

    private void WinEventCallback(IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == _targetHwnd && idObject == 0)
        {
            if (eventType == NativeMethods.EVENT_OBJECT_DESTROY)
                Dispose();
            else
                PositionOverlay();
        }
    }

    private void PositionOverlay()
    {
        if (_disposed) return;
        if (NativeMethods.GetWindowRect(_targetHwnd, out var rect))
        {
            NativeMethods.SetWindowPos(_overlayHwnd, NativeMethods.HWND_TOPMOST,
                rect.Left + 4, rect.Top + 4, SIZE, SIZE,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }
    }

    private bool IsTargetAlive()
    {
        return IsWindow(_targetHwnd);
    }

    // ═══════════════════════════════════════
    //  绘制图钉图标 (GDI+)
    // ═══════════════════════════════════════

    private static Bitmap RenderThumbPinIcon(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Color.FromArgb(220, 46, 46));
        using var pen = new Pen(Color.FromArgb(180, 30, 30), 1.5f);
        using var highlight = new SolidBrush(Color.FromArgb(120, 255, 255, 255));

        var rect = new RectangleF(3, 2, 17, 13);
        FillRoundedRect(g, brush, rect, 3);
        g.DrawLine(new Pen(highlight, 1.5f), 7, 4, 15, 10);

        var tip = new PointF[]
        {
            new(rect.X + rect.Width / 2 - 3.5f, rect.Bottom),
            new(rect.X + rect.Width / 2 + 3.5f, rect.Bottom),
            new(rect.X + rect.Width / 2, rect.Bottom + 7)
        };
        g.FillPolygon(brush, tip);
        g.DrawPolygon(pen, tip);

        return bmp;
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

    // ═══════════════════════════════════════
    //  Win32 helpers
    // ═══════════════════════════════════════

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

        if (_overlayHwnd != IntPtr.Zero)
            NativeMethods.DestroyWindow(_overlayHwnd);

        _bitmap?.Dispose();
        _bitmap = null;
    }
}
