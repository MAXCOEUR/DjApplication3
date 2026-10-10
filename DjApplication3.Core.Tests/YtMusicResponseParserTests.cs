using DjApplication3.DataSource;

namespace DjApplication3.Core.Tests;

public sealed class YtMusicResponseParserTests
{
    [Fact]
    public void ParsePlaylists_FindsItemsWithoutDependingOnSectionIndexes()
    {
        const string json = """
        {
          "contents": {
            "changedLayout": {
              "items": [
                {
                  "musicTwoRowItemRenderer": {
                    "title": { "runs": [{ "text": "Ma playlist" }] },
                    "navigationEndpoint": {
                      "browseEndpoint": { "browseId": "VLPL123" }
                    }
                  }
                }
              ]
            }
          }
        }
        """;

        var playlists = YtMusicResponseParser.ParsePlaylists(json);

        var playlist = Assert.Single(playlists);
        Assert.Equal("PL123", playlist.Id);
        Assert.Equal("Ma playlist", playlist.Name);
    }

    [Fact]
    public void ParseSongs_FindsResponsiveRowsAndArtists()
    {
        const string json = """
        {
          "contents": [
            {
              "musicResponsiveListItemRenderer": {
                "playlistItemData": { "videoId": "video123" },
                "flexColumns": [
                  { "musicResponsiveListItemFlexColumnRenderer": { "text": { "runs": [{ "text": "Titre" }] } } },
                  { "musicResponsiveListItemFlexColumnRenderer": { "text": { "runs": [
                    { "text": "Artiste 1", "navigationEndpoint": { "browseEndpoint": { "browseId": "UC1" } } },
                    { "text": " • " },
                    { "text": "Artiste 2", "navigationEndpoint": { "browseEndpoint": { "browseId": "UC2" } } }
                  ] } } }
                ]
              }
            }
          ]
        }
        """;

        var songs = YtMusicResponseParser.ParseSongs(json);

        var song = Assert.Single(songs);
        Assert.Equal("video123", song.Id);
        Assert.Equal("Titre", song.Title);
        Assert.Equal("Artiste 1, Artiste 2", song.Artists);
    }

    [Fact]
    public void AuthenticationRequired_DetectsSignedOutResponse()
    {
        const string json = """
        { "contents": { "messageRenderer": { "text": { "runs": [{ "text": "Sign in to see your library" }] } } } }
        """;

        Assert.True(YtMusicResponseParser.IsAuthenticationRequired(json));
    }

    [Fact]
    public void EmptyAuthenticatedLibrary_IsNotAnAuthenticationFailure()
    {
        const string json = """{ "contents": { "sectionListRenderer": { "contents": [] } } }""";

        Assert.Empty(YtMusicResponseParser.ParsePlaylists(json));
        Assert.False(YtMusicResponseParser.IsAuthenticationRequired(json));
    }

    [Fact]
    public void GetContinuationToken_FindsTokenAtAnySupportedDepth()
    {
        const string json = """
        { "continuations": [{ "nextContinuationData": { "continuation": "next-token" } }] }
        """;

        Assert.Equal("next-token", YtMusicResponseParser.GetContinuationToken(json));
    }
}
