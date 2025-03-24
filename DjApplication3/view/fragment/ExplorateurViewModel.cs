using DjApplication3.model;
using DjApplication3.repository;
using DjApplication3.view.windows;
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

        private CancellationTokenSource _cancellationTokenGet;

        public async void getMusique(string folderPath)
        {
            try
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                new ToastMessage(e.Message, ToastMessage.ToastType.Error).Show();
            }

        }

        public int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
    }
}
