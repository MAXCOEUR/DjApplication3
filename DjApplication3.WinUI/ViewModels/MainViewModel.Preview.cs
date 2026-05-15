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
            row.IsPreviewLoading = true;
            Status = "Preparation pre-ecoute...";

            try
            {
                await _previewPlayer.PlayAsync(row.Musique, SelectedSource, HeadphoneVolume, _previewCancellation.Token);
                row.IsPreviewLoading = false;
                row.IsPreviewing = true;
                Status = "Pre-ecoute casque";
            }
            catch (OperationCanceledException)
            {
                row.IsPreviewLoading = false;
            }
            catch (Exception ex)
            {
                row.IsPreviewLoading = false;
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

        private void ClearPreviewState()
        {
            if (_previewRow != null)
            {
                _previewRow.IsPreviewLoading = false;
                _previewRow.IsPreviewing = false;
            }

            _previewRow = null;
        }

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
