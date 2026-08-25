namespace MoodyBlues;

/// <summary>
/// 虚拟键码 → 可读键名 转换 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal static class VkHelper
{
    private static readonly Dictionary<int, string> _names = new()
    {
        { 0x01, "LButton" }, { 0x02, "RButton" }, { 0x04, "MButton" },
        { 0x05, "X1" }, { 0x06, "X2" },
        { 0x08, "Backspace" }, { 0x09, "Tab" },
        { 0x0C, "Clear" }, { 0x0D, "Enter" },
        { 0x10, "Shift" }, { 0x11, "Ctrl" }, { 0x12, "Alt" },
        { 0x13, "Pause" }, { 0x14, "CapsLock" },
        { 0x1B, "Esc" },
        { 0x20, "Space" }, { 0x21, "PageUp" }, { 0x22, "PageDown" },
        { 0x23, "End" }, { 0x24, "Home" },
        { 0x25, "←" }, { 0x26, "↑" }, { 0x27, "→" }, { 0x28, "↓" },
        { 0x2C, "PrintScreen" }, { 0x2D, "Insert" }, { 0x2E, "Delete" },
        { 0x5B, "LWin" }, { 0x5C, "RWin" },
        { 0x60, "Num0" }, { 0x61, "Num1" }, { 0x62, "Num2" },
        { 0x63, "Num3" }, { 0x64, "Num4" }, { 0x65, "Num5" },
        { 0x66, "Num6" }, { 0x67, "Num7" }, { 0x68, "Num8" },
        { 0x69, "Num9" },
        { 0x6A, "Num*" }, { 0x6B, "Num+" }, { 0x6D, "Num-" },
        { 0x6E, "Num." }, { 0x6F, "Num/" },
        { 0x70, "F1" }, { 0x71, "F2" }, { 0x72, "F3" },
        { 0x73, "F4" }, { 0x74, "F5" }, { 0x75, "F6" },
        { 0x76, "F7" }, { 0x77, "F8" }, { 0x78, "F9" },
        { 0x79, "F10" }, { 0x7A, "F11" }, { 0x7B, "F12" },
        { 0xA0, "LShift" }, { 0xA1, "RShift" },
        { 0xA2, "LCtrl" }, { 0xA3, "RCtrl" },
        { 0xA4, "LAlt" }, { 0xA5, "RAlt" },
    };

    public static string GetName(int vkCode)
    {
        if (_names.TryGetValue(vkCode, out var name))
            return name;

        // 字母和数字用对应的 ASCII 字符
        if (vkCode is >= 0x30 and <= 0x39)
            return ((char)vkCode).ToString();
        if (vkCode is >= 0x41 and <= 0x5A)
            return ((char)vkCode).ToString();

        return $"VK_{vkCode:X2}";
    }
}
