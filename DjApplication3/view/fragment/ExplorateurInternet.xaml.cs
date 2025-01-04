using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.View.userControlDJ;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DjApplication3.view.fragment
{
    /// <summary>
    /// Logique d'interaction pour ExplorateurInternet.xaml
    /// </summary>
    public partial class ExplorateurInternet : System.Windows.Controls.UserControl
    {
        public event EventHandler<Musique> eventMusiqueSlected;
        public event EventHandler<(Musique, int)> eventMusiqueSlectedWithPiste;

        List<MusiqueColonne> musiques = new List<MusiqueColonne>();


        private ExplorateurInternetViewModel viewModel;
        private ExplorateurYtMusicViewModel viewModelYtMusic;
        private System.Windows.Forms.Timer searchTimer = new System.Windows.Forms.Timer();
        static public string rootFolder = System.IO.Path.GetFullPath("musique/tmp");
        public ExplorateurInternet()
        {
            InitializeComponent();
        }

        public void setViewModel(ExplorateurInternetViewModel model)
        {
            viewModel = model;

            if(model is ExplorateurYtMusicViewModel)
            {
                viewModelYtMusic = (ExplorateurYtMusicViewModel) model;

                // Créez une nouvelle ColumnDefinition
                ColumnDefinition columnDefinition = new ColumnDefinition();
                columnDefinition.Width = new GridLength(1, GridUnitType.Star); // Définissez la largeur à "*"

                // Insérez la nouvelle colonne à la position 0
                g_column.ColumnDefinitions.Insert(0, columnDefinition);

                GridSplitter.Visibility = Visibility.Visible;
                g_tree.Visibility = Visibility.Visible;

                viewModelYtMusic.TacheGetMusiqueInPlayListe += ViewModelYtMusic_TacheGetMusiqueInPlayListe;
                viewModelYtMusic.TacheGetPlayListe += ViewModelYtMusic_TacheGetPlayListe;

                viewModelYtMusic.getPlayListe();

            }

            searchTimer.Interval = 1000; // Délai en millisecondes (01 seconde)
            searchTimer.Tick += SearchTimer_Tick;

            viewModel.TacheSearch += ViewModel_TacheSearch;
            viewModel.TacheDownload += ViewModel_TacheDownload;
            viewModel.search("");
        }

        private void ViewModelYtMusic_TacheGetPlayListe(object? sender, List<PlayListe>? e)
        {
            if (e == null)
            {
                displayErrorNetWorkTree();
                return;
            }
            displayTree();
            tv_tree.ItemsSource = e;
        }

        private void ViewModelYtMusic_TacheGetMusiqueInPlayListe(object? sender, List<Musique>? e)
        {
            musiques.Clear();

            if (e == null)
            {
                displayErrorNetWorkListMusique();
                return;
            }

            displayListMusique();

            foreach (Musique musique in e)
            {
                Musique musiqueTmp = new Musique(System.IO.Path.Combine(rootFolder, $"{musique.title} ({musique.author}).mp3"), musique.title, musique.author);
                int? bpm = viewModel.getBpm(musiqueTmp);
                musiques.Add(new MusiqueColonne(musique, bpm));
            }
            dgv_listeMusic.ItemsSource = musiques;
            if (dgv_listeMusic.Items.Count > 0)
            {
                // Récupère le premier élément
                var firstItem = dgv_listeMusic.Items[0];

                // Sélectionne le premier élément
                dgv_listeMusic.SelectedItem = firstItem;
            }
            
        }

        private void ViewModel_TacheSearch(object? sender, List<Musique>? e)
        {
            musiques.Clear();

            if (e == null)
            {
                displayErrorNetWorkListMusique();
                return;
            }

            displayListMusique();

            foreach (Musique musique in e)
            {
                Musique musiqueTmp = new Musique(System.IO.Path.Combine(rootFolder, $"{musique.title} ({musique.author}).mp3"), musique.title, musique.author);
                int? bpm = viewModel.getBpm(musiqueTmp);
                musiques.Add(new MusiqueColonne(musique, bpm));
            }
            dgv_listeMusic.ItemsSource = musiques;
            if (dgv_listeMusic.Items.Count > 0)
            {
                // Récupère le premier élément
                var firstItem = dgv_listeMusic.Items[0];

                // Sélectionne le premier élément
                dgv_listeMusic.SelectedItem = firstItem;
            }
            
        }
        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            // Le minuteur a expiré, déclencher la recherche
            searchTimer.Stop();
            PerformSearch(tb_serach.Text);
        }
        private void PerformSearch(string searchText)
        {
            viewModel.search(searchText);
        }

        private void tb_serach_TextChanged(object sender, TextChangedEventArgs e)
        {
            cleatDGV();

            var treeViewSource = (List<PlayListe>)tv_tree.ItemsSource;
            tv_tree.ItemsSource = null;
            tv_tree.ItemsSource = treeViewSource;

            displayLoadingListMusique();

            searchTimer.Stop();
            searchTimer.Start();
        }

        private void dgv_listeMusic_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;

            if (selectedItem != null)
            {
                valideRow(selectedItem.musique, dgv_listeMusic.SelectedIndex);
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
            var tuple = ((ValueTuple<Musique, int>)menuItem.Tag);
            valideRow(tuple.Item1, dgv_listeMusic.SelectedIndex, tuple.Item2);
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
        private void valideRow(Musique musique, int rowIndex, int? numeroPisteAssocie = null)
        {
            MusiqueColonne tmp = (MusiqueColonne)dgv_listeMusic.Items[rowIndex];

            tmp.Dl = "...";
            viewModel.DownloadMusique(musique, numeroPisteAssocie);
        }
        private void ViewModel_TacheDownload(object? sender, (Musique?,int?) TupleMusiqueNumeroPiste)
        {
            if (TupleMusiqueNumeroPiste.Item1 == null)
            {
                return;
            }

            string url = System.IO.Path.Combine(ExplorateurInternet.rootFolder, $"{TupleMusiqueNumeroPiste.Item1.title} ({TupleMusiqueNumeroPiste.Item1.author}).mp3");
            if (File.Exists(url))
            {
                for (int i = 0; i < dgv_listeMusic.Items.Count; i++)
                {
                    MusiqueColonne MusiqueColonne = (MusiqueColonne)dgv_listeMusic.Items[i];
                    if (MusiqueColonne.musique == TupleMusiqueNumeroPiste.Item1)
                    {
                        MusiqueColonne.Dl = "✔️";
                    }
                }
            }

            if (TupleMusiqueNumeroPiste.Item2.HasValue)
            {
                eventMusiqueSlectedWithPiste?.Invoke(this, (TupleMusiqueNumeroPiste.Item1, TupleMusiqueNumeroPiste.Item2.Value));
            }
            else
            {
                eventMusiqueSlected?.Invoke(this, TupleMusiqueNumeroPiste.Item1);
            }
        }
        public void updateBPM()
        {
            for (int i = 0; i < dgv_listeMusic.Items.Count; i++)
            {
                MusiqueColonne musiqueColonne = (MusiqueColonne)dgv_listeMusic.Items[i];

                Musique musiqueTmp = new Musique(System.IO.Path.Combine(rootFolder, $"{musiqueColonne.musique.title} ({musiqueColonne.musique.author}).mp3"), musiqueColonne.musique.title, musiqueColonne.musique.author);

                int? bpm = viewModel.getBpm(musiqueTmp);
                musiqueColonne.Bpm = bpm;
            }
            
        }

        private void bt_reload_Click(object sender, RoutedEventArgs e)
        {
            displayLoadingTree();
            viewModelYtMusic.getPlayListe();
        }

        private void tv_tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                cleatDGV();

                var selectedItem = (PlayListe)e.NewValue;
                viewModelYtMusic.getMusiqueInPlayListe(selectedItem.id);
                tb_serach.Text = "";

                displayLoadingListMusique();
            }
        }

        private void cleatDGV()
        {
            // Définir la source de données à null
            dgv_listeMusic.ItemsSource = null;

            // Nettoyer les éléments du contrôle
            dgv_listeMusic.Items.Clear();

            // Actualiser le contrôle
            
        }

        private void displayErrorNetWorkTree()
        {
            errorMessageTree.Visibility = Visibility.Visible;
            tv_tree.Visibility = Visibility.Hidden;
            LoadingBarTree.Visibility = Visibility.Hidden;
        }

        private void displayErrorNetWorkListMusique()
        {
            errorMessageListeMusique.Visibility = Visibility.Visible;
            dgv_listeMusic.Visibility = Visibility.Hidden;
            LoadingBar.Visibility = Visibility.Hidden;
        }

        private void displayLoadingTree()
        {
            LoadingBarTree.Visibility = Visibility.Visible;
            errorMessageTree.Visibility = Visibility.Hidden;
            tv_tree.Visibility = Visibility.Hidden;
        }

        private void displayLoadingListMusique()
        {
            LoadingBar.Visibility = Visibility.Visible;
            errorMessageListeMusique.Visibility = Visibility.Hidden;
            dgv_listeMusic.Visibility = Visibility.Hidden;
        }
        private void displayTree()
        {
            tv_tree.Visibility = Visibility.Visible;
            LoadingBarTree.Visibility = Visibility.Hidden;
            errorMessageTree.Visibility = Visibility.Hidden;
        }

        private void displayListMusique()
        {
            dgv_listeMusic.Visibility = Visibility.Visible;
            LoadingBar.Visibility = Visibility.Hidden;
            errorMessageListeMusique.Visibility = Visibility.Hidden;
        }

        private void g_tree_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                //case Key.Up:
                //    keyUp();
                //    e.Handled = true;
                //    break;
                //case Key.Down:
                //    keyDown();
                //    e.Handled = true;
                //    break;
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

        private void dgv_listeMusic_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
                    valideRow(selectedItem.musique, dgv_listeMusic.SelectedIndex);
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
        public void keyLoadLeft()
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
            valideRow(selectedItem.musique, dgv_listeMusic.SelectedIndex,0);
        }
        public void keyLoadRight()
        {
            MusiqueColonne selectedItem = (MusiqueColonne)dgv_listeMusic.SelectedItem;
            valideRow(selectedItem.musique, dgv_listeMusic.SelectedIndex,1);
        }
        public void keyUp()
        {
            int indexCurrent = dgv_listeMusic.SelectedIndex;
            var newItemSelectedItem = dgv_listeMusic.Items[(indexCurrent<=0)?0:--indexCurrent];

            dgv_listeMusic.SelectedItem = newItemSelectedItem;
            dgv_listeMusic.ScrollIntoView(newItemSelectedItem);
        }
        public void keyDown()
        {
            int indexCurrent = dgv_listeMusic.SelectedIndex;
            var newItemSelectedItem = dgv_listeMusic.Items[(indexCurrent >= dgv_listeMusic.Items.Count-1) ? dgv_listeMusic.Items.Count-1 : ++indexCurrent];

            dgv_listeMusic.SelectedItem = newItemSelectedItem;
            dgv_listeMusic.ScrollIntoView(newItemSelectedItem);
        }
        public void keyLeft()
        {
            tv_tree.Focus();
            if (tv_tree.SelectedItem is DossierPerso selectedDossier)
            {
                // Si l'élément a des enfants et est déplié, on le plie
                TreeViewItem? selectedItem = tv_tree.ItemContainerGenerator.ContainerFromItem(selectedDossier) as TreeViewItem;
                if (selectedItem == null)
                {
                    return;
                }
                selectedItem.IsExpanded = !selectedItem.IsExpanded;
            }
            else
            {
                Console.WriteLine("Aucun élément sélectionné ou pas un DossierPerso.");
            }
        }
        public void keyRight()
        {
            dgv_listeMusic.Focus();
        }
    }
    public class MusiqueColonne : INotifyPropertyChanged
    {
        public Musique musique { get; set; }

        private int? bpm;
        private string bpmString="";

        public int? Bpm
        {
            get => bpm;
            set
            {
                if (bpm != value)
                {
                    bpm = value;
                    BpmString = bpm?.ToString() ?? ""; // Met à jour BpmString automatiquement
                    OnPropertyChanged(nameof(Bpm));   // Notifie la vue que Bpm a changé
                }
            }
        }

        public string BpmString
        {
            get => bpmString;
            set
            {
                if (bpmString != value)
                {
                    bpmString = value;
                    OnPropertyChanged(nameof(BpmString)); // Notifie la vue que BpmString a changé
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => musique.title;
        public string Author => musique.author;

        private string dl;
        public string Dl
        {
            get
            {
                string url = System.IO.Path.Combine(ExplorateurInternet.rootFolder, $"{musique.title} ({musique.author}).mp3");
                if (File.Exists(url))
                {
                    return "✔️";
                }
                return dl;
            }
            set
            {
                if (dl != value)
                {
                    dl = value;
                    OnPropertyChanged(nameof(Dl));
                }
            }
        }

        public MusiqueColonne(Musique musique, int? bpm)
        {
            this.musique = musique;
            Bpm = bpm;
            dl = "";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
