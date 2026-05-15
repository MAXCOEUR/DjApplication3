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

        public event EventHandler? PlaybackStopped;

        public PreviewPlayerService(IMusicLibraryService library, ISettingsService settings)
        {
            _library = library;
            _audio = new CsCoreAudioPlayerService(settings);
            _audio.SetHeadphoneEnabled(true);
            _audio.PlaybackStopped += (_, _) => PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public Musique? CurrentMusic { get; private set; }
        public bool IsPlaying => _audio.IsPlaying;

        public async Task PlayAsync(Musique musique, string source, int headphoneVolume, CancellationToken cancellationToken = default)
        {
            Stop();
            cancellationToken.ThrowIfCancellationRequested();

            var previewMusic = await _library.GetPreviewAsync(musique, source);
            cancellationToken.ThrowIfCancellationRequested();

            CurrentMusic = musique;
            _audio.SetHeadphoneEnabled(true);
            _audio.SetHeadphoneVolume(headphoneVolume);
            _audio.Load(previewMusic);
            _audio.Play();
        }

        public void Stop()
        {
            _audio.Stop();
            CurrentMusic = null;
        }

        public void SetHeadphoneVolume(float volume) => _audio.SetHeadphoneVolume(volume);

        public void Dispose() => _audio.Dispose();
    }
}
