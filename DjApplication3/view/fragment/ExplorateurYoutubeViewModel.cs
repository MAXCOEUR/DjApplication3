using DjApplication3.model;
using DjApplication3.repository;
using DjApplication3.view.fragment;
using DjApplication3.view.windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    internal class ExplorateurYoutubeViewModel : ExplorateurInternetViewModel
    {
        public override event EventHandler<List<Musique>?> TacheSearch;
        public override event EventHandler<(Musique?, int?)> TacheDownload;

        private CancellationTokenSource _cancellationTokenSearch;

        public async override void search(string search)
        {
            try
            {
                // Annule la recherche précédente si elle est en cours
                _cancellationTokenSearch?.Cancel();
                _cancellationTokenSearch = new CancellationTokenSource();
                var token = _cancellationTokenSearch.Token;

                MusiqueRepository musiqueRepository = new MusiqueRepository();
                List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMusiqueYoutube(search), token);

                if (!token.IsCancellationRequested)
                {
                    TacheSearch?.Invoke(this, musiques);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error, ex).Show();
            }

        }
        public override int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
        async public override void DownloadMusique(Musique musiqueyt, int? numeroPisteAssocie)
        {
            try
            {
                MusiqueRepository musiqueRepository = new MusiqueRepository();
                Musique musique = await Task.Run(() => musiqueRepository.DownloadMusiqueYoutube(musiqueyt));
                TacheDownload?.Invoke(this, (musique, numeroPisteAssocie));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error, ex).Show();
            }
        }
    }
}
