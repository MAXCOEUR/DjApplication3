using System.Text.Json;
using System.Text.Json.Nodes;

namespace DjApplication3.DataSource;

public sealed record YtMusicPlaylistItem(string Id, string Name);

public sealed record YtMusicSongItem(string Id, string Title, string Artists);

public static class YtMusicResponseParser
{
    public static IReadOnlyList<YtMusicPlaylistItem> ParsePlaylists(string json)
    {
        var root = Parse(json);
        var playlists = new Dictionary<string, YtMusicPlaylistItem>(StringComparer.Ordinal);

        foreach (var renderer in FindNamedObjects(root, "musicTwoRowItemRenderer"))
        {
            var browseId = GetString(renderer, "navigationEndpoint", "browseEndpoint", "browseId");
            if (string.IsNullOrWhiteSpace(browseId)
                || (!browseId.StartsWith("VL", StringComparison.Ordinal)
                    && !browseId.StartsWith("PL", StringComparison.Ordinal)))
            {
                continue;
            }

            var name = FirstRunText(renderer["title"]);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var id = browseId.StartsWith("VL", StringComparison.Ordinal) ? browseId[2..] : browseId;
            playlists.TryAdd(id, new YtMusicPlaylistItem(id, name));
        }

        return playlists.Values.ToList();
    }

    public static IReadOnlyList<YtMusicSongItem> ParseSongs(string json)
    {
        var root = Parse(json);
        var songs = new Dictionary<string, YtMusicSongItem>(StringComparer.Ordinal);

        foreach (var renderer in FindNamedObjects(root, "musicResponsiveListItemRenderer"))
        {
            var videoId = GetString(renderer, "playlistItemData", "videoId")
                ?? FindFirstNamedString(renderer, "videoId");
            if (string.IsNullOrWhiteSpace(videoId))
            {
                continue;
            }

            var columns = renderer["flexColumns"] as JsonArray;
            var title = columns is { Count: > 0 }
                ? FirstRunText(columns[0]?["musicResponsiveListItemFlexColumnRenderer"]?["text"])
                : null;
            title ??= FirstRunText(renderer["title"]);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var artists = columns is { Count: > 1 }
                ? ArtistNames(columns[1]?["musicResponsiveListItemFlexColumnRenderer"]?["text"])
                : [];

            songs.TryAdd(videoId, new YtMusicSongItem(
                videoId,
                title,
                artists.Count == 0 ? "Artiste Inconnu" : string.Join(", ", artists)));
        }

        return songs.Values.ToList();
    }

    public static bool IsAuthenticationRequired(string json)
    {
        var root = Parse(json);
        foreach (var text in FindAllStringValues(root))
        {
            if (text.Contains("sign in", StringComparison.OrdinalIgnoreCase)
                || text.Contains("se connecter", StringComparison.OrdinalIgnoreCase)
                || text.Contains("connectez-vous", StringComparison.OrdinalIgnoreCase)
                || text.Contains("authentication required", StringComparison.OrdinalIgnoreCase)
                || text.Contains("unauthenticated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string? GetContinuationToken(string json)
    {
        var root = Parse(json);
        foreach (var nextData in FindNamedObjects(root, "nextContinuationData"))
        {
            var token = GetString(nextData, "continuation");
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        foreach (var command in FindNamedObjects(root, "continuationCommand"))
        {
            var token = GetString(command, "token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        return null;
    }

    private static JsonNode Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json) ?? throw new InvalidDataException("Réponse Youtube Music vide.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Réponse Youtube Music JSON invalide.", ex);
        }
    }

    private static IEnumerable<JsonObject> FindNamedObjects(JsonNode? node, string propertyName)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                if (property.Key.Equals(propertyName, StringComparison.Ordinal)
                    && property.Value is JsonObject namedObject)
                {
                    yield return namedObject;
                }

                foreach (var nested in FindNamedObjects(property.Value, propertyName))
                {
                    yield return nested;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var nested in FindNamedObjects(item, propertyName))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<string> FindAllStringValues(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                foreach (var value in FindAllStringValues(property.Value))
                {
                    yield return value;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var value in FindAllStringValues(item))
                {
                    yield return value;
                }
            }
        }
        else if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            yield return text;
        }
    }

    private static string? FindFirstNamedString(JsonNode? node, string propertyName)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                if (property.Key.Equals(propertyName, StringComparison.Ordinal)
                    && property.Value is JsonValue value
                    && value.TryGetValue<string>(out var text))
                {
                    return text;
                }

                var nested = FindFirstNamedString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                var nested = FindFirstNamedString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? FirstRunText(JsonNode? textNode)
    {
        var runs = textNode?["runs"] as JsonArray;
        return runs?[0]?["text"]?.GetValue<string>();
    }

    private static IReadOnlyList<string> ArtistNames(JsonNode? textNode)
    {
        var runs = textNode?["runs"] as JsonArray;
        if (runs is null)
        {
            return [];
        }

        var linkedArtists = runs
            .Where(run => GetString(run, "navigationEndpoint", "browseEndpoint", "browseId")
                ?.StartsWith("UC", StringComparison.Ordinal) == true)
            .Select(run => run?["text"]?.GetValue<string>())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (linkedArtists.Count > 0)
        {
            return linkedArtists;
        }

        var fallback = new List<string>();
        foreach (var run in runs)
        {
            var text = run?["text"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(text) || text.Contains('•'))
            {
                if (fallback.Count > 0)
                {
                    break;
                }

                continue;
            }

            fallback.Add(text.Trim());
        }

        return fallback;
    }

    private static string? GetString(JsonNode? node, params string[] path)
    {
        foreach (var segment in path)
        {
            node = node?[segment];
            if (node is null)
            {
                return null;
            }
        }

        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    }
}
