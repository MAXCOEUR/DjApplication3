using DjApplication3.model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DjApplication3.Services
{
    public interface IMusicLibraryService
    {
        List<Musique> GetLocalMusic(string folderPath);
        Task<List<Musique>> SearchYoutubeAsync(string search);
        Task<List<Musique>> SearchYtMusicAsync(string search);
        Task<List<PlayListe>> GetYtMusicPlaylistsAsync();
        Task<List<Musique>> GetYtMusicPlaylistTracksAsync(string playlistId, IProgress<List<Musique>>? progress = null);
        Task<Musique> DownloadYoutubeAsync(Musique musique);
        Task<Musique> DownloadYtMusicAsync(Musique musique);
        Task<Musique> GetPreviewAsync(Musique musique, string source);
        Task UpdateYtDlpAsync();
        int? GetBpmHistory(Musique musique);
        Task<int> GetBpmAsync(Musique musique);
        Task<sbyte[]> GetWaveAsync(Musique musique);
    }
}
