using System;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
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

            await Task.CompletedTask;
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
    }
}
