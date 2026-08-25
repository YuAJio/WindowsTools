using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace QqPlaylist;

internal static class PlaylistFetcher
{
    private static readonly HttpClient _http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        // Referer 在 .NET 8 是非标准头，用 TryAddWithoutValidation 避免编译失败
        c.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://y.qq.com/");
        return c;
    }

    /// <summary>
    /// 从 ID 或完整 URL 中提取歌单 ID (^ω^)～
    /// </summary>
    public static string ExtractId(string idOrUrl)
    {
        if (string.IsNullOrWhiteSpace(idOrUrl))
            throw new PlaylistFetchException("歌单 ID 不能为空喵~");

        var s = idOrUrl.Trim();

        // 完整 URL 形式：https://y.qq.com/n/ryqq_v2/playlist/9768457679
        var m = Regex.Match(s, @"playlist/(\d+)");
        if (m.Success) return m.Groups[1].Value;

        // 纯数字 ID
        m = Regex.Match(s, @"^\d{5,}$");
        if (m.Success) return s;

        throw new PlaylistFetchException("无法识别歌单 ID，请输入完整 URL 或纯数字 ID 喵~");
    }

    public static async Task<PlaylistFetchOutcome> FetchAsync(
        string idOrUrl,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var id = ExtractId(idOrUrl);
        progress?.Report($"🔍 解析歌单 ID：{id}");

        var url = $"https://c.y.qq.com/v8/fcg-bin/fcg_v8_playlist_cp.fcg?newsong=1&id={id}&format=json&inCharset=GB2312&outCharset=utf-8";
        progress?.Report("📡 正在调 QQ 音乐接口…");

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
        req.Headers.TryAddWithoutValidation("sec-fetch-site", "same-site");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

        // 检测编码（QQ 接口有时给 GBK 而不是 UTF-8）
        var charset = resp.Content.Headers.ContentType?.CharSet;
        Encoding encoding = charset?.ToLowerInvariant() switch
        {
            "gbk" or "gb2312" => Encoding.GetEncoding("GB18030"),
            "utf-8" or "utf8" => Encoding.UTF8,
            _ => TryDetectUtf8(bytes) ? Encoding.UTF8 : Encoding.GetEncoding("GB18030")
        };
        var json = encoding.GetString(bytes);

        // 失败时把原始 JSON 写到 %AppData%\QqPlaylist\playlist_<id>_<ts>.json
        var rawPath = "";
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QqPlaylist");
            Directory.CreateDirectory(dir);
            rawPath = Path.Combine(dir, $"playlist_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(rawPath, json, new UTF8Encoding(false), ct).ConfigureAwait(false);
        }
        catch { /* best-effort */ }

        progress?.Report("📦 JSON 已到手，反序列化中…");

        ApiResponse? apiResp;
        try
        {
            apiResp = JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts);
        }
        catch (JsonException jex)
        {
            var preview = json.Length > 500 ? json[..500] + "..." : json;
            throw new PlaylistFetchException(
                $"JSON 解析失败喵~：{jex.Message}\n\n原始响应已保存到：\n{rawPath}\n\n前 500 字符：\n{preview}");
        }

        if (apiResp?.Data?.Cdlist is null || apiResp.Data.Cdlist.Count == 0)
            throw new PlaylistFetchException($"接口返回空数据，可能歌单不存在或被风控了喵~\n原始响应：{rawPath}");

        var cd = apiResp.Data.Cdlist[0];
        if (cd.Songlist is null || cd.Songlist.Count == 0)
            throw new PlaylistFetchException("歌单里没歌曲喵~");

        progress?.Report($"✅ 成功抓到 {cd.Songlist.Count} 首");

        var tracks = new List<Track>(cd.Songlist.Count);
        for (int i = 0; i < cd.Songlist.Count; i++)
        {
            var s = cd.Songlist[i];
            var artist = s.Singer is { Count: > 0 }
                ? string.Join(" / ", s.Singer.Select(x => x.Name ?? "").Where(x => x.Length > 0))
                : "";
            tracks.Add(new Track(
                Index: i + 1,
                Name: s.Name ?? "",
                Artist: artist,
                Album: s.Album?.Name ?? "",
                DurationSec: s.Interval
            ));
        }

        var result = new PlaylistResult(
            Id: cd.Disstid ?? id,
            Name: cd.Dissname ?? "(无名歌单)",
            Desc: cd.Desc ?? "",
            TotalCount: cd.CurSongNum ?? tracks.Count,
            Creator: cd.Creator?.Name ?? cd.Creator?.Nick ?? "",
            CoverUrl: cd.CoverUrl ?? "",
            Tags: cd.Tags is { Count: > 0 } ? string.Join(" / ", cd.Tags) : "",
            CreateTime: cd.CreateTime ?? 0,
            Songs: tracks
        );
        return new PlaylistFetchOutcome(result, json);
    }

    /// <summary>
    /// 生成精美的歌单 Markdown（含封面、统计、表格） (^ω^)～
    /// </summary>
    public static string ToMarkdown(PlaylistResult result)
    {
        string Esc(string? s) => (s ?? "").Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ").Trim();
        string Fmt(int sec)
        {
            var m = sec / 60;
            var s = sec % 60;
            return $"{m}:{s:D2}";
        }

        // 总时长
        var totalSec = result.Songs.Sum(t => t.DurationSec);
        var th = totalSec / 3600;
        var tm = (totalSec % 3600) / 60;
        var ts = totalSec % 60;
        var totalDur = th > 0 ? $"{th}小时{tm:D2}分" : $"{tm}分{ts:D2}秒";

        // 平均时长
        var avgSec = result.Songs.Count > 0 ? totalSec / result.Songs.Count : 0;
        var avgDur = $"{avgSec / 60}:{avgSec % 60:D2}";

        // 创建时间
        string createTime = "";
        if (result.CreateTime > 0)
        {
            try
            {
                createTime = DateTimeOffset.FromUnixTimeSeconds(result.CreateTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            }
            catch { /* ignore */ }
        }

        var nameLen = result.Name.Length;
        var bar = new string('═', Math.Max(nameLen + 6, 30));

        var sb = new StringBuilder();
        sb.AppendLine(bar);
        sb.Append("  🎵  ").AppendLine(result.Name);
        sb.AppendLine(bar);
        sb.AppendLine();

        // 元信息卡片
        sb.Append("  🆔  ID          : ").AppendLine(result.Id);
        if (!string.IsNullOrEmpty(result.Creator))
            sb.Append("  👤  创建者      : ").AppendLine(result.Creator);
        if (!string.IsNullOrEmpty(createTime))
            sb.Append("  📅  创建时间    : ").AppendLine(createTime);
        sb.Append("  📊  歌曲总数    : ").Append(result.TotalCount).AppendLine(" 首");
        sb.Append("  ⏱   总时长      : ").Append(totalDur).Append("   (平均 ").Append(avgDur).AppendLine("/首)");
        if (!string.IsNullOrEmpty(result.Tags))
            sb.Append("  🏷  标签        : ").AppendLine(result.Tags);
        if (!string.IsNullOrEmpty(result.CoverUrl))
        {
            sb.AppendLine();
            sb.Append("  🖼  封面        : ").AppendLine(result.CoverUrl);
        }
        if (!string.IsNullOrWhiteSpace(result.Desc))
        {
            sb.AppendLine();
            sb.AppendLine("  📝  简介:");
            foreach (var line in result.Desc.Split('\n'))
                sb.Append("      ").AppendLine(line.TrimEnd());
        }

        sb.AppendLine();
        sb.AppendLine("─".PadRight(80, '─'));
        sb.AppendLine();

        // 歌曲表
        sb.AppendLine("| #  | 歌名 | 歌手 | 专辑 | 时长 |");
        sb.AppendLine("|----|------|------|------|------|");

        foreach (var t in result.Songs)
        {
            sb.Append("| ").Append(t.Index.ToString().PadLeft(3))
              .Append(" | ").Append(Esc(t.Name))
              .Append(" | ").Append(Esc(t.Artist))
              .Append(" | ").Append(Esc(t.Album))
              .Append(" | ").Append(Fmt(t.DurationSec).PadLeft(6))
              .AppendLine(" |");
        }

        sb.AppendLine();
        sb.Append("—— 共 ").Append(result.Songs.Count).Append(" 首 · 总时长 ").Append(totalDur).AppendLine(" ——");
        sb.AppendLine();
        sb.Append("🎵 QqPlaylist · 抓取于 ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).AppendLine();

        return sb.ToString();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// 简单 UTF-8 BOM 检测 + ASCII 友好判定
    /// </summary>
    private static bool TryDetectUtf8(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return true;
        // 全 ASCII 视为 UTF-8 安全
        foreach (var b in bytes)
            if (b > 0x7F) return false;
        return true;
    }

    // ─── DTO ───
    private sealed class ApiResponse
    {
        [JsonPropertyName("data")] public ApiData? Data { get; set; }
    }

    private sealed class ApiData
    {
        [JsonPropertyName("cdlist")] public List<CdEntry>? Cdlist { get; set; }
    }

    private sealed class CdEntry
    {
        [JsonPropertyName("disstid")] public string? Disstid { get; set; }
        [JsonPropertyName("dissname")] public string? Dissname { get; set; }
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        [JsonPropertyName("cur_song_num")] public int? CurSongNum { get; set; }
        [JsonPropertyName("songlist")] public List<SongEntry>? Songlist { get; set; }
        [JsonPropertyName("dir_pic_url2")] public string? CoverUrl { get; set; }
        [JsonPropertyName("headurl")] public string? CreatorHead { get; set; }
        [JsonPropertyName("tagname")] public List<string>? Tags { get; set; }
        [JsonPropertyName("ctime")] public long? CreateTime { get; set; }
        [JsonPropertyName("creator")] public CreatorInfo? Creator { get; set; }
    }

    private sealed class CreatorInfo
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("nick")] public string? Nick { get; set; }
    }

    private sealed class SongEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("singer")] public List<SingerEntry>? Singer { get; set; }
        [JsonPropertyName("album")] public AlbumEntry? Album { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
    }

    private sealed class SingerEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private sealed class AlbumEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}

internal sealed record PlaylistResult(
    string Id,
    string Name,
    string Desc,
    int TotalCount,
    string Creator,
    string CoverUrl,
    string Tags,
    long CreateTime,
    List<Track> Songs);

internal sealed record PlaylistFetchOutcome(PlaylistResult Result, string RawJson);

internal sealed record Track(int Index, string Name, string Artist, string Album, int DurationSec);

internal sealed class PlaylistFetchException(string message) : Exception(message);