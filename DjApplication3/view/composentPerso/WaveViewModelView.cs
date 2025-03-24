using DjApplication3.model;
using DjApplication3.repository;
using DjApplication3.view.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.view.composentPerso
{
    internal class WaveViewModelView
    {
        public event EventHandler<sbyte[]> TacheGetWave;
        public async void getWave(Musique musique)
        {
            try
            {
                MusiqueRepository musiqueRepository = new MusiqueRepository();
                sbyte[] wave = await Task.Run(() => musiqueRepository.getWave(musique));
                TacheGetWave?.Invoke(this, wave);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
            }
        }
    }
}
