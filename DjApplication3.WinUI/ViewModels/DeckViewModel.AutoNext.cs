using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class DeckViewModel
    {
        public async Task ShufflePlaylistAsync()
        {
            try
            {
                if (_currentMusic?.musiquesInPlayliste == null) return;

                var currentMusic = _currentMusic;
                var list = currentMusic.musiquesInPlayliste;
                Shuffle(list);
                list.Remove(currentMusic);
                if (_nextDownloadedMusic is not null)
                {
                    list.Remove(_nextDownloadedMusic);
                    list.Insert(0, _nextDownloadedMusic);
                }
                list.Insert(0, currentMusic);
                await PreloadNextMusicAsync();
                UpdateNextMusicPreview();
            }
            catch (Exception ex)
            {
                NextMusicPreview = $"Shuffle impossible: {ex.Message}";
            }
        }

        private async Task DownloadNextMusicAsync()
        {
            var playlist = _currentMusic?.musiquesInPlayliste;
            if (!IsAutoNext || playlist == null)
            {
                UpdateNextMusicPreview();
                return;
            }

            var next = GetNextMusic();
            if (next is null)
            {
                UpdateNextMusicPreview();
                return;
            }

            _nextDownloadedMusic = File.Exists(next.url)
                ? next
                : await _library.DownloadYtMusicAsync(next);

            if (_nextDownloadedMusic is not null)
            {
                _nextDownloadedMusic.musiquesInPlayliste = playlist;
                MusicIdentity.ReplaceInPlaylist(playlist, next, _nextDownloadedMusic);
            }

            UpdateNextMusicPreview();
        }

        private async Task PreloadNextMusicAsync()
        {
            try
            {
                await DownloadNextMusicAsync();
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    NextMusicPreview = $"Prechargement impossible: {ex.Message}";
                });
                Debug.WriteLine($"Prechargement auto impossible: {ex}");
            }
        }

        private Musique? GetNextMusic()
        {
            if (_currentMusic?.musiquesInPlayliste == null) return null;

            var currentMusic = _currentMusic;
            var playlist = currentMusic.musiquesInPlayliste;
            var currentIndex = MusicIdentity.FindIndex(playlist, currentMusic);
            return currentIndex >= 0 && currentIndex < playlist.Count - 1
                ? playlist[currentIndex + 1]
                : null;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            var random = new Random();
            for (var n = list.Count; n > 1;)
            {
                n--;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private void UpdateNextMusicPreview()
        {
            if (_currentMusic?.musiquesInPlayliste == null || _currentMusic.musiquesInPlayliste.Count == 0)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            var currentMusic = _currentMusic;
            var playlist = currentMusic.musiquesInPlayliste;
            var currentIndex = MusicIdentity.FindIndex(playlist, currentMusic);

            if (currentIndex < 0 || currentIndex >= playlist.Count - 1)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            var nextMusics = playlist
                .Skip(currentIndex + 1)
                .Take(5)
                .Select((music, index) =>
                {
                    var title = string.IsNullOrWhiteSpace(music.title) ? "Titre inconnu" : music.title;
                    var author = string.IsNullOrWhiteSpace(music.author) ? "Artiste inconnu" : music.author;

                    return $"{index + 1}. {title} - {author}";
                })
                .ToList();

            if (nextMusics.Count == 0)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            NextMusicPreview = "Musiques suivantes :\n" + string.Join("\n", nextMusics);
        }
    }
}
