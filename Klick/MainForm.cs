using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Klick;

public partial class MainForm : Form
{
    // ── 状态 ──
    private bool _isClicking;
    private volatile bool _stopRequested; // 线程安全标记
    private CancellationTokenSource _cts = new();

    // ── 热键 ID ──
    private const int HOTKEY_START = 1;
    private const int HOTKEY_STOP = 2;

    // ── 目标 ──
    private int _targetVk = NativeMethods.VK_LBUTTON;  // 默认左键
    private string _targetKeyName = "左键";
    private ClickMode _mode = ClickMode.Mouse;

    // ── 键盘钩子 ──
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;

    private enum ClickMode { Mouse, Keyboard }

    public MainForm()
    {
        InitializeComponent();
        RegisterHotKeys();
    }

    // ═══════════════════════════════════════
    //  热键注册 / 卸载
    // ═══════════════════════════════════════

    private void RegisterHotKeys()
    {
        if (!NativeMethods.RegisterHotKey(this.Handle, HOTKEY_START, NativeMethods.MOD_NONE, NativeMethods.VK_F8))
            MessageBox.Show("F8 热键注册失败（可能被其他程序占用）", "Klick", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        if (!NativeMethods.RegisterHotKey(this.Handle, HOTKEY_STOP, NativeMethods.MOD_NONE, NativeMethods.VK_F9))
            MessageBox.Show("F9 热键注册失败（可能被其他程序占用）", "Klick", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void UnregisterHotKeys()
    {
        NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_START);
        NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_STOP);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (id == HOTKEY_START) StartClicking();
            else if (id == HOTKEY_STOP) StopClicking();
        }
        base.WndProc(ref m);
    }

    // ═══════════════════════════════════════
    //  连点核心 — 后台线程 + SendInput
    // ═══════════════════════════════════════

