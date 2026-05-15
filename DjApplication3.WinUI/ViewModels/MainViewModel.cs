using DjApplication3.DataSource;
using DjApplication3.Infrastructure;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        private enum LibraryFocus
        {
            Folders,
            Playlists,
            Musics
        }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IMusicLibraryService _library = new MusicLibraryService();
        private readonly ISettingsService _settings = new SettingsService();
        private readonly IMidiControllerService _midi = new HerculesMidiControllerService();
        private readonly IPreviewPlayerService _previewPlayer;
        private int _trackCount = 2;
        private int _leftDeckIndex;
        private int _rightDeckIndex = 1;
        private int _crossfade = 50;
        private int _headphoneVolume = 100;
        private string _selectedSource = "Local";
        private string _searchText = "";
        private string _localRootPath = AppPaths.MusicDirectory;
        private string _localFolderPath = AppPaths.MusicDirectory;
        private string _localPathDisplay = "Racine";
        private string _currentPlaylistName = "";
        private string _status = "Prêt";
        private bool _isSettingsOpen;
        private int _selectedMusicIndex = -1;
        private int _leftDeckNumber = 1;
        private int _rightDeckNumber = 2;
        private int _selectedFolderIndex = -1;
        private int _selectedPlaylistIndex = -1;
        private LibraryFocus _libraryFocus = LibraryFocus.Musics;
        private bool _leftWasPlayingBeforeScratch;
        private bool _rightWasPlayingBeforeScratch;
        private bool _isLibraryLoading;
        private string _libraryLoadingText = "Chargement...";
        private int _libraryLoadingDepth;
        private MusicRowViewModel? _previewRow;
        private CancellationTokenSource? _previewCancellation;
        private DispatcherQueueTimer? _midiAutoDetectionTimer;
        private int _lastSeenMidiDeviceCount = -1;
        private bool _isCheckingMidiDevices;

        public MainViewModel(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            AppPaths.EnsureRuntimeDirectories();
            _previewPlayer = new PreviewPlayerService(_library, _settings);
            _previewPlayer.PlaybackStopped += (_, _) => _dispatcherQueue.TryEnqueue(ClearPreviewState);
            RefreshDecks();
        }

        public ObservableCollection<DeckViewModel> Decks { get; } = new();
        public ObservableCollection<MusicRowViewModel> Musics { get; } = new();
        public ObservableCollection<PlaylistRowViewModel> Playlists { get; } = new();
        public ObservableCollection<LocalFolderViewModel> LocalFolders { get; } = new();
        public ObservableCollection<int> TrackNumbers { get; } = new();
        public string[] Sources { get; } = ["Local", "Youtube Music", "Youtube"];
        public string[] TrackCountChoices { get; } = ["2", "3", "4"];

        public int TrackCount
        {
            get => _trackCount;
            set
            {
                if (SetProperty(ref _trackCount, Math.Clamp(value, 2, 4)))
                {
                    _settings.TrackCount = _trackCount;
                    RefreshDecks();
                }
            }
        }

        public int LeftDeckIndex
        {
            get => _leftDeckIndex;
            set
            {
                if (SetProperty(ref _leftDeckIndex, value))
                {
                    ApplyCrossfade();
                    SyncControllerState();
                }
            }
        }

        public int RightDeckIndex
        {
            get => _rightDeckIndex;
            set
            {
                if (SetProperty(ref _rightDeckIndex, value))
                {
                    ApplyCrossfade();
                    SyncControllerState();
                }
            }
        }

        public int LeftDeckNumber
        {
            get => _leftDeckNumber;
            set
            {
                var bounded = Math.Clamp(value, 1, Math.Max(1, Decks.Count));
                if (SetProperty(ref _leftDeckNumber, bounded))
                {
                    LeftDeckIndex = bounded - 1;
                }
            }
        }

        public int RightDeckNumber
        {
            get => _rightDeckNumber;
            set
            {
                var bounded = Math.Clamp(value, 1, Math.Max(1, Decks.Count));
                if (SetProperty(ref _rightDeckNumber, bounded))
                {
                    RightDeckIndex = bounded - 1;
                }
            }
        }

        public int Crossfade { get => _crossfade; set { if (SetProperty(ref _crossfade, value)) ApplyCrossfade(); } }

        public int HeadphoneVolume
        {
            get => _headphoneVolume;
            set
            {
                if (SetProperty(ref _headphoneVolume, Math.Clamp(value, 0, 100)))
                {
                    foreach (var deck in Decks) deck.SetHeadphoneVolume(_headphoneVolume);
                    _previewPlayer.SetHeadphoneVolume(_headphoneVolume);
                }
            }
        }

        public string SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (SetProperty(ref _selectedSource, value))
                {
                    OnPropertyChanged(nameof(IsLocalMode));
                    OnPropertyChanged(nameof(IsYtMusicMode));
                    OnPropertyChanged(nameof(IsYoutubeMode));
                    NotifyLibraryFocusChanged();
                    _ = RunSafeAsync(RefreshCurrentSourceAsync(), "Changement de source impossible");
                }
            }
        }

        public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
        public string LocalFolderPath { get => _localFolderPath; set => SetProperty(ref _localFolderPath, value); }
        public string LocalRootPath { get => _localRootPath; private set => SetProperty(ref _localRootPath, value); }
        public string LocalPathDisplay { get => _localPathDisplay; private set => SetProperty(ref _localPathDisplay, value); }

        public string CurrentPlaylistName
        {
            get => _currentPlaylistName;
            private set
            {
                if (SetProperty(ref _currentPlaylistName, value))
                {
                    OnPropertyChanged(nameof(CurrentPlaylistDisplay));
                }
            }
        }

        public string CurrentPlaylistDisplay => string.IsNullOrWhiteSpace(CurrentPlaylistName)
            ? "Playlist chargee : aucune"
            : $"Playlist chargee : {CurrentPlaylistName}";

        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public bool IsSettingsOpen { get => _isSettingsOpen; set => SetProperty(ref _isSettingsOpen, value); }

        public int SelectedMusicIndex
        {
            get => _selectedMusicIndex;
            set => SetProperty(ref _selectedMusicIndex, Math.Clamp(value, -1, Musics.Count - 1));
        }

        public int SelectedFolderIndex
        {
            get => _selectedFolderIndex;
            set => SetProperty(ref _selectedFolderIndex, Math.Clamp(value, -1, LocalFolders.Count - 1));
        }

        public int SelectedPlaylistIndex
        {
            get => _selectedPlaylistIndex;
            set => SetProperty(ref _selectedPlaylistIndex, Math.Clamp(value, -1, Playlists.Count - 1));
        }

        public bool IsLocalMode => SelectedSource == "Local";
        public bool IsYtMusicMode => SelectedSource == "Youtube Music";
        public bool IsYoutubeMode => SelectedSource == "Youtube";
        public bool IsLibraryLoading { get => _isLibraryLoading; private set => SetProperty(ref _isLibraryLoading, value); }
        public string LibraryLoadingText { get => _libraryLoadingText; private set => SetProperty(ref _libraryLoadingText, value); }
        public string FolderHeaderLabel => _libraryFocus == LibraryFocus.Folders ? "Dossiers [FOCUS]" : "Dossiers";
        public string PlaylistHeaderLabel => _libraryFocus == LibraryFocus.Playlists ? "Playlists Youtube Music [FOCUS]" : "Playlists Youtube Music";
        public string MusicHeaderLabel => _libraryFocus == LibraryFocus.Musics ? "Titre [FOCUS]" : "Titre";

        public string LibraryFocusStatus => _libraryFocus switch
        {
            LibraryFocus.Folders => "Focus: Dossiers",
            LibraryFocus.Playlists => "Focus: Playlists",
            _ => "Focus: Musiques"
        };

        public ISettingsService Settings => _settings;
        public bool IsYtMusicConnected => YtMusicDataSource.isConnected();

        public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

        public void Dispose()
        {
            foreach (var deck in Decks)
            {
                deck.PropertyChanged -= Deck_PropertyChanged;
                deck.BpmCalculated -= Deck_BpmCalculated;
                deck.PlayedEnough -= Deck_PlayedEnough;
                deck.Dispose();
            }

            _previewCancellation?.Cancel();
            _previewCancellation?.Dispose();
            StopMidiAutoDetection();
            _previewPlayer.Dispose();
            _midi.Dispose();
        }
    }
}
