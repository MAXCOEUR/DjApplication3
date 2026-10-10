using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DjApplication3.DataSource;

public sealed class YtMusicAuthenticationException : Exception
{
    public YtMusicAuthenticationException(string message) : base(message)
    {
    }
}

public sealed class YtMusicInnertubeClient
{
    private const string MusicOrigin = "https://music.youtube.com";
    private const string DefaultApiKey = "AIzaSyA8eiZmM1FaDVjRy-df2KTyQ_vz_yYM39w";
    private const string DefaultClientVersion = "1.20260715.04.00";
    private const string SongSearchFilter = "EgWKAQIIAWoKEAkQBRAKEAMQBA%3D%3D";
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();
    private readonly YtMusicCookieSession? _session;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
    private BootstrapData? _bootstrap;

    public YtMusicInnertubeClient(YtMusicCookieSession? session, HttpClient? httpClient = null)
    {
        _session = session;
        _httpClient = httpClient ?? SharedHttpClient;
    }

    public async Task<IReadOnlyList<YtMusicPlaylistItem>> GetPlaylistsAsync(
        CancellationToken cancellationToken = default)
    {
        RequireSession();
        var playlists = new Dictionary<string, YtMusicPlaylistItem>(StringComparer.Ordinal);
        var seenContinuations = new HashSet<string>(StringComparer.Ordinal);
        string? continuation = null;

        for (var page = 0; page < 100; page++)
        {
            var payload = continuation is null
                ? new Dictionary<string, object?> { ["browseId"] = "FEmusic_liked_playlists" }
                : new Dictionary<string, object?> { ["continuation"] = continuation };
            var json = await SendInnertubeAsync("browse", payload, cancellationToken);
            EnsureAuthenticated(json);

            foreach (var playlist in YtMusicResponseParser.ParsePlaylists(json))
            {
                playlists.TryAdd(playlist.Id, playlist);
            }

            var next = YtMusicResponseParser.GetContinuationToken(json);
            if (string.IsNullOrWhiteSpace(next) || !seenContinuations.Add(next))
            {
                break;
            }

            continuation = next;
        }

        return playlists.Values.ToList();
    }

    public async Task<IReadOnlyList<YtMusicSongItem>> GetLikedSongsAsync(
        CancellationToken cancellationToken = default)
    {
        RequireSession();
        var songs = new Dictionary<string, YtMusicSongItem>(StringComparer.Ordinal);
        var seenContinuations = new HashSet<string>(StringComparer.Ordinal);
        string? continuation = null;

        for (var page = 0; page < 100; page++)
        {
            var payload = continuation is null
                ? new Dictionary<string, object?> { ["browseId"] = "FEmusic_liked_videos" }
                : new Dictionary<string, object?> { ["continuation"] = continuation };
            var json = await SendInnertubeAsync("browse", payload, cancellationToken);
            EnsureAuthenticated(json);

            foreach (var song in YtMusicResponseParser.ParseSongs(json))
            {
                songs.TryAdd(song.Id, song);
            }

            var next = YtMusicResponseParser.GetContinuationToken(json);
            if (string.IsNullOrWhiteSpace(next) || !seenContinuations.Add(next))
            {
                break;
            }

            continuation = next;
        }

        return songs.Values.ToList();
    }

    public async Task<IReadOnlyList<YtMusicSongItem>> GetPlaylistSongsAsync(
        string playlistId,
        IProgress<IReadOnlyList<YtMusicSongItem>>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("Identifiant de playlist Youtube Music manquant.", nameof(playlistId));
        }

        RequireSession();
        var songs = new Dictionary<string, YtMusicSongItem>(StringComparer.Ordinal);
        var seenContinuations = new HashSet<string>(StringComparer.Ordinal);
        string? continuation = null;
        var page = 0;

        do
        {
            var payload = new Dictionary<string, object?>();
            if (continuation is null)
            {
                payload["browseId"] = playlistId.StartsWith("VL", StringComparison.Ordinal)
                    ? playlistId
                    : "VL" + playlistId;
            }
            else
            {
                payload["continuation"] = continuation;
            }

            var json = await SendInnertubeAsync("browse", payload, cancellationToken);
            EnsureAuthenticated(json);
            var batch = YtMusicResponseParser.ParseSongs(json)
                .Where(song => songs.TryAdd(song.Id, song))
                .ToList();
            if (batch.Count > 0)
            {
                progress?.Report(batch);
            }

            var next = YtMusicResponseParser.GetContinuationToken(json);
            page++;

            if (string.IsNullOrWhiteSpace(next) || !seenContinuations.Add(next))
            {
                continuation = null;
            }
            else
            {
                continuation = next;
            }
        }
        while (!string.IsNullOrWhiteSpace(continuation) && page < 100);

