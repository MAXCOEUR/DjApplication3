﻿using CSCore.XAudio2.X3DAudio;
using DjApplication3.model;
using DjApplication3.View.userControlDJ;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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

        public Explorateur()
        {
            InitializeComponent();

            viewModel.TacheGetDossierPerso += ViewModel_TacheGetDossierPerso;
            viewModel.TacheGetMusique += ModelView_TacheGetMusique;

            
            initRoot("musique");
        }

        private void initRoot(string rootFolder)
        {
            this.rootFolder = Path.GetFullPath(rootFolder);
            viewModel.GetDossierPerso(this.rootFolder);
            viewModel.getMusique(this.rootFolder);
        }
        private void ViewModel_TacheGetDossierPerso(object? sender, DossierPerso e)
        {
            ObservableCollection<DossierPerso> dossier = new ObservableCollection<DossierPerso> { e };
            tv_tree.ItemsSource = dossier;
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
            musiques = new List<MusiqueColonne>();

            foreach (Musique musique in e)
            {
                int? bpm = viewModel.getBpm(musique);
                musiques.Add(new MusiqueColonne(musique, bpm));
            }
            dgv_listeMusic.ItemsSource = musiques;
        }

        private void tv_tree_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
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

        private void dgv_listeMusic_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source != null && source.Parent is DataGridCell)
            {
                // Sélectionnez la ligne associée au clic droit
                DataGridCell row = (DataGridCell)source.Parent;
                row.IsSelected = !row.IsSelected;

                e.Handled = true; // Empêche la propagation de l'événement
            }
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
    }
}

public class MusiqueColonne
{
    public Musique musique;
    public int? Bpm;

    public string Title => musique.title;
    public string Author => musique.author;
    public MusiqueColonne(Musique musique, int? bpm)
    {
        this.musique = musique;
        Bpm = bpm;
    }
}