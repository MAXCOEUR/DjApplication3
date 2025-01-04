using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    internal class ExplorateurViewModel
    {
        public event EventHandler<List<Musique>> TacheGetMusique;
        public event EventHandler<DossierPerso> TacheGetDossierPerso;

        private CancellationTokenSource _cancellationTokenGet;

        public async void getMusique(string folderPath)
        {
            // Annule la recherche précédente si elle est en cours
            _cancellationTokenGet?.Cancel();
            _cancellationTokenGet = new CancellationTokenSource();
            var token = _cancellationTokenGet.Token;

            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMp3Files(folderPath));

            if (!token.IsCancellationRequested)
            {
                TacheGetMusique?.Invoke(this, musiques);
            }
            
        }

        public async void GetDossierPerso(string rootFolder)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            DossierPerso dossier = await Task.Run(() => musiqueRepository.GetDossierPerso(rootFolder));
            TacheGetDossierPerso?.Invoke(this, dossier);
        }
        public int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
    }
}
