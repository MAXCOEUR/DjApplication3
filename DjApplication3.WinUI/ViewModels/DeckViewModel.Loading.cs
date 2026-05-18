using DjApplication3.Infrastructure;
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
                if (IsPlaying || _audio.IsPlaying)
                {
                    return Task.FromResult(3);
                }

                _isHandlingTrackEnd = false;
                _playedEnoughReported = false;
                _lastPlaybackUpdateUtc = null;
                _listenedDuration = TimeSpan.Zero;

                _currentMusic = musique;
                var waveformLoadVersion = ++_waveformLoadVersion;
                Waveform = Array.Empty<sbyte>();
                IsWaveformLoading = true;
                UpdateNextMusicPreview();
                _audio.Load(musique);
                HasMusic = true;
                Title = $"{musique.title} ({musique.author})";
                ClearBaseBpm("000 BPM");
                UpdatePosition();
                _ = LoadBpmAsync(musique);
                _ = LoadWaveAsync(musique, waveformLoadVersion);
                _ = PreloadNextMusicAsync();
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                IsEndingSoon = false;
                IsWaveformLoading = false;
                NextMusicPreview = $"Chargement impossible: {ex.Message}";
                AppLogger.Error(ex, $"Music load failed on deck {TrackNumber}");
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
                    SetBaseBpm(bpm);
                    BpmCalculated?.Invoke(this, bpm);
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => ClearBaseBpm("BPM --"));
                AppLogger.Warning(ex, $"BPM analysis failed on deck {TrackNumber}");
                Debug.WriteLine($"BPM impossible: {ex}");
            }
        }

        private async Task LoadWaveAsync(Musique musique, int waveformLoadVersion)
        {
            try
            {
                var waveform = await _library.GetWaveAsync(musique);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (waveformLoadVersion != _waveformLoadVersion)
                    {
                        return;
                    }

                    Waveform = waveform;
                    IsWaveformLoading = false;
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (waveformLoadVersion != _waveformLoadVersion)
                    {
                        return;
                    }

                    Waveform = Array.Empty<sbyte>();
                    IsWaveformLoading = false;
                });
                AppLogger.Warning(ex, $"Waveform analysis failed on deck {TrackNumber}");
                Debug.WriteLine($"Waveform impossible: {ex}");
            }
        }
    }
}
