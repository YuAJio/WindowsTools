using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QqPlaylist;

/// <summary>
/// 把 QQ 音乐 Cookie 用 Windows DPAPI 加密后保存到本地 (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 绑定当前 Windows 用户 + 当前机器，其他用户/机器解不开
/// </summary>
internal static class CookieStore
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QqPlaylist");

    // 应用专属盐值，让别人即使拿到文件也得知道这个常量才能破解
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("QqPlaylist.NingQingHan.v1");

    private static readonly string LastUinPath = Path.Combine(BaseDir, "last_uin.json");

    public static string StorePath => BaseDir;

    // ════════ Cookie 加密存取 ════════

    public static void SaveCookie(string uin, string cookie)
    {
        if (string.IsNullOrWhiteSpace(uin))
            throw new ArgumentException("QQ 号不能为空", nameof(uin));
        if (string.IsNullOrWhiteSpace(cookie))
            throw new ArgumentException("Cookie 不能为空", nameof(cookie));

        Directory.CreateDirectory(BaseDir);
        var plain = Encoding.UTF8.GetBytes(cookie);
        var protectedBytes = ProtectedData.Protect(
            plain,
            Entropy,
            DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath(uin), protectedBytes);

        SaveLastUin(uin);
    }

    public static string? LoadCookie(string uin)
    {
        if (string.IsNullOrWhiteSpace(uin)) return null;
        var path = FilePath(uin);
        if (!File.Exists(path)) return null;
        try
        {
            var protectedBytes = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }

    public static bool CookieExists(string uin)
        => !string.IsNullOrWhiteSpace(uin) && File.Exists(FilePath(uin));

    public static void DeleteCookie(string uin)
    {
        var path = FilePath(uin);
        if (File.Exists(path)) File.Delete(path);
    }

    // ════════ 最近使用 QQ 号（明文 JSON） ════════

    public static string? LoadLastUin()
    {
        try
        {
            if (!File.Exists(LastUinPath)) return null;
            var json = File.ReadAllText(LastUinPath);
            var data = JsonSerializer.Deserialize<LastUinDto>(json);
            return data?.Uin;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLastUin(string uin)
    {
        try
        {
            Directory.CreateDirectory(BaseDir);
            var json = JsonSerializer.Serialize(new LastUinDto(uin));
            File.WriteAllText(LastUinPath, json);
        }
        catch
        {
            // best-effort
        }
    }

    public static void ClearLastUin()
    {
        if (File.Exists(LastUinPath)) File.Delete(LastUinPath);
    }

    private static string FilePath(string uin)
        => Path.Combine(BaseDir, $"cookie_{Sanitize(uin)}.dat");

    private static string Sanitize(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Where(c => !invalid.Contains(c)).ToArray());
    }

    private sealed record LastUinDto(string Uin);
}