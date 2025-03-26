using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.repository;
using DjApplication3.view.windows;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DjApplication3.view.fragment
{
    internal class ExplorateurYtMusicViewModel : ExplorateurInternetViewModel
    {
        public override event EventHandler<List<Musique>?> TacheSearch;
        public override event EventHandler<(Musique?, int?)> TacheDownload;

        public event EventHandler<List<Musique>?> TacheGetMusiqueInPlayListe;
        public event EventHandler<List<PlayListe>?> TacheGetPlayListe;

        private CancellationTokenSource _cancellationTokenSearch;
        private CancellationTokenSource _cancellationTokenGetPlaylist;
        private CancellationTokenSource _cancellationTokenGetPlaylistIn;

        public async override void DownloadMusique(Musique musiqueyt, int? numeroPisteAssocie)
        {
            try
            {
                MusiqueRepository musiqueRepository = new MusiqueRepository();
                Musique musique = await Task.Run(() => musiqueRepository.DownloadMusiqueYtMusic(musiqueyt));
                TacheDownload?.Invoke(this, (musique, numeroPisteAssocie));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
            }
        }

        public override int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }

        public async override void search(string search)
        {
            try
            {
                // Annule la recherche précédente si elle est en cours
                _cancellationTokenSearch?.Cancel();
                _cancellationTokenSearch = new CancellationTokenSource();
                var token = _cancellationTokenSearch.Token;

                MusiqueRepository musiqueRepository = new MusiqueRepository();
                List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMusiqueYtMusic(search));

                if (!token.IsCancellationRequested)
                {
                    TacheSearch?.Invoke(this, musiques);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
            }
        }

        public async void getMusiqueInPlayListe(string idPlayliste)
        {
            try
            {
                // Annule la recherche précédente si elle est en cours
                _cancellationTokenGetPlaylistIn?.Cancel();
                _cancellationTokenGetPlaylistIn = new CancellationTokenSource();
                var token = _cancellationTokenGetPlaylistIn.Token;

                MusiqueRepository musiqueRepository = new MusiqueRepository();
                List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMusiqueInPlayListeYtMusic(idPlayliste));

                if (!token.IsCancellationRequested)
                {
                    TacheGetMusiqueInPlayListe?.Invoke(this, musiques);
                }
            }
            catch (NotConnectedException ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Info).Show();
                TacheGetMusiqueInPlayListe?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
                TacheGetMusiqueInPlayListe?.Invoke(this, null);
            }
        }
        public async void getPlayListe()
        {
            try
            {
                // Annule la recherche précédente si elle est en cours
                _cancellationTokenGetPlaylist?.Cancel();
                _cancellationTokenGetPlaylist = new CancellationTokenSource();
                var token = _cancellationTokenGetPlaylist.Token;
                MusiqueRepository musiqueRepository = new MusiqueRepository();
                List<PlayListe> playListe = await Task.Run(() => musiqueRepository.GetPlayListeYtMusic());
                if (!token.IsCancellationRequested)
                {
                    TacheGetPlayListe?.Invoke(this, playListe);
                }
            }
            catch (NotConnectedException ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Info).Show();
                TacheGetPlayListe?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error).Show();
                TacheGetPlayListe?.Invoke(this, null);
            }
        }
    }
}
