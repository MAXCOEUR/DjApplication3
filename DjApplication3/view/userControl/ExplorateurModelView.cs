using DjApplication3.model;
using DjApplication3.repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.View.userControlDJ
{
    internal class ExplorateurModelView
    {
        public event EventHandler<List<Musique>> TacheGetMusique;
        //public event EventHandler<TreeNode> TacheGetTreeNode;
        public async void getMusique(string folderPath)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            List<Musique> musiques = await Task.Run(() => musiqueRepository.GetMp3Files(folderPath));
            TacheGetMusique?.Invoke(this, musiques);
        }

        public async void GetTreeNode(string rootFolder)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            //TreeNode treeNode = await Task.Run(() => musiqueRepository.GetTreeNode(rootFolder));
            //TacheGetTreeNode?.Invoke(this, treeNode);
        }
        public int? getBpm(Musique musique)
        {
            MusiqueRepository musiqueRepository = new MusiqueRepository();
            return musiqueRepository.getBpmHistory(musique);
        }
    }
}
