using System.Text.Json;

namespace MoodyBlues;

/// <summary>
/// 预设 JSON 存储 — 跟 RecordStore 一样的模式 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal static class PresetStore
{
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private static readonly string PresetsDir =
        Path.Combine(BaseDir, "presets");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true, // 预设可手改 JSON，缩进友好
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static PresetStore() => Directory.CreateDirectory(PresetsDir);

    public static List<Preset> ListAll()
    {
        var result = new List<Preset>();
        foreach (var file in Directory.GetFiles(PresetsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<Preset>(json, JsonOpts);
                if (preset != null) result.Add(preset);
            }
            catch { /* 损坏文件跳过 */ }
        }
        result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return result;
    }

    public static void Save(Preset preset)
    {
        var json = JsonSerializer.Serialize(preset, JsonOpts);
        var path = Path.Combine(PresetsDir, $"{preset.Id}.json");
        File.WriteAllText(path, json);
    }

    public static void Delete(string id)
    {
        var path = Path.Combine(PresetsDir, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    public static Preset? Load(string id)
    {
        var path = Path.Combine(PresetsDir, $"{id}.json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Preset>(json, JsonOpts);
    }
}
