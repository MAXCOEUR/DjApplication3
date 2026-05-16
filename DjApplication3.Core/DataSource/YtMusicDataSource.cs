using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.Infrastructure;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models.Library;
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
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }
        public string Domain { get; set; }
    }

    public class YtMusicDataSource
    {
        private readonly YoutubeClient _youtube = new YoutubeClient();
        private readonly YouTubeMusicClient _ytMusicClient;
        private const string baseUrl = "https://music.youtube.com/watch?v=";

        // Fichier pour stocker tes cookies/session
        private static string appPath => AppPaths.BaseDirectory;
        private static string pathOutilsExtern => AppPaths.ExternalToolsDirectory;
        public static string sessionFile => AppPaths.SessionCookieFile;
        public static string ytdlpCookieFile => AppPaths.YtDlpCookieFile;

        public YtMusicDataSource()
        {
            List<Cookie> cookieList = new List<Cookie>();

            if (isConnected())
            {
                try
                {
                    string jsonString = File.ReadAllText(sessionFile);
                    var loadedCookies = JsonSerializer.Deserialize<List<CookieModel>>(jsonString);

                    if (loadedCookies != null)
                    {
                        foreach (var c in loadedCookies)
                        {
                            if (string.IsNullOrWhiteSpace(c.Name)
                                || string.IsNullOrWhiteSpace(c.Value)
                                || string.IsNullOrWhiteSpace(c.Path)
                                || string.IsNullOrWhiteSpace(c.Domain))
                            {
                                continue;
                            }

                            cookieList.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur de chargement des cookies : " + ex.Message);
                }
            }

            _ytMusicClient = new YouTubeMusicClient(cookies: cookieList.Any() ? cookieList : null);
        }

        public async Task<List<Musique>> search(string search)
        {
            if (string.IsNullOrWhiteSpace(search)) search = "musique";

            // 1. On spécifie qu'on veut uniquement des CHANSONS (SearchCategory.Songs)
            // Cela retourne un énumérateur asynchrone paginé
            PaginatedAsyncEnumerable<SearchResult> searchResults = _ytMusicClient.SearchAsync(search, SearchCategory.Songs);

            // 2. On récupère les 30 premiers résultats (tu peux ajuster le nombre)
            IReadOnlyList<SearchResult> bufferedResults = await searchResults.FetchItemsAsync(0, 50);

            List<Musique> musiques = new List<Musique>();

            // 3. On cast en SongSearchResult pour accéder aux propriétés spécifiques comme .Artists ou .Album
            foreach (var item in bufferedResults.Cast<SongSearchResult>())
            {
                // Construction de la chaîne des artistes (ex: "Daft Punk | Pharrell Williams")
                string authors = string.Join(" | ", item.Artists.Select(a => a.Name));

                musiques.Add(new Musique(
                    baseUrl + item.Id,
                    CleanFileName(item.Name),
                    CleanFileName(string.IsNullOrWhiteSpace(authors) ? "Artiste Inconnu" : authors)
                ));
            }

            return musiques;
        }

        public async Task<List<Musique>> getMusiqueInPlayListe(string idPlayliste, IProgress<List<Musique>>? progress = null)
        {
            if (string.IsNullOrEmpty(idPlayliste)) throw new Exception("Aucune playlist sélectionnée");

            string browseId = _ytMusicClient.GetCommunityPlaylistBrowseId(idPlayliste);
            var playlistSongsEnum = _ytMusicClient.GetCommunityPlaylistSongsAsync(browseId);

            List<Musique> allMusiques = new List<Musique>();
            int offset = 0;
            int limit = 100;
            bool hasMore = true;

            while (hasMore)
            {
                // On récupère un paquet de 100
                var bufferedSongs = await playlistSongsEnum.FetchItemsAsync(offset, limit);

                if (bufferedSongs.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                // Conversion en tes objets Musique
                var currentBatch = bufferedSongs.Select(t => new Musique(
                    baseUrl + t.Id,
                    CleanFileName(t.Name),
                    CleanFileName(string.Join(", ", t.Artists.Select(a => a.Name)) ?? "Artiste Inconnu")
                )).ToList();

                allMusiques.AddRange(currentBatch);

                // --- C'est ici que la magie opère ---
                // On notifie l'UI qu'on a un nouveau paquet de musiques
                progress?.Report(currentBatch);

                // Si on a reçu moins que la limite, c'est qu'on est à la fin
                if (bufferedSongs.Count < limit)
                    hasMore = false;
                else
                    offset += limit;
            }

            return allMusiques;
        }
        public async Task<List<Musique>> getMusiqueLike()
        {
            var songs = await _ytMusicClient.GetLibrarySongsAsync();

            return songs.Select(t => new Musique(
                baseUrl + t.Id, // t.Id est le videoId pour le lien
                CleanFileName(t.Name),
                CleanFileName(string.Join(", ", t.Artists.Select(a => a.Name)) ?? "Artiste Inconnu")
            )).ToList();
        }

        public async Task<List<PlayListe>> getPlayListe()
        {
            if (!isConnected())
                throw new NotConnectedException("Vous n'êtes pas connecté !");

            // Récupère les playlists de la bibliothèque de l'utilisateur
            var myPlaylists = await _ytMusicClient.GetLibraryCommunityPlaylistsAsync();
            IEnumerable<LibraryAlbum> albums = await _ytMusicClient.GetLibraryAlbumsAsync();

            var res = myPlaylists.Select(p => new PlayListe(
                p.Id,
                CleanFileName(p.Name)
            )).ToList();

            var res2 = albums.Select(p => new PlayListe(
                p.Id,
                CleanFileName(p.Name)
            )).ToList();
            res.AddRange(res2);

            if (res.Count == 0)
            {
                throw new NotConnectedException(
                    "Aucune playlist Youtube Music reçue. La session existe, mais elle est probablement expirée ou incomplète. Déconnecte puis reconnecte Youtube Music.");
            }

            return res;
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
            catch
            {
                if (!isConnected())
                {
                    throw new NotConnectedException("Vous n'êtes pas connecté !");
                }

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
            catch
            {
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

        public static bool isConnected()
        {
            if (!File.Exists(sessionFile)
                || new FileInfo(sessionFile).Length == 0
                || !File.Exists(ytdlpCookieFile)
                || new FileInfo(ytdlpCookieFile).Length == 0)
            {
                return false;
            }

            try
            {
                var jsonString = File.ReadAllText(sessionFile);
                var loadedCookies = JsonSerializer.Deserialize<List<CookieModel>>(jsonString) ?? new List<CookieModel>();
                var names = loadedCookies
                    .Select(cookie => cookie.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var hasSapisid = names.Contains("SAPISID")
                    || names.Contains("__Secure-1PAPISID")
                    || names.Contains("__Secure-3PAPISID");
                var hasSession = names.Contains("SID")
                    || names.Contains("__Secure-1PSID")
                    || names.Contains("__Secure-3PSID");

                return hasSapisid && hasSession;
            }
            catch
            {
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
