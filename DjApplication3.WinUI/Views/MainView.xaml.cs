using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.WinUI.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Threading;

namespace DjApplication3.WinUI.Views
{
    public sealed partial class MainView : UserControl
    {
        public MainViewModel ViewModel { get; }
        private WebView2? _loginWebView;
        private CancellationTokenSource? _libraryActionCancellation;
        private object? _selectedLibraryItem;
        private object? _openedLibraryItem;

        public MainView()
        {
            ViewModel = new MainViewModel(DispatcherQueue);
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Loaded += MainView_Loaded;
            Unloaded += (_, _) =>
            {
                _libraryActionCancellation?.Cancel();
                _libraryActionCancellation?.Dispose();
                _libraryActionCancellation = null;
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.Dispose();
            };
        }

        public void SetSelectedLibraryItem(object? item)
        {
            if (_selectedLibraryItem == item)
            {
                return;
            }

            if (_selectedLibraryItem is ILibrarySelectableItem previousSelected)
            {
                previousSelected.IsSelected = false;
            }

            _selectedLibraryItem = item;

            if (_selectedLibraryItem is ILibrarySelectableItem newSelected)
            {
                newSelected.IsSelected = true;
            }
        }

        public void SetOpenedLibraryItem(object? item)
        {
            if (_openedLibraryItem == item)
            {
                return;
            }

            if (_openedLibraryItem is ILibrarySelectableItem previousOpened)
            {
                previousOpened.IsOpened = false;
            }

            _openedLibraryItem = item;

            if (_openedLibraryItem is ILibrarySelectableItem newOpened)
            {
                newOpened.IsOpened = true;
            }
        }

