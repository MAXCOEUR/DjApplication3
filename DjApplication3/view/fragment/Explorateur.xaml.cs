using CSCore.XAudio2.X3DAudio;
using DjApplication3.model;
using DjApplication3.View.userControlDJ;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace DjApplication3.view.fragment
{
    /// <summary>
    /// Logique d'interaction pour Explorateur.xaml
    /// </summary>
    public partial class Explorateur : System.Windows.Controls.UserControl
    {
        public event EventHandler<Musique> eventMusiqueSlected;
        public event EventHandler<(Musique, int)> eventMusiqueSlectedWithPiste;

        string rootFolder;
        private ExplorateurViewModel viewModel = new ExplorateurViewModel();
        List<MusiqueColonne> musiques = new List<MusiqueColonne>();

        string search = "";

        public Explorateur()
        {
            InitializeComponent();

            viewModel.TacheGetDossierPerso += ViewModel_TacheGetDossierPerso;
            viewModel.TacheGetMusique += ModelView_TacheGetMusique;

            
            initRoot("musique");
        }

        private void initRoot(string rootFolder)
        {
            displayLoadingTree();
            this.rootFolder = Path.GetFullPath(rootFolder);
            viewModel.GetDossierPerso(this.rootFolder);
            viewModel.getMusique(this.rootFolder);
        }
        private void ViewModel_TacheGetDossierPerso(object? sender, DossierPerso e)
        {
            displayTree();
            tv_tree.ItemsSource = e.Children;
        }

        private void bt_changeRoot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                // Définissez le titre de la boîte de dialogue
                folderBrowser.Description = "Sélectionnez le dossier racine pour l'explorateur de musique";

                // Affichez la boîte de dialogue et vérifiez si l'utilisateur a appuyé sur OK
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    // Le chemin du dossier sélectionné par l'utilisateur
                    string selectedFolderPath = folderBrowser.SelectedPath;
                    initRoot(selectedFolderPath);

                }
            }
        }

        private void bt_reload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            initRoot(rootFolder);
        }
        private void ModelView_TacheGetMusique(object? sender, List<Musique> e)
        {
            musiques.Clear();            

            foreach (Musique musique in e)
            {
                int? bpm = viewModel.getBpm(musique);
                musiques.Add(new MusiqueColonne(musique, bpm));
            }
            filtreMusique();
            displayListMusique();


        }

        private void filtreMusique()
        {
            displayLoadingListMusique();
            List<MusiqueColonne> musiquesTmp = musiques.Where(m => m.musique.title.Contains(search, StringComparison.OrdinalIgnoreCase) || m.musique.author.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            dgv_listeMusic.ItemsSource = musiquesTmp;
            dgv_listeMusic.Items.Refresh();
        }

        public void updateData()
        {
            dgv_listeMusic.Items.Refresh();
        }
        private void tv_tree_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                displayLoadingListMusique();
                cleatDGV();

                var selectedItem = (DossierPerso)e.NewValue;
                string path = Path.Combine(Path.GetDirectoryName(rootFolder), selectedItem.getPath());
                viewModel.getMusique(path);
            }
        }

        private void dgv_listeMusic_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;

            if (selectedItem != null)
            {
                eventMusiqueSlected?.Invoke(this, selectedItem.musique);
            }
        }

        private void dgv_listeMusic_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                cm_PisteList.Items.Clear();
                MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
                for (int i = 0; i < SettingsManager.Instance.nbrPiste; i++)
                {
                    MenuItem menuItem = new MenuItem { Header = $"Piste {i + 1}", Tag = (selectedItem.musique, i) };
                    menuItem.Click += MenuItem_Click;
                    cm_PisteList.Items.Add(menuItem);
                }
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Code à exécuter lorsqu'une option du menu est cliquée
            MenuItem menuItem = (MenuItem)sender;
            (Musique m, int p) = ((ValueTuple<Musique, int>)menuItem.Tag);
            eventMusiqueSlectedWithPiste?.Invoke(this, (m, p));
        }


        private void dgv_listeMusic_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Assurez-vous que l'élément source est une cellule du DataGrid
            var source = e.OriginalSource as FrameworkElement;
            if (source != null && source.Parent is DataGridCell)
            {
                // Sélectionnez la ligne associée au clic
                DataGridCell cell = (DataGridCell)source.Parent;
                DataGridRow row = FindVisualParent<DataGridRow>(cell);

                // Assurez-vous que la ligne est valide
                if (row != null)
                {
                    dgv_listeMusic.SelectedItem = row.Item;
                }
            }
        }
        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                if (parent is T correctlyTyped)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }
        public void updateBPM()
        {
            for (int i = 0;i<dgv_listeMusic.Items.Count;i++)
            {
                MusiqueColonne musiqueColonne = (MusiqueColonne) dgv_listeMusic.Items[i];
                int? bpm = viewModel.getBpm(musiqueColonne.musique);
                musiqueColonne.Bpm = bpm;
            }
            dgv_listeMusic.Items.Refresh();
        }

        private void tb_serach_TextChanged(object sender, TextChangedEventArgs e)
        {
            search=tb_serach.Text;
            filtreMusique();
        }

        private void cleatDGV()
        {
            // Définir la source de données à null
            dgv_listeMusic.ItemsSource = null;

            // Nettoyer les éléments du contrôle
            dgv_listeMusic.Items.Clear();

            // Actualiser le contrôle
            dgv_listeMusic.Items.Refresh();
        }

        private void dgv_listeMusic_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if(dgv_listeMusic.SelectedIndex < dgv_listeMusic.Items.Count - 1)
                {
                    dgv_listeMusic.SelectedIndex = dgv_listeMusic.SelectedIndex - 1;
                }
                MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
                eventMusiqueSlected?.Invoke(this, selectedItem.musique);
            }
        }

        private void displayLoadingTree()
        {
            LoadingBarTree.Visibility = Visibility.Visible;
            tv_tree.Visibility = Visibility.Hidden;
        }

        private void displayLoadingListMusique()
        {
            LoadingBar.Visibility = Visibility.Visible;
            dgv_listeMusic.Visibility = Visibility.Hidden;
        }
        private void displayTree()
        {
            tv_tree.Visibility = Visibility.Visible;
            LoadingBarTree.Visibility = Visibility.Hidden;
        }

        private void displayListMusique()
        {
            dgv_listeMusic.Visibility = Visibility.Visible;
            LoadingBar.Visibility = Visibility.Hidden;
        }
    }
}

public class MusiqueColonne
{
    public Musique musique;
    public int? Bpm;
    public string getBpm
    {
        get
        {
            string v = "";
            if (Bpm != null)
            {
                v = Bpm?.ToString();
            }
            return v;
        }
    }

    public string Title => musique.title;
    public string Author => musique.author;
    public MusiqueColonne(Musique musique, int? bpm)
    {
        this.musique = musique;
        Bpm = bpm;
    }
}
