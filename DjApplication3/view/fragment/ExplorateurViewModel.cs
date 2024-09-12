using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    internal class ExplorateurViewModel
    {
        public event EventHandler<List<Musique>> TacheGetMusique;
        public event EventHandler<DossierPerso> TacheGetDossierPerso;
        public async void getMusique(string folderPath)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<Musique> musiques = musiqueRepository.GetMp3Files(folderPath);
            TacheGetMusique?.Invoke(this, musiques);
        }

        public async void GetDossierPerso(string rootFolder)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            DossierPerso dossier =  musiqueRepository.GetDossierPerso(rootFolder);
            TacheGetDossierPerso?.Invoke(this, dossier);
        }
        public int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
    }
}
