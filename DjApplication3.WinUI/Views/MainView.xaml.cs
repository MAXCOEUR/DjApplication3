using DjApplication3.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
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
                ApplySavedDeckAreaWidth();
                PopulateSettings();
                UpdateSettingsVisibility();
                UpdateLibraryModeVisibility();
                UpdateMusicLoadingUi();
                UpdateYtMusicButton();
            }
            catch (System.Exception ex)
            {
                ViewModel.Status = $"Démarrage incomplet: {ex.Message}";
            }
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
    }
}
