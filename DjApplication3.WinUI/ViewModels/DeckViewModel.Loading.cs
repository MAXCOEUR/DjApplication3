using DjApplication3.model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class DeckViewModel
    {
        public Task<int> SetMusicAsync(Musique? musique)
        {
            if (musique is null)
            {
                return Task.FromResult(2);
            }

            try
            {
                if (_audio.IsPlaying)
                {
                    _audio.Stop();
                    IsPlaying = false;
                }

                _isHandlingTrackEnd = false;
                _playedEnoughReported = false;
                _lastPlaybackUpdateUtc = null;
                _listenedDuration = TimeSpan.Zero;

                _currentMusic = musique;
                UpdateNextMusicPreview();
                _audio.Load(musique);
                HasMusic = true;
                Title = $"{musique.title} ({musique.author})";
                Bpm = "000 BPM";
                UpdatePosition();
                _ = LoadBpmAsync(musique);
                _ = LoadWaveAsync(musique);
                _ = PreloadNextMusicAsync();
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                IsEndingSoon = false;
                NextMusicPreview = $"Chargement impossible: {ex.Message}";
                Debug.WriteLine($"Chargement musique impossible: {ex}");
                return Task.FromResult(1);
            }
        }

        private async Task LoadBpmAsync(Musique musique)
        {
            try
            {
                var bpm = await _library.GetBpmAsync(musique);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Bpm = $"{bpm} BPM";
                    BpmCalculated?.Invoke(this, bpm);
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => Bpm = "BPM --");
                Debug.WriteLine($"BPM impossible: {ex}");
            }
        }

        private async Task LoadWaveAsync(Musique musique)
        {
            try
            {
                var waveform = await _library.GetWaveAsync(musique);
                _dispatcherQueue.TryEnqueue(() => Waveform = waveform);
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => Waveform = Array.Empty<sbyte>());
                Debug.WriteLine($"Waveform impossible: {ex}");
            }
        }
    }
}
