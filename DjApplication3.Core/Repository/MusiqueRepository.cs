using DjApplication3.DataSource;
using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.repository
{
    public class MusiqueRepository
    {
        public int getBpm(Musique musique)
        {
            int? bpm = CacheDataSource.Instance.GetBpm(musique);
            if(bpm != null)
            {
                return bpm.Value;
            }
            BpmDetect bpmDetect = new BpmDetect();
            int bpmDetected = bpmDetect.getBpm(musique.url);
            CacheDataSource.Instance.AddMusiqueBPM(musique, bpmDetected);
            return bpmDetected;
        }
        public int? getBpmHistory(Musique musique)
        {
            int? bpm = CacheDataSource.Instance.GetBpm(musique);
            if (bpm != null)
            {
                return bpm.Value;
            }
            return null;
        }
        public sbyte[] getWave(Musique musique)
        {
            GraphiqueDataSource dataSource = new GraphiqueDataSource();
            return dataSource.getWaveForme(musique);
        }



        public List<Musique> GetMp3Files(string folderPath)
        {
            LocalDataSource dataSource = new LocalDataSource();
            return dataSource.GetMp3Files(folderPath);
        }
        public async Task<List<Musique>> GetMusiqueYoutube(string search)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return await dataSource.search(search);
        }
        public async Task<List<Musique>> GetMusiqueYtMusic(string search)
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();
            return await dataSource.search(search);
        }
        public async Task<List<Musique>> GetMusiqueInPlayListeYtMusic(string idPlayliste, IProgress<List<Musique>>? progress = null)
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();

            return await dataSource.getMusiqueInPlayListe(idPlayliste, progress);
        }
        public async Task<List<Musique>> GetMusiqueLikeYtMusic()
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();
            return await dataSource.getMusiqueLike();
        }




        public async Task<List<PlayListe>> GetPlayListeYtMusic()
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();
            return await dataSource.getPlayListe();
        }


        async public Task UpdateYtDlp()
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();
            await dataSource.UpdateYtDlp();
        }


        async public Task<Musique> DownloadMusiqueYoutube(Musique musiqueyt)
        {
            YoutubeDataSource dataSource = new YoutubeDataSource();
            return await dataSource.DownloadMusique(musiqueyt);
        }
        async public Task<Musique> DownloadMusiqueYtMusic(Musique musiqueyt)
        {
            YtMusicDataSource dataSource = new YtMusicDataSource();
            return await dataSource.DownloadMusique(musiqueyt);
        }

        async public Task<Musique> GetPreviewAsync(Musique musique, string source)
        {
            if (File.Exists(musique.url))
            {
                return musique;
            }

            if (source == "Youtube")
            {
                return await PreviewDataSource.CreateInternetPreviewAsync(musique, useCookies: false);
            }

            if (source == "Youtube Music")
            {
                return await PreviewDataSource.CreateInternetPreviewAsync(musique, useCookies: true);
            }

            throw new FileNotFoundException("Fichier local introuvable pour la pre-ecoute.", musique.url);
        }
    }
}
