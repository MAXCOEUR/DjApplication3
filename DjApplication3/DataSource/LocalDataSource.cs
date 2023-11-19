using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TagLib;

namespace DjApplication3.DataSource
{
    internal class LocalDataSource
    {
        public List<Musique> GetMp3Files(string pathfull)
        {
            try
            {
                // Vérifiez si le dossier existe
                if (Directory.Exists(pathfull))
                {
                    // Obtenez tous les fichiers dans le dossier
                    string[] allFiles = Directory.GetFiles(pathfull);

                    // Filtrer les fichiers avec l'extension .mp3
                    List<string> mp3Files = allFiles
                        .Where(file => Path.GetExtension(file).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Créer une liste de Musique à partir des fichiers MP3
                    List<Musique> musiqueList = mp3Files
                        .Select(GetMusiqueFromFilePath)
                        .Where(musique => musique != null) // Filtrer les éventuels objets null
                        .ToList();

                    return musiqueList;
                }
                else
                {
                    Console.WriteLine("Le dossier spécifié n'existe pas.");
                    return new List<Musique>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des fichiers : {ex.Message}");
                return new List<Musique>();
            }
        }
        private Musique GetMusiqueFromFilePath(string filePath)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(filePath);

                if (file != null && file.Tag != null)
                {
                    string title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath);
                    string author = string.Join(", ",file.Tag.Artists) ?? "";

                    return new Musique(filePath, title, author);
                }
                else
                {
                    Console.WriteLine($"Les métadonnées du fichier {filePath} ne peuvent pas être extraites.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des métadonnées : {ex.Message}");
                return null;
            }
        }

        public DossierPerso GetDossierPerso(string rootFolder)
        {
            rootFolder = rootFolder.TrimEnd('\\');
            DossierPerso rootDossier= new DossierPerso(Path.GetFileName(rootFolder));
            GenerateSubfolders(rootFolder, rootDossier);
            return rootDossier;
        }
        private void GenerateSubfolders(string folderPath, DossierPerso parentDossier)
        {
            try
            {
                string[] subfolders = Directory.GetDirectories(folderPath);

                foreach (string subfolder in subfolders)
                {
                    var subfolderNode = new DossierPerso(parentDossier,Path.GetFileName(subfolder));

                    parentDossier.Children.Add(subfolderNode);

                    GenerateSubfolders(subfolder, subfolderNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Gérer les erreurs d'accès non autorisé
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de la génération de l'arborescence des dossiers : " + ex.Message);
            }
        }
    }
}