    private async void StartClicking()
    {
        if (_isClicking) return;

        _isClicking = true;
        _stopRequested = false;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        int interval = (int)nudInterval.Value;

        UpdateStatusLabel("▶ 连点中...", Color.Crimson);

        // 后台线程跑连点循环，保证精度
        await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            long nextFire = 0;

            while (!_stopRequested && !token.IsCancellationRequested)
            {
                if (stopwatch.ElapsedMilliseconds >= nextFire)
                {
                    SendOneClick();
                    nextFire = stopwatch.ElapsedMilliseconds + interval;
                }
                // 自旋等待 1ms 精度（Timer 最低 15ms 太粗糙）
                Thread.Sleep(1);
            }
        }, token);

        _isClicking = false;
        UpdateStatusLabel("⏸ 已停止", Color.DimGray);
    }

    private void StopClicking()
    {
        if (!_isClicking) return;
        _stopRequested = true;
        _cts.Cancel();
    }

    /// <summary>
    /// 发送一次模拟点击（SendInput 方案，最稳定 (⁎⁍̴̛ᴗ⁍̴̛⁎)）
    /// </summary>
    private void SendOneClick()
    {
        if (_mode == ClickMode.Mouse)
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = MouseDownFlag() }
                }
            };
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = MouseUpFlag() }
                }
            };

            NativeMethods.SendInput(2, inputs, NativeMethods.INPUT_SIZE);
        }
        else
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.INPUTUNION
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = (ushort)_targetVk,
                        dwFlags = NativeMethods.KEYEVENTF_KEYDOWN
                    }
                }
            };
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.INPUTUNION
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = (ushort)_targetVk,
                        dwFlags = NativeMethods.KEYEVENTF_KEYUP
                    }
                }
            };

            NativeMethods.SendInput(2, inputs, NativeMethods.INPUT_SIZE);
        }
    }

    private uint MouseDownFlag() => _targetVk switch
    {
        NativeMethods.VK_RBUTTON => NativeMethods.MOUSEEVENTF_RIGHTDOWN,
        NativeMethods.VK_MBUTTON => NativeMethods.MOUSEEVENTF_MIDDLEDOWN,
        _ => NativeMethods.MOUSEEVENTF_LEFTDOWN
    };

    private uint MouseUpFlag() => _targetVk switch
    {
        NativeMethods.VK_RBUTTON => NativeMethods.MOUSEEVENTF_RIGHTUP,
        NativeMethods.VK_MBUTTON => NativeMethods.MOUSEEVENTF_MIDDLEUP,
        _ => NativeMethods.MOUSEEVENTF_LEFTUP
    };

    // ═══════════════════════════════════════
    //  状态 UI
    // ═══════════════════════════════════════

    private void UpdateStatusLabel(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatusLabel(text, color));
            return;
        }
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    // ═══════════════════════════════════════
    //  捕获模式 — 低级键盘钩子
    // ═══════════════════════════════════════

    private void OnCaptureKeyClick(object? sender, EventArgs e)
    {
        if (_hookId != IntPtr.Zero) return; // 已经在捕获中

        btnCaptureKey.Text = "⏳ 请按下目标键...";
        btnCaptureKey.Enabled = false;

        _hookProc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule?.ModuleName == null) return;

        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            int vkCode = (int)kb.vkCode;

            // 忽略 F8/F9/Esc，避免干扰热键
            if (vkCode == NativeMethods.VK_F8 || vkCode == NativeMethods.VK_F9 || vkCode == NativeMethods.VK_ESCAPE)
            {
                // Esc 取消捕获
                if (vkCode == NativeMethods.VK_ESCAPE) CancelCapture();
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // 捕获成功
            _targetVk = vkCode;
            _targetKeyName = ((Keys)vkCode).ToString();
            _mode = ClickMode.Keyboard;
            rbKeyboard.Checked = true;

            // UI 更新必须在主线程
            BeginInvoke(() =>
            {
                lblTargetKey.Text = _targetKeyName;
                lblTargetKey.ForeColor = Color.Black;
                btnCaptureKey.Text = "🎯 重新捕获";
                btnCaptureKey.Enabled = true;
            });

            ReleaseHook();
            return (IntPtr)1; // 吃掉这次按键
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void CancelCapture()
    {
        BeginInvoke(() =>
        {
            btnCaptureKey.Text = "🎯 点击捕获按键";
            btnCaptureKey.Enabled = true;
            if (string.IsNullOrEmpty(_targetKeyName) || _mode != ClickMode.Keyboard)
            {
                lblTargetKey.Text = "未设置";
                lblTargetKey.ForeColor = Color.Gray;
            }
        });
        ReleaseHook();
    }

    private void ReleaseHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _hookProc = null;
    }

    // ═══════════════════════════════════════
    //  事件处理
    // ═══════════════════════════════════════

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        if (rbMouse.Checked)
        {
            _mode = ClickMode.Mouse;
            _targetKeyName = cmbMouseButton.SelectedItem?.ToString() ?? "左键";
            _targetVk = cmbMouseButton.SelectedIndex switch
            {
                1 => NativeMethods.VK_RBUTTON,
                2 => NativeMethods.VK_MBUTTON,
                _ => NativeMethods.VK_LBUTTON
            };
            _gbMouse.Visible = true;
            _gbKeyboard.Visible = false;
        }
        else
        {
            _mode = ClickMode.Keyboard;
            // 保留之前捕获的按键
            _gbMouse.Visible = false;
            _gbKeyboard.Visible = true;
        }
    }

    private void OnMinimizeToTray(object? sender, EventArgs e)
    {
        this.Hide();
        notifyIcon.Visible = true;
        notifyIcon.ShowBalloonTip(2000, "Klick", "程序已最小化到托盘\nF8 启动 | F9 停止", ToolTipIcon.Info);
    }

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
        {
            this.Hide();
            notifyIcon.Visible = true;
        }
    }

    private void OnShowFromTray(object? sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
        notifyIcon.Visible = false;
    }

    private void OnExit(object? sender, EventArgs e)
    {
        notifyIcon.Visible = false;
        this.FormClosing -= OnFormClosing; // 避免二次确认
        Application.Exit();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // 不是退出（是系统关机等），直接关
        if (e.CloseReason != CloseReason.UserClosing) return;

        // 提示退出
        var result = MessageBox.Show("确定退出 Klick 吗？", "Klick", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.No)
        {
            e.Cancel = true;
            return;
        }

        // 清理
        StopClicking();
        ReleaseHook();
        UnregisterHotKeys();
        notifyIcon.Visible = false;
    }
}
