using DjApplication3.Infrastructure;
using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagLib;

namespace DjApplication3.DataSource
{
    public class LocalDataSource
    {
        public List<Musique> GetMp3Files(string pathfull)
            => GetAudioFiles(pathfull);

        public List<Musique> GetAudioFiles(string pathfull)
        {
            if (!Directory.Exists(pathfull))
            {
                Console.WriteLine("Le dossier specifie n'existe pas.");
                throw new Exception("Le dossier specifie n'existe pas.");
            }

            var musiques = new List<Musique>();
            foreach (var file in Directory.GetFiles(pathfull)
                         .Where(SupportedAudioFormats.IsSupported)
                         .OrderBy(Path.GetFileName))
            {
                var musique = GetMusiqueFromFilePath(file);
                if (musique is not null)
                {
                    musiques.Add(musique);
                }
            }

            return musiques;
        }

        private Musique? GetMusiqueFromFilePath(string filePath)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(filePath);

                if (file != null && file.Tag != null)
                {
                    string title = string.IsNullOrWhiteSpace(file.Tag.Title)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : file.Tag.Title;
                    string author = string.Join(", ", file.Tag.Performers ?? Array.Empty<string>());

                    return new Musique(filePath, title, author);
                }

                Console.WriteLine($"Les metadonnees du fichier {filePath} ne peuvent pas etre extraites.");
                return CreateFallbackMusic(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la recuperation des metadonnees : {ex.Message}");
                return CreateFallbackMusic(filePath);
            }
        }

        private static Musique CreateFallbackMusic(string filePath)
            => new Musique(filePath, Path.GetFileNameWithoutExtension(filePath), "");
    }
}
