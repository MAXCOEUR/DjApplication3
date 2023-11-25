using DjApplication3.model;
using DjApplication3.view.fragment;
using DjApplication3.view.page;
using DjApplication3.view.windows;
using DjApplication3.View.userControlDJ;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DjApplication3.view.activity
{
    /// <summary>
    /// Logique d'interaction pour MainPageMixage.xaml
    /// </summary>
    public partial class MainPageMixage : UserControl
    {

        List<LecteurMusique> lecteurMusiques = new List<LecteurMusique>();
        public MainPageMixage()
        {
            InitializeComponent();

            exploLocal.eventMusiqueSlected += eventMusiqueSlected;
            exploLocal.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploYoutube.setViewModel(new ExplorateurYoutubeViewModel());
            exploSpotify.setViewModel(new ExplorateurSpotifyViewModel());
            exploYtMusic.setViewModel(new ExplorateurYtMusicViewModel());

            exploYtMusic.eventMusiqueSlected += eventMusiqueSlected;
            exploYtMusic.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploYoutube.eventMusiqueSlected += eventMusiqueSlected;
            exploYoutube.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploSpotify.eventMusiqueSlected += eventMusiqueSlected;
            exploSpotify.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            mixage2Pistes.tb_mixage.ValueChanged += Tb_mixage_ValueChanged;
            mixage2Pistes.eventSetPiste += Mixage2Pistes_eventSetPiste;

            Loaded += MainPageMixage_Loaded;
        }

        public void Dispose()
        {
            foreach(LecteurMusique lecteur in lecteurMusiques)
            {
                lecteur.Dispose();
            }
        }

        private void MainPageMixage_Loaded(object sender, RoutedEventArgs e)
        {
            updatePiste();
        }

        private void ExploLocal_eventMusiqueSlectedWithPiste(object? sender, (Musique, int) e)
        {
            try
            {
                int code = lecteurMusiques[e.Item2].setMusique(e.Item1);
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
        private void eventMusiqueSlected(object? sender, Musique musique)
        {
            foreach (LecteurMusique lecteur in lecteurMusiques)
            {
                try
                {
                    int code = lecteur.setMusique(musique);

                    if (code == 0)
                    {
                        return;
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                

            }
            Console.WriteLine("Toutes les pistes sont prises");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_nbrPiste.SelectedItem is ComboBoxItem selectedItem)
            {
                SettingsManager.Instance.nbrPiste = int.Parse(selectedItem.Content.ToString());
                updatePiste();
            }
        }

        private void updatePiste()
        {
            AjouterLecteursMusique(SettingsManager.Instance.nbrPiste);
            mixage2Pistes?.updatePiste();
        }

        private void AjouterLecteursMusique(int nombreDePistes)
        {
            if (!IsInitialized) return;
            // Supprime les lecteurs de musique excédentaires
            while (lecteurMusiques.Count > nombreDePistes)
            {
                LecteurMusique lecteurASupprimer = lecteurMusiques[lecteurMusiques.Count - 1];
                lecteurASupprimer.Dispose();
                t_player.RowDefinitions.RemoveAt(t_player.RowDefinitions.Count - 1);
                lecteurMusiques.RemoveAt(lecteurMusiques.Count - 1);
            }

            // Ajoute les nouveaux lecteurs de musique nécessaires
            for (int i = lecteurMusiques.Count; i < nombreDePistes; i++)
            {
                RowDefinition rowDefinition = new RowDefinition();
                t_player.RowDefinitions.Add(rowDefinition);

                LecteurMusique lecteurMusique = new LecteurMusique();
                lecteurMusique.setNbrPiste(i + 1);

                lecteurMusique.LecteurMusiqueViewModel.TacheGetBPM += LecteurMusiqueViewModel_TacheGetBPM;
                lecteurMusiques.Add(lecteurMusique);

                // Ajoutez le lecteurMusique à la collection Children du Grid en spécifiant la ligne
                t_player.Children.Add(lecteurMusique);
                Grid.SetRow(lecteurMusique, i);
            }

        }

        private void Tb_mixage_ValueChanged(object? sender, int e)
        {
            if (lecteurMusiques.Count < 2) return;

            float pisteGauche = (e <= mixage2Pistes.tb_mixage.Default) ? 1 : 1 - ((float)(e - mixage2Pistes.tb_mixage.Default) / mixage2Pistes.tb_mixage.Default);

            float pisteDroite = (e >= mixage2Pistes.tb_mixage.Default) ? 1 : ((float)e / mixage2Pistes.tb_mixage.Default);

            lecteurMusiques[mixage2Pistes.nbrPisteGauche].setVolume(pisteGauche);

            lecteurMusiques[mixage2Pistes.nbrPisteDroite].setVolume(pisteDroite);
        }
        private void Mixage2Pistes_eventSetPiste(object? sender, EventArgs e)
        {
            foreach (LecteurMusique lecteur in lecteurMusiques)
            {
                lecteur.setVolume(1);
            }
            Tb_mixage_ValueChanged(mixage2Pistes, mixage2Pistes.tb_mixage.Value);
        }
        private void LecteurMusiqueViewModel_TacheGetBPM(object? sender, int e)
        {
            exploYoutube.updateBPM();
            exploLocal.updateBPM();
            exploSpotify.updateBPM();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ParametresForm parametresForm = new ParametresForm();
            parametresForm.Show();
        }
    }
}
