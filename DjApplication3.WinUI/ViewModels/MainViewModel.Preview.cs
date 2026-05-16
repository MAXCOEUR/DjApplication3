using DjApplication3.Infrastructure;
using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
        public async Task TogglePreviewAsync(MusicRowViewModel row)
        {
            if (row.IsPreviewing || row.IsPreviewLoading)
            {
                StopPreview();
                return;
            }

            StopPreview();
            _previewCancellation = new CancellationTokenSource();
            _previewRow = row;
            var originalMusic = row.Musique;
            var playlist = originalMusic.musiquesInPlayliste;
            row.IsPreviewLoading = true;
            row.IsDownloading = !File.Exists(originalMusic.url);
            PreviewBpmText = row.Bpm.HasValue ? $"{row.Bpm.Value} BPM" : "BPM --";
            Status = row.IsDownloading ? "Telechargement pre-ecoute..." : "Preparation pre-ecoute...";

            try
            {
                var resolvedMusic = await _previewPlayer.PlayAsync(originalMusic, SelectedSource, HeadphoneVolume, _previewCancellation.Token);
                resolvedMusic.musiquesInPlayliste = playlist;
                if (File.Exists(resolvedMusic.url))
                {
                    MusicIdentity.ReplaceInPlaylist(playlist, originalMusic, resolvedMusic);
                    row.UseResolvedMusic(resolvedMusic);
                }

                row.IsPreviewLoading = false;
                row.IsDownloading = false;
                row.IsPreviewing = true;
                UpdatePreviewPlayerState();
                _ = LoadPreviewAnalysisAsync(row, resolvedMusic);
                Status = "Pre-ecoute casque";
            }
            catch (OperationCanceledException)
            {
                row.IsPreviewLoading = false;
                row.IsDownloading = false;
            }
            catch (Exception ex)
            {
                row.IsPreviewLoading = false;
                row.IsDownloading = false;
                Status = $"Pre-ecoute impossible: {ex.Message}";
                ClearPreviewState();
            }
        }

        public void StopPreview()
        {
            _previewCancellation?.Cancel();
            _previewCancellation?.Dispose();
            _previewCancellation = null;
            _previewPlayer.Stop();
            ClearPreviewState();
        }

        public void TogglePreviewPlayback()
        {
            if (!IsPreviewActive)
            {
                return;
            }

            if (_previewPlayer.IsPlaying)
            {
                _previewPlayer.Pause();
            }
            else
            {
                _previewPlayer.Play();
            }

            UpdatePreviewPlayerState();
        }

        public void SeekPreview(double positionRatio)
        {
            if (!IsPreviewActive)
            {
                return;
            }

            _previewPlayer.Seek(positionRatio / 100.0);
            UpdatePreviewPlayerState();
        }

        private void ClearPreviewState()
        {
            if (_previewRow != null)
            {
                _previewRow.IsPreviewLoading = false;
                _previewRow.IsPreviewing = false;
            }

            _previewRow = null;
            PreviewTitle = "Aucune pre-ecoute";
            PreviewTimeText = "00:00 / 00:00";
            PreviewBpmText = "BPM --";
            PreviewPositionRatio = 0;
            PreviewWavePosition = 0;
            PreviewWaveform = Array.Empty<sbyte>();
            IsPreviewActive = false;
            IsPreviewPlaying = false;
        }

        private void UpdatePreviewPlayerState()
        {
            var currentMusic = _previewPlayer.CurrentMusic;
            IsPreviewActive = currentMusic is not null;
            IsPreviewPlaying = _previewPlayer.IsPlaying;

            if (currentMusic is null)
            {
                PreviewTitle = "Aucune pre-ecoute";
                PreviewTimeText = "00:00 / 00:00";
                PreviewPositionRatio = 0;
                PreviewWavePosition = 0;
                return;
            }

            PreviewTitle = $"{currentMusic.title} - {currentMusic.author}";
            PreviewPositionRatio = _previewPlayer.PositionRatio * 100.0;
            PreviewWavePosition = _previewPlayer.PositionRatio;
            PreviewTimeText = $"{FormatPreviewTime(_previewPlayer.Position)} / {FormatPreviewTime(_previewPlayer.Duration)}";
            // Sync VINYL (note 53) LED with preview playback state
            try
            {
                _midi.SetPreviewPlayPause(_previewPlayer.IsPlaying);
            }
            catch
            {
                // ignore if MIDI controller not available
            }
        }

        private async Task LoadPreviewAnalysisAsync(MusicRowViewModel row, Musique musique)
        {
            try
            {
                var bpm = await _library.GetBpmAsync(musique);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    row.Bpm = bpm;
                    PreviewBpmText = $"{bpm} BPM";
                });
            }
            catch
            {
                _dispatcherQueue.TryEnqueue(() => PreviewBpmText = "BPM --");
            }

            try
            {
                var waveform = await _library.GetWaveAsync(musique);
                _dispatcherQueue.TryEnqueue(() => PreviewWaveform = waveform);
            }
            catch
            {
                _dispatcherQueue.TryEnqueue(() => PreviewWaveform = Array.Empty<sbyte>());
            }
        }

        private static string FormatPreviewTime(TimeSpan time)
            => time.TotalHours >= 1
                ? time.ToString(@"h\:mm\:ss")
                : time.ToString(@"mm\:ss");

        private void MarkMusicPlayed(Musique musique)
        {
            if (_playedMusicKeys.Add(MusicIdentity.GetStableKey(musique)))
            {
                SavePlayedMusicKeys();
            }

            foreach (var row in Musics.Where(row => MusicIdentity.SameTrack(row.Musique, musique)))
            {
                row.Played = true;
            }
        }

        private bool IsMusicPlayed(Musique musique)
            => _playedMusicKeys.Contains(MusicIdentity.GetStableKey(musique));

        public void ResetPlayedMusicHistory()
        {
            _playedMusicKeys.Clear();
            SavePlayedMusicKeys();

            foreach (var row in Musics)
            {
                row.Played = false;
            }

            Status = "Historique des musiques lues reinitialise";
        }

        private static HashSet<string> LoadPlayedMusicKeys()
        {
            try
            {
                if (!File.Exists(AppPaths.PlayedMusicFile))
                {
                    return new HashSet<string>(StringComparer.Ordinal);
                }

                var keys = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(AppPaths.PlayedMusicFile));
                return new HashSet<string>(keys ?? [], StringComparer.Ordinal);
            }
            catch
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private void SavePlayedMusicKeys()
        {
            try
            {
                AppPaths.EnsureRuntimeDirectories();
                var keys = _playedMusicKeys.OrderBy(key => key, StringComparer.Ordinal).ToList();
                File.WriteAllText(AppPaths.PlayedMusicFile, JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Status = $"Sauvegarde des musiques lues impossible: {ex.Message}";
            }
        }
    }
}
