using System;
using System.Collections.Generic;
using System.Linq;

namespace DjApplication3.model
{
    public static class MusicIdentity
    {
        public static bool SameTrack(Musique? first, Musique? second)
            => first is not null
               && second is not null
               && string.Equals(first.title, second.title, StringComparison.OrdinalIgnoreCase)
               && string.Equals(first.author, second.author, StringComparison.OrdinalIgnoreCase);

        public static string GetStableKey(Musique music)
            => $"{Normalize(music.title)}\u001F{Normalize(music.author)}";

        private static string Normalize(string? value)
            => (value ?? "").Trim().ToUpperInvariant();

        public static int FindIndex(IList<Musique> playlist, Musique music)
        {
            for (var i = 0; i < playlist.Count; i++)
            {
                if (ReferenceEquals(playlist[i], music)
                    || playlist[i] == music
                    || SameTrack(playlist[i], music))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool ReplaceInPlaylist(IList<Musique>? playlist, Musique oldMusic, Musique newMusic)
        {
            if (playlist == null)
            {
                return false;
            }

            var index = FindIndex(playlist, oldMusic);
            if (index < 0)
            {
                return false;
            }

            newMusic.musiquesInPlayliste = playlist as List<Musique> ?? playlist.ToList();
            playlist[index] = newMusic;
            return true;
        }
    }
}
