using System.Diagnostics;

namespace MoodyBlues;

/// <summary>
/// 播放引擎 — 按时间戳还原每一个输入事件 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal class PlaybackEngine
{
    private CancellationTokenSource? _cts;
    public bool IsPlaying { get; private set; }

    public event Action? OnStarted;
    public event Action? OnFinished;

    /// <summary>
    /// 异步播放录制
    /// </summary>
    public async Task PlayAsync(Recording recording, CancellationToken token = default)
    {
        if (IsPlaying) return;
        IsPlaying = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        OnStarted?.Invoke();

        try
        {
            var events = recording.Events;
            if (events.Count == 0) return;

            var sw = Stopwatch.StartNew();
            var lastOffset = 0L;
            var trackCursor = recording.TrackCursor; // 录制标记决定是否追坐标 (⁎⁍̴̛ᴗ⁍̴̛⁎)

            for (int i = 0; i < events.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var evt = events[i];

                // 等待到预定的时间偏移
                var waitMs = evt.OffsetMs - lastOffset;
                if (waitMs > 0)
                {
                    while (sw.ElapsedMilliseconds - lastOffset < waitMs)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        await Task.Delay(1, _cts.Token);
                    }
                }

                lastOffset = evt.OffsetMs;
                SendEvent(evt, trackCursor);
            }
        }
        catch (OperationCanceledException) { /* 用户手动停止 */ }
        finally
        {
            IsPlaying = false;
            OnFinished?.Invoke();
        }
    }

    /// <summary>
    /// 发送一个输入事件
    /// </summary>
    private static void SendEvent(InputEvent evt, bool trackCursor)
    {
        // 鼠标事件：仅在追踪模式下移动光标 (⁎⁍̴̛ᴗ⁍̴̛⁎)
        if (evt.VkCode is NativeMethods.VK_LBUTTON or NativeMethods.VK_RBUTTON
            or NativeMethods.VK_MBUTTON or NativeMethods.VK_XBUTTON1 or NativeMethods.VK_XBUTTON2)
        {
            // 只有 trackCursor=true 才移动光标
            if (trackCursor)
            {
                NativeMethods.SetCursorPos(evt.MouseX, evt.MouseY);
                Thread.Sleep(5); // 小延迟让系统注册位置变化
            }

            bool isDown = evt.Type == "MouseDown";
            uint downFlag = evt.VkCode switch
            {
                NativeMethods.VK_RBUTTON => NativeMethods.MOUSEEVENTF_RIGHTDOWN,
                NativeMethods.VK_MBUTTON => NativeMethods.MOUSEEVENTF_MIDDLEDOWN,
                NativeMethods.VK_XBUTTON1 => NativeMethods.MOUSEEVENTF_XDOWN,
                NativeMethods.VK_XBUTTON2 => NativeMethods.MOUSEEVENTF_XDOWN,
                _ => NativeMethods.MOUSEEVENTF_LEFTDOWN
            };
            uint upFlag = evt.VkCode switch
            {
                NativeMethods.VK_RBUTTON => NativeMethods.MOUSEEVENTF_RIGHTUP,
                NativeMethods.VK_MBUTTON => NativeMethods.MOUSEEVENTF_MIDDLEUP,
                NativeMethods.VK_XBUTTON1 => NativeMethods.MOUSEEVENTF_XUP,
                NativeMethods.VK_XBUTTON2 => NativeMethods.MOUSEEVENTF_XUP,
                _ => NativeMethods.MOUSEEVENTF_LEFTUP
            };

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = isDown ? downFlag : upFlag,
                        mouseData = (evt.VkCode == NativeMethods.VK_XBUTTON1) ? 0x00010000u :
                                    (evt.VkCode == NativeMethods.VK_XBUTTON2) ? 0x00020000u : 0u
                    }
                }
            };
            NativeMethods.SendInput(1, [input], NativeMethods.INPUT_SIZE);
        }
        else
        {
            // 键盘事件 — 优先用扫描码（游戏兼容）(⁎⁍̴̛ᴗ⁍̴̛⁎)
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.INPUTUNION
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)evt.ScanCode,
                        dwFlags = NativeMethods.KEYEVENTF_SCANCODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            if (evt.ScanCode == 0)
            {
                // 旧录制：回退到虚拟键码模式
                input.u.ki.wVk = (ushort)evt.VkCode;
                input.u.ki.wScan = 0;
                input.u.ki.dwFlags = 0;
            }
            if (evt.Type == "KeyUp")
                input.u.ki.dwFlags |= NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(1, [input], NativeMethods.INPUT_SIZE);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
