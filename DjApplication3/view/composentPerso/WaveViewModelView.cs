using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.view.composentPerso
{
    internal class WaveViewModelView
    {
        public event EventHandler<List<float>> TacheGetWave;
        public async void getWave(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<float> wave = await Task.Run(() => musiqueRepository.getWave(musique));
            TacheGetWave?.Invoke(this, wave);
        }
    }
}
