using DjApplication3.model;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace DjApplication3.view.windows
{
    /// <summary>
    /// Logique d'interaction pour ParametresForm.xaml
    /// </summary>
    public partial class ParametresForm : Window
    {
        private SettingsManager settingsManager = SettingsManager.Instance;
        public ParametresForm()
        {
            InitializeComponent();
            PopulateComboBoxes();
        }
        private void PopulateComboBoxes()
        {
            cb_audioStandard.Items.Clear();
            foreach (var sortie in settingsManager.dispositifsAudio)
            {
                cb_audioStandard.Items.Add(sortie);
                cb_audioHeadPhone.Items.Add(sortie);
            }
            cb_audioStandard.SelectedItem = cb_audioStandard.Items[settingsManager.nbrOut];
            cb_audioHeadPhone.SelectedItem = cb_audioHeadPhone.Items[settingsManager.nbrHeadPhone];
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cb_audioHeadPhone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.nbrHeadPhone = cb_audioHeadPhone.SelectedIndex;
        }

        private void cb_audioStandard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.nbrOut = cb_audioStandard.SelectedIndex;
        }
    }
}
