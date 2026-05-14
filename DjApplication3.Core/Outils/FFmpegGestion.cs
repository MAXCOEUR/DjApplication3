using DjApplication3.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DjApplication3.outils
{
    public class FFmpegGestion
    {
        public static string ffmpegPath => Path.Combine(AppPaths.FfmpegDirectory, "ffmpeg.exe");

        public static async Task ConvertWebmToMp3(string inputWebm, string outputMp3)
        {
            if (!File.Exists(inputWebm))
            {
                throw new FileNotFoundException("Le fichier WEBM n'existe pas.", inputWebm);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -i \"{inputWebm}\" -acodec libmp3lame -b:a 192k \"{outputMp3}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"Error: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());

            if (!File.Exists(outputMp3))
            {
                throw new IOException("La conversion a échoué, fichier MP3 non créé.");
            }
        }
    }
}
