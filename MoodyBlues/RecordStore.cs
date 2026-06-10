using System.Text.Json;

namespace MoodyBlues;

/// <summary>
/// 输入事件 — 录音的基本单位 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal record InputEvent(
    long OffsetMs,      // 距录制开始的毫秒偏移
    string Type,        // "KeyDown" | "KeyUp" | "MouseDown" | "MouseUp"
    int VkCode,         // 虚拟键码（键盘 OR 鼠标按键）
    int MouseX,         // 鼠标事件时的屏幕绝对坐标
    int MouseY
);

/// <summary>
/// 一次录制
/// </summary>
internal record Recording(
    string Id,          // 文件名（ISO 时间戳，不含非法字符）
    DateTime CreatedAt,
    List<InputEvent> Events
);

/// <summary>
/// JSON 存储 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal static class RecordStore
{
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private static readonly string RecordsDir =
        Path.Combine(BaseDir, "records");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false, // 节省空间
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static RecordStore() => Directory.CreateDirectory(RecordsDir);

    /// <summary>
    /// 列出所有录制，按时间倒序
    /// </summary>
    public static List<Recording> ListAll()
    {
        var result = new List<Recording>();
        foreach (var file in Directory.GetFiles(RecordsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var rec = JsonSerializer.Deserialize<Recording>(json, JsonOpts);
                if (rec != null) result.Add(rec);
            }
            catch { /* 损坏文件跳过 */ }
        }
        result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return result;
    }

    /// <summary>
    /// 保存录制
    /// </summary>
    public static void Save(Recording recording)
    {
        var json = JsonSerializer.Serialize(recording, JsonOpts);
        var path = Path.Combine(RecordsDir, $"{recording.Id}.json");
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 删除录制
    /// </summary>
    public static void Delete(string id)
    {
        var path = Path.Combine(RecordsDir, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// 按 ID 加载录制
    /// </summary>
    public static Recording? Load(string id)
    {
        var path = Path.Combine(RecordsDir, $"{id}.json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Recording>(json, JsonOpts);
    }
}
