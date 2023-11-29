using CSCore.XAudio2;
using DjApplication3.DataSource;
using DjApplication3.model;
using Microsoft.Win32;
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
using TagLib.Tiff.Pef;

namespace DjApplication3.view.windows
{
    /// <summary>
    /// Logique d'interaction pour ParametresForm.xaml
    /// </summary>
    public partial class ParametresForm : Window
    {
        private string CoYtMusic ="Connexion Youtube Music";
        private string decoYtMusic = "Deconnexion Youtube Music";
        private SettingsManager settingsManager = SettingsManager.Instance;
        public ParametresForm()
        {
            InitializeComponent();
            PopulateComboBoxes();
            setButtonConnectionYoutubeMusic();
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

            List<string> installedBrowsers = GetInstalledBrowsers();
            cb_browser.ItemsSource= installedBrowsers;
            cb_browser.SelectedIndex=settingsManager.browser;
            settingsManager.browserName = cb_browser.SelectedItem.ToString();
            tb_pathTshark.Text = settingsManager.pathTShark;
            tb_numeroUSB.Text = settingsManager.numeroUSB;
        }

        private void setButtonConnectionYoutubeMusic() {
            if (YtMusicDataSource.isConnected())
            {
                bt_connectYtMusic.Content = decoYtMusic;
            }
            else
            {
                bt_connectYtMusic.Content = CoYtMusic;
            }
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


        private List<string> GetInstalledBrowsers()
        {
            List<string> browsers = new List<string>();

            // Les navigateurs sont souvent enregistrés dans le registre
            // Sous la clé "SOFTWARE\Clients\StartMenuInternet"
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet"))
            {
                if (key != null)
                {
                    // Obtenez les noms des sous-clés (qui représentent les navigateurs)
                    string[] browserNames = key.GetSubKeyNames();

                    foreach (string browserName in browserNames)
                    {
                        // Ajoutez le nom du navigateur à la liste
                        browsers.Add(browserName);
                    }
                }
            }

            return browsers;
        }

        private void cb_browser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.browser = cb_browser.SelectedIndex;
            settingsManager.browserName = cb_browser.SelectedItem.ToString();
        }


        private async void btConnected()
        {
            if (YtMusicDataSource.isConnected())
            {
                YtMusicDataSource.removeConnect();
            }
            else
            {
                ConnectedYtMusic connectedytMusic = new ConnectedYtMusic();
                connectedytMusic.ShowDialog();
            }
            setButtonConnectionYoutubeMusic();
        }

        private void bt_connectYtMusic_Click(object sender, RoutedEventArgs e)
        {
            btConnected();
        }

        private void tb_chemainTshark_TextChanged(object sender, TextChangedEventArgs e)
        {
            settingsManager.pathTShark = tb_pathTshark.Text;
        }

        private void tb_numeroUSB_TextChanged(object sender, TextChangedEventArgs e)
        {
            settingsManager.numeroUSB=tb_numeroUSB.Text;
        }
    }
}
