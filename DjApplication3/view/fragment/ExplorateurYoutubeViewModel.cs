﻿using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    internal class ExplorateurYoutubeViewModel
    {
        public event EventHandler<List<Musique>> TacheSearch;
        public event EventHandler<Musique> TacheDownload;
        public async void search(string search)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMusiqueYoutube(search));
            TacheSearch?.Invoke(this, musiques);
        }
        public int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
        async public void DownloadMusique(Musique musiqueyt)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            Musique musique = await Task.Run(() => musiqueRepository.DownloadMusiqueYoutube(musiqueyt));
            TacheDownload?.Invoke(this, musique);
        }
    }
}