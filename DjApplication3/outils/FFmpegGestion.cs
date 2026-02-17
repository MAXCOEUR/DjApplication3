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
        static public string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "outilsExtern", "ffmpeg", "ffmpeg.exe");

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

            process.Start();

            // Commencer la redirection de la sortie standard et d'erreur de manière asynchrone
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Attendre que le processus se termine
            await Task.Run(() => process.WaitForExit());

            if (!File.Exists(outputMp3))
            {
                throw new IOException("La conversion a échoué, fichier MP3 non créé.");
            }
        }
    }
}
