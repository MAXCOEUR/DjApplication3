using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.Infrastructure;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace DjApplication3.DataSource
{
    public class YoutubeDataSource
    {
        YoutubeClient _youtube = new YoutubeClient();
        async public Task<List<Musique>> search(string search)
        {
            int i = 0;
            List<Musique> musiques = new List<Musique>();
            await foreach (var result in _youtube.Search.GetVideosAsync(search))
            {
                // Use pattern matching to handle different results (videos, playlists, channels)
                switch (result)
                {
                    case VideoSearchResult video:
                        {

                            musiques.Add(new Musique(video.Url, CleanFileName(video.Title), CleanFileName(video.Author.ChannelTitle)));
                            break;
                        }
                }
                i++;
                if (i >= 20)
                {
                    break;
                }
            }

            return musiques;
        }
        async public Task<Musique> DownloadMusique(Musique musiqueyt)
        {
            
            AppPaths.EnsureRuntimeDirectories();
            var baseName = $"{musiqueyt.title} ({musiqueyt.author})";
            var existingPath = SupportedAudioFormats.FindExistingAudioFile(AppPaths.TempMusicDirectory, baseName);

            if (existingPath != null)
            {
                var resolvedPath = await EnsureDirectDownloadReadyAsync(existingPath, baseName);
                return CreateMusicFromFile(resolvedPath, musiqueyt.title, musiqueyt.author);
            }

            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musiqueyt.url);
            var streamInfo = SelectBestAudioStream(streamManifest.GetAudioOnlyStreams());
            var directExtension = GetDirectDownloadExtension(streamInfo);
            string lienMusique = Path.Combine(AppPaths.TempMusicDirectory, baseName + (directExtension ?? ".mp3"));
            string lienMusiqueTmp = directExtension != null
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
                AppLogger.Warning(ex, $"Youtube ID3 tag read failed for {Path.GetFileName(path)}");
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
                AppLogger.Warning(ex, $"Youtube artwork tag write failed for {Path.GetFileName(musique.url)}");
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
