using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DjApplication3.DataSource;

public sealed class YtMusicCookieSession
{
    private static readonly string[] PapisidNames = ["__Secure-3PAPISID", "__Secure-1PAPISID", "SAPISID"];
    private static readonly string[] SessionNames = ["__Secure-3PSID", "__Secure-1PSID", "SID"];
    private readonly IReadOnlyList<CookieModel> _cookies;
    private readonly string _papisid;

    private YtMusicCookieSession(IReadOnlyList<CookieModel> cookies, string papisid)
    {
        _cookies = cookies;
        _papisid = papisid;
        CookieHeader = string.Join("; ", cookies
            .GroupBy(cookie => cookie.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(cookie => $"{cookie.Name}={cookie.Value}"));
    }

    public string CookieHeader { get; }

    public IReadOnlyList<CookieModel> Cookies => _cookies;

    public static bool TryLoad(string path, out YtMusicCookieSession? session)
    {
        session = null;
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            return false;
        }

        try
        {
            var cookies = JsonSerializer.Deserialize<List<CookieModel>>(File.ReadAllText(path));
            return TryCreate(cookies ?? [], out session);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static bool TryCreate(IEnumerable<CookieModel> cookies, out YtMusicCookieSession? session)
    {
        session = null;
        var youtubeCookies = cookies
            .Where(IsUsableYoutubeCookie)
            .ToList();

        var papisid = FindCookieValue(youtubeCookies, PapisidNames);
        var sid = FindCookieValue(youtubeCookies, SessionNames);
        if (string.IsNullOrWhiteSpace(papisid) || string.IsNullOrWhiteSpace(sid))
        {
            return false;
        }

        session = new YtMusicCookieSession(youtubeCookies, papisid);
        return true;
    }

    public string CreateAuthorizationHeader(Uri uri, long? unixTimeSeconds = null)
    {
        var timestamp = unixTimeSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var origin = $"{uri.Scheme}://{uri.Host}";
        var input = $"{timestamp} {_papisid} {origin}";
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return $"SAPISIDHASH {timestamp}_{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public IReadOnlyList<Cookie> ToSystemNetCookies()
    {
        var result = new List<Cookie>();
        foreach (var cookie in _cookies)
        {
            try
            {
                result.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }
            catch (CookieException)
            {
                // A malformed optional cookie must not invalidate the entire session.
            }
        }

        return result;
    }

    private static bool IsUsableYoutubeCookie(CookieModel cookie)
    {
        if (string.IsNullOrWhiteSpace(cookie.Name)
            || string.IsNullOrWhiteSpace(cookie.Value)
            || string.IsNullOrWhiteSpace(cookie.Path)
            || string.IsNullOrWhiteSpace(cookie.Domain))
        {
            return false;
        }

        var domain = cookie.Domain.TrimStart('.');
        return "music.youtube.com".Equals(domain, StringComparison.OrdinalIgnoreCase)
            || "music.youtube.com".EndsWith('.' + domain, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindCookieValue(IEnumerable<CookieModel> cookies, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var value = cookies.FirstOrDefault(cookie =>
                cookie.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
