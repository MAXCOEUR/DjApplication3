using DjApplication3.model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.Services
{
    public interface IPreviewPlayerService : IDisposable
    {
        event EventHandler? PositionChanged;
        event EventHandler? PlaybackStopped;
        Musique? CurrentMusic { get; }
        bool IsPlaying { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; }
        float PositionRatio { get; }
        Task<Musique> PlayAsync(Musique musique, string source, int headphoneVolume, CancellationToken cancellationToken = default);
        void Play();
        void Pause();
        void Seek(double positionRatio);
        void Stop();
        void SetHeadphoneVolume(float volume);
    }
}
