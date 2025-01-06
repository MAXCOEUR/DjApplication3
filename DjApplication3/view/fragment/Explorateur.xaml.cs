using CSCore.XAudio2.X3DAudio;
using DjApplication3.model;
using DjApplication3.view.windows;
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

        FrameworkElement CurrentVisualSelected;

        public Explorateur()
        {
            InitializeComponent();

            viewModel.TacheGetMusique += ModelView_TacheGetMusique;
            Fn_navigation.SelectionChanged += Fn_navigation_SelectionChanged;

            CurrentVisualSelected = Fn_navigation;

            initRoot("musique");
        }

        private void initRoot(string rootFolder)
        {
            displayLoadingTree();
            this.rootFolder = Path.GetFullPath(rootFolder);
            viewModel.getMusique(this.rootFolder);

            Fn_navigation.setRootPath(this.rootFolder);
            displayTree();

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
            if (dgv_listeMusic.Items.Count > 0)
            {
                // Récupère le premier élément
                var firstItem = dgv_listeMusic.Items[0];

                // Sélectionne le premier élément
                dgv_listeMusic.SelectedItem = firstItem;
            }

        }

        private void filtreMusique()
        {
            List<MusiqueColonne> musiquesTmp = musiques.Where(m => m.musique.title.Contains(search, StringComparison.OrdinalIgnoreCase) || m.musique.author.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            dgv_listeMusic.ItemsSource = musiquesTmp;
            
        }

        public void updateData()
        {
            dgv_listeMusic.Items.Refresh();
        }

        private void dgv_listeMusic_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;

            if (selectedItem != null)
            {
                valideRow(selectedItem.musique);
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
            valideRow(m, p);
        }

        private void valideRow(Musique musique,  int? numeroPisteAssocie = null)
        {
            if (numeroPisteAssocie.HasValue)
            {
                eventMusiqueSlectedWithPiste?.Invoke(this, (musique, numeroPisteAssocie.Value));
            }
            else
            {
                eventMusiqueSlected?.Invoke(this, musique);
            }
            
        }


        private void dgv_listeMusic_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentVisualSelected = dgv_listeMusic;
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

        }

        public void EnterSelected(int? piste = null)
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
            valideRow(selectedItem.musique, piste);
        }

        private void dgv_listeMusic_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    EnterSelected();
                    e.Handled = true;
                    break;
                case Key.Up:
                    keyUp();
                    e.Handled = true;
                    break;
                case Key.Down:
                    keyDown();
                    e.Handled = true;
                    break;
                case Key.Left:
                    keyLeft();
                    e.Handled = true;
                    break;
                case Key.Right:
                    keyRight();
                    e.Handled = true;
                    break;
            }
        }

        public void keyUp()
        {
            if(CurrentVisualSelected is DataGrid)
            {
                int indexCurrent = dgv_listeMusic.SelectedIndex;
                if(indexCurrent != -1)
                {
                    var newItemSelectedItem = dgv_listeMusic.Items[(indexCurrent <= 0) ? 0 : --indexCurrent];

                    dgv_listeMusic.SelectedItem = newItemSelectedItem;
                    dgv_listeMusic.ScrollIntoView(newItemSelectedItem);
                }
            }
            if (CurrentVisualSelected is FolderNavigation)
            {
                Fn_navigation.Up();
            }

            Console.WriteLine("Flèche haut pressée");
        }
        public void keyDown()
        {
            if (CurrentVisualSelected is DataGrid)
            {
                int indexCurrent = dgv_listeMusic.SelectedIndex;
                if (indexCurrent != -1)
                {
                    
                    var newItemSelectedItem = dgv_listeMusic.Items[(indexCurrent >= dgv_listeMusic.Items.Count - 1) ? dgv_listeMusic.Items.Count - 1 : ++indexCurrent];

                    dgv_listeMusic.SelectedItem = newItemSelectedItem;
                    dgv_listeMusic.ScrollIntoView(newItemSelectedItem);
                }
            }
            else if (CurrentVisualSelected is FolderNavigation)
            {
                Fn_navigation.Down();
            }
            
            Console.WriteLine("Flèche bas pressée");
        }
        public void keyLeft()
        {
            if (CurrentVisualSelected is FolderNavigation)
            {
                Fn_navigation.EnterFolder();
            }
            Fn_navigation.FocusItemListBox();
            CurrentVisualSelected = Fn_navigation;
            Console.WriteLine("keyLeft");
        }
        public void keyRight()
        {
            dgv_listeMusic.Focus();
            CurrentVisualSelected = dgv_listeMusic;
            Console.WriteLine("keyRight");
        }

        private void displayLoadingTree()
        {
            LoadingBarTree.Visibility = Visibility.Visible;
            Fn_navigation.Visibility = Visibility.Hidden;
        }

        private void displayLoadingListMusique()
        {
            LoadingBar.Visibility = Visibility.Visible;
            dgv_listeMusic.Visibility = Visibility.Hidden;
        }
        private void displayTree()
        {
            Fn_navigation.Visibility = Visibility.Visible;
            LoadingBarTree.Visibility = Visibility.Hidden;
        }

        private void displayListMusique()
        {
            dgv_listeMusic.Visibility = Visibility.Visible;
            LoadingBar.Visibility = Visibility.Hidden;
        }

        private void Fn_navigation_SelectionChanged(object sender, FileSystemNode e)
        {
            if (e != null)
            {
                displayLoadingListMusique();
                cleatDGV();

                if(e.Name =="<= Retour")
                {
                    viewModel.getMusique(Fn_navigation.CurrentItems.FullPath);
                }
                else
                {
                    viewModel.getMusique(e.FullPath);
                }
                
            }
        }

        private void Fn_navigation_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    keyLeft();
                    e.Handled = true;
                    break;
                case Key.Right:
                    keyRight();
                    e.Handled = true;
                    break;
            }
        }

        private void Fn_navigation_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentVisualSelected = Fn_navigation;
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
