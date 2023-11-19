using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.outils
{
    internal class FFmpegGestion
    {
        public FFmpegGestion()
        {
            bool ffmpegInstalled = IsFFmpegAccessible();

            if (ffmpegInstalled)
            {
                Console.WriteLine($"FFmpeg is installed");
            }
            else
            {
                Console.WriteLine("FFmpeg is not installed. Downloading FFmpeg...");

                // Exécuter la commande pour télécharger FFmpeg
                DownloadFFmpeg();
            }
        }

        bool IsFFmpegAccessible()
        {
            // Vérifier si la commande "ffmpeg" est accessible dans le terminal
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
                    process.WaitForExit();


                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                }
            }
            catch { }


            // Vérifier si le fichier "ffmpeg.exe" existe dans le répertoire ".spotdl" de l'utilisateur
            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string spotdlDirectory = Path.Combine(userDirectory, ".spotdl");
            string ffmpegPath = Path.Combine(spotdlDirectory, "ffmpeg.exe");

            return File.Exists(ffmpegPath);
        }

        void DownloadFFmpeg()
        {
            string spotdlPath = "./outilsExtern/spotdl.exe";
            string arguments = "--download-ffmpeg";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = spotdlPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
    }
}
