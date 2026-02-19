using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.view.fragment;
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
using System.Windows.Forms;
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

    internal class YtMusicDataSource
    {
        private readonly YoutubeClient _youtube = new YoutubeClient();
        private readonly YouTubeMusicClient _ytMusicClient;
        private const string baseUrl = "https://music.youtube.com/watch?v=";

        // Fichier pour stocker tes cookies/session
        private static string appPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string pathOutilsExtern = Path.Combine(appPath, "outilsExtern");
        public static string sessionFile = Path.Combine(pathOutilsExtern, "session_cookies.txt");
        public static string ytdlpCookieFile = Path.Combine(pathOutilsExtern, "ytdlp_cookies.txt");

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

        public async Task<List<Musique>> getMusiqueInPlayListe(string idPlayliste, int offset, int limit = 100)
        {
            if (string.IsNullOrEmpty(idPlayliste)) throw new Exception("Aucune playlist sélectionnée");

            // 1. Obtenir le BrowseId (pour les playlists normales)
            // Note : Si idPlayliste est "LM", cette étape peut varier selon la version de ta lib
            string browseId = _ytMusicClient.GetCommunityPlaylistBrowseId(idPlayliste);

            // 2. Récupérer l'énumérateur
            var playlistSongsEnum = _ytMusicClient.GetCommunityPlaylistSongsAsync(browseId);

            // 3. Utiliser l'offset et la limite fournis
            // FetchItemsAsync(index de départ, nombre d'items à prendre)
            var bufferedSongs = await playlistSongsEnum.FetchItemsAsync(offset, limit);

            return bufferedSongs.Select(t => new Musique(
                baseUrl + t.Id,
                CleanFileName(t.Name),
                CleanFileName(string.Join(", ", t.Artists.Select(a => a.Name)) ?? "Artiste Inconnu")
            )).ToList();
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
            return res;
        }
        



        async public Task<Musique> DownloadMusique(Musique musiqueyt)
        {

            //string lienMusique = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");
            string lienMusique = Path.Combine(ExplorateurInternet.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");
            string lienMusiqueTmp="";

            if (File.Exists(lienMusique))
            {
                TagLib.File chemain = TagLib.File.Create(lienMusique);

                if (chemain != null && chemain.Tag != null)
                {
                    string title = chemain.Tag.Title ?? Path.GetFileNameWithoutExtension(lienMusique);
                    string author = string.Join(", ", chemain.Tag.Artists) ?? "";

                    return new Musique(lienMusique, title, author);
                }

            }
            try
            {
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                lienMusiqueTmp = Path.Combine(ExplorateurInternet.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).{streamInfo.Container}");

                Console.WriteLine("start download :" + musiqueyt.title + " " + musiqueyt.url);
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
                Console.WriteLine("end download");
                Console.WriteLine("start mp3");

                Musique musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);

                await FFmpegGestion.ConvertWebmToMp3(lienMusiqueTmp, lienMusique);

                var file = TagLib.File.Create(musique.url);
                file.Tag.Title = musique.title;
                file.Tag.Performers = new[] { musique.author };
                file.Save();
                Console.WriteLine("end mp3");

                File.Delete(lienMusiqueTmp);
                Console.WriteLine("delete tmp file");

                return musique;
            }
            catch (Exception ex)
            {
                if (!isConnected())
                {
                    MessageBox.Show(ex.Message);
                    throw new NotConnectedException("Vous n'êtes pas connecté !");
                }

                if (lienMusiqueTmp != "")
                {
                    File.Delete(lienMusiqueTmp);
                    File.Delete(lienMusique);
                }

                return await otherdl(musiqueyt);

            }

        }

        private async Task<Musique> otherdl(Musique musiqueyt)
        {
            // Utilisation du dossier défini dans ton ancien projet
            string directory = ExplorateurInternet.rootFolder;
            Directory.CreateDirectory(directory);

            // Préparation des chemins
            string outputTemplate = Path.Combine(directory, $"{musiqueyt.title} ({musiqueyt.author}).%(ext)s");
            string finalMp3Path = Path.Combine(directory, $"{musiqueyt.title} ({musiqueyt.author}).mp3");

            // Chemin vers l'outil externe (qjs.exe au lieu de deno pour la légèreté)
            string qjsPath = Path.Combine(pathOutilsExtern, "qjs.exe");

            // CONSTRUCTION DES ARGUMENTS MODERNES
            // On garde ta structure d'arguments mais avec les fix de contournement YouTube
            string arguments = $"-x --audio-format mp3 --no-check-certificate " +
                               $"--js-runtimes \"quickjs:{qjsPath}\" " +
                               $"--extractor-args \"youtube:player-client=ios,android,web;player-skip=web_music\" " +
                               $"-o \"{outputTemplate}\" ";
            if (File.Exists(ytdlpCookieFile))
            {
                // C'est cet argument qui va débloquer le Premium pour yt-dlp
                arguments += $"--cookies \"{ytdlpCookieFile}\" ";
            }
            // Ajout de ffmpeg si trouvé via ta classe FFmpegGestion
            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += $" --ffmpeg-location \"{FFmpegGestion.ffmpegPath}\"";
            }

            // Ajout de l'URL
            arguments += $" \"{musiqueyt.url}\"";

            using (var process = new Process())
            {
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

                // Attente asynchrone
                await Task.Run(() => process.WaitForExit());

                // Si le fichier MP3 existe, on applique les tags et on retourne l'objet Musique
                if (File.Exists(finalMp3Path))
                {
                    try
                    {
                        // On crée le nouvel objet avec le chemin local du MP3
                        Musique musiqueResult = new Musique(finalMp3Path, musiqueyt.title, musiqueyt.author);

                        var file = TagLib.File.Create(musiqueResult.url);
                        file.Tag.Title = musiqueResult.title;
                        file.Tag.Performers = new[] { musiqueResult.author };
                        file.Save();

                        return musiqueResult;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur Tags: {ex.Message}");
                        // Retourne l'objet quand même car le fichier est là
                        return new Musique(finalMp3Path, musiqueyt.title, musiqueyt.author);
                    }
                }

                // Si ça échoue, on retourne null ou on lève une exception selon tes besoins
                return null;
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
            // Vérifie si le fichier de cookies existe et contient quelque chose
            return File.Exists(sessionFile) && new FileInfo(sessionFile).Length > 0 && File.Exists(ytdlpCookieFile) && new FileInfo(ytdlpCookieFile).Length > 0;
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
