using System.Runtime.InteropServices;

namespace MoodyBlues;

/// <summary>
/// 输入捕获弹窗 — 键盘用低层钩子，鼠标走 WndProc（侧键更稳定）(⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal class KeyCaptureForm : Form
{
    private IntPtr _kbdHook = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _kbdProc;

    public int CapturedVk { get; private set; }
    public int CapturedScanCode { get; private set; }
    public bool IsMouse { get; private set; }
    public int CapturedMouseX { get; private set; }
    public int CapturedMouseY { get; private set; }
    public string KeyName => VkHelper.GetName(CapturedVk);

    public KeyCaptureForm()
    {
        this.Text = "🎯 捕获输入";
        this.Size = new Size(340, 180);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.TopMost = true;

        var lbl = new Label
        {
            Text = "按下任意键盘按键 或 点击鼠标...\n(支持左/右/中/侧键 X1/X2，自动记录坐标)",
            Location = new Point(20, 25),
            Size = new Size(300, 55),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(130, 95),
            Size = new Size(80, 30)
        };
        btnCancel.Click += (_, _) => { this.DialogResult = DialogResult.Cancel; };

        this.Controls.Add(lbl);
        this.Controls.Add(btnCancel);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // 键盘：低层钩子（已验证可用）
        _kbdProc = KeyboardProc;
        _kbdHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _kbdProc, IntPtr.Zero, 0);
    }

    /// <summary>
    /// 鼠标事件走 WndProc — 侧键 XButton 比钩子更可靠 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        int msg = m.Msg;

        int? vk = msg switch
        {
            NativeMethods.WM_LBUTTONDOWN => NativeMethods.VK_LBUTTON,
            NativeMethods.WM_RBUTTONDOWN => NativeMethods.VK_RBUTTON,
            NativeMethods.WM_MBUTTONDOWN => NativeMethods.VK_MBUTTON,
            NativeMethods.WM_XBUTTONDOWN => -1, // 需要从 WParam 高位读取具体键
            _ => null
        };

        if (vk.HasValue)
        {
            if (vk.Value == -1)
            {
                // XButton：WParam 高位 == 1 → X1, == 2 → X2
                int which = (int)(m.WParam.ToInt64() >> 16) & 0xFFFF;
                CapturedVk = which == 2 ? NativeMethods.VK_XBUTTON2 : NativeMethods.VK_XBUTTON1;
            }
            else
            {
                CapturedVk = vk.Value;
            }

            CapturedScanCode = 0;
            IsMouse = true;
            NativeMethods.GetCursorPos(out var pt);
            CapturedMouseX = pt.x;
            CapturedMouseY = pt.y;

            this.DialogResult = DialogResult.OK;
            return; // 吞掉消息，不继续分发
        }

        base.WndProc(ref m);
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == NativeMethods.WM_KEYDOWN)
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // 过滤 ESC（留给取消按钮）
            if (kb.vkCode != 0x1B)
            {
                CapturedVk = (int)kb.vkCode;
                CapturedScanCode = (int)kb.scanCode;
                IsMouse = false;

                this.BeginInvoke(() =>
                {
                    this.DialogResult = DialogResult.OK;
                });
            }
        }
        return NativeMethods.CallNextHookEx(_kbdHook, nCode, wParam, lParam);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_kbdHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_kbdHook);
            _kbdHook = IntPtr.Zero;
        }
        _kbdProc = null;
        base.OnFormClosed(e);
    }
}
