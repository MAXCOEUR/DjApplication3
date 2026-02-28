using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.view.fragment;
using DjApplication3.view.windows;
using DjApplication3.View.userControlDJ;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DjApplication3.view.page
{

    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
    /// <summary>
    /// Logique d'interaction pour LecteurMusique.xaml
    /// </summary>
    public partial class LecteurMusique : UserControl
    {
        public LecteurMusiqueViewModel LecteurMusiqueViewModel = new LecteurMusiqueViewModel();

        Musique musique = null;
        WasapiOut audioPlayer = new WasapiOut();
        IWaveSource? fichierAudio = null;
        private System.Timers.Timer timer = null;

        BitmapImage imgPause = new BitmapImage(new Uri("/DjApplication3;component/Resources/pause.png", UriKind.Relative));
        BitmapImage imgPlay = new BitmapImage(new Uri("/DjApplication3;component/Resources/play.png", UriKind.Relative));

        bool isHeadPhone = false;

        float volumeMaster = 1;
        float volumeHeadPhone = 100.0F;

        Musique? nextMusiqueDl = null;

        int nbrPist = 0;

        private ExplorateurInternetViewModel viewModel;
        static public string rootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "musique", "tmp");

        public LecteurMusique()
        {
            InitializeComponent();
            InitializeTimer();
            tb_volume.ValueChanged += Tb_volume_ValueChanged;
            LecteurMusiqueViewModel.TacheGetBPM += LecteurMusiqueViewModel_TacheGetBPM;
        }

        public void setViewModel(ExplorateurInternetViewModel model)
        {
            viewModel = model;
            viewModel.TacheDownload += ViewModel_TacheDownload;
        }

        private void ViewModel_TacheDownload(object? sender, (Musique?, int?) e)
        {
            var musiqueTelechargee = e.Item1;

            if (musiqueTelechargee != null)
            {
                musiqueTelechargee.musiquesInPlayliste = musique?.musiquesInPlayliste;

                nextMusiqueDl = musiqueTelechargee;

                Console.WriteLine($"Prêt pour l'enchaînement : {musiqueTelechargee.title}");
            }
            Console.WriteLine("TacheDownload received in LecteurMusique");
        }

        private void LecteurMusiqueViewModel_TacheGetBPM(object? sender, int bpm)
        {
            tv_bpm.Content = $"{bpm} BPM";
        }

        private void Tb_volume_ValueChanged(object? sender, int e)
        {
            updateVolume();
        }

        private void btn_AutoNext_Click(object sender, RoutedEventArgs e)
        {
            // Logique optionnelle au moment du clic
            bool isAuto = btn_AutoNext.IsChecked ?? false;
            Console.WriteLine($"Mode automatique : {isAuto}");
            downloadNextMusique();
        }

        private void InitializeTimer()
        {
            timer = new System.Timers.Timer();
            timer.Interval = 500;
            timer.Elapsed += timer_Tick;
        }

        private void timer_Tick(object sender, ElapsedEventArgs e)
        {
            if (audioPlayer != null)
            {
                Dispatcher.Invoke(() =>
                {
                    updateDuration();
                });
            }
        }

        public void play()
        {
            if (audioPlayer.DebuggingId == -1) return;

            Console.WriteLine("play");
            audioPlayer.Play();
            timer.Start();

            bt_playPause.Background = System.Windows.Media.Brushes.Green;
            System.Windows.Controls.Image img_PlayPause = (System.Windows.Controls.Image)bt_playPause.Template.FindName("img_PlayPause", bt_playPause);
            if (img_PlayPause != null)
            {
                img_PlayPause.Source = imgPause;
            }

            if (nbrPist == 1)
            {
                HerculesDJ.Instance.playLeft(true);
            }
            if (nbrPist == 2)
            {
                HerculesDJ.Instance.playRight(true);
            }

        }


        public void pause()
        {
            if (audioPlayer.DebuggingId == -1) return;
            Console.WriteLine("pause");
            audioPlayer.Pause();
            timer.Stop();

            bt_playPause.Background = System.Windows.Media.Brushes.Red;
            System.Windows.Controls.Image img_PlayPause = (System.Windows.Controls.Image)bt_playPause.Template.FindName("img_PlayPause", bt_playPause);
            if (img_PlayPause != null)
            {
                img_PlayPause.Source = imgPlay;
            }

            if (nbrPist == 1)
            {
                HerculesDJ.Instance.playLeft(false);
            }
            if (nbrPist == 2)
            {
                HerculesDJ.Instance.playRight(false);
            }
        }
        public void stop()
        {
            if (audioPlayer.DebuggingId == -1) return;
            Console.WriteLine("stop");

            pause();
            audioPlayer.Dispose();
            audioPlayer = new WasapiOut();
            timer.Stop();

            musique = null;
            fichierAudio = null;

            waveForme.clearMusique();
            tv_titleAuthor.Content = "aucune musique";
        }
        public int setMusique(Musique musique)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing) return 1;
            if (musique==null) return 2;
            try
            {
                
                Console.WriteLine("setMusique start");

                

                // Exécutez ces opérations dans un thread séparé
                fichierAudio = CodecFactory.Instance.GetCodec(musique.url);
                if (audioPlayer.WaveSource != null)
                {
                    audioPlayer.WaveSource.Position = 0;
                }

                this.musique = musique;

                initaudioPlayer();
                updateOutAudio();
                updateDuration();

                tv_titleAuthor.Content = $"{musique.title} ({musique.author})";
                tv_bpm.Content = $"000 BPM";
                LecteurMusiqueViewModel.getBpm(musique);
                waveForme.setMusique(musique);

                Console.WriteLine("setMusique end");

                if (btn_AutoNext.IsChecked ?? false)
                {
                    play();
                }


                downloadNextMusique();


                return 0;
            }catch(Exception e)
            {
                new ToastMessage(e.Message, ToastMessage.ToastType.Error, e).Show();
                return 2;
            }

            
        }

        private Musique? getMusiqueNext()
        {
            if(musique?.musiquesInPlayliste == null)
            {
                return null;
            }
            int currentIndex = musique.musiquesInPlayliste.IndexOf(musique);

            // On vérifie s'il y a un élément après
            if (currentIndex != -1 && currentIndex < musique.musiquesInPlayliste.Count - 1)
            {
                return musique.musiquesInPlayliste[currentIndex + 1];
            }
            return null;
        }
        private void downloadNextMusique()
        {
            if (musique?.musiquesInPlayliste != null && (btn_AutoNext.IsChecked ?? false))
            {
                Console.WriteLine("start download next musique");
                Musique nextMusique = getMusiqueNext();                

                if (nextMusique != null)
                {
                    // On lance le téléchargement/lecture de la SUIVANTE
                    viewModel.DownloadMusique(nextMusique);
                }
                else
                {
                    // Soit on est à la fin, soit la liste n'est pas chargée
                    Console.WriteLine("Pas de musique suivante trouvée.");
                }
            }
        }
        private void updateOutAudio()
        {
            bool tmpIsPlay = audioPlayer.PlaybackState == PlaybackState.Playing;

            // Arrêtez la lecture audio actuelle si elle est en cours
            if (tmpIsPlay)
            {
                pause();
            }

            // Modifiez le numéro du périphérique audio (ajustez en conséquence)
            audioPlayer.Device = isHeadPhone ? SettingsManager.Instance.dispositifsAudio[SettingsManager.Instance.nbrHeadPhone] : SettingsManager.Instance.dispositifsAudio[SettingsManager.Instance.nbrOut];
            if (audioPlayer.DebuggingId == -1) return;
            bt_headphone.Background = isHeadPhone ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

            if (isHeadPhone)
            {
                if (nbrPist == 1)
                {
                    HerculesDJ.Instance.PreviewLeft(true);
                }
                if (nbrPist == 2)
                {
                    HerculesDJ.Instance.PreviewRight(true);
                }
            }
            else
            {
                if (nbrPist == 1)
                {
                    HerculesDJ.Instance.PreviewLeft(false);
                }
                if (nbrPist == 2)
                {
                    HerculesDJ.Instance.PreviewRight(false);
                }
            }
            

            // Réinitialisez le lecteur audio
            initaudioPlayer();

            // Redémarrez la lecture si elle était en cours avant le changement de périphérique
            if (tmpIsPlay)
            {
                play();
            }
        }


        public void initaudioPlayer()
        {
            audioPlayer.Stop();

            float positionActuelle=0F;
            try
            {
                if (audioPlayer.WaveSource != null)
                {
                    positionActuelle = (float)audioPlayer.WaveSource.Position / audioPlayer.WaveSource.Length;
                    audioPlayer.WaveSource.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            audioPlayer.Initialize(fichierAudio);

            try
            {
                if (audioPlayer.WaveSource != null)
                {
                    audioPlayer.WaveSource.Position = (int)(positionActuelle * audioPlayer.WaveSource.Length);
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            

            updateVolume();
        }
        public void updateDuration()
        {
            TimeSpan dureeTotale = audioPlayer.WaveSource.GetLength();
            TimeSpan positionActuelle = audioPlayer.WaveSource.GetPosition();
            TimeSpan durationRestant = dureeTotale - positionActuelle;

            waveForme.indicateurPosition = ((float)(positionActuelle / dureeTotale));

            string dureeTotaleFormattee = $"{(int)dureeTotale.TotalHours:D2}h {dureeTotale.Minutes:D2}m {dureeTotale.Seconds:D2}s";

            string positionActuelleFormattee = $"{(int)positionActuelle.TotalHours:D2}h {positionActuelle.Minutes:D2}m {positionActuelle.Seconds:D2}s";

            string durationRestantFormattee = $"{(int)durationRestant.TotalHours:D2}h {durationRestant.Minutes:D2}m {durationRestant.Seconds:D2}s";

            tv_durationCurrent.Content = $"{positionActuelleFormattee}";
            tv_durationTotal.Content = $"{dureeTotaleFormattee}";
            tv_durationRest.Content = $"{durationRestantFormattee}";
            waveForme.changeColorBack((durationRestant).TotalSeconds);

            if (durationRestant.TotalMilliseconds <=100)
            {
                waveForme.setEndColorBack();
                pause();
                if(musique?.musiquesInPlayliste != null && (btn_AutoNext.IsChecked ?? false))
                {
                    setMusique(nextMusiqueDl);
                }
            }

        }
        private void setPosition(double positionPourcentage)
        {
            if (audioPlayer.DebuggingId == -1) return;
            audioPlayer.WaveSource.Position = (long)(audioPlayer.WaveSource.Length * positionPourcentage);
            updateDuration();
        }

        public void changePosition(bool isForward)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing) return ;
            if (audioPlayer.DebuggingId == -1) return;
            float currentPosition = (float) audioPlayer.WaveSource.Position / audioPlayer.WaveSource.Length;
            if (isForward)
            {
                currentPosition += 0.001F;
            }
            else
            {
                currentPosition -= 0.001F;
            }
            audioPlayer.WaveSource.Position = (long)(audioPlayer.WaveSource.Length * currentPosition);
            updateDuration();
        }
        public void setNbrPiste(int nbr)
        {
            nbrPist = nbr;
            tv_nbrPiste.Content = $"Piste {nbrPist}";
        }
        public void setMasterVolume(float volume)
        {
            volumeMaster = volume;
            updateVolume();
        }
        public void setVolumeHeadPhone(float volume)
        {
            volumeHeadPhone = volume;
            updateVolume();
        }
        private void updateVolume()
        {
            if (audioPlayer.DebuggingId == -1) return;
            audioPlayer.Volume = isHeadPhone ? volumeHeadPhone/ 100.0F : tb_volume.Value / 100.0F * volumeMaster;
        }

        public void setTb_volume(float volume)
        {
            tb_volume.Value = (int)(volume * 100);
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            // Libérer les autres ressources managées
            if (audioPlayer != null)
            {
                audioPlayer.Dispose();
            }

            if (fichierAudio != null)
            {
                fichierAudio.Dispose();
            }

        }

        private void waveForme_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            double pourcentage = e.GetPosition(waveForme).X / ActualWidth;
            setPosition(pourcentage);
        }

        private void bt_playPause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            btPlayPause();
        }

        public void btPlayPause()
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                pause();
            }
            else
            {
                play();
            }
        }

        private void bt_stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            stop();
        }

        private void bt_headphone_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            btHeadphone();
        }

        public void btHeadphone()
        {
            if (audioPlayer.DebuggingId == -1) return;

            isHeadPhone = !isHeadPhone;
            updateOutAudio();
        }

        public bool isPlay()
        {
            if (audioPlayer.DebuggingId == -1) return false;
            return audioPlayer.PlaybackState == PlaybackState.Playing;
        }

        private void bt_random_Click(object sender, RoutedEventArgs e)
        {
            if (musique?.musiquesInPlayliste == null) return;

            var liste = musique.musiquesInPlayliste;

            liste.Shuffle();

            if (musique != null)
            {
                liste.Remove(musique);
            }

            if (nextMusiqueDl != null)
            {
                liste.Remove(nextMusiqueDl);
            }
            
            if (nextMusiqueDl != null)
            {
                liste.Insert(0, nextMusiqueDl);
            }

            if (musique != null)
            {
                liste.Insert(0, musique);
            }

            // 5. Facultatif : On informe l'utilisateur
            new ToastMessage("Playlist mélangée (Lecture actuelle préservée)", ToastMessage.ToastType.Info).Show();

            Console.WriteLine($"Ordre rétabli : 1. {musique?.title} | 2. {nextMusiqueDl?.title}");
        }

        private void bt_random_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (musique?.musiquesInPlayliste == null || musique.musiquesInPlayliste.Count == 0)
            {
                bt_random.ToolTip = "Aucune playlist chargée";
                return;
            }

            int currentIndex = musique.musiquesInPlayliste.IndexOf(musique);

            // On récupère par exemple les 10 prochaines musiques
            var suivantes = musique.musiquesInPlayliste
                .Skip(currentIndex + 1) // On saute celles déjà passées
                .Take(10)               // On en prend 10 max pour ne pas avoir une bulle géante
                .ToList();

            if (suivantes.Count > 0)
            {
                string listeTexte = "Prochainement :\n";
                foreach (var m in suivantes)
                {
                    listeTexte += $"• {m.title} - {m.author}\n";
                }

                if (musique.musiquesInPlayliste.Count > currentIndex + 11)
                    listeTexte += "... et plus encore";

                bt_random.ToolTip = listeTexte;
            }
            else
            {
                bt_random.ToolTip = "Fin de la playlist";
            }
        }
    }
}
