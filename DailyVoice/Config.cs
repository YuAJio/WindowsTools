using System.Text.Json;

namespace DailyVoice;

/// <summary>
/// 配置模型 + JSON 读写 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal record Config
{
    public string PlayTime { get; set; } = "13:55";
    public int Volume { get; set; } = 80;
}

internal static class ConfigManager
{
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.json");

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
