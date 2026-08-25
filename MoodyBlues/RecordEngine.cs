using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MoodyBlues;

/// <summary>
/// 录制引擎 — 挂键盘+鼠标双钩子，记录每个事件 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal class RecordEngine : IDisposable
{
    private readonly List<InputEvent> _events = [];
    private readonly Stopwatch _stopwatch = new();
    private IntPtr _kbdHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _kbdProc;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private bool _recording;
    private bool _trackCursor = true;

    public event Action? OnStarted;
    public event Action? OnStopped;

    public bool IsRecording => _recording;

    public void Start(bool trackCursor = true)
    {
        if (_recording) return;

        _trackCursor = trackCursor;

        _events.Clear();
        _stopwatch.Restart();

        _kbdProc = KeyboardProc;
        _mouseProc = MouseProc;

        _kbdHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _kbdProc, IntPtr.Zero, 0);

        _mouseHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);

        _recording = true;
        OnStarted?.Invoke();
    }

    public Recording Stop()
    {
        if (!_recording) return new Recording("", DateTime.MinValue, [], false);

        _recording = false;
        if (_kbdHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_kbdHook); _kbdHook = IntPtr.Zero; }
        if (_mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        _kbdProc = null;
        _mouseProc = null;

        var id = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var recording = new Recording(id, DateTime.Now, [.. _events], _trackCursor);
        RecordStore.Save(recording);
        OnStopped?.Invoke();
        return recording;
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _recording)
        {
            var msg = (int)wParam;
            // 过滤 F4/F5/F6 自身
            if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN ||
                msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
            {
                var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                int vk = (int)kb.vkCode;

                // 不录制热键
                if (vk != NativeMethods.VK_F4 && vk != NativeMethods.VK_F5 && vk != NativeMethods.VK_F6)
                {
                    NativeMethods.GetCursorPos(out var pt);
                    _events.Add(new InputEvent(
                        _stopwatch.ElapsedMilliseconds,
                        (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN) ? "KeyDown" : "KeyUp",
                        vk, pt.x, pt.y, (int)kb.scanCode));
                }
            }
        }
        return NativeMethods.CallNextHookEx(_kbdHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _recording)
        {
            var msg = (int)wParam;
            var ms = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            string? type = msg switch
            {
                NativeMethods.WM_LBUTTONDOWN => "MouseDown",
                NativeMethods.WM_LBUTTONUP => "MouseUp",
                NativeMethods.WM_RBUTTONDOWN => "MouseDown",
                NativeMethods.WM_RBUTTONUP => "MouseUp",
                NativeMethods.WM_MBUTTONDOWN => "MouseDown",
                NativeMethods.WM_MBUTTONUP => "MouseUp",
                NativeMethods.WM_XBUTTONDOWN => "MouseDown",
                NativeMethods.WM_XBUTTONUP => "MouseUp",
                _ => null
            };

            if (type != null)
            {
                int vk = msg switch
                {
                    NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_LBUTTONUP => NativeMethods.VK_LBUTTON,
                    NativeMethods.WM_RBUTTONDOWN or NativeMethods.WM_RBUTTONUP => NativeMethods.VK_RBUTTON,
                    NativeMethods.WM_MBUTTONDOWN or NativeMethods.WM_MBUTTONUP => NativeMethods.VK_MBUTTON,
                    NativeMethods.WM_XBUTTONDOWN or NativeMethods.WM_XBUTTONUP =>
                        (int)(ms.mouseData >> 16) == 1 ? NativeMethods.VK_XBUTTON1 : NativeMethods.VK_XBUTTON2,
                    _ => 0
                };

                _events.Add(new InputEvent(
                    _stopwatch.ElapsedMilliseconds,
                    type, vk,
                    // 使用实际的点击坐标，'u'p 事件可能位置不同，但也记录
                    ms.pt.x, ms.pt.y));
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_recording) Stop();
    }
}