        return songs.Values.ToList();
    }

    public async Task<IReadOnlyList<YtMusicSongItem>> SearchSongsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            query = "musique";
        }

        var json = await SendInnertubeAsync(
            "search",
            new Dictionary<string, object?>
            {
                ["query"] = query,
                ["params"] = SongSearchFilter
            },
            cancellationToken);
        return YtMusicResponseParser.ParseSongs(json);
    }

    private async Task<string> SendInnertubeAsync(
        string endpoint,
        Dictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        var bootstrap = await GetBootstrapAsync(cancellationToken);
        var payload = new Dictionary<string, object?>(values)
        {
            ["context"] = new
            {
                client = new
                {
                    clientName = "WEB_REMIX",
                    clientVersion = bootstrap.ClientVersion,
                    browserName = "Chrome",
                    browserVersion = "142.0.0.0",
                    osName = "Windows",
                    osVersion = "10.0",
                    platform = "DESKTOP",
                    hl = "fr",
                    gl = "FR",
                    visitorData = bootstrap.VisitorData
                }
            }
        };

        var uri = new Uri($"{MusicOrigin}/youtubei/v1/{endpoint}?key={Uri.EscapeDataString(bootstrap.ApiKey)}&prettyPrint=false");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        PrepareRequest(request, bootstrap);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (_session is not null && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new YtMusicAuthenticationException(
                    "La session Youtube Music a été refusée. Reconnecte ton compte.");
            }

            throw new HttpRequestException(
                $"Youtube Music a répondu {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode);
        }

        return body;
    }

    private async Task<BootstrapData> GetBootstrapAsync(CancellationToken cancellationToken)
    {
        if (_bootstrap is not null)
        {
            return _bootstrap;
        }

        await _bootstrapLock.WaitAsync(cancellationToken);
        try
        {
            if (_bootstrap is not null)
            {
                return _bootstrap;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, MusicOrigin + "/");
            PrepareRequest(request, null);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _bootstrap = new BootstrapData(DefaultApiKey, DefaultClientVersion, null, "0");
                return _bootstrap;
            }

            _bootstrap = new BootstrapData(
                ExtractConfig(html, "INNERTUBE_API_KEY") ?? DefaultApiKey,
                ExtractConfig(html, "INNERTUBE_CLIENT_VERSION")
                    ?? ExtractConfig(html, "INNERTUBE_CONTEXT_CLIENT_VERSION")
                    ?? DefaultClientVersion,
                ExtractConfig(html, "VISITOR_DATA"),
                ExtractNumericConfig(html, "SESSION_INDEX") ?? "0");
            return _bootstrap;
        }
        finally
        {
            _bootstrapLock.Release();
        }
    }

    private void PrepareRequest(HttpRequestMessage request, BootstrapData? bootstrap)
    {
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
        if (request.Method != HttpMethod.Get)
        {
            request.Headers.Referrer = new Uri(MusicOrigin + "/");
            request.Headers.TryAddWithoutValidation("Origin", MusicOrigin);
            request.Headers.TryAddWithoutValidation("X-Origin", MusicOrigin);
            request.Headers.TryAddWithoutValidation("X-Youtube-Client-Name", "67");
        }

        if (bootstrap is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Youtube-Client-Version", bootstrap.ClientVersion);
            if (!string.IsNullOrWhiteSpace(bootstrap.VisitorData))
            {
                request.Headers.TryAddWithoutValidation("X-Goog-Visitor-Id", bootstrap.VisitorData);
            }
        }

        if (_session is null)
        {
            return;
        }

        request.Headers.TryAddWithoutValidation("Cookie", _session.CookieHeader);
        if (request.Method == HttpMethod.Get)
        {
            return;
        }

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(
            _session.CreateAuthorizationHeader(request.RequestUri!));
        request.Headers.TryAddWithoutValidation("X-Goog-AuthUser", bootstrap?.SessionIndex ?? "0");
        request.Headers.TryAddWithoutValidation("X-Youtube-Bootstrap-Logged-In", "true");
    }

    private void RequireSession()
    {
        if (_session is null)
        {
            throw new YtMusicAuthenticationException(
                "Aucune session Youtube Music valide. Connecte ton compte depuis les options.");
        }
    }

    private static void EnsureAuthenticated(string json)
    {
        if (YtMusicResponseParser.IsAuthenticationRequired(json))
        {
            throw new YtMusicAuthenticationException(
                "La session Youtube Music n'est plus authentifiée. Reconnecte ton compte.");
        }
    }

    private static string? ExtractConfig(string html, string name)
    {
        var match = Regex.Match(
            html,
            $"[\\\"']{Regex.Escape(name)}[\\\"']\\s*:\\s*[\\\"'](?<value>[^\\\"']+)[\\\"']",
            RegexOptions.CultureInvariant);
        return match.Success ? Regex.Unescape(match.Groups["value"].Value) : null;
    }

    private static string? ExtractNumericConfig(string html, string name)
    {
        var match = Regex.Match(
            html,
            $"[\\\"']{Regex.Escape(name)}[\\\"']\\s*:\\s*[\\\"']?(?<value>\\d+)[\\\"']?",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    private sealed record BootstrapData(
        string ApiKey,
        string ClientVersion,
        string? VisitorData,
        string SessionIndex);
}
