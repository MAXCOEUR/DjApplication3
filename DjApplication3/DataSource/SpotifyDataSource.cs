using DjApplication3.model;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.DataSource
{
    internal class SpotifyDataSource
    {
        SpotifyClient spotify;
        private const string command = ".\\outilsExtern\\spotdl.exe";

        public SpotifyDataSource()
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator("d12858cd02794788bc16d19f66f97f63", "9c78fc1636e04dd2be29504b530ae0f7"));

            spotify = new SpotifyClient(config);
        }

        async public Task<List<Musique>> search(string search)
        {
            SearchResponse searchResponse;
            try
            {
                searchResponse = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, search));
            }catch (Exception ex)
            {
                searchResponse = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, "hit"));
            }
            
            List<Musique> musiques = new List<Musique>();
            foreach (var track in searchResponse.Tracks.Items)
            {
                musiques.Add(new Musique(track.ExternalUrls.Values.First(), track.Name, track.Artists[0].Name));
            }
            return musiques;
        }
        async public Task<Musique> DownloadMusique(Musique musiqueSp)
        {
            //string lienMusique = Path.Combine(ExplorateurSpotify.rootFolder, $"{musiqueSp.title} ({musiqueSp.author}).mp3");
            string lienMusique = Path.Combine("musique/tmp", $"{musiqueSp.title} ({musiqueSp.author}).mp3");
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
                string arguments = "--user-auth --format mp3 --output \"../musique/tmp/{title} ("+ musiqueSp.author + ").{output-ext}\" " + musiqueSp.url ;
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetFullPath(".\\outilsExtern")
                };
                string output;
                Console.WriteLine("start dowload");
                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
                Console.WriteLine("end dowload");
                if (!output.Contains("Downloaded") && !output.Contains("Skipping"))
                {
                    throw new Exception("La ou les musiques n'a pas pu etre telecharger");
                }
                Musique musique = new Musique(lienMusique, musiqueSp.title, musiqueSp.author);
                return musique;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.Message, "Alerte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new Musique("", "", "");
            }
        }
    }
}
