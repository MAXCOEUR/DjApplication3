using DjApplication3.DataSource;
using DjApplication3.model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DjApplication3.view.activity
{
    /// <summary>
    /// Logique d'interaction pour ParametresForm.xaml
    /// </summary>
    public partial class ParametresForm : UserControl
    {
        private string CoYtMusic = "Connexion Youtube Music";
        private string decoYtMusic = "Deconnexion Youtube Music";
        private SettingsManager settingsManager = SettingsManager.Instance;

        public event EventHandler Closing;

        public ParametresForm()
        {
            InitializeComponent();
            PopulateComboBoxes();
            setButtonConnectionYoutubeMusic();
            connectedYtMusic.Closing += connectedYtMusicClosing;
        }

        private void PopulateComboBoxes()
        {
            cb_audioStandard.Items.Clear();
            foreach (var sortie in settingsManager.dispositifsAudio)
            {
                cb_audioStandard.Items.Add(sortie);
                cb_audioHeadPhone.Items.Add(sortie);
            }
            if (cb_audioStandard.Items.Count > 0 && cb_audioHeadPhone.Items.Count > 0)
            {
                cb_audioStandard.SelectedItem = cb_audioStandard.Items[settingsManager.nbrOut];
                cb_audioHeadPhone.SelectedItem = cb_audioHeadPhone.Items[settingsManager.nbrHeadPhone];
            }

            foreach (var midi in settingsManager.listMidi)
            {
                cb_midi.Items.Add(midi.ProductName);
            }
            if (cb_midi.Items.Count > 0)
            {
                cb_midi.SelectedItem = cb_midi.Items[settingsManager.nbrMidi];
            }
        }

        private void setButtonConnectionYoutubeMusic()
        {
            if (YtMusicDataSource.isConnected())
            {
                bt_connectYtMusic.Content = decoYtMusic;
            }
            else
            {
                bt_connectYtMusic.Content = CoYtMusic;
            }
        }

        private void cb_audioHeadPhone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.nbrHeadPhone = cb_audioHeadPhone.SelectedIndex;
        }

        private void cb_audioStandard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.nbrOut = cb_audioStandard.SelectedIndex;
        }


        private async void btConnected()
        {
            if (YtMusicDataSource.isConnected())
            {
                YtMusicDataSource.removeConnect();
                setButtonConnectionYoutubeMusic();
            }
            else
            {
                connectedYtMusic.OpenPage();
            }
        }

        private void connectedYtMusicClosing(object? sender, EventArgs e)
        {
            setButtonConnectionYoutubeMusic();
        }

        private void bt_connectYtMusic_Click(object sender, RoutedEventArgs e)
        {
            btConnected();
        }

        private void cb_midi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            settingsManager.nbrMidi = cb_midi.SelectedIndex;
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            Closing.Invoke(this, EventArgs.Empty);
        }
    }
}
