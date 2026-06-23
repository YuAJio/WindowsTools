using System.Text.Json;

namespace DailyVoice;

/// <summary>
/// 持久化洗牌状态 — 防重启后重复播放同一文件 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// </summary>
internal sealed class ShuffleState
{
    /// <summary>洗牌后的文件全路径队列</summary>
    public List<string> Queue { get; set; } = [];

    /// <summary>当前队列游标</summary>
    public int QueueIndex { get; set; }

    /// <summary>最近播放历史（最多 20 条），避免短期重复</summary>
    public List<string> RecentlyPlayed { get; set; } = [];
}

internal static class ShuffleStateManager
{
    private static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath!)!;

    private static readonly string StatePath =
        Path.Combine(BaseDir, "shuffle_state.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ShuffleState Load()
    {
        try
        {
            if (File.Exists(StatePath))
            {
                var json = File.ReadAllText(StatePath);
                var state = JsonSerializer.Deserialize<ShuffleState>(json, JsonOptions);
                if (state != null) return state;
            }
        }
        catch { /* 状态损坏 → 全新开始 */ }

        return new ShuffleState();
    }

    public static void Save(ShuffleState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(StatePath, json);
        }
        catch { /* 写入失败不崩溃 */ }
    }
}
