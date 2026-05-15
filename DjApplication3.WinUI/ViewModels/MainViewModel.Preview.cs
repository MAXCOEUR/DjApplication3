using DjApplication3.model;
using System;
using System.Linq;
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
            foreach (var row in Musics.Where(row => MusicIdentity.SameTrack(row.Musique, musique)))
            {
                row.Played = true;
            }
        }
    }
}