        private async void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.InitializeAsync();
                PopulateSettings();
                UpdateSettingsVisibility();
                UpdateLibraryModeVisibility();
                UpdateMusicLoadingUi();
                UpdateYtMusicButton();
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Démarrage incomplet: {ex.Message}";
            }
        }

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

        private void DeckScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdateDeckHeights(e.NewSize.Height);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedMusicIndex) &&
                ViewModel.SelectedMusicIndex >= 0 &&
                ViewModel.SelectedMusicIndex < MusicList.Items.Count &&
                MusicList.SelectedIndex != ViewModel.SelectedMusicIndex)
            {
                MusicList.SelectedIndex = ViewModel.SelectedMusicIndex;
                MusicList.ScrollIntoView(MusicList.SelectedItem);
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedFolderIndex) &&
                ViewModel.SelectedFolderIndex >= 0 &&
                ViewModel.SelectedFolderIndex < FolderList.Items.Count &&
                FolderList.SelectedIndex != ViewModel.SelectedFolderIndex)
            {
                FolderList.SelectedIndex = ViewModel.SelectedFolderIndex;
                FolderList.ScrollIntoView(FolderList.SelectedItem);
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedPlaylistIndex) &&
                ViewModel.SelectedPlaylistIndex >= 0 &&
                ViewModel.SelectedPlaylistIndex < PlaylistList.Items.Count &&
                PlaylistList.SelectedIndex != ViewModel.SelectedPlaylistIndex)
            {
                PlaylistList.SelectedIndex = ViewModel.SelectedPlaylistIndex;
                PlaylistList.ScrollIntoView(PlaylistList.SelectedItem);
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedSource) ||
                e.PropertyName == nameof(MainViewModel.IsLocalMode) ||
                e.PropertyName == nameof(MainViewModel.IsYtMusicMode) ||
                e.PropertyName == nameof(MainViewModel.IsYoutubeMode))
            {
                UpdateLibraryModeVisibility();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsLibraryLoading)
                || e.PropertyName == nameof(MainViewModel.LibraryLoadingText))
            {
                UpdateMusicLoadingUi();
            }
        }

        private void TrackCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (TrackCountCombo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out var count))
            {
                ViewModel.TrackCount = count;

                if (DeckScrollViewer != null)
                {
                    ViewModel.UpdateDeckHeights(DeckScrollViewer.ActualHeight);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.ToggleSettings();
                if (ViewModel.IsSettingsOpen)
                {
                    PopulateSettings(refreshDevices: true);
                }
                UpdateSettingsVisibility();
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Options indisponibles: {ex.Message}";
            }
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSettingsOpen = false;
            UpdateSettingsVisibility();
        }

        private void UpdateSettingsVisibility()
            => SettingsOverlay.Visibility = ViewModel.IsSettingsOpen ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateLibraryModeVisibility()
        {
            LocalRootPanel.Visibility = ViewModel.IsLocalMode ? Visibility.Visible : Visibility.Collapsed;
            PlaylistTitlePanel.Visibility = ViewModel.IsYtMusicMode ? Visibility.Visible : Visibility.Collapsed;
            LocalFolderPanel.Visibility = ViewModel.IsLocalMode ? Visibility.Visible : Visibility.Collapsed;
            PlaylistPanel.Visibility = ViewModel.IsYtMusicMode ? Visibility.Visible : Visibility.Collapsed;
            NavigationColumn.Width = ViewModel.IsLocalMode || ViewModel.IsYtMusicMode
                ? new GridLength(190)
                : new GridLength(0);
        }

        private void PopulateSettings(bool refreshDevices = false)
        {
            if (refreshDevices)
            {
                ViewModel.RefreshDevicesForOptions();
            }

            AudioOutputCombo.Items.Clear();
            HeadphoneOutputCombo.Items.Clear();
            var audioDevices = ViewModel.Settings.AudioDevices;
            if (audioDevices != null)
            {
                foreach (var device in audioDevices)
                {
                    AudioOutputCombo.Items.Add(device);
                    HeadphoneOutputCombo.Items.Add(device);
                }
            }
            if (AudioOutputCombo.Items.Count > ViewModel.Settings.OutputDeviceIndex)
            {
                AudioOutputCombo.SelectedIndex = ViewModel.Settings.OutputDeviceIndex;
            }
            if (HeadphoneOutputCombo.Items.Count > ViewModel.Settings.HeadphoneDeviceIndex)
            {
                HeadphoneOutputCombo.SelectedIndex = ViewModel.Settings.HeadphoneDeviceIndex;
            }

            MidiCombo.Items.Clear();
            foreach (var midi in ViewModel.Settings.MidiDevices)
            {
                MidiCombo.Items.Add(midi.ProductName);
            }
            if (MidiCombo.Items.Count > ViewModel.Settings.MidiDeviceIndex)
            {
                MidiCombo.SelectedIndex = ViewModel.Settings.MidiDeviceIndex;
            }
        }

        private void AudioOutputCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (AudioOutputCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.OutputDeviceIndex = AudioOutputCombo.SelectedIndex;
            }
        }

        private void HeadphoneOutputCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (HeadphoneOutputCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.HeadphoneDeviceIndex = HeadphoneOutputCombo.SelectedIndex;
            }
        }

        private void MidiCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (MidiCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.MidiDeviceIndex = MidiCombo.SelectedIndex;
                ViewModel.RestartMidiController();
            }
        }

        private async void YtMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (YtMusicDataSource.isConnected())
            {
                YtMusicDataSource.removeConnect();
                UpdateYtMusicButton();
                return;
            }

            LoginPanel.Visibility = Visibility.Visible;
            try
            {
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrWhiteSpace(version))
                {
                    throw new InvalidOperationException("Runtime Microsoft Edge WebView2 introuvable.");
                }

                if (_loginWebView == null)
                {
                    _loginWebView = new WebView2();
                    LoginWebViewHost.Children.Add(_loginWebView);
                }

                if (_loginWebView.CoreWebView2 == null)
                {
                    await _loginWebView.EnsureCoreWebView2Async();
                }

                var coreWebView = _loginWebView.CoreWebView2;
                if (coreWebView == null)
                {
                    throw new InvalidOperationException("WebView2 n'est pas pret.");
                }

                coreWebView.Navigate("https://accounts.google.com/ServiceLogin?continue=https://music.youtube.com/");
            }
            catch (COMException ex)
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                ViewModel.Status = $"WebView2 indisponible: {ex.Message}";
            }
            catch (Exception ex)
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                ViewModel.Status = $"Ouverture Youtube Music impossible: {ex.Message}";
            }
        }

        private async void ContinueYtMusicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_loginWebView?.CoreWebView2 == null)
                {
                    ViewModel.Status = "WebView2 n'est pas prêt.";
                    return;
                }

                var cookieManager = _loginWebView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://music.youtube.com");
                if (cookies == null || cookies.Count == 0)
                {
                    ViewModel.Status = "Aucun cookie trouvé. Connecte-toi d'abord.";
                    return;
                }

                var cookieData = cookies.Select(c => new
                {
                    c.Name,
                    c.Value,
                    c.Path,
                    c.Domain
                }).ToList();

                File.WriteAllText(YtMusicDataSource.sessionFile, JsonSerializer.Serialize(cookieData));

                var sb = new StringBuilder();
                sb.AppendLine("# Netscape HTTP Cookie File");
                sb.AppendLine("# This file is generated by DjApplication3 - do not edit.");
                foreach (var c in cookies)
                {
                    var flag = c.Domain.StartsWith(".") ? "TRUE" : "FALSE";
                    sb.AppendLine($"{c.Domain}\t{flag}\t{c.Path}\tTRUE\t0\t{c.Name}\t{c.Value}");
                }
                File.WriteAllText(YtMusicDataSource.ytdlpCookieFile, sb.ToString());

                LoginPanel.Visibility = Visibility.Collapsed;
                ViewModel.Status = "Connexion Youtube Music réussie.";
                UpdateYtMusicButton();
                await ViewModel.LoadPlaylistsAsync();
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Connexion Youtube Music impossible: {ex.Message}";
            }
        }
        private async System.Threading.Tasks.Task RunLibraryActionAsync(
            Func<CancellationToken, System.Threading.Tasks.Task> action,
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

                await System.Threading.Tasks.Task.Yield();

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

        private async System.Threading.Tasks.Task RunUiActionAsync(
            Func<System.Threading.Tasks.Task> action,
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

        private void UpdateYtMusicButton()
            => YtMusicButton.Content = YtMusicDataSource.isConnected()
                ? "Deconnexion Youtube Music"
                : "Connexion Youtube Music";
    }
}
