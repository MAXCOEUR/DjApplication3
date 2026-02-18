using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.view.composentPerso;
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
        ExplorateurYtMusicViewModel explorateurYtMusicViewModel;
        bool statePlayPresScratchLeft = false;
        bool statePlayPresScratchRight = false;

        public event EventHandler eventOptionButton;


        public MainPageMixage()
        {
            InitializeComponent();

            exploLocal.eventMusiqueSlected += eventMusiqueSlected;
            exploLocal.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploYoutube.setViewModel(new ExplorateurYoutubeViewModel());
            explorateurYtMusicViewModel = new ExplorateurYtMusicViewModel();
            exploYtMusic.setViewModel(explorateurYtMusicViewModel);

            exploYtMusic.eventMusiqueSlected += eventMusiqueSlected;
            exploYtMusic.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploYoutube.eventMusiqueSlected += eventMusiqueSlected;
            exploYoutube.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            mixage2Pistes.tb_mixage.ValueChanged += Tb_mixage_ValueChanged;
            mixage2Pistes.eventSetPiste += Mixage2Pistes_eventSetPiste;

            tb_volume_headPhone.ValueChanged += volume_headPhone_ValueChanged;

            Loaded += MainPageMixage_Loaded;

            
            startHercule();
        }

        private void volume_headPhone_ValueChanged(object? sender, int e)
        {
            foreach(LecteurMusique lecteurMusique in lecteurMusiques)
            {
                lecteurMusique.setVolumeHeadPhone(e);
            }
        }

        void startHercule()
        {
            HerculesDJ.Instance?.Dispose();
            HerculesDJ.Instance.eventPlayPauseLeft += Hercules_eventPlayPauseLeft;
            HerculesDJ.Instance.eventPlayPauseRight += Hercules_eventPlayPauseRight;
            HerculesDJ.Instance.eventCasqueLeft += Hercules_eventCasqueLeft;
            HerculesDJ.Instance.eventCasqueRight += Hercules_eventCasqueRight;
            HerculesDJ.Instance.eventMixe += Hercules_eventMixe;
            HerculesDJ.Instance.eventVolumeLeft += Hercules_eventVolumeLeft;
            HerculesDJ.Instance.eventVolumeRight += Hercules_eventVolumeRight;

            HerculesDJ.Instance.eventPisteLeft += Hercules_eventPisteLeft;
            HerculesDJ.Instance.eventPisteRight += Hercules_eventPisteRight;

            HerculesDJ.Instance.eventScratchLeft += Hercules_eventScratchLeft;
            HerculesDJ.Instance.eventScratchRight += Hercules_eventScratchRight;
            HerculesDJ.Instance.eventScratchLeftPress += Hercules_eventScratchLeftPress;
            HerculesDJ.Instance.eventScratchRightPress += Hercules_eventScratchRightPress;

            HerculesDJ.Instance.eventVolumeDownHeadPhone += Hercules_eventVolumeDownHeadPhone;
            HerculesDJ.Instance.eventVolumeUpHeadPhone += Hercules_eventVolumeUpHeadPhone;

            HerculesDJ.Instance.eventButtonDown += Hercules_eventButtonDown;
            HerculesDJ.Instance.eventButtonUp += Hercules_eventButtonUp;
            HerculesDJ.Instance.eventButtonLeft += Hercules_eventButtonLeft;
            HerculesDJ.Instance.eventButtonRight += Hercules_eventButtonRight;

            HerculesDJ.Instance.eventButtonLoadLeft += Hercules_eventButtonLoadLeft;
            HerculesDJ.Instance.eventButtonLoadRight += Hercules_eventButtonLoadRight;

            HerculesDJ.Instance.start();
        }

        private void Hercules_eventButtonLoadRight(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.EnterSelected(1);
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.EnterSelected(1);
                }
                else if (selectedIndex == 2)
                {
                    exploYoutube.EnterSelected(1);
                }
            });
            
        }

        private void Hercules_eventButtonLoadLeft(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.EnterSelected(0);
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.EnterSelected(0);
                }
                else if (selectedIndex == 2)
                {
                    exploYoutube.EnterSelected(0);
                }
            });
            
        }

        private void Hercules_eventButtonRight(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.keyRight();
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.keyRight();
                }
            });
            
        }

        private void Hercules_eventButtonLeft(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.keyLeft();
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.keyLeft();
                }
            });
            
        }

        private void Hercules_eventButtonUp(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.keyUp();
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.keyUp();
                }
                else if (selectedIndex == 2)
                {
                    exploYoutube.keyUp();
                }
            });
            
        }

        private void Hercules_eventButtonDown(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int selectedIndex = tab_navigation.SelectedIndex;
                if (selectedIndex == 0)
                {
                    exploLocal.keyDown();
                }
                else if (selectedIndex == 1)
                {
                    exploYtMusic.keyDown();
                }
                else if (selectedIndex == 2)
                {
                    exploYoutube.keyDown();
                }
            });
            
        }

        private void Hercules_eventVolumeUpHeadPhone(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                tb_volume_headPhone.Value += 5;
            });
        }

        private void Hercules_eventVolumeDownHeadPhone(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                tb_volume_headPhone.Value -= 5;
            });
        }

        private void Hercules_eventScratchRightPress(object? sender, bool e)
        {
            if (e)
            {
                Dispatcher.Invoke(() =>
                {
                    statePlayPresScratchRight = lecteurMusiques[mixage2Pistes.nbrPisteDroite].isPlay();
                    lecteurMusiques[mixage2Pistes.nbrPisteDroite].pause();
                });

            }
            else
            {
                if (statePlayPresScratchRight)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lecteurMusiques[mixage2Pistes.nbrPisteDroite].play();
                    });
                }

            }

        }

        private void Hercules_eventScratchLeftPress(object? sender, bool e)
        {
            if (e)
            {
                Dispatcher.Invoke(() =>
                {
                    statePlayPresScratchLeft = lecteurMusiques[mixage2Pistes.nbrPisteGauche].isPlay();
                    lecteurMusiques[mixage2Pistes.nbrPisteGauche].pause();
                });
            }
            else
            {
                if (statePlayPresScratchLeft)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lecteurMusiques[mixage2Pistes.nbrPisteGauche].play();
                    });
                }
            }
        }

        private void Hercules_eventScratchRight(object? sender, int e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteDroite].changePosition(e != 127);
            });
        }

        private void Hercules_eventScratchLeft(object? sender, int e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteGauche].changePosition(e!=127);
            });
        }

        private void Hercules_eventPisteRight(object? sender, int e)
        {
            if(e <= SettingsManager.Instance.nbrPiste)
            {
                Dispatcher.Invoke(() =>
                {
                    mixage2Pistes.cb_pisteDroite.SelectedIndex = e - 1;
                });
            }
           
        }

        private void Hercules_eventPisteLeft(object? sender, int e)
        {
            if (e <= SettingsManager.Instance.nbrPiste)
            {
                Dispatcher.Invoke(() =>
                {
                    mixage2Pistes.cb_pisteGauche.SelectedIndex = e - 1;
                });
            }
                
        }

        private void Hercules_eventVolumeRight(object? sender, float e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteDroite].setTb_volume(e);
            });
        }

        private void Hercules_eventVolumeLeft(object? sender, float e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteGauche].setTb_volume(e);
            });
        }

        private void Hercules_eventMixe(object? sender, float e)
        {
            Dispatcher.Invoke(() =>
            {
                mixage2Pistes.tb_mixage.Value = (int)(e * 100);
            });
        }

        private void Hercules_eventCasqueRight(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteDroite].btHeadphone();
            });
        }

        private void Hercules_eventCasqueLeft(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteGauche].btHeadphone();
            });
        }

        private void Hercules_eventPlayPauseRight(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteDroite].btPlayPause();
            });
        }

        private void Hercules_eventPlayPauseLeft(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lecteurMusiques[mixage2Pistes.nbrPisteGauche].btPlayPause();
            });
        }

        public void Dispose()
        {
            foreach(LecteurMusique lecteur in lecteurMusiques)
            {
                lecteur.Dispose();
            }

            HerculesDJ.Instance.Dispose();
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
                new ToastMessage(ex.Message, ToastMessage.ToastType.Error,ex).Show();
            }
            
        }
        private void eventMusiqueSlected(object? sender, Musique musique)
        {
            foreach (LecteurMusique lecteur in lecteurMusiques)
            {
                try
                {
                    int code = lecteur.setMusique(musique);

                    if (code == 0 || code == 2)
                    {
                        return;
                    }

                }
                catch (Exception ex)
                {
                    new ToastMessage(ex.Message, ToastMessage.ToastType.Error,ex).Show();
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
                t_player.Children.Remove(lecteurASupprimer);
                t_player.RowDefinitions.RemoveAt(t_player.RowDefinitions.Count-1);
                lecteurMusiques.RemoveAt(lecteurMusiques.Count-1);
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

            lecteurMusiques[mixage2Pistes.nbrPisteGauche].setMasterVolume(pisteGauche);

            lecteurMusiques[mixage2Pistes.nbrPisteDroite].setMasterVolume(pisteDroite);
        }
        private void Mixage2Pistes_eventSetPiste(object? sender, EventArgs e)
        {
            foreach (LecteurMusique lecteur in lecteurMusiques)
            {
                lecteur.setMasterVolume(1);
            }
            Tb_mixage_ValueChanged(mixage2Pistes, mixage2Pistes.tb_mixage.Value);
        }
        private void LecteurMusiqueViewModel_TacheGetBPM(object? sender, int e)
        {
            exploYoutube.updateBPM();
            exploLocal.updateBPM();
            exploYtMusic.updateBPM();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            eventOptionButton?.Invoke(this, EventArgs.Empty);
        }

        public void ParametresForm_Closing(object? sender, EventArgs e)
        {
            startHercule();
            explorateurYtMusicViewModel.getPlayListe();
        }
    }
}
