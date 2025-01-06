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
    public partial class PlayListNavigation : UserControl
    {
        public event EventHandler<PlayListe> SelectionChanged;
        public List<PlayListe> PlayLists { get; set; }
        public PlayListNavigation()
        {
            InitializeComponent();
        }

        public void setPlayLists(List<PlayListe> Pl)
        {
            PlayLists = Pl;
            DisplayFileSystemNote();
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
            if (indexSelection!=-1 && indexSelection < PlayLists.Count - 1)
            {
                setSelectedIndex(indexSelection + 1);
            }
        }
        public void FocusItemListBox()
        {
            ItemListBox.Focus();
        }

        private void ItemListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
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
            PlayListe selectedItem = (PlayListe)ItemListBox.SelectedItem;
            ItemListBox.ScrollIntoView(selectedItem);
        }

        private void DisplayFileSystemNote()
        {
            ItemListBox.ItemsSource = PlayLists;
            setSelectedIndex(0);
        }

        private void ItemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayListe selectedItem = (PlayListe)ItemListBox.SelectedItem;
            SelectionChanged?.Invoke(this, selectedItem);
        }
    }
}
