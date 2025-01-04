using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DjApplication3.view.fragment
{
    public abstract class ExplorateurInternetViewModel
    {
        public abstract event EventHandler<List<Musique>?> TacheSearch;
        public abstract event EventHandler<Musique?> TacheDownload;

        public abstract void search(string search);
        public abstract int? getBpm(Musique musique);
        public abstract void DownloadMusique(Musique musique);
    }
}
