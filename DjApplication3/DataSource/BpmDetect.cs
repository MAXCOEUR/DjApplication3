using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.DataSource
{
    internal class BpmDetect
    {
        public int getBpm(string filePath)
        {
            // Assurez-vous que le fichier existe avant d'essayer de l'exécuter
            if (!File.Exists(".\\outilsExtern\\BPM_Detect.exe"))
            {
                // Gérer l'erreur ou lancer une exception
                return -1;
            }

            // Utilisez Task.Run pour exécuter le code de manière asynchrone sur un thread de fond
            // Préparez le processus d'exécution
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ".\\outilsExtern\\BPM_Detect.exe",
                    Arguments = $"\"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            })
            {
                process.Start();

                // Attendre que le processus se termine
                process.WaitForExit();

                // Récupérer la sortie standard une fois le processus terminé
                string output = process.StandardOutput.ReadToEnd();
                try
                {
                    output = output.Remove(output.Length - 4, 4);
                    float parsedValue;
                    float.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue);

                    process.Dispose();
                    return (int)parsedValue;
                }catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return 0;
                }
                
            }
        }
    }
}
