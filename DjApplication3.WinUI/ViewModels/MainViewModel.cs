using DjApplication3.DataSource;
using DjApplication3.Infrastructure;
using DjApplication3.model;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private string _localFolderPath = AppPaths.MusicDirectory;
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

        public MainViewModel(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            AppPaths.EnsureRuntimeDirectories();
            RefreshDecks();
        }

        public ObservableCollection<DeckViewModel> Decks { get; } = new();
        public ObservableCollection<MusicRowViewModel> Musics { get; } = new();
        public ObservableCollection<PlayListe> Playlists { get; } = new();
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
                    _ = RefreshCurrentSourceAsync();
                }
            }
        }
        public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
        public string LocalFolderPath { get => _localFolderPath; set => SetProperty(ref _localFolderPath, value); }
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
            _ = UpdateYtDlpAsync();

            try
            {
                StartMidi();
            }
            catch (Exception ex)
            {
                Status = $"MIDI indisponible: {ex.Message}";
            }
        }

        public async Task SearchAsync()
        {
            try
            {
                Status = "Recherche en cours...";
                Musics.Clear();
                if (SelectedSource == "Youtube")
                {
                    foreach (var musique in await _library.SearchYoutubeAsync(SearchText))
                    {
                        Musics.Add(CreateInternetRow(musique));
                    }
                }
                else if (SelectedSource == "Youtube Music")
                {
                    foreach (var musique in await _library.SearchYtMusicAsync(SearchText))
                    {
                        Musics.Add(CreateInternetRow(musique));
                    }
                }
                else
                {
                    await RefreshLocalAsync();
                }
                var currentList = Musics.Select(row => row.Musique).ToList();
                foreach (var row in Musics)
                {
                    row.Musique.musiquesInPlayliste = currentList;
                }
                SelectedMusicIndex = Musics.Count > 0 ? 0 : -1;
                Status = $"{Musics.Count} titres";
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        }

        public async Task LoadMusicAsync(MusicRowViewModel row, int? deckIndex = null)
        {
            var musique = row.Musique;
            row.IsDownloading = false;
            if (SelectedSource == "Youtube")
            {
                row.IsDownloading = true;
                Status = "Téléchargement Youtube...";
                musique = await _library.DownloadYoutubeAsync(musique);
                row.UseResolvedMusic(musique);
            }
            else if (SelectedSource == "Youtube Music" && !File.Exists(musique.url))
            {
                row.IsDownloading = true;
                Status = "Téléchargement Youtube Music...";
                musique = await _library.DownloadYtMusicAsync(musique);
                row.UseResolvedMusic(musique);
            }
            row.IsDownloading = false;
            if (deckIndex.HasValue)
            {
                await Decks[deckIndex.Value].SetMusicAsync(musique);
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

        public async Task RefreshLocalAsync()
        {
            Musics.Clear();
            var folder = Directory.Exists(LocalFolderPath) ? LocalFolderPath : AppPaths.MusicDirectory;
            LocalFolderPath = folder;
            RefreshLocalFolders(folder);
            foreach (var musique in _library.GetLocalMusic(folder))
            {
                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !musique.title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                    !musique.author.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Musics.Add(new MusicRowViewModel(musique, _library.GetBpmHistory(musique)));
            }
            if (Musics.Count > 0 && SelectedMusicIndex < 0)
            {
                SelectedMusicIndex = 0;
            }
            var visiblePlaylist = Musics.Select(row => row.Musique).ToList();
            foreach (var row in Musics)
            {
                row.Musique.musiquesInPlayliste = visiblePlaylist;
            }
            Status = $"{Musics.Count} titres locaux";
        }

        public async Task LoadPlaylistsAsync()
        {
            try
            {
                Playlists.Clear();
                foreach (var playlist in await _library.GetYtMusicPlaylistsAsync())
                {
                    Playlists.Add(playlist);
                }
                SelectedPlaylistIndex = Playlists.Count > 0 ? 0 : -1;
                Status = $"{Playlists.Count} playlists Youtube Music";
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        }

        public async Task LoadPlaylistAsync(PlayListe playlist)
        {
            Musics.Clear();
            var all = await _library.GetYtMusicPlaylistTracksAsync(playlist.id, new Progress<System.Collections.Generic.List<Musique>>(batch =>
            {
                foreach (var musique in batch)
                {
                    musique.musiquesInPlayliste = batch;
                    Musics.Add(CreateInternetRow(musique));
                }
            }));
            foreach (var musique in all)
            {
                musique.musiquesInPlayliste = all;
            }
            foreach (var row in Musics)
            {
                row.Musique.musiquesInPlayliste = all;
            }
            SelectedMusicIndex = Musics.Count > 0 ? 0 : -1;
            Status = $"{all.Count} titres dans {playlist.name}";
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

        public async Task OpenLocalFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;
            LocalFolderPath = folderPath;
            SearchText = "";
            _libraryFocus = LibraryFocus.Folders;
            await RefreshLocalAsync();
        }

        public async Task OpenSelectedPlaylistAsync()
        {
            if (SelectedPlaylistIndex < 0 || SelectedPlaylistIndex >= Playlists.Count) return;
            _libraryFocus = LibraryFocus.Musics;
            await LoadPlaylistAsync(Playlists[SelectedPlaylistIndex]);
        }

        public async Task NavigateLibraryLeftAsync()
        {
            if (IsLocalMode)
            {
                if (_libraryFocus == LibraryFocus.Folders)
                {
                    await OpenSelectedLocalFolderAsync();
                }
                else
                {
                    _libraryFocus = LibraryFocus.Folders;
                }
                return;
            }

            if (IsYtMusicMode)
            {
                _libraryFocus = LibraryFocus.Playlists;
            }
        }

        public void NavigateLibraryRight()
        {
            _libraryFocus = LibraryFocus.Musics;
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
            => deck.CurrentMusic != null
               && rowMusic.title == deck.CurrentMusic.title
               && rowMusic.author == deck.CurrentMusic.author;

        private void RefreshLocalFolders(string folder)
        {
            LocalFolders.Clear();

            try
            {
                var parent = Directory.GetParent(folder);
                if (parent != null)
                {
                    LocalFolders.Add(new LocalFolderViewModel("..", parent.FullName, true));
                }

                foreach (var directory in Directory.GetDirectories(folder).OrderBy(Path.GetFileName))
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

            if (SelectedSource == "Local")
            {
                _libraryFocus = LibraryFocus.Folders;
                await RefreshLocalAsync();
            }
            else if (SelectedSource == "Youtube Music")
            {
                _libraryFocus = LibraryFocus.Playlists;
                await LoadPlaylistsAsync();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    await SearchAsync();
                }
            }
            else
            {
                _libraryFocus = LibraryFocus.Musics;
                LocalFolders.Clear();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    await SearchAsync();
                }
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
            _midi.NavigateLeft += (_, _) => Enqueue(() => _ = NavigateLibraryLeftAsync());
            _midi.NavigateRight += (_, _) => Enqueue(NavigateLibraryRight);
            _midi.LoadLeft += (_, _) => Enqueue(() => _ = LoadSelectedAsync(LeftDeckIndex));
            _midi.LoadRight += (_, _) => Enqueue(() => _ = LoadSelectedAsync(RightDeckIndex));
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
    }
}
