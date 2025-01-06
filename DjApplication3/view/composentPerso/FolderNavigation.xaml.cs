using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel;

namespace DjApplication3.view.windows
{
    /// <summary>
    /// Logique d'interaction pour FolderNavigation.xaml
    /// </summary>
    public partial class FolderNavigation : UserControl
    {
        public event EventHandler<FileSystemNode> SelectionChanged;

        public FileSystemNode CurrentItems;

        public FileSystemNode RootItems { get; set; }
        public FolderNavigation()
        {
            InitializeComponent();

            //// Initialisation des données
            //string directoryPath = @"F:\document\Documents\devloppement perso";
            //string folderName = System.IO.Path.GetFileName(directoryPath);
            //string[] subdirectories = Directory.GetDirectories(directoryPath);

            //// Créer le node racine (dossier principal)
            //FileSystemNode Items = new FileSystemNode
            //{
            //    Name = folderName,
            //    FullPath = directoryPath,
            //    Children = new ObservableCollection<FileSystemNode>()
            //};
            //setRootPath(Items.FullPath);

            //DisplayFileSystemNote(RootItems);
        }

        public void setRootPath(string path)
        {
            string folderName = System.IO.Path.GetFileName(path);

            // Créer le node racine (dossier principal)
            FileSystemNode Items = new FileSystemNode
            {
                Name = folderName,
                FullPath = path,
                Children = new ObservableCollection<FileSystemNode>()
            };

            RootItems = Items;
            string[] subdirectories = Directory.GetDirectories(RootItems.FullPath);
            // Ajouter les sous-dossiers comme enfants du node racine
            foreach (var subdirectory in subdirectories)
            {
                RootItems.Children.Add(new FileSystemNode
                {
                    Name = System.IO.Path.GetFileName(subdirectory),
                    FullPath = subdirectory,
                    Children = new ObservableCollection<FileSystemNode>() // Enfants (sous-dossiers) peuvent être ajoutés plus tard
                });
            }
            DisplayFileSystemNote(RootItems);
            ItemListBox.Focus();
        }

        public void Up()
        {
            int indexSelection = ItemListBox.SelectedIndex;
            if (indexSelection != -1 && indexSelection > 0)
            {
                setSelectedIndex(indexSelection - 1);
            }
        }
        public void Down()
        {
            int indexSelection = ItemListBox.SelectedIndex;
            if (indexSelection!=-1 && indexSelection < CurrentItems.Children.Count - 1)
            {
                setSelectedIndex(indexSelection + 1);
            }
        }
        public void FocusItemListBox()
        {
            ItemListBox.Focus();
        }
        public void EnterFolder()
        {
            FileSystemNode selectedItem = (FileSystemNode)ItemListBox.SelectedItem;
            MouvToChild(selectedItem);
        }

        private void ItemListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Enter:
                    EnterFolder();
                    e.Handled = true;
                    break;
                case Key.Up:
                    Up();
                    e.Handled = true;
                    break;
                case Key.Down:
                    Down();
                    e.Handled = true;
                    break;
            }
        }

        private void setSelectedIndex(int index)
        {
            ItemListBox.SelectedIndex = index;
            FileSystemNode selectedItem = (FileSystemNode)ItemListBox.SelectedItem;
            ItemListBox.ScrollIntoView(selectedItem);
        }

        private void MouvToChild(FileSystemNode selectedItem)
        {
            string[] subdirectories = Directory.GetDirectories(selectedItem.FullPath);
            foreach (var subdirectory in subdirectories)
            {
                selectedItem.Children.Add(new FileSystemNode
                {
                    Name = System.IO.Path.GetFileName(subdirectory),
                    FullPath = subdirectory,
                    Children = new ObservableCollection<FileSystemNode>() // Enfants (sous-dossiers) peuvent être ajoutés plus tard
                });
            }
            DisplayFileSystemNote(selectedItem);
        }

        private void DisplayFileSystemNote(FileSystemNode selectedItem)
        {
            if (RootItems != selectedItem)
            {
                selectedItem.Children.Insert(0, new FileSystemNode
                {
                    Name = "<= Retour", // Nom du dossier parent (bouton de retour)
                    FullPath = Directory.GetParent(selectedItem.FullPath).FullName, // Obtention du chemin du dossier parent
                    Children = new ObservableCollection<FileSystemNode>() // Liste vide d'enfants pour le moment
                });
            }
            string relativePath = selectedItem.FullPath.Replace(Directory.GetParent(RootItems.FullPath).FullName, "").TrimStart(System.IO.Path.DirectorySeparatorChar);

            tv_path.Content = relativePath;
            CurrentItems = selectedItem;
            ItemListBox.ItemsSource = CurrentItems.Children;
            setSelectedIndex(0);
        }

        private void ItemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileSystemNode selectedItem = (FileSystemNode)ItemListBox.SelectedItem;
            SelectionChanged?.Invoke(this, selectedItem);
        }

        private void ItemListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileSystemNode selectedItem = (FileSystemNode)ItemListBox.SelectedItem;
            MouvToChild(selectedItem);
        }
    }
}
