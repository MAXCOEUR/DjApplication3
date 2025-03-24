using DjApplication3.model;
using DjApplication3.repository;
using DjApplication3.view.windows;
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
            try
            {
                MusiqueRepository musiqueRepository = new MusiqueRepository();
                int bpm = await Task.Run(() => musiqueRepository.getBpm(musique));
                TacheGetBPM?.Invoke(this, bpm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
            }
        }
    }
}
