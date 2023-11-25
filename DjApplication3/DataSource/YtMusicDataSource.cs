using DjApplication3.model;
using DjApplication3.view.fragment;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace DjApplication3.DataSource
{
    internal class YtMusicDataSource
    {
        YoutubeClient _youtube = new YoutubeClient();
        private string baseUrl = "https://music.youtube.com/watch?v=";
        async public Task<List<Musique>> search(string search)
        {
            if (!File.Exists(".\\outilsExtern\\apiYouMusic.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                return null;
            }

            if (search == "")
            {
                search = "musique";
            }

            List<Musique> musiques = new List<Musique>();

            string output = "";

            using (var process = new Process())
            {
                process.StartInfo.FileName = ".\\outilsExtern\\apiYouMusic.exe";
                process.StartInfo.Arguments = $"\"{search}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;

                // Événement pour la sortie standard
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data;
                    }
                };

                // Événement pour la sortie d'erreur
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"Error: {e.Data}");
                        // Traiter la sortie d'erreur ici
                    }
                };

                // Démarrer le processus
                process.Start();

                // Commencer la redirection de la sortie standard et d'erreur de manière asynchrone
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Attendre que le processus se termine
                await Task.Run(() => process.WaitForExit());

                using (JsonDocument document = JsonDocument.Parse(output))
                {
                    // Parcourir le tableau JSON
                    foreach (JsonElement element in document.RootElement.EnumerateArray())
                    {
                        string authorTmp = "";
                        foreach (JsonElement artistElement in element.GetProperty("artists").EnumerateArray())
                        {
                            authorTmp = artistElement.GetProperty("name").GetString();
                            break;
                        }

                        Musique musique = new Musique(
                            baseUrl + element.GetProperty("videoId").GetString(),
                            CleanFileName(element.GetProperty("title").GetString()),
                            CleanFileName(authorTmp)
                            );

                        // Parcourir les artistes


                        musiques.Add(musique);
                    }
                }

            }

            return musiques;
        }

        async public Task<Musique> DownloadMusique(Musique musiqueyt)
        {

            //string lienMusique = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");
            string lienMusique = Path.Combine(ExplorateurInternet.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");


            if (System.IO.File.Exists(lienMusique))
            {
                try
                {
                    TagLib.File chemain = TagLib.File.Create(lienMusique);

                    if (chemain != null && chemain.Tag != null)
                    {
                        string title = chemain.Tag.Title ?? Path.GetFileNameWithoutExtension(lienMusique);
                        string author = string.Join(", ", chemain.Tag.Artists) ?? "";

                        return new Musique(lienMusique, title, author);
                    }
                }catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
            }
            try
            {
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                //string lienMusiqueTmp = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).{streamInfo.Container}");
                string lienMusiqueTmp = Path.Combine(ExplorateurInternet.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).{streamInfo.Container}");

                Console.WriteLine("start download :" + musiqueyt.title + " " + musiqueyt.url);
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
                Console.WriteLine("end download");
                Console.WriteLine("start mp3");

                Musique musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);

                using (var reader = new MediaFoundationReader(lienMusiqueTmp))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, lienMusique);
                }
                var file = TagLib.File.Create(musique.url);
                file.Tag.Title = musique.title;
                file.Tag.Performers = new[] { musique.author };
                file.Save();
                Console.WriteLine("end mp3");

                System.IO.File.Delete(lienMusiqueTmp);
                Console.WriteLine("delete tmp file");

                return musique;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.Message, "Alerte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new Musique("", "", "");
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
    }
}
