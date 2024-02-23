using DjApplication3.model;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DjApplication3.view.composentPerso
{
    /// <summary>
    /// Logique d'interaction pour WaveView.xaml
    /// </summary>
    public partial class WaveView : UserControl
    {
        private sbyte[] wafeFrome;
        private Musique musique;
        private WaveViewModelView modelView = new WaveViewModelView();
        private float _indicateurPosition;

        private System.Windows.Media.Brush defaultBack = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40)); 
        private System.Windows.Media.Brush warningBack = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 128, 114));

        public float indicateurPosition
        {
            get { return _indicateurPosition; }
            set
            {
                _indicateurPosition = value;
                indicateurPositionChanged();
            }
        }
        public WaveView()
        {
            InitializeComponent();
            waveCanvas.Background = defaultBack;
            modelView.TacheGetWave += TacheGetWaveHandler;
        }

        private void indicateurPositionChanged()
        {

            // Mettre à jour la position du rectangle du curseur
            cursorTranslateTransform.X = ActualWidth * indicateurPosition;
        }
        public void setMusique(Musique musique)
        {
            this.musique = musique;

            clearMusique();
            modelView.getWave(musique);
        }

        public void clearMusique()
        {
            indicateurPosition = 0;
            TacheGetWaveHandler(this, new sbyte[0]);
        }
        private void TacheGetWaveHandler(object sender, sbyte[] wave)
        {
            wafeFrome = wave;
            updateGraph();
        }

        private async void updateGraph()
        {
            if (wafeFrome == null) return;

            indicateurPosition = indicateurPosition;

            int multiplicateur = 10;
            int nbrx = (int)(ActualWidth * multiplicateur);

            double yBase = (int)(ActualHeight / 2);
            int x = 0;

            await Task.Run(() =>
            {
                sbyte[] tableauReduit = new sbyte[0];
                if(wafeFrome.Length > 0)
                {
                    tableauReduit = new sbyte[nbrx];
                    for (int i = 0; i < nbrx; i++)
                    {
                        int tauxReduc = wafeFrome.Length / nbrx;
                        int nbr = i * tauxReduc;
                        tableauReduit[i] = wafeFrome[nbr];
                    }
                }
                
                if(ActualWidth<2 || ActualHeight < 2)
                {
                    return;
                }
                Bitmap bitmap = new Bitmap((int)ActualWidth, (int)ActualHeight);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(175,255,255,255));

                    for (int i = 0; i < tableauReduit.Length; i++)
                    {
                        sbyte nbr = tableauReduit[i];
                        g.DrawLine(pen, x, (float)yBase, x, (float)(yBase + (nbr / 100.0f) * yBase));

                        if (i % multiplicateur == 0)
                        {
                            x++;
                        }
                    }
                }

                

                this.Dispatcher.Invoke(() =>
                {
                    waveCanvas.Children.Clear();

                    // Convert Bitmap to BitmapImage
                    BitmapImage bitmapImage = BitmapToBitmapImage(bitmap);

                    // Create ImageBrush
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = bitmapImage;

                    // Apply ImageBrush to Canvas
                    waveCanvas.Background = brush;
                });
            });
        }





        private void waveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updateGraph();
        }

        public void changeColorBack(double timeRight)
        {
            if (timeRight < SettingsManager.Instance.timeBeforBlinkSecond && timeRight != 0)
            {
                if (background.Background == defaultBack)
                {
                    background.Background = warningBack;
                }
                else
                {
                    background.Background = defaultBack;
                }
            }
            else if (background.Background != defaultBack)
            {
                background.Background = defaultBack;
            }
        }
        public void setEndColorBack()
        {
            background.Background = warningBack;
        }

        // Method to convert Bitmap to BitmapImage
        public BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
