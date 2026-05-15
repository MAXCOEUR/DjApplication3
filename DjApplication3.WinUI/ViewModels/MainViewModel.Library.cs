using DjApplication3.Infrastructure;
using DjApplication3.model;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
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
                    MusicIdentity.ReplaceInPlaylist(playlist, originalMusic, musique);
                    row.UseResolvedMusic(musique);
                }
                else if (SelectedSource == "Youtube Music" && !File.Exists(musique.url))
                {
                    var originalMusic = musique;
                    row.IsDownloading = true;
                    Status = "Téléchargement Youtube Music...";
                    musique = await _library.DownloadYtMusicAsync(musique);
                    musique.musiquesInPlayliste = playlist;
                    MusicIdentity.ReplaceInPlaylist(playlist, originalMusic, musique);
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

        private MusicRowViewModel CreateInternetRow(Musique musique)
        {
            var localPath = Path.Combine(AppPaths.TempMusicDirectory, $"{musique.title} ({musique.author}).mp3");
            var localMusic = new Musique(localPath, musique.title, musique.author, musique.musiquesInPlayliste);
            return new MusicRowViewModel(musique, _library.GetBpmHistory(localMusic), File.Exists(localPath));
        }

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
    }
}
