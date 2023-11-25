using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.view.fragment
{
    internal class ExplorateurYtMusicViewModel : ExplorateurInternetViewModel
    {
        public override event EventHandler<List<Musique>> TacheSearch;
        public override event EventHandler<Musique> TacheDownload;

        public async override void DownloadMusique(Musique musiqueyt)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            Musique musique = await Task.Run(() => musiqueRepository.DownloadMusiqueYtMusic(musiqueyt));
            TacheDownload?.Invoke(this, musique);
        }

        public override int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }

        public async override void search(string search)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMusiqueYtMusic(search));
            TacheSearch?.Invoke(this, musiques);
        }
    }
}
