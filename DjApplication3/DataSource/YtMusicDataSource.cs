﻿using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.view.fragment;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DjApplication3.DataSource
{

    public class NotConnectedException : Exception
    {
        public NotConnectedException(string message) : base(message) { }
    }
    internal class YtMusicDataSource
    {
        YoutubeClient _youtube = new YoutubeClient();
        private string baseUrl = "https://music.youtube.com/watch?v=";
        private static string oauthJson="outilsExtern/oauth.json";
        async public Task<List<Musique>> search(string search)
        {
            if (!File.Exists(".\\outilsExtern\\apiYouMusic.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                throw new Exception("apiYouMusic.exe n'existe pas !");
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
                process.StartInfo.Arguments = $"-m m -s \"{search}\"";
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
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
                            authorTmp += artistElement.GetProperty("name").GetString();
                            authorTmp += " | ";
                        }
                        if (!string.IsNullOrEmpty(authorTmp) && authorTmp.Length > 3)
                        {
                            authorTmp = authorTmp.Remove(authorTmp.Length - 3);
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

        async public Task<List<Musique>> getMusiqueInPlayListe(string idPlayliste)
        {
            if (!File.Exists(".\\outilsExtern\\apiYouMusic.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                throw new Exception("apiYouMusic.exe n'existe pas !");
            }
            else if (idPlayliste == "")
            {
                // Gérer l'erreur ou lancer une exception
                throw new Exception("aucune playlist selectioné");
            }
            else if (!isConnected())
            {
                throw new NotConnectedException("Vous n'êtes pas connecté !");
            }

            List<Musique> musiques = new List<Musique>();

            string output = "";


            using (var process = new Process())
            {
                process.StartInfo.FileName = ".\\outilsExtern\\apiYouMusic.exe";
                process.StartInfo.Arguments = $"-m p -s \"{idPlayliste}\"";
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
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
                            authorTmp += artistElement.GetProperty("name").GetString();
                            authorTmp += " | ";
                        }
                        if (!string.IsNullOrEmpty(authorTmp) && authorTmp.Length > 3)
                        {
                            authorTmp = authorTmp.Remove(authorTmp.Length - 3);
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

        async public Task<List<PlayListe>> getPlayListe()
        {
            if (!File.Exists(".\\outilsExtern\\apiYouMusic.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                throw new Exception("apiYouMusic.exe n'existe pas !");
            }
            if (!isConnected())
            {
                throw new NotConnectedException("Vous n'êtes pas connecté !");
            }

            List<PlayListe> playListes = [];

            string output = "";


            using (var process = new Process())
            {
                process.StartInfo.FileName = ".\\outilsExtern\\apiYouMusic.exe";
                process.StartInfo.Arguments = $"-m p";
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
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
                        PlayListe playListe = new PlayListe(
                            element.GetProperty("id").GetString(),
                            CleanFileName(element.GetProperty("name").GetString())
                            );
                        playListes.Add(playListe);
                    }
                }

            }


            return playListes;
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
            
            string lienMusique = Path.Combine(ExplorateurInternet.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");

            string arguments = $"-x --audio-format mp3 -o \"{lienMusique}\" ";
            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += " --ffmpeg-location \"" + FFmpegGestion.ffmpegPath + "\"";
            }

            if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("chrome"))
            {
                arguments += " --cookies-from-browser chrome";
            }
            else if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("edge"))
            {
                arguments += " --cookies-from-browser edge";
            }
            else if (SettingsManager.Instance.browsers[SettingsManager.Instance.browserIndice].ToLower().Contains("firefox"))
            {
                arguments += " --cookies-from-browser firefox";
            }


            arguments += " " + musiqueyt.url;

            using (var process = new Process())
            {
                process.StartInfo.FileName = ".\\outilsExtern\\yt-dlp.exe";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
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
                        Console.WriteLine(e.Data);
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

                Musique musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);

                var file = TagLib.File.Create(musique.url);
                file.Tag.Title = musique.title;
                file.Tag.Performers = new[] { musique.author };
                file.Save();
                return musique;
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

        public static async Task<Process?> Connected()
        {
            if (!File.Exists(".\\outilsExtern\\apiYouMusicOAuth.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                return null;
            }

            if (isConnected())
            {
                Console.WriteLine("l'utilisateur est deja connecté");
                return null;
            }



            var process = new Process();
            if (process != null)
            {
                process.StartInfo.FileName = ".\\outilsExtern\\apiYouMusicOAuth.exe";
                process.StartInfo.WorkingDirectory = ".\\outilsExtern";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;

                string output = "";
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

                return process;
            }
            return null;
        }

        public static bool isConnected()
        {
            if (File.Exists(oauthJson))
            {
                return true;
            }
            return false;
        }
        public static void removeConnect()
        {
            if (File.Exists(oauthJson))
            {
                File.Delete(oauthJson);
            }
        }
    }
}
