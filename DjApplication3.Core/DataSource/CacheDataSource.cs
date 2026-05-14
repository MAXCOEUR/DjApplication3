using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.DataSource
{
    internal class CacheDataSource
    {
        private static CacheDataSource instance;
        private Dictionary<Musique,int> musiquesBPM = new Dictionary<Musique, int>();
        private Musique musique;

        private CacheDataSource()
        {
            // Constructeur privé pour empêcher l'instanciation en dehors de la classe.
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

            if (musiquesBPM.ContainsKey(musique))
            {
                return musiquesBPM[musique];
            }
            if (this.musique == musique)
            {
                return null;
            }

            return null;
        }

        public void AddMusiqueBPM(Musique musique,int bpm)
        {
            // Vérifier si la clé existe déjà dans le dictionnaire
            if (musiquesBPM.ContainsKey(musique))
            {
                // La clé existe, mettre à jour la valeur associée
                musiquesBPM[musique] = bpm;
            }
            else
            {
                // La clé n'existe pas, ajouter une nouvelle entrée
                musiquesBPM.Add(musique, bpm);
            }
        }
    }

}
