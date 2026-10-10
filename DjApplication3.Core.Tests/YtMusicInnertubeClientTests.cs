using DjApplication3.DataSource;
using System.Net;
using System.Text;

namespace DjApplication3.Core.Tests;

public sealed class YtMusicInnertubeClientTests
{
    [Fact]
    public async Task GetPlaylistsAsync_UsesCurrentBootstrapAndAuthenticatedHeaders()
    {
        var handler = new QueueHttpHandler(
            Html("""<script>ytcfg.set({"INNERTUBE_API_KEY":"dynamic-key","INNERTUBE_CLIENT_VERSION":"1.20261010.01.00","VISITOR_DATA":"visitor","SESSION_INDEX":"2"});</script>"""),
            Json("""{"contents":{"musicTwoRowItemRenderer":{"title":{"runs":[{"text":"Mix"}]},"navigationEndpoint":{"browseEndpoint":{"browseId":"VLPL42"}}}}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(CreateSession(), httpClient);

        var playlists = await client.GetPlaylistsAsync();

        Assert.Equal("PL42", Assert.Single(playlists).Id);
        var request = Assert.Single(handler.Requests, item => item.Method == HttpMethod.Post);
        Assert.Contains("key=dynamic-key", request.Uri.Query);
        Assert.StartsWith("SAPISIDHASH ", request.Authorization);
        Assert.Contains("__Secure-3PAPISID=papisid", request.Cookie);
        Assert.Equal("2", request.AuthUser);
        Assert.Contains("1.20261010.01.00", request.Body);
        Assert.Contains("visitor", request.Body);
    }

    [Fact]
    public async Task GetPlaylistsAsync_ThrowsAuthenticationExceptionForSignedOutPayload()
    {
        var handler = new QueueHttpHandler(
            Html("""{"INNERTUBE_API_KEY":"key","INNERTUBE_CLIENT_VERSION":"version"}"""),
            Json("""{"contents":{"messageRenderer":{"text":{"runs":[{"text":"Sign in to see your library"}]}}}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(CreateSession(), httpClient);

        await Assert.ThrowsAsync<YtMusicAuthenticationException>(() => client.GetPlaylistsAsync());
    }

    [Fact]
    public async Task GetPlaylistsAsync_FollowsContinuationsAndDeduplicatesItems()
    {
        var handler = new QueueHttpHandler(
            Html("""{"INNERTUBE_API_KEY":"key","INNERTUBE_CLIENT_VERSION":"version"}"""),
            Json("""{"musicTwoRowItemRenderer":{"title":{"runs":[{"text":"Première"}]},"navigationEndpoint":{"browseEndpoint":{"browseId":"VLPL1"}}},"nextContinuationData":{"continuation":"next"}}"""),
            Json("""{"contents":[{"musicTwoRowItemRenderer":{"title":{"runs":[{"text":"Première"}]},"navigationEndpoint":{"browseEndpoint":{"browseId":"VLPL1"}}}},{"musicTwoRowItemRenderer":{"title":{"runs":[{"text":"Deuxième"}]},"navigationEndpoint":{"browseEndpoint":{"browseId":"VLPL2"}}}}]}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(CreateSession(), httpClient);

        var playlists = await client.GetPlaylistsAsync();

        Assert.Equal(2, playlists.Count);
        Assert.Contains("\"continuation\":\"next\"", handler.Requests.Last().Body);
    }

    [Fact]
    public async Task GetLikedSongsAsync_FollowsContinuations()
    {
        var handler = new QueueHttpHandler(
            Html("""{"INNERTUBE_API_KEY":"key","INNERTUBE_CLIENT_VERSION":"version"}"""),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v1"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Un"}]}}}]},"nextContinuationData":{"continuation":"next"}}"""),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v2"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Deux"}]}}}]}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(CreateSession(), httpClient);

        var songs = await client.GetLikedSongsAsync();

        Assert.Equal(["v1", "v2"], songs.Select(song => song.Id));
    }

    [Fact]
    public async Task GetPlaylistSongsAsync_StopsWhenContinuationTokenRepeats()
    {
        var handler = new QueueHttpHandler(
            Html("""{"INNERTUBE_API_KEY":"key","INNERTUBE_CLIENT_VERSION":"version"}"""),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v1"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Un"}]}}}]},"nextContinuationData":{"continuation":"same"}}"""),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v2"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Deux"}]}}}]},"nextContinuationData":{"continuation":"same"}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(CreateSession(), httpClient);

        var songs = await client.GetPlaylistSongsAsync("PL1");

        Assert.Equal(["v1", "v2"], songs.Select(song => song.Id));
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task SearchSongsAsync_WorksWithoutAuthenticatedSession()
    {
        var handler = new QueueHttpHandler(
            Html("""{"INNERTUBE_API_KEY":"key","INNERTUBE_CLIENT_VERSION":"version"}"""),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v1"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Titre"}]}}},{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Artiste"}]}}}]}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(null, httpClient);

        var songs = await client.SearchSongsAsync("titre");

        Assert.Equal("v1", Assert.Single(songs).Id);
        Assert.Empty(handler.Requests.Last().Authorization);
    }

    [Fact]
    public async Task SearchSongsAsync_UsesFallbackBootstrapWhenConsentPageRejectsGet()
    {
        var handler = new QueueHttpHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            Json("""{"musicResponsiveListItemRenderer":{"playlistItemData":{"videoId":"v2"},"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Titre"}]}}}]}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new YtMusicInnertubeClient(null, httpClient);

        var songs = await client.SearchSongsAsync("titre");

        Assert.Equal("v2", Assert.Single(songs).Id);
        Assert.Contains("key=", handler.Requests.Last().Uri.Query);
    }

    private static YtMusicCookieSession CreateSession()
    {
        var cookies = new[]
        {
            new CookieModel { Name = "__Secure-3PAPISID", Value = "papisid", Domain = ".youtube.com", Path = "/" },
            new CookieModel { Name = "__Secure-3PSID", Value = "sid", Domain = ".youtube.com", Path = "/" }
        };
        Assert.True(YtMusicCookieSession.TryCreate(cookies, out var session));
        return session!;
    }

    private static HttpResponseMessage Html(string value)
        => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "text/html") };

    private static HttpResponseMessage Json(string value)
        => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    private sealed class QueueHttpHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.ToString() ?? "",
                request.Headers.TryGetValues("Cookie", out var cookies) ? string.Join(";", cookies) : "",
                request.Headers.TryGetValues("X-Goog-AuthUser", out var authUsers) ? authUsers.Single() : "",
                request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            return _responses.Dequeue();
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        string Authorization,
        string Cookie,
        string AuthUser,
        string Body);
}
