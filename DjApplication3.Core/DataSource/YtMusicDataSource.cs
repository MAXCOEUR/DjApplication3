using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.Infrastructure;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Pagination;

namespace DjApplication3.DataSource
{

    public class NotConnectedException : Exception
    {
        public NotConnectedException(string message) : base(message) { }
    }

    public class CookieModel
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Path { get; set; } = "/";
        public string Domain { get; set; } = "";
    }

    public class YtMusicDataSource
    {
        private readonly YoutubeClient _youtube = new YoutubeClient();
        private readonly YouTubeMusicClient _ytMusicClient;
        private readonly YtMusicCookieSession? _session;
        private readonly YtMusicInnertubeClient _innertubeClient;
        private const string baseUrl = "https://music.youtube.com/watch?v=";

        // Fichier pour stocker tes cookies/session
        private static string appPath => AppPaths.BaseDirectory;
        private static string pathOutilsExtern => AppPaths.ExternalToolsDirectory;
        public static string sessionFile => AppPaths.SessionCookieFile;
        public static string ytdlpCookieFile => AppPaths.YtDlpCookieFile;

        public YtMusicDataSource()
        {
            YtMusicCookieSession.TryLoad(sessionFile, out _session);
            var cookies = _session?.ToSystemNetCookies();
            _ytMusicClient = new YouTubeMusicClient(cookies: cookies is { Count: > 0 } ? cookies : null);
            _innertubeClient = new YtMusicInnertubeClient(_session);
        }

        public async Task<List<Musique>> search(string search, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(search)) search = "musique";
            Exception? packageFailure = null;
            try
            {
                PaginatedAsyncEnumerable<SearchResult> searchResults =
                    _ytMusicClient.SearchAsync(search, SearchCategory.Songs);
                IReadOnlyList<SearchResult> bufferedResults = await searchResults.FetchItemsAsync(0, 50, cancellationToken);
                var packageResults = bufferedResults
                    .OfType<SongSearchResult>()
                    .Select(item => new Musique(
                        baseUrl + item.Id,
                        CleanFileName(item.Name),
                        CleanFileName(FormatArtists(item.Artists.Select(artist => artist.Name)))))
                    .ToList();
                if (packageResults.Count > 0)
                {
                    return packageResults;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                packageFailure = ex;
                AppLogger.Warning(ex, "YouTubeMusicAPI search failed; using Innertube fallback");
            }

            try
            {
                return (await _innertubeClient.SearchSongsAsync(search, cancellationToken))
                    .Select(ToMusic)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception fallbackFailure) when (packageFailure is not null)
            {
                throw new InvalidOperationException(
                    "La recherche Youtube Music a échoué avec les deux moteurs.",
                    new AggregateException(packageFailure, fallbackFailure));
            }
        }

        public async Task<List<Musique>> getMusiqueInPlayListe(
            string idPlayliste,
            IProgress<List<Musique>>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(idPlayliste)) throw new Exception("Aucune playlist sélectionnée");
            EnsureConnected();
            Exception? innertubeFailure = null;
            try
            {
                var result = (await _innertubeClient.GetPlaylistSongsAsync(
                    idPlayliste,
                    cancellationToken: cancellationToken))
                    .Select(ToMusic)
                    .ToList();
                progress?.Report(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                innertubeFailure = ex;
                AppLogger.Warning(ex, "Innertube playlist load failed; using YouTubeMusicAPI fallback");
            }

            try
            {
                string browseId = _ytMusicClient.GetCommunityPlaylistBrowseId(idPlayliste);
                var playlistSongs = _ytMusicClient.GetCommunityPlaylistSongsAsync(browseId);
                var allSongs = new List<Musique>();
                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                const int limit = 100;

                for (var offset = 0; ; offset += limit)
                {
                    var bufferedSongs = await playlistSongs.FetchItemsAsync(offset, limit, cancellationToken);
                    var batch = bufferedSongs
                        .Where(song => seenIds.Add(song.Id))
                        .Select(song => new Musique(
                            baseUrl + song.Id,
                            CleanFileName(song.Name),
                            CleanFileName(FormatArtists(song.Artists.Select(artist => artist.Name)))))
                        .ToList();
                    allSongs.AddRange(batch);
                    if (batch.Count > 0)
                    {
                        progress?.Report(batch);
                    }

                    if (bufferedSongs.Count < limit)
                    {
                        return allSongs;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception packageFailure)
            {
                if (innertubeFailure is YtMusicAuthenticationException authenticationFailure
                    && IsAuthenticationFailure(packageFailure))
                {
                    throw new NotConnectedException(authenticationFailure.Message);
                }

                throw new InvalidOperationException(
                    "Le chargement de la playlist YouTube Music a échoué avec les deux moteurs.",
                    new AggregateException(innertubeFailure!, packageFailure));
            }
        }

        public async Task<List<Musique>> getMusiqueLike(CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            Exception? innertubeFailure = null;
            try
            {
                return (await _innertubeClient.GetLikedSongsAsync(cancellationToken)).Select(ToMusic).ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                innertubeFailure = ex;
                AppLogger.Warning(ex, "Innertube liked songs failed; using YouTubeMusicAPI fallback");
            }

            try
            {
                var songs = await _ytMusicClient.GetLibrarySongsAsync(cancellationToken);
                return songs.Select(song => new Musique(
                    baseUrl + song.Id,
                    CleanFileName(song.Name),
                    CleanFileName(FormatArtists(song.Artists.Select(artist => artist.Name)))))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception packageFailure)
            {
                if (innertubeFailure is YtMusicAuthenticationException authenticationFailure
                    && IsAuthenticationFailure(packageFailure))
                {
                    throw new NotConnectedException(authenticationFailure.Message);
                }

                throw new InvalidOperationException(
                    "Le chargement des favoris YouTube Music a échoué avec les deux moteurs.",
                    new AggregateException(innertubeFailure!, packageFailure));
            }
        }

        public async Task<List<PlayListe>> getPlayListe(CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            Exception? innertubeFailure = null;
            try
            {
                return (await _innertubeClient.GetPlaylistsAsync(cancellationToken))
                    .Select(playlist => new PlayListe(playlist.Id, CleanFileName(playlist.Name)))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                innertubeFailure = ex;
                AppLogger.Warning(ex, "Innertube playlists failed; using YouTubeMusicAPI fallback");
            }

            try
            {
                var playlists = await _ytMusicClient.GetLibraryCommunityPlaylistsAsync(cancellationToken);
                return playlists.Select(playlist => new PlayListe(
                    playlist.Id,
                    CleanFileName(playlist.Name)))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception packageFailure)
            {
                if (innertubeFailure is YtMusicAuthenticationException authenticationFailure
                    && IsAuthenticationFailure(packageFailure))
                {
                    throw new NotConnectedException(authenticationFailure.Message);
                }

                throw new InvalidOperationException(
                    "Le chargement des playlists YouTube Music a échoué avec les deux moteurs.",
                    new AggregateException(innertubeFailure!, packageFailure));
            }
        }
        



        async public Task<Musique> DownloadMusique(Musique musiqueyt)
        {

            AppPaths.EnsureRuntimeDirectories();
            var baseName = $"{musiqueyt.title} ({musiqueyt.author})";
            var existingPath = SupportedAudioFormats.FindExistingAudioFile(AppPaths.TempMusicDirectory, baseName);
            string lienMusiqueTmp="";

            if (existingPath != null)
            {
                var resolvedPath = await EnsureDirectDownloadReadyAsync(existingPath, baseName);
                return CreateMusicFromFile(resolvedPath, musiqueyt.title, musiqueyt.author);
            }

            string lienMusique = Path.Combine(AppPaths.TempMusicDirectory, $"{baseName}.mp3");
            try
            {
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.url);
                var streamInfo = SelectBestAudioStream(streamManifest.GetAudioOnlyStreams());
                var directExtension = GetDirectDownloadExtension(streamInfo);
                lienMusique = Path.Combine(AppPaths.TempMusicDirectory, baseName + (directExtension ?? ".mp3"));
                lienMusiqueTmp = directExtension != null
                    ? lienMusique + ".download"
                    : Path.Combine(AppPaths.TempMusicDirectory, $"{baseName}.{streamInfo.Container.Name}");

                Console.WriteLine("start download :" + musiqueyt.title + " " + musiqueyt.url);
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
                Console.WriteLine("end download");

                Musique musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);

                if (directExtension != null)
                {
                    if (File.Exists(lienMusique))
                    {
                        File.Delete(lienMusique);
                    }

                    File.Move(lienMusiqueTmp, lienMusique);
                    lienMusique = await EnsureDirectDownloadReadyAsync(lienMusique, baseName);
                    musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);
                }
                else
                {
                    Console.WriteLine("start mp3");
                    await FFmpegGestion.ConvertAudioToMp3(lienMusiqueTmp, lienMusique);
                    File.Delete(lienMusiqueTmp);
                    Console.WriteLine("delete tmp file");
                }

                TryWriteTags(musique);
                Console.WriteLine("end download audio");


                return musique;
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, "YoutubeExplode download failed; using yt-dlp fallback");

                if (lienMusiqueTmp != "")
                {
                    TryDelete(lienMusiqueTmp);
                    TryDelete(lienMusique);
                }

                return await otherdl(musiqueyt);

            }

        }

        private async Task<Musique> otherdl(Musique musiqueyt)
        {
            // Utilisation du dossier défini dans ton ancien projet
            string directory = AppPaths.TempMusicDirectory;
            Directory.CreateDirectory(directory);

            // Préparation des chemins
            string baseName = $"{musiqueyt.title} ({musiqueyt.author})";
            string outputTemplate = Path.Combine(directory, $"{baseName}.%(ext)s");
            string finalMp3Path = Path.Combine(directory, $"{baseName}.mp3");

            // Chemin vers l'outil externe (qjs.exe au lieu de deno pour la légèreté)
            string qjsPath = Path.Combine(pathOutilsExtern, "qjs.exe");

            var useCookies = File.Exists(ytdlpCookieFile);
            var arguments = BuildYtDlpAudioArguments(outputTemplate, qjsPath, musiqueyt.url, useCookies);
            var exitCode = await RunYtDlpAsync(arguments);

            if (exitCode != 0 && useCookies)
            {
                Console.WriteLine("yt-dlp avec cookies a echoue, nouvel essai sans cookies.");
                exitCode = await RunYtDlpAsync(BuildYtDlpAudioArguments(outputTemplate, qjsPath, musiqueyt.url, useCookies: false));
            }

            var downloadedPath = FindDownloadedFile(directory, baseName);
            if (downloadedPath != null && SupportedAudioFormats.IsSupported(downloadedPath))
            {
                var resolvedPath = await EnsureDirectDownloadReadyAsync(downloadedPath, baseName);
                var musiqueResult = new Musique(resolvedPath, musiqueyt.title, musiqueyt.author);
                TryWriteTags(musiqueResult);
                return musiqueResult;
            }

            if (downloadedPath != null)
            {
                await FFmpegGestion.ConvertAudioToMp3(downloadedPath, finalMp3Path);
                TryDelete(downloadedPath);
                var musiqueResult = new Musique(finalMp3Path, musiqueyt.title, musiqueyt.author);
                TryWriteTags(musiqueResult);
                return musiqueResult;
            }

            throw new InvalidOperationException($"Telechargement Youtube Music impossible. Code yt-dlp: {exitCode}.");
        }

        private static string BuildYtDlpAudioArguments(string outputTemplate, string qjsPath, string url, bool useCookies)
        {
            var arguments = "-f \"bestaudio[ext=m4a]/bestaudio[acodec^=mp4a]/bestaudio\" --no-check-certificate ";

            if (File.Exists(qjsPath))
            {
                arguments += $"--js-runtimes \"quickjs:{qjsPath}\" ";
            }

            if (useCookies && File.Exists(ytdlpCookieFile))
            {
                arguments += $"--cookies \"{ytdlpCookieFile}\" ";
            }

            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += $"--ffmpeg-location \"{FFmpegGestion.ffmpegPath}\" ";
            }

            return arguments + $"-o \"{outputTemplate}\" \"{url}\"";
        }

        private static async Task<int> RunYtDlpAsync(string arguments)
        {
            using var process = new Process();
            process.StartInfo.FileName = Path.Combine(pathOutilsExtern, "yt-dlp.exe");
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = pathOutilsExtern;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[yt-dlp]: {e.Data}"); };
            process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Error]: {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        private static AudioOnlyStreamInfo SelectBestAudioStream(IEnumerable<AudioOnlyStreamInfo> streams)
        {
            var audioStreams = streams.ToList();
            return audioStreams
                .Where(stream => GetDirectDownloadExtension(stream) != null)
                .OrderByDescending(stream => stream.Bitrate.BitsPerSecond)
                .FirstOrDefault()
                ?? (AudioOnlyStreamInfo)audioStreams.GetWithHighestBitrate();
        }

        private static string? GetDirectDownloadExtension(AudioOnlyStreamInfo streamInfo)
        {
            var container = streamInfo.Container.Name;
            var codec = streamInfo.AudioCodec ?? "";

            if (container.Equals("mp4", StringComparison.OrdinalIgnoreCase)
                && (codec.Contains("mp4a", StringComparison.OrdinalIgnoreCase)
                    || codec.Contains("aac", StringComparison.OrdinalIgnoreCase)))
            {
                return ".m4a";
            }

            if (container.Equals("mp3", StringComparison.OrdinalIgnoreCase))
            {
                return ".mp3";
            }

            return null;
        }

        private static async Task<string> EnsureDirectDownloadReadyAsync(string path, string baseName)
        {
            if (!SupportedAudioFormats.IsM4aContainer(path) || AudioCompatibility.CanReadSamples(path))
            {
                return path;
            }

            var remuxedPath = Path.Combine(AppPaths.TempMusicDirectory, baseName + ".m4a");
            var temporaryPath = Path.Combine(AppPaths.TempMusicDirectory, baseName + ".remux.m4a");
            Console.WriteLine($"Remux M4A requis: {Path.GetFileName(path)} est un conteneur AAC non lisible directement.");
            TryDelete(temporaryPath);
            await FFmpegGestion.RemuxAudioToM4a(path, temporaryPath);

            if (!AudioCompatibility.CanReadSamples(temporaryPath))
            {
                TryDelete(temporaryPath);
                return path;
            }

            TryDelete(path);
            TryDelete(remuxedPath);
            File.Move(temporaryPath, remuxedPath);
            return remuxedPath;
        }

        private static string? FindDownloadedFile(string directory, string baseName)
        {
            if (!Directory.Exists(directory))
            {
                return null;
            }

            return Directory.GetFiles(directory, baseName + ".*")
                .Where(path => !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.EndsWith(".download", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.EndsWith(".remux.m4a", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static Musique CreateMusicFromFile(string path, string fallbackTitle, string fallbackAuthor)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(path);
                if (file != null && file.Tag != null)
                {
                    string title = string.IsNullOrWhiteSpace(file.Tag.Title)
                        ? fallbackTitle
                        : file.Tag.Title;
                    string author = file.Tag.Performers != null && file.Tag.Performers.Length > 0
                        ? string.Join(", ", file.Tag.Performers)
                        : fallbackAuthor;

                    return new Musique(path, title, author);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, $"Youtube Music ID3 tag read failed for {Path.GetFileName(path)}");
                Console.WriteLine($"Erreur Tags: {ex.Message}");
            }

            return new Musique(path, fallbackTitle, fallbackAuthor);
        }

        private static void TryWriteTags(Musique musique)
        {
            try
            {
                var file = TagLib.File.Create(musique.url);
                file.Tag.Title = musique.title;
                file.Tag.Performers = new[] { musique.author };
                file.Save();
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, $"Youtube Music artwork tag write failed for {Path.GetFileName(musique.url)}");
                Console.WriteLine($"Erreur Tags: {ex.Message}");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, $"Temporary file cleanup failed for {Path.GetFileName(path)}");
            }
        }

        public async Task UpdateYtDlp()
        {
            Console.WriteLine("Vérification des mises à jour pour yt-dlp...");
            string toolsPath = Path.Combine(appPath, "outilsExtern");
            string ytDlpPath = Path.Combine(toolsPath, "yt-dlp.exe");

            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine("Erreur : yt-dlp.exe introuvable.");
                return;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = ytDlpPath;
                process.StartInfo.Arguments = "-U";
                process.StartInfo.WorkingDirectory = toolsPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) Console.WriteLine($"[yt-dlp Update]: {e.Data}");
                };

                process.Start();

                process.BeginOutputReadLine();
                await Task.Run(() => process.WaitForExit());

                Console.WriteLine("Processus de mise à jour terminé.");
            }
        }

        private string CleanFileName(string fileName)
        {
            // Remplacez les caractères invalides pour les noms de fichiers par des tirets
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidCharsPattern = "[" + invalidChars + "]";
            string cleanedFileName = Regex.Replace(fileName, invalidCharsPattern, "-");

            return cleanedFileName;
        }

        private Musique ToMusic(YtMusicSongItem song)
            => new(
                baseUrl + song.Id,
                CleanFileName(song.Title),
                CleanFileName(string.IsNullOrWhiteSpace(song.Artists) ? "Artiste Inconnu" : song.Artists));

        private static string FormatArtists(IEnumerable<string> artists)
        {
            var value = string.Join(", ", artists.Where(artist => !string.IsNullOrWhiteSpace(artist)));
            return string.IsNullOrWhiteSpace(value) ? "Artiste Inconnu" : value;
        }

        private static bool IsAuthenticationFailure(Exception exception)
        {
            if (exception is HttpRequestException
                {
                    StatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                })
            {
                return true;
            }

            return exception.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("sign in", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("se connecter", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureConnected()
        {
            if (_session is null)
            {
                throw new NotConnectedException(
                    "Aucune session Youtube Music valide. Connecte ton compte depuis les options.");
            }
        }

        public static bool isConnected()
        {
            try
            {
                return YtMusicCookieSession.TryLoad(sessionFile, out _);
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, "Youtube Music connection status read failed");
                return false;
            }
        }
        public static void removeConnect()
        {
            if (File.Exists(sessionFile))
            {
                File.Delete(sessionFile);
            }
            if (File.Exists(ytdlpCookieFile))
            {
                File.Delete(ytdlpCookieFile);
            }
        }
    }
}
