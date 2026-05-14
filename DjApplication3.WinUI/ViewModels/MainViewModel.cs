using DjApplication3.DataSource;
using DjApplication3.Infrastructure;
using DjApplication3.model;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;


namespace DjApplication3.WinUI.ViewModels
{
    public sealed class MainViewModel : ObservableObject, IDisposable
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

        public MainViewModel(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            AppPaths.EnsureRuntimeDirectories();
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

        public async Task InitializeAsync()
        {
            try
            {
                TrackCount = _settings.TrackCount;
            }
            catch (Exception ex)
            {
                Status = $"Périphériques indisponibles: {ex.Message}";
            }

            await RefreshLocalAsync();
            _ = RunSafeAsync(UpdateYtDlpAsync(), "Mise a jour yt-dlp impossible");

            try
            {
                StartMidi();
            }
            catch (Exception ex)
            {
                Status = $"MIDI indisponible: {ex.Message}";
            }
        }

        public async Task SearchAsync(CancellationToken cancellationToken = default)
        {
            BeginLibraryLoading("Recherche des musiques...");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Status = "Recherche en cours...";
                Musics.Clear();
                if (SelectedSource == "Youtube")
                {
                    foreach (var musique in await _library.SearchYoutubeAsync(SearchText))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Musics.Add(CreateInternetRow(musique));
                    }
                }
                else if (SelectedSource == "Youtube Music")
                {
                    CurrentPlaylistName = "";
                    foreach (var musique in await _library.SearchYtMusicAsync(SearchText))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Musics.Add(CreateInternetRow(musique));
                    }
                }
                else
                {
                    await RefreshLocalAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                var currentList = Musics.Select(row => row.Musique).ToList();
                foreach (var row in Musics)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    row.Musique.musiquesInPlayliste = currentList;
                }
                SelectedMusicIndex = Musics.Count > 0 ? 0 : -1;
                Status = $"{Musics.Count} titres";
            }
            catch (OperationCanceledException)
            {
                Status = "Chargement annulé";
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            finally
            {
                EndLibraryLoading();
            }
        }

        public async Task LoadMusicAsync(MusicRowViewModel row, int? deckIndex = null)
        {
            try
            {
            var musique = row.Musique;
            var playlist = musique.musiquesInPlayliste;
            row.IsDownloading = false;
            if (SelectedSource == "Youtube")
            {
                var originalMusic = musique;
                row.IsDownloading = true;
                Status = "Téléchargement Youtube...";
                musique = await _library.DownloadYoutubeAsync(musique);
                musique.musiquesInPlayliste = playlist;
                ReplacePlaylistMusic(playlist, originalMusic, musique);
                row.UseResolvedMusic(musique);
            }
            else if (SelectedSource == "Youtube Music" && !File.Exists(musique.url))
            {
                var originalMusic = musique;
                row.IsDownloading = true;
                Status = "Téléchargement Youtube Music...";
                musique = await _library.DownloadYtMusicAsync(musique);
                musique.musiquesInPlayliste = playlist;
                ReplacePlaylistMusic(playlist, originalMusic, musique);
                row.UseResolvedMusic(musique);
            }
            row.IsDownloading = false;
            if (deckIndex.HasValue)
            {
                if (deckIndex.Value < 0 || deckIndex.Value >= Decks.Count)
                {
                    Status = "Piste indisponible";
                    return;
                }

                var code = await Decks[deckIndex.Value].SetMusicAsync(musique);
                if (code != 0)
                {
                    Status = "Chargement de la piste impossible";
                    return;
                }
            }
            else
            {
                foreach (var deck in Decks)
                {
                    var code = await deck.SetMusicAsync(musique);
                    if (code == 0 || code == 2) break;
                }
            }
            row.Played = true;
            Status = "Titre charge";
            }
            catch (Exception ex)
            {
                row.IsDownloading = false;
                Status = $"Chargement impossible: {ex.Message}";
            }
        }

        public async Task RefreshLocalAsync(CancellationToken cancellationToken = default)
        {
            BeginLibraryLoading("Scan du dossier local...");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Musics.Clear();
                if (!Directory.Exists(LocalRootPath))
                {
                    LocalRootPath = Directory.Exists(AppPaths.MusicDirectory)
                        ? Path.GetFullPath(AppPaths.MusicDirectory)
                        : AppPaths.MusicDirectory;
                }

                var root = Path.GetFullPath(LocalRootPath);
                var folder = Directory.Exists(LocalFolderPath) ? Path.GetFullPath(LocalFolderPath) : root;
                if (!IsPathInsideRoot(folder, root))
                {
                    folder = root;
                }

                LocalFolderPath = folder;
                UpdateLocalPathDisplay();
                RefreshLocalFolders(folder);

                // Slow USB scans can take time: enumerate in background to keep UI responsive.
                var localMusics = await Task.Run(() => _library.GetLocalMusic(folder), cancellationToken);

                var index = 0;
                foreach (var musique in localMusics)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!string.IsNullOrWhiteSpace(SearchText) &&
                        !musique.title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                        !musique.author.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Musics.Add(new MusicRowViewModel(musique, _library.GetBpmHistory(musique)));

                    index++;
                    if (index % 25 == 0)
                    {
                        await Task.Yield();
                    }
                }
                if (Musics.Count > 0 && SelectedMusicIndex < 0)
                {
                    SelectedMusicIndex = 0;
                }
                var visiblePlaylist = Musics.Select(row => row.Musique).ToList();
                foreach (var row in Musics)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    row.Musique.musiquesInPlayliste = visiblePlaylist;
                }
                Status = $"{Musics.Count} titres locaux";
            }
            finally
            {
                EndLibraryLoading();
            }
        }

        public async Task LoadPlaylistsAsync(CancellationToken cancellationToken = default)
        {
            BeginLibraryLoading("Chargement des playlists...");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Playlists.Clear();
                foreach (var playlist in await _library.GetYtMusicPlaylistsAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Playlists.Add(new PlaylistRowViewModel(playlist));
                }
                SelectedPlaylistIndex = Playlists.Count > 0 ? 0 : -1;
                Status = $"{Playlists.Count} playlists Youtube Music";
            }
            catch (OperationCanceledException)
            {
                Status = "Chargement annulé";
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            finally
            {
                EndLibraryLoading();
            }
        }

        public async Task LoadPlaylistAsync(PlayListe playlist, CancellationToken cancellationToken = default)
        {
            BeginLibraryLoading("Chargement de la playlist...");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Musics.Clear();
                CurrentPlaylistName = playlist.name;
                var all = await _library.GetYtMusicPlaylistTracksAsync(playlist.id, new Progress<System.Collections.Generic.List<Musique>>(batch =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    foreach (var musique in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        musique.musiquesInPlayliste = batch;
                        Musics.Add(CreateInternetRow(musique));
                    }
                }));

                cancellationToken.ThrowIfCancellationRequested();
                foreach (var musique in all)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    musique.musiquesInPlayliste = all;
                }
                foreach (var row in Musics)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    row.Musique.musiquesInPlayliste = all;
                }
                SelectedMusicIndex = Musics.Count > 0 ? 0 : -1;
                Status = $"{all.Count} titres dans {playlist.name}";
            }
            finally
            {
                EndLibraryLoading();
            }
        }

        public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

        public void MoveSelection(int delta)
        {
            if (_libraryFocus == LibraryFocus.Folders && IsLocalMode)
            {
                if (LocalFolders.Count == 0) return;
                SelectedFolderIndex = SelectedFolderIndex < 0 ? 0 : Math.Clamp(SelectedFolderIndex + delta, 0, LocalFolders.Count - 1);
                return;
            }

            if (_libraryFocus == LibraryFocus.Playlists && IsYtMusicMode)
            {
                if (Playlists.Count == 0) return;
                SelectedPlaylistIndex = SelectedPlaylistIndex < 0 ? 0 : Math.Clamp(SelectedPlaylistIndex + delta, 0, Playlists.Count - 1);
                return;
            }

            if (Musics.Count == 0) return;
            SelectedMusicIndex = SelectedMusicIndex < 0 ? 0 : Math.Clamp(SelectedMusicIndex + delta, 0, Musics.Count - 1);
        }

        public async Task LoadSelectedAsync(int deckIndex)
        {
            if (SelectedMusicIndex >= 0 && SelectedMusicIndex < Musics.Count)
            {
                await LoadMusicAsync(Musics[SelectedMusicIndex], deckIndex);
            }
        }

        public async Task OpenSelectedLocalFolderAsync()
        {
            if (SelectedFolderIndex < 0 || SelectedFolderIndex >= LocalFolders.Count) return;
            await OpenLocalFolderAsync(LocalFolders[SelectedFolderIndex].Path); 
        }

        public async Task OpenLocalFolderAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(folderPath)) return;
            cancellationToken.ThrowIfCancellationRequested();
            var root = Path.GetFullPath(LocalRootPath);
            var folder = Path.GetFullPath(folderPath);
            if (!IsPathInsideRoot(folder, root))
            {
                folder = root;
            }

            LocalFolderPath = folder;
            SearchText = "";
            SetLibraryFocus(LibraryFocus.Folders);
            await RefreshLocalAsync(cancellationToken);
        }

        public async Task SetLocalRootAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(folderPath)) return;
            cancellationToken.ThrowIfCancellationRequested();
            var folder = Path.GetFullPath(folderPath);
            LocalRootPath = folder;
            LocalFolderPath = folder;
            SearchText = "";
            SetLibraryFocus(LibraryFocus.Folders);
            await RefreshLocalAsync(cancellationToken);
        }

        public async Task OpenSelectedPlaylistAsync()
        {
            if (SelectedPlaylistIndex < 0 || SelectedPlaylistIndex >= Playlists.Count) return;
            await LoadPlaylistAsync(Playlists[SelectedPlaylistIndex].Playlist);
        }

        public async Task NavigateLibraryLeftAsync()
        {
            if (IsLocalMode)
            {
                if (_libraryFocus == LibraryFocus.Musics)
                {
                    SetLibraryFocus(LibraryFocus.Folders);
                    return;
                }

                if (_libraryFocus == LibraryFocus.Folders)
                {
                    await OpenSelectedLocalFolderAsync();
                }
                return;
            }

            if (IsYtMusicMode)
            {
                if (_libraryFocus == LibraryFocus.Musics)
                {
                    SetLibraryFocus(LibraryFocus.Playlists);
                    return;
                }

                if (_libraryFocus == LibraryFocus.Playlists)
                {
                    await OpenSelectedPlaylistAsync();
                }
            }
        }

        public async Task NavigateLibraryRightAsync()
        {
            if (IsLocalMode)
            {
                if (_libraryFocus == LibraryFocus.Folders)
                {
                    SetLibraryFocus(LibraryFocus.Musics);
                }
                return;
            }

            if (IsYtMusicMode)
            {
                if (_libraryFocus == LibraryFocus.Playlists)
                {
                    SetLibraryFocus(LibraryFocus.Musics);
                }
            }
        }

        public void RefreshDevicesForOptions()
        {
            try
            {
                _settings.RefreshDevices();
                Status = "Peripheriques mis a jour";
            }
            catch (Exception ex)
            {
                Status = $"Mise a jour peripheriques impossible: {ex.Message}";
            }
        }

        public void RestartMidiController()
        {
            try
            {
                _midi.Start();
                SyncControllerState();
                Status = "Controleur MIDI reconnecte";
            }
            catch (Exception ex)
            {
                Status = $"Controleur MIDI indisponible: {ex.Message}";
            }
        }

        public void UpdateDeckHeights(double availableHeight)
        {
            if (Decks.Count == 0 || double.IsNaN(availableHeight) || availableHeight <= 0)
            {
                return;
            }

            const double minimumDeckHeight = 250;
            var spacing = Math.Max(0, Decks.Count - 1) * 10;
            var targetHeight = Math.Max(minimumDeckHeight, (availableHeight - spacing) / Decks.Count);
            foreach (var deck in Decks)
            {
                deck.DeckHeight = targetHeight;
            }
        }

        public void Dispose()
        {
            foreach (var deck in Decks)
            {
                deck.PropertyChanged -= Deck_PropertyChanged;
                deck.BpmCalculated -= Deck_BpmCalculated;
                deck.Dispose();
            }
            _midi.Dispose();
        }

        private void RefreshDecks()
        {
            while (Decks.Count > TrackCount)
            {
                var deck = Decks[^1];
                deck.PropertyChanged -= Deck_PropertyChanged;
                deck.BpmCalculated -= Deck_BpmCalculated;
                deck.Dispose();
                Decks.RemoveAt(Decks.Count - 1);
            }
            while (Decks.Count < TrackCount)
            {
                var deck = new DeckViewModel(Decks.Count + 1, _library, _settings, _dispatcherQueue);
                deck.PropertyChanged += Deck_PropertyChanged;
                deck.BpmCalculated += Deck_BpmCalculated;
                Decks.Add(deck);
            }
            TrackNumbers.Clear();
            for (var i = 1; i <= TrackCount; i++)
            {
                TrackNumbers.Add(i);
            }
            LeftDeckNumber = Math.Clamp(LeftDeckNumber, 1, TrackCount);
            RightDeckNumber = Math.Clamp(RightDeckNumber, 1, TrackCount);
            LeftDeckIndex = LeftDeckNumber - 1;
            RightDeckIndex = RightDeckNumber - 1;
            ApplyCrossfade();
            SyncControllerState();
        }

        private void Deck_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DeckViewModel.IsPlaying)
                or nameof(DeckViewModel.IsHeadphone)
                or nameof(DeckViewModel.HasMusic))
            {
                SyncControllerState();
            }
        }

        private void Deck_BpmCalculated(object? sender, int bpm)
        {
            if (sender is not DeckViewModel deck) return;

            foreach (var row in Musics.Where(row => SameMusic(row.Musique, deck)))
            {
                row.Bpm = bpm;
            }
        }

        private MusicRowViewModel CreateInternetRow(Musique musique)
        {
            var localPath = Path.Combine(AppPaths.TempMusicDirectory, $"{musique.title} ({musique.author}).mp3");
            var localMusic = new Musique(localPath, musique.title, musique.author, musique.musiquesInPlayliste);
            return new MusicRowViewModel(musique, _library.GetBpmHistory(localMusic), File.Exists(localPath));
        }

        private static bool SameMusic(Musique rowMusic, DeckViewModel deck)
            => deck.CurrentMusic is not null
               && rowMusic.title == deck.CurrentMusic.title
               && rowMusic.author == deck.CurrentMusic.author;

        private static void ReplacePlaylistMusic(List<Musique>? playlist, Musique oldMusic, Musique newMusic)
        {
            if (playlist == null)
            {
                return;
            }

            for (var i = 0; i < playlist.Count; i++)
            {
                if (ReferenceEquals(playlist[i], oldMusic)
                    || playlist[i] == oldMusic
                    || SameTrack(playlist[i], oldMusic))
                {
                    newMusic.musiquesInPlayliste = playlist;
                    playlist[i] = newMusic;
                    return;
                }
            }
        }

        private static bool SameTrack(Musique first, Musique second)
            => string.Equals(first.title, second.title, StringComparison.OrdinalIgnoreCase)
               && string.Equals(first.author, second.author, StringComparison.OrdinalIgnoreCase);

        private void UpdateLocalPathDisplay()
        {
            try
            {
                var root = Path.GetFullPath(LocalRootPath);
                var current = Path.GetFullPath(LocalFolderPath);
                var relative = Path.GetRelativePath(root, current);
                LocalPathDisplay = string.IsNullOrWhiteSpace(relative) || relative == "."
                    ? "Racine"
                    : relative;
            }
            catch
            {
                LocalPathDisplay = "Racine";
            }
        }

        private static bool IsSamePath(string firstPath, string secondPath)
            => string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(firstPath)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(secondPath)),
                StringComparison.OrdinalIgnoreCase);

        private static bool IsPathInsideRoot(string path, string root)
        {
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase)
                   || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshLocalFolders(string folder)
        {
            LocalFolders.Clear();

            try
            {
                var root = Path.GetFullPath(LocalRootPath);
                var current = Path.GetFullPath(folder);
                var parent = Directory.GetParent(current);
                if (!IsSamePath(current, root) && parent != null)
                {
                    var parentPath = Path.GetFullPath(parent.FullName);
                    if (!IsPathInsideRoot(parentPath, root))
                    {
                        parentPath = root;
                    }

                    LocalFolders.Add(new LocalFolderViewModel("..", parentPath, true));
                }

                foreach (var directory in Directory.GetDirectories(current).OrderBy(Path.GetFileName))
                {
                    LocalFolders.Add(new LocalFolderViewModel(Path.GetFileName(directory), directory));
                }

                SelectedFolderIndex = LocalFolders.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                Status = $"Dossiers locaux indisponibles: {ex.Message}";
            }
        }

        private async Task RefreshCurrentSourceAsync()
        {
            Musics.Clear();
            Playlists.Clear();
            CurrentPlaylistName = "";

            if (SelectedSource == "Local")
            {
                SetLibraryFocus(LibraryFocus.Folders);
                await RefreshLocalAsync();
            }
            else if (SelectedSource == "Youtube Music")
            {
                SetLibraryFocus(LibraryFocus.Playlists);
                await LoadPlaylistsAsync();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    await SearchAsync();
                }
            }
            else
            {
                SetLibraryFocus(LibraryFocus.Musics);
                LocalFolders.Clear();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    await SearchAsync();
                }
            }
        }

        private void SetLibraryFocus(LibraryFocus focus)
        {
            if (_libraryFocus == focus)
            {
                return;
            }

            _libraryFocus = focus;
            NotifyLibraryFocusChanged();
        }

        private void NotifyLibraryFocusChanged()
        {
            OnPropertyChanged(nameof(FolderHeaderLabel));
            OnPropertyChanged(nameof(PlaylistHeaderLabel));
            OnPropertyChanged(nameof(MusicHeaderLabel));
            OnPropertyChanged(nameof(LibraryFocusStatus));
        }

        private void BeginLibraryLoading(string loadingText)
        {
            _libraryLoadingDepth++;
            LibraryLoadingText = loadingText;
            IsLibraryLoading = true;
        }

        private void EndLibraryLoading()
        {
            _libraryLoadingDepth = Math.Max(0, _libraryLoadingDepth - 1);
            if (_libraryLoadingDepth == 0)
            {
                IsLibraryLoading = false;
                LibraryLoadingText = "Chargement...";
            }
        }

        private void ApplyCrossfade()
        {
            if (Decks.Count < 2) return;
            var leftVolume = Crossfade <= 50 ? 1 : 1 - ((Crossfade - 50) / 50.0f);
            var rightVolume = Crossfade >= 50 ? 1 : Crossfade / 50.0f;
            if (LeftDeckIndex >= 0 && LeftDeckIndex < Decks.Count) Decks[LeftDeckIndex].SetMasterVolume(leftVolume);
            if (RightDeckIndex >= 0 && RightDeckIndex < Decks.Count) Decks[RightDeckIndex].SetMasterVolume(rightVolume);
        }

        private void SyncControllerState()
        {
            var leftDeck = Decks.ElementAtOrDefault(LeftDeckIndex);
            var rightDeck = Decks.ElementAtOrDefault(RightDeckIndex);

            _midi.SetSelectedLeftDeck(LeftDeckNumber);
            _midi.SetSelectedRightDeck(RightDeckNumber);
            _midi.SetPlayLeft(leftDeck?.IsPlaying == true);
            _midi.SetPlayRight(rightDeck?.IsPlaying == true);
            _midi.SetPreviewLeft(leftDeck?.IsHeadphone == true);
            _midi.SetPreviewRight(rightDeck?.IsHeadphone == true);
            _midi.SetLoadedLeft(leftDeck?.HasMusic == true);
            _midi.SetLoadedRight(rightDeck?.HasMusic == true);
        }

        private void HandleScratchPress(bool isLeft, bool isPressed)
        {
            var deck = Decks.ElementAtOrDefault(isLeft ? LeftDeckIndex : RightDeckIndex);
            if (deck == null) return;

            if (isPressed)
            {
                if (isLeft)
                {
                    _leftWasPlayingBeforeScratch = deck.IsPlaying;
                }
                else
                {
                    _rightWasPlayingBeforeScratch = deck.IsPlaying;
                }
                deck.Pause();
                return;
            }

            if ((isLeft && _leftWasPlayingBeforeScratch) || (!isLeft && _rightWasPlayingBeforeScratch))
            {
                deck.Play();
            }
        }

        private async Task UpdateYtDlpAsync()
        {
            try
            {
                Status = "Mise à jour yt-dlp...";
                await _library.UpdateYtDlpAsync();
                Status = "Prêt";
            }
            catch (Exception ex)
            {
                Status = $"Mise à jour yt-dlp impossible: {ex.Message}";
            }
        }

        private void StartMidi()
        {
            _midi.PlayPauseLeft += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.TogglePlayPause());
            _midi.PlayPauseRight += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.TogglePlayPause());
            _midi.HeadphoneLeft += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.ToggleHeadphone());
            _midi.HeadphoneRight += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.ToggleHeadphone());
            _midi.NavigateUp += (_, _) => Enqueue(() => MoveSelection(-1));
            _midi.NavigateDown += (_, _) => Enqueue(() => MoveSelection(1));
            _midi.NavigateLeft += (_, _) => Enqueue(() => _ = RunSafeAsync(NavigateLibraryLeftAsync(), "Navigation impossible"));
            _midi.NavigateRight += (_, _) => Enqueue(() => _ = RunSafeAsync(NavigateLibraryRightAsync(), "Navigation impossible"));
            _midi.LoadLeft += (_, _) => Enqueue(() => _ = RunSafeAsync(LoadSelectedAsync(LeftDeckIndex), "Chargement piste gauche impossible"));
            _midi.LoadRight += (_, _) => Enqueue(() => _ = RunSafeAsync(LoadSelectedAsync(RightDeckIndex), "Chargement piste droite impossible"));
            _midi.PisteLeft += (_, piste) => Enqueue(() => LeftDeckNumber = Math.Clamp(piste, 1, Decks.Count));
            _midi.PisteRight += (_, piste) => Enqueue(() => RightDeckNumber = Math.Clamp(piste, 1, Decks.Count));
            _midi.VolumeLeft += (_, volume) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(LeftDeckIndex);
                if (deck != null) deck.Volume = (int)(volume * 100);
            });
            _midi.VolumeRight += (_, volume) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(RightDeckIndex);
                if (deck != null) deck.Volume = (int)(volume * 100);
            });
            _midi.Mix += (_, mix) => Enqueue(() => Crossfade = (int)(mix * 100));
            _midi.ScratchLeft += (_, value) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.ChangePosition(value != 127));
            _midi.ScratchRight += (_, value) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.ChangePosition(value != 127));
            _midi.ScratchLeftPress += (_, isPressed) => Enqueue(() => HandleScratchPress(true, isPressed));
            _midi.ScratchRightPress += (_, isPressed) => Enqueue(() => HandleScratchPress(false, isPressed));
            _midi.VolumeUpHeadPhone += (_, _) => Enqueue(() => HeadphoneVolume += 5);
            _midi.VolumeDownHeadPhone += (_, _) => Enqueue(() => HeadphoneVolume -= 5);
            _midi.Start();
            SyncControllerState();
        }

        private void Enqueue(Action action) => _dispatcherQueue.TryEnqueue(() =>
        {
            try { action(); }
            catch (Exception ex) { Status = ex.Message; }
        });

        private async Task RunSafeAsync(Task task, string errorPrefix)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Status = "Operation annulee";
            }
            catch (Exception ex)
            {
                Status = $"{errorPrefix}: {ex.Message}";
            }
        }
    }
}
