using DjApplication3.model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.Services
{
    public sealed class PreviewPlayerService : IPreviewPlayerService
    {
        private readonly IMusicLibraryService _library;
        private readonly IAudioPlayerService _audio;

        public event EventHandler? PositionChanged;
        public event EventHandler? PlaybackStopped;

        public PreviewPlayerService(IMusicLibraryService library, ISettingsService settings)
        {
            _library = library;
            _audio = new CsCoreAudioPlayerService(settings);
            _audio.SetHeadphoneEnabled(true);
            _audio.PositionChanged += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
            _audio.PlaybackStopped += (_, _) => PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public Musique? CurrentMusic { get; private set; }
        public bool IsPlaying => _audio.IsPlaying;
        public TimeSpan Duration => _audio.Duration;
        public TimeSpan Position => _audio.Position;
        public float PositionRatio => _audio.PositionRatio;

        public async Task<Musique> PlayAsync(Musique musique, string source, int headphoneVolume, CancellationToken cancellationToken = default)
        {
            Stop();
            cancellationToken.ThrowIfCancellationRequested();

            var previewMusic = await _library.GetPreviewAsync(musique, source);
            cancellationToken.ThrowIfCancellationRequested();

            CurrentMusic = previewMusic;
            _audio.SetHeadphoneEnabled(true);
            _audio.SetHeadphoneVolume(headphoneVolume);
            _audio.Load(previewMusic);
            _audio.Play();
            PositionChanged?.Invoke(this, EventArgs.Empty);
            return previewMusic;
        }

        public void Play()
        {
            _audio.Play();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            _audio.Pause();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Seek(double positionRatio)
        {
            _audio.Seek(positionRatio);
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            _audio.Stop();
            CurrentMusic = null;
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetHeadphoneVolume(float volume) => _audio.SetHeadphoneVolume(volume);

        public void Dispose() => _audio.Dispose();
    }
}
