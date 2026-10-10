using DjApplication3.DataSource;

namespace DjApplication3.Core.Tests;

public sealed class YtMusicCookieSessionTests
{
    [Fact]
    public void TryCreate_RequiresYoutubeAuthenticationCookies()
    {
        var cookies = new[]
        {
            Cookie("SAPISID", "papisid", ".google.com"),
            Cookie("SID", "sid", ".google.com")
        };

        var created = YtMusicCookieSession.TryCreate(cookies, out var session);

        Assert.False(created);
        Assert.Null(session);
    }

    [Fact]
    public void TryCreate_AcceptsSecureYoutubeAuthenticationCookies()
    {
        var cookies = new[]
        {
            Cookie("__Secure-3PAPISID", "papisid", ".youtube.com"),
            Cookie("__Secure-3PSID", "sid", ".youtube.com"),
            Cookie("PREF", "hl=fr", ".youtube.com")
        };

        var created = YtMusicCookieSession.TryCreate(cookies, out var session);

        Assert.True(created);
        Assert.NotNull(session);
        Assert.Contains("__Secure-3PAPISID=papisid", session.CookieHeader);
        Assert.DoesNotContain(".google.com", session.CookieHeader);
    }

    [Fact]
    public void TryCreate_AcceptsFirstPartyYoutubeAuthenticationCookies()
    {
        var cookies = new[]
        {
            Cookie("__Secure-1PAPISID", "papisid", ".youtube.com"),
            Cookie("__Secure-1PSID", "sid", ".youtube.com")
        };

        var created = YtMusicCookieSession.TryCreate(cookies, out var session);

        Assert.True(created);
        Assert.NotNull(session);
    }

    [Fact]
    public void CreateAuthorizationHeader_UsesSapisidHashFormat()
    {
        var cookies = new[]
        {
            Cookie("SAPISID", "secret", ".youtube.com"),
            Cookie("SID", "sid", ".youtube.com")
        };
        Assert.True(YtMusicCookieSession.TryCreate(cookies, out var session));

        var header = session!.CreateAuthorizationHeader(
            new Uri("https://music.youtube.com/youtubei/v1/browse"),
            1_700_000_000);

        Assert.Matches("^SAPISIDHASH 1700000000_[0-9a-f]{40}$", header);
    }

    private static CookieModel Cookie(string name, string value, string domain)
        => new() { Name = name, Value = value, Domain = domain, Path = "/" };
}
