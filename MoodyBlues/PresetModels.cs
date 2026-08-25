namespace MoodyBlues;

/// <summary>
/// 预设步骤 — 一个按键动作 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal record PresetStep(
    string KeyName,     // 显示名（如 "A", "Enter", "LButton"）
    int VkCode,         // 虚拟键码
    int ScanCode,       // 硬件扫描码（游戏兼容）
    int HoldMs = 100,   // 按键按下持续时长(ms)
    int GapMs = 50,     // 与下一步骤的间隔(ms)
    int MouseX = 0,     // 鼠标事件时的屏幕坐标（键盘事件留 0）
    int MouseY = 0
);

/// <summary>
/// 一个完整的按键预设
/// </summary>
internal record Preset(
    string Id,          // 文件名（ISO 时间戳）
    string Name,        // 用户自定义名称
    DateTime CreatedAt,
    List<PresetStep> Steps
);
