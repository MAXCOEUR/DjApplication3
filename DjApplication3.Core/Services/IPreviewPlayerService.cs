using DjApplication3.model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.Services
{
    public interface IPreviewPlayerService : IDisposable
    {
        event EventHandler? PlaybackStopped;
        Musique? CurrentMusic { get; }
        bool IsPlaying { get; }
        Task PlayAsync(Musique musique, string source, int headphoneVolume, CancellationToken cancellationToken = default);
        void Stop();
        void SetHeadphoneVolume(float volume);
    }
}
