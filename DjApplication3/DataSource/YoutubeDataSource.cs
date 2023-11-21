using DjApplication3.model;
using DjApplication3.view.fragment;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Riff;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DjApplication3.DataSource
{
    internal class YoutubeDataSource
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
            
            //string lienMusique = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");
            string lienMusique = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).mp3");
            

            if (System.IO.File.Exists(lienMusique))
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
                //string lienMusiqueTmp = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).{streamInfo.Container}");
                string lienMusiqueTmp = Path.Combine(ExplorateurYoutube.rootFolder, $"{musiqueyt.title} ({musiqueyt.author}).{streamInfo.Container}");

                Console.WriteLine("start download :"+ musiqueyt.title + " " + musiqueyt.url);
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, lienMusiqueTmp);
                Console.WriteLine("end download");
                Console.WriteLine("start mp3");

                Musique musique = new Musique(lienMusique, musiqueyt.title, musiqueyt.author);

                using (var reader = new MediaFoundationReader(lienMusiqueTmp))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, lienMusique);
                }
                var file = TagLib.File.Create(musique.url);
                file.Tag.Title = musique.title;
                file.Tag.Performers = new[] { musique.author };
                file.Save();
                Console.WriteLine("end mp3");

                System.IO.File.Delete(lienMusiqueTmp);
                Console.WriteLine("delete tmp file");

                return musique;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.Message, "Alerte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new Musique("", "", "");
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
