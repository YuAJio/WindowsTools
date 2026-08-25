using System.Text.Json;

namespace DailyVoice;

/// <summary>
/// 配置模型 + JSON 读写 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal record Config
{
    public string PlayTime { get; set; } = "13:55";
    public int Volume { get; set; } = 80;
    /// <summary>视频独立播放时间 HH:mm</summary>
    public string? VideoPlayTime { get; set; }
    /// <summary>视频排班列表：按顺序播放，同一视频可重复排班（路径列表）</summary>
    public List<string> VideoPlaylist { get; set; } = new();

    /// <summary>
    /// 旧版单选视频字段（仅用于迁移到排班，迁移后置 null）
    /// </summary>
    public string? VideoFile { get; set; }
}

internal static class ConfigManager
{
    // PublishSingleFile 下 AppContext.BaseDirectory 指向临时目录，
    // 用 ProcessPath 取 exe 真实所在目录 (⁎⁍̴̛ᴗ⁍̴̛⁎)
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private static readonly string ConfigPath =
        Path.Combine(BaseDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Config Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<Config>(json, JsonOptions);
                if (cfg != null) return cfg;
            }
        }
        catch { /* 配置损坏用默认 */ }

        var defaultCfg = new Config();
        Save(defaultCfg);
        return defaultCfg;
    }

    public static void Save(Config config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
