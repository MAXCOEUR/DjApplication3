using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DjApplication3.DataSource
{
    internal class CacheDataSource
    {
        private static CacheDataSource? instance;
        private readonly Dictionary<string, int> musiquesBPM = new(StringComparer.OrdinalIgnoreCase);

        private CacheDataSource()
        {
            // Constructeur prive pour empecher l'instanciation en dehors de la classe.
        }

        public static CacheDataSource Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CacheDataSource();
                }
                return instance;
            }
        }

        public int? GetBpm(Musique musique)
        {
            var key = GetCacheKey(musique);
            if (musiquesBPM.ContainsKey(key))
            {
                return musiquesBPM[key];
            }

            return null;
        }

        public void AddMusiqueBPM(Musique musique, int bpm)
        {
            var key = GetCacheKey(musique);
            if (musiquesBPM.ContainsKey(key))
            {
                musiquesBPM[key] = bpm;
            }
            else
            {
                musiquesBPM.Add(key, bpm);
            }
        }

        private static string GetCacheKey(Musique musique)
        {
            if (!string.IsNullOrWhiteSpace(musique.url) && File.Exists(musique.url))
            {
                return Path.GetFullPath(musique.url);
            }

            return MusicIdentity.GetStableKey(musique);
        }
    }
}
