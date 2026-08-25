using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace QqPlaylist;

internal static class ProfileFetcher
{
    private static readonly HttpClient _http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36");
        c.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://y.qq.com/");
        c.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://y.qq.com");
        return c;
    }

    /// <summary>
    /// QQ 系通用 g_tk 算法（基于 skey cookie）
    /// </summary>
    public static int CalculateGtk(string skey)
    {
        int hash = 5381;
        foreach (char c in skey)
            hash = (hash + (hash << 5) + c) & 0x7fffffff;
        return hash;
    }

    public static string? ExtractSkey(string cookie)
    {
        foreach (var part in cookie.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            var eq = trimmed.IndexOf('=');
            if (eq < 0) continue;
            var name = trimmed[..eq].Trim();
            if (name.Equals("skey", StringComparison.OrdinalIgnoreCase))
                return trimmed[(eq + 1)..].Trim();
        }
        return null;
    }

    public static async Task<ProfileFetchResult> FetchAsync(
        string uin,
        string cookie,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(uin))
            throw new ProfileFetchException("QQ 号不能为空喵~");
        if (string.IsNullOrWhiteSpace(cookie))
            throw new ProfileFetchException("Cookie 不能为空喵~");

        var skey = ExtractSkey(cookie)
            ?? throw new ProfileFetchException("Cookie 里没找到 skey 字段，可能登录已过期喵~");
        var gtk = CalculateGtk(skey);

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var url =
            $"https://c6.y.qq.com/rsc/fcgi-bin/fcg_get_profile_homepage.fcg" +
            $"?_={ts}" +
            $"&cv=4747474&ct=24&format=json" +
            $"&inCharset=utf-8&outCharset=utf-8" +
            $"&notice=0&platform=yqq.json&needNewCode=0" +
            $"&uin={uin}" +
            $"&g_tk_new_20200303={gtk}" +
            $"&g_tk={gtk}" +
            $"&cid=205360838" +
            $"&userid={uin}" +
            $"&reqfrom=1&reqtype=0" +
            $"&hostUin=0&loginUin={uin}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Cookie", cookie);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
        req.Headers.TryAddWithoutValidation("sec-fetch-site", "same-site");
        req.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new ProfileFetchException(
                $"接口返回 HTTP {(int)resp.StatusCode} {resp.StatusCode}，可能 Cookie 过期了喵~");

        // 完整 JSON 写到本地，方便用户把全量数据贴回给宁宁诊断
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QqPlaylist");
        Directory.CreateDirectory(dir);
        var jsonPath = Path.Combine(dir, $"profile_{SanitizeFileName(uin)}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(jsonPath, body, Encoding.UTF8, ct).ConfigureAwait(false);

        // 用 JsonDocument 智能遍历查找歌单
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // code 检查
        if (root.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.Number)
        {
            var code = codeEl.GetInt32();
            if (code != 0)
            {
                var msg = root.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;
                throw new ProfileFetchException($"接口报错 code={code}, msg={msg ?? "(无)"}\n完整 JSON 已保存到：{jsonPath}");
            }
        }

        var data = root.TryGetProperty("data", out var dataEl) ? dataEl : root;

        // 智能查找歌单
        var created = FindPlaylists(data, prefer: "created");
        var collected = FindPlaylists(data, prefer: "collected");

        // 找昵称
        var nick = TryFindNick(data);
        if (string.IsNullOrEmpty(nick)) nick = uin;

        return new ProfileFetchResult(
            Uin: uin,
            Nickname: nick,
            CreatedPlaylists: created,
            CollectedPlaylists: collected,
            RawJsonPath: jsonPath,
            RawJsonLength: body.Length,
            RawJsonPreview: Truncate(body, 5000)
        );
    }

    /// <summary>
    /// 在 data 节点下智能查找所有歌单对象（含 dissid/tid 的对象）
    /// 兼容多种接口结构：data.creator.albumList、data.dissList、data.tabList[].list 等
    /// </summary>
    private static List<UserPlaylist> FindPlaylists(JsonElement data, string prefer)
    {
        var all = new List<UserPlaylist>();
        ScanForPlaylists(data, "<root>", all);

        // 去重（同一首歌单可能在多处出现）
        var deduped = all.GroupBy(p => p.Id).Select(g => g.First()).ToList();

        // 启发式分类：根据父级字段名判断 created/collected
        // 包含 creator/mine/myCreated/own 的归为"创建"
        // 包含 collect/fav/follow/subscribed 的归为"收藏"
        var created = new List<UserPlaylist>();
        var collected = new List<UserPlaylist>();
        var unknown = new List<UserPlaylist>();
        foreach (var p in deduped)
        {
            var src = p.Group.ToLowerInvariant();
            if (src.Contains("creator") || src.Contains("mine") || src.Contains("created") || src.Contains("own") || src.Contains("my"))
                created.Add(p);
            else if (src.Contains("collect") || src.Contains("fav") || src.Contains("sub") || src.Contains("follow"))
                collected.Add(p);
            else
                unknown.Add(p);
        }

        // 如果启发式都没分到，全归为 prefer 指定的那边
        if (created.Count == 0 && collected.Count == 0 && unknown.Count > 0)
        {
            if (prefer == "created") created.AddRange(unknown);
            else collected.AddRange(unknown);
        }
        else
        {
            // 兜底：unknown 合并到 prefer 那一边
            if (prefer == "created") created.AddRange(unknown);
            else collected.AddRange(unknown);
        }

        return prefer == "created" ? created : collected;
    }

    private static void ScanForPlaylists(JsonElement el, string path, List<UserPlaylist> result)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                // 看这个对象自己是不是歌单（含 dissid/tid 且有 dissname）
                if (TryReadPlaylist(el, path, out var pl))
                {
                    result.Add(pl);
                    return; // 不要再下钻
                }
                foreach (var prop in el.EnumerateObject())
                    ScanForPlaylists(prop.Value, path + "." + prop.Name, result);
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    ScanForPlaylists(item, path + "[]", result);
                break;
        }
    }

    private static bool TryReadPlaylist(JsonElement obj, string sourcePath, out UserPlaylist playlist)
    {
        playlist = default!;

        // ── 排除 mymusic[] 子树里的"卡片导航项"（评论 / 我喜欢 / 已购 / 那年今日）──
        if (sourcePath.Contains(".mymusic", StringComparison.OrdinalIgnoreCase))
            return false;

        // ── ID：dissid 优先（QQ 音乐歌单标准字段），可数字可字符串 ──
        string? id = null;
        if (obj.TryGetProperty("dissid", out var idEl))
        {
            if (idEl.ValueKind == JsonValueKind.String) id = idEl.GetString();
            else if (idEl.ValueKind == JsonValueKind.Number) id = idEl.GetInt64().ToString();
        }
        else if (obj.TryGetProperty("tid", out var tidEl))
        {
            if (tidEl.ValueKind == JsonValueKind.String) id = tidEl.GetString();
            else if (tidEl.ValueKind == JsonValueKind.Number) id = tidEl.GetInt64().ToString();
        }
        else if (obj.TryGetProperty("id", out var idEl2))
        {
            // mymusic 已被排除，这里只对纯数字 id 兜底（避免把 creator.encrypt_uin 等误判）
            if (idEl2.ValueKind == JsonValueKind.Number) id = idEl2.GetInt64().ToString();
            else if (idEl2.ValueKind == JsonValueKind.String && long.TryParse(idEl2.GetString(), out _))
                id = idEl2.GetString();
        }

        if (string.IsNullOrEmpty(id)) return false;

        // ── 名字：QQ 实际接口用 "title"，但其他接口可能用 dissname / name ──
        string name = "";
        if (obj.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String) name = tEl.GetString() ?? "";
        else if (obj.TryGetProperty("dissname", out var dnEl) && dnEl.ValueKind == JsonValueKind.String) name = dnEl.GetString() ?? "";
        else if (obj.TryGetProperty("name", out var nEl) && nEl.ValueKind == JsonValueKind.String) name = nEl.GetString() ?? "";

        if (string.IsNullOrEmpty(name)) name = "(无名歌单)";

        // ── 歌曲数：优先 song_cnt / songnum，否则从 subtitle "72首    1055次播放    " 提取 ──
        int songCount = 0;
        if (obj.TryGetProperty("song_cnt", out var cEl) && cEl.ValueKind == JsonValueKind.Number) songCount = cEl.GetInt32();
        else if (obj.TryGetProperty("songnum", out var c2El) && c2El.ValueKind == JsonValueKind.Number) songCount = c2El.GetInt32();
        else if (obj.TryGetProperty("subtitle", out var subEl) && subEl.ValueKind == JsonValueKind.String)
        {
            var m = Regex.Match(subEl.GetString() ?? "", @"(\d+)\s*首");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n)) songCount = n;
        }

        // ── 创建者（可选）──
        string creator = "";
        if (obj.TryGetProperty("creator", out var crEl) && crEl.TryGetProperty("name", out var crName))
            creator = crName.GetString() ?? "";
        else if (obj.TryGetProperty("owner", out var owEl) && owEl.TryGetProperty("name", out var owName))
            creator = owName.GetString() ?? "";

        playlist = new UserPlaylist(id, name, songCount, creator, sourcePath);
        return true;
    }

    private static string? TryFindNick(JsonElement data)
    {
        // 优先从 creator/owner/mineInfo 等用户对象里找 nick/nickname
        if (data.ValueKind != JsonValueKind.Object) return null;
        foreach (var prop in data.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;
            // 不下钻歌单数组（避免误中）
            if (prop.Value.TryGetProperty("dissid", out _)) continue;
            if (prop.Value.TryGetProperty("tid", out _)) continue;

            if (prop.Value.TryGetProperty("nick", out var nickEl) && nickEl.ValueKind == JsonValueKind.String)
                return nickEl.GetString();
            if (prop.Value.TryGetProperty("nickname", out var nnEl) && nnEl.ValueKind == JsonValueKind.String)
                return nnEl.GetString();
        }
        return null;
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + $"\n\n... (截断,共 {s.Length} 字符)";

    private static string SanitizeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Where(c => !invalid.Contains(c)).ToArray());
    }
}

internal sealed record ProfileFetchResult(
    string Uin,
    string? Nickname,
    List<UserPlaylist> CreatedPlaylists,
    List<UserPlaylist> CollectedPlaylists,
    string RawJsonPath,
    int RawJsonLength,
    string RawJsonPreview);

internal sealed record UserPlaylist(
    string Id,
    string Name,
    int SongCount,
    string Creator,
    string Group);

internal sealed class ProfileFetchException(string message) : Exception(message);