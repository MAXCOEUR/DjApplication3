using DjApplication3.DataSource;
using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.repository
{
    internal class MusiqueRepository
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
        public async Task<List<Musique>> GetMusiqueSpotify(string search)
        {
            SpotifyDataSource dataSource = new SpotifyDataSource();
            return await dataSource.search(search);
        }




        public DossierPerso GetDossierPerso(string rootFolder)
        {
            LocalDataSource dataSource = new LocalDataSource();
            return dataSource.GetDossierPerso(rootFolder);
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
        async public Task<Musique> DownloadMusiqueSpotify(Musique musiqueSp)
        {
            SpotifyDataSource dataSource = new SpotifyDataSource();
            return await dataSource.DownloadMusique(musiqueSp);
        }
    }
}
