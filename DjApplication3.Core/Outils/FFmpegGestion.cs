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
            => await ConvertAudioToMp3(inputWebm, outputMp3);

        public static async Task ConvertAudioToMp3(string inputPath, string outputMp3)
        {
            await ConvertAudio(inputPath, outputMp3, "-vn -acodec libmp3lame -b:a 192k");
        }

        public static async Task ConvertAudioToWave(string inputPath, string outputWave)
        {
            await ConvertAudio(inputPath, outputWave, "-vn -ac 2 -ar 44100 -f wav");
        }

        public static async Task RemuxAudioToM4a(string inputPath, string outputM4a)
        {
            await ConvertAudio(inputPath, outputM4a, "-vn -map 0:a:0 -c:a copy -movflags +faststart -f mp4");
        }

        private static async Task ConvertAudio(string inputPath, string outputPath, string outputArguments)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("Le fichier audio n'existe pas.", inputPath);
            }

            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("ffmpeg.exe introuvable.", ffmpegPath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -i \"{inputPath}\" {outputArguments} \"{outputPath}\"",
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

            if (process.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                throw new IOException("La conversion a echoue, fichier audio non cree.");
            }
        }
    }
}
