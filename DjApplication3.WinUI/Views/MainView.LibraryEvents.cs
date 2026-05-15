using DjApplication3.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DjApplication3.WinUI.Views
{
    public sealed partial class MainView
    {
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiActionAsync(
                () => RunLibraryActionAsync(
                    token => ViewModel.SearchAsync(token),
                    "Recherche des musiques..."),
                "Recherche impossible");
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiActionAsync(
                () => RunLibraryActionAsync(async token =>
                {
                    if (ViewModel.SelectedSource == "Local")
                    {
                        await ViewModel.RefreshLocalAsync(token);
                    }
                    else if (ViewModel.SelectedSource == "Youtube Music")
                    {
                        await ViewModel.LoadPlaylistsAsync(token);
                    }
                    else
                    {
                        await ViewModel.SearchAsync(token);
                    }
                }, "Chargement..."),
                "Rechargement impossible");
        }

        private async void MusicList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MusicList.SelectedItem is MusicRowViewModel row)
            {
                await RunUiActionAsync(() => ViewModel.LoadMusicAsync(row), "Chargement impossible");
            }
        }

        private async void LoadLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is MusicRowViewModel row)
            {
                await RunUiActionAsync(() => ViewModel.LoadMusicAsync(row, ViewModel.LeftDeckIndex), "Chargement gauche impossible");
            }
        }

        private async void LoadRightButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is MusicRowViewModel row)
            {
                await RunUiActionAsync(() => ViewModel.LoadMusicAsync(row, ViewModel.RightDeckIndex), "Chargement droite impossible");
            }
        }

        private async void PlaylistList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (PlaylistList.SelectedItem is PlaylistRowViewModel playlistRow)
            {
                SetOpenedLibraryItem(playlistRow);

                await RunUiActionAsync(
                    () => RunLibraryActionAsync(
                        token => ViewModel.LoadPlaylistAsync(playlistRow.Playlist, token),
                        "Chargement de la playlist..."),
                    "Playlist impossible");
            }
        }

        private void MusicRow_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not MusicRowViewModel row)
            {
                return;
            }

            MusicList.SelectedItem = row;

            var flyout = new MenuFlyout();
            for (var i = 0; i < ViewModel.TrackCount; i++)
            {
                var deckIndex = i;
                var item = new MenuFlyoutItem
                {
                    Text = $"Piste {i + 1}",
                    Tag = (row, deckIndex)
                };
                item.Click += ContextLoadTrack_Click;
                flyout.Items.Add(item);
            }

            flyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }

        private async void ContextLoadTrack_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuFlyoutItem)?.Tag is ValueTuple<MusicRowViewModel, int> selection)
            {
                await RunUiActionAsync(() => ViewModel.LoadMusicAsync(selection.Item1, selection.Item2), "Chargement piste impossible");
            }
        }

        private async void FolderList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (FolderList.SelectedItem is LocalFolderViewModel folder)
            {
                SetOpenedLibraryItem(folder);

                await RunUiActionAsync(
                    () => RunLibraryActionAsync(
                        token => ViewModel.OpenLocalFolderAsync(folder.Path, token),
                        "Chargement du dossier..."),
                    "Dossier impossible");
            }
        }

        private async void SearchBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;

                await RunUiActionAsync(
                    () => RunLibraryActionAsync(
                        token => ViewModel.SearchAsync(token),
                        "Recherche des musiques..."),
                    "Recherche impossible");
            }
        }

        private async void LocalFolderBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;

                await RunUiActionAsync(
                    () => RunLibraryActionAsync(
                        token => ViewModel.RefreshLocalAsync(token),
                        "Scan du dossier local..."),
                    "Scan local impossible");
            }
        }

        private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");

                if (App.MainAppWindow != null)
                {
                    InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
                }

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    await RunLibraryActionAsync(
                        token => ViewModel.SetLocalRootAsync(folder.Path, token),
                        "Scan du dossier local...");
                }
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Selection dossier impossible: {ex.Message}";
            }
        }

        private void MusicList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedMusicIndex = MusicList.SelectedIndex;
        }

        private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedFolderIndex = FolderList.SelectedIndex;
            SetSelectedLibraryItem(FolderList.SelectedItem);
        }

        private void PlaylistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedPlaylistIndex = PlaylistList.SelectedIndex;
            SetSelectedLibraryItem(PlaylistList.SelectedItem);
        }
    }
}
