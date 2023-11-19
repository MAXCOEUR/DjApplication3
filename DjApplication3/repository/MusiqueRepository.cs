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
        public List<float> getWave(Musique musique)
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
        //public TreeNode GetTreeNode(string rootFolder)
        //{
        //    LocalDataSource dataSource = new LocalDataSource();
        //    return dataSource.GetTreeNode(rootFolder);
        //}

        async public Task<Musique> DownloadMusiqueYoutube(Musique musiqueyt)
        {
            YoutubeDataSource dataSrouce = new YoutubeDataSource();
            return await dataSrouce.DownloadMusique(musiqueyt);
        }
        public async Task<List<Musique>> GetMusiqueSpotify(string search)
        {
            SpotifyDataSource dataSource = new SpotifyDataSource();
            return await dataSource.search(search);
        }
        async public Task<Musique> DownloadMusiqueSpotify(Musique musiqueSp)
        {
            SpotifyDataSource dataSrouce = new SpotifyDataSource();
            return await dataSrouce.DownloadMusique(musiqueSp);
        }
    }
}
