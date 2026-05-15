using DjApplication3.model;
using DjApplication3.repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DjApplication3.Services
{
    public sealed class MusicLibraryService : IMusicLibraryService
    {
        private readonly MusiqueRepository _repository = new();

        public List<Musique> GetLocalMusic(string folderPath) => _repository.GetMp3Files(folderPath);

        public Task<List<Musique>> SearchYoutubeAsync(string search) => _repository.GetMusiqueYoutube(search);

        public Task<List<Musique>> SearchYtMusicAsync(string search) => _repository.GetMusiqueYtMusic(search);

        public Task<List<PlayListe>> GetYtMusicPlaylistsAsync() => _repository.GetPlayListeYtMusic();

        public Task<List<Musique>> GetYtMusicPlaylistTracksAsync(string playlistId, IProgress<List<Musique>>? progress = null)
            => _repository.GetMusiqueInPlayListeYtMusic(playlistId, progress);

        public Task<Musique> DownloadYoutubeAsync(Musique musique) => _repository.DownloadMusiqueYoutube(musique);

        public Task<Musique> DownloadYtMusicAsync(Musique musique) => _repository.DownloadMusiqueYtMusic(musique);

        public Task<Musique> GetPreviewAsync(Musique musique, string source) => _repository.GetPreviewAsync(musique, source);

        public Task UpdateYtDlpAsync() => _repository.UpdateYtDlp();

        public int? GetBpmHistory(Musique musique) => _repository.getBpmHistory(musique);

        public Task<int> GetBpmAsync(Musique musique) => Task.Run(() => _repository.getBpm(musique));

        public Task<sbyte[]> GetWaveAsync(Musique musique) => Task.Run(() => _repository.getWave(musique));
    }
}
