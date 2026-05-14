using DjApplication3.model;
using System;

namespace DjApplication3.Services
{
    public interface IAudioPlayerService : IDisposable
    {
        event EventHandler? PositionChanged;
        event EventHandler? PlaybackStopped;
        bool IsPlaying { get; }
        float PositionRatio { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; }
        void Load(Musique musique);
        void Play();
        void Pause();
        void Stop();
        void Seek(double positionRatio);
        void ChangePosition(bool isForward);
        void SetMasterVolume(float volume);
        void SetTrackVolume(float volume);
        void SetHeadphoneVolume(float volume);
        void SetHeadphoneEnabled(bool enabled);
        void UpdateOutputDevice();
    }
}
