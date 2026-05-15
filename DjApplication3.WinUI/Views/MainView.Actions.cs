using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.Views
{
    public sealed partial class MainView
    {
        private async Task RunLibraryActionAsync(
            Func<CancellationToken, Task> action,
            string loadingText)
        {
            _libraryActionCancellation?.Cancel();
            _libraryActionCancellation?.Dispose();
            var currentCancellation = new CancellationTokenSource();
            _libraryActionCancellation = currentCancellation;
            var cancellationToken = currentCancellation.Token;

            try
            {
                ViewModel.Status = $"{loadingText} (annulable...)";

                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();
                await action(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ViewModel.Status = "Chargement annulé";
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Erreur: {ex.Message}";
            }
            finally
            {
                if (ReferenceEquals(_libraryActionCancellation, currentCancellation))
                {
                    _libraryActionCancellation?.Dispose();
                    _libraryActionCancellation = null;
                }
            }
        }

        private async Task RunUiActionAsync(
            Func<Task> action,
            string errorPrefix)
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                ViewModel.Status = "Operation annulee";
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"{errorPrefix}: {ex.Message}";
            }
        }

        private void UpdateMusicLoadingUi()
        {
            if (MusicLoadingPanel == null || MusicLoadingRing == null || MusicLoadingText == null)
            {
                return;
            }

            MusicLoadingPanel.Visibility = ViewModel.IsLibraryLoading ? Visibility.Visible : Visibility.Collapsed;
            MusicLoadingRing.IsActive = ViewModel.IsLibraryLoading;
            MusicLoadingText.Text = string.IsNullOrWhiteSpace(ViewModel.LibraryLoadingText)
                ? "Chargement..."
                : ViewModel.LibraryLoadingText;
        }
    }
}
