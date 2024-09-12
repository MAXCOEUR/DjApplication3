using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    public class LecteurMusiqueViewModel
    {
        public event EventHandler<int> TacheGetBPM;
        public async void getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            int bpm = musiqueRepository.getBpm(musique);
            TacheGetBPM?.Invoke(this, bpm);
        }
    }
}
