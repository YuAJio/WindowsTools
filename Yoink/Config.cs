using System.Text.Json;

namespace Yoink;

/// <summary>
/// Yoink 配置模型 + JSON 读写 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal record Config
{
    /// <summary>yt-dlp.exe 路径（null = 自动查找）</summary>
    public string? YtDlpPath { get; set; }

    /// <summary>ffmpeg.exe 路径（null = 自动查找）</summary>
    public string? FfmpegPath { get; set; }

    /// <summary>默认下载目录</summary>
    public string OutputDir { get; set; } = "downloads";

    /// <summary>默认质量选择 (best / 1080 / 720 / 480 / audio)</summary>
    public string DefaultQuality { get; set; } = "best";

    /// <summary>默认仅音频</summary>
    public bool AudioOnly { get; set; }
}

internal static class ConfigManager
{
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
                return JsonSerializer.Deserialize<Config>(json, JsonOptions) ?? new();
            }
        }
        catch { /* 配置损坏用默认 */ }
        return new Config();
    }

    public static void Save(Config config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
