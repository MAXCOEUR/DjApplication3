using DjApplication3.Infrastructure;
using DjApplication3.model;
using DjApplication3.outils;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DjApplication3.DataSource
{
    internal static class PreviewDataSource
    {
        private static readonly YoutubeClient _youtube = new YoutubeClient();
        private const int PreviewSeconds = 30;

        public static async Task<Musique> CreateInternetPreviewAsync(Musique musique, bool useCookies)
        {
            AppPaths.EnsureRuntimeDirectories();
            CleanupOldPreviews();

            var safeName = CleanFileName($"{musique.title} ({musique.author})");
            var hash = ShortHash(musique.url);
            var previewBaseName = $"{safeName}-{hash}";
            var outputTemplate = Path.Combine(AppPaths.PreviewMusicDirectory, $"{previewBaseName}.%(ext)s");
            var previewPath = FindPreviewFile(previewBaseName);

            if (previewPath != null)
            {
                return new Musique(previewPath, musique.title, musique.author, musique.musiquesInPlayliste);
            }

            try
            {
                previewPath = await DownloadPreviewWithYoutubeExplodeAsync(musique, previewBaseName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Preview YoutubeExplode fallback: {ex.Message}");

                previewPath = await DownloadPreviewWithYtDlpAsync(musique, previewBaseName, outputTemplate, useCookies);
            }

            return new Musique(previewPath, musique.title, musique.author, musique.musiquesInPlayliste);
        }

        private static async Task<string> DownloadPreviewWithYoutubeExplodeAsync(Musique musique, string previewBaseName)
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(musique.url);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var tempInputPath = Path.Combine(AppPaths.PreviewMusicDirectory, $"{previewBaseName}.{streamInfo.Container}");
            var finalMp3Path = Path.Combine(AppPaths.PreviewMusicDirectory, $"{previewBaseName}.mp3");

            if (File.Exists(finalMp3Path) && new FileInfo(finalMp3Path).Length > 0)
            {
                return finalMp3Path;
            }

            if (File.Exists(tempInputPath))
            {
                File.Delete(tempInputPath);
            }

            await _youtube.Videos.Streams.DownloadAsync(streamInfo, tempInputPath);
            await ConvertToPreviewMp3Async(tempInputPath, finalMp3Path);

            try
            {
                File.Delete(tempInputPath);
            }
            catch
            {
            }

            if (!File.Exists(finalMp3Path) || new FileInfo(finalMp3Path).Length == 0)
            {
                throw new InvalidOperationException("La pre-ecoute YoutubeExplode n'a pas produit de fichier MP3.");
            }

            return finalMp3Path;
        }

        private static async Task<string> DownloadPreviewWithYtDlpAsync(Musique musique, string previewBaseName, string outputTemplate, bool useCookies)
        {
            var ytDlpPath = Path.Combine(AppPaths.ExternalToolsDirectory, "yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
            {
                throw new FileNotFoundException("yt-dlp.exe introuvable pour la pre-ecoute.", ytDlpPath);
            }

            var qjsPath = Path.Combine(AppPaths.ExternalToolsDirectory, "qjs.exe");
            var arguments = "-x --audio-format mp3 --no-check-certificate " +
                $"--js-runtimes \"quickjs:{qjsPath}\" " +
                "--extractor-args \"youtube:player-client=ios,android,web;player-skip=web_music\" " +
                $"--download-sections \"*00:00-00:{PreviewSeconds:00}\" ";

            if (useCookies && File.Exists(YtMusicDataSource.ytdlpCookieFile))
            {
                arguments += $"--cookies \"{YtMusicDataSource.ytdlpCookieFile}\" ";
            }

            if (File.Exists(FFmpegGestion.ffmpegPath))
            {
                arguments += $"--ffmpeg-location \"{FFmpegGestion.ffmpegPath}\" ";
            }

            arguments += $"-o \"{outputTemplate}\" \"{musique.url}\"";

            using var process = new Process();
            process.StartInfo.FileName = ytDlpPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = AppPaths.ExternalToolsDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            var previewPath = FindPreviewFile(previewBaseName);
            if (process.ExitCode != 0 || previewPath == null)
            {
                throw new InvalidOperationException($"Pre-ecoute impossible: {FirstUsefulLine(error, output)}");
            }

            return previewPath;
        }

        private static async Task ConvertToPreviewMp3Async(string inputPath, string outputPath)
        {
            if (!File.Exists(FFmpegGestion.ffmpegPath))
            {
                throw new FileNotFoundException("ffmpeg.exe introuvable pour la pre-ecoute.", FFmpegGestion.ffmpegPath);
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            using var process = new Process();
            process.StartInfo.FileName = FFmpegGestion.ffmpegPath;
            process.StartInfo.Arguments = $"-y -i \"{inputPath}\" -t {PreviewSeconds} -vn -acodec libmp3lame -b:a 192k \"{outputPath}\"";
            process.StartInfo.WorkingDirectory = AppPaths.ExternalToolsDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                throw new InvalidOperationException($"Conversion preview impossible: {FirstUsefulLine(error, output)}");
            }
        }

        private static string? FindPreviewFile(string previewBaseName)
        {
            foreach (var extension in new[] { ".mp3" })
            {
                var path = Path.Combine(AppPaths.PreviewMusicDirectory, previewBaseName + extension);
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    return path;
                }
            }

            return null;
        }

        private static void CleanupOldPreviews()
        {
            if (!Directory.Exists(AppPaths.PreviewMusicDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(AppPaths.PreviewMusicDirectory))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-3))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore cleanup failures; playback should not depend on cache housekeeping.
                }
            }
        }

        private static string FirstUsefulLine(string error, string output)
        {
            var text = string.IsNullOrWhiteSpace(error) ? output : error;
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line.Trim();
                }
            }

            return "aucun detail retourne par yt-dlp";
        }

        private static string ShortHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes, 0, 6).ToLowerInvariant();
        }

        private static string CleanFileName(string fileName)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            return Regex.Replace(fileName, "[" + invalidChars + "]", "-");
        }
    }
}
