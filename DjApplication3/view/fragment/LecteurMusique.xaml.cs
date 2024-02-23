using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.View.userControlDJ;
using System;
using System.Drawing;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DjApplication3.view.page
{
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

        HerculesDJ hercules;

        public LecteurMusique()
        {
            InitializeComponent();
            InitializeTimer();
            hercules = HerculesDJ.Instance;
            tb_volume.ValueChanged += Tb_volume_ValueChanged;
            LecteurMusiqueViewModel.TacheGetBPM += LecteurMusiqueViewModel_TacheGetBPM;
        }

        private void LecteurMusiqueViewModel_TacheGetBPM(object? sender, int bpm)
        {
            tv_bpm.Content = $"{bpm} BPM";
        }

        private void Tb_volume_ValueChanged(object? sender, int e)
        {
            updateVolume();
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
                    audioPlayer.WaveSource.Dispose();
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

                return 0;
            }catch(Exception e)
            {
                return 2;
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
            tv_nbrPiste.Content = $"Piste {nbr}";
        }
        public void setMasterVolume(float volume)
        {
            volumeMaster = volume;
            updateVolume();
        }
        private void updateVolume()
        {
            if (audioPlayer.DebuggingId == -1) return;
            audioPlayer.Volume = isHeadPhone ? tb_volume.Value / 100.0F : tb_volume.Value / 100.0F * volumeMaster;
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
    }
}
