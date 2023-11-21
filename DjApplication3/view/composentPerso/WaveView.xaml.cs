using DjApplication3.model;
using Newtonsoft.Json.Linq;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.LinkLabel;

namespace DjApplication3.view.composentPerso
{
    /// <summary>
    /// Logique d'interaction pour WaveView.xaml
    /// </summary>
    public partial class WaveView : UserControl
    {
        private List<float> wafeFrome;
        private Musique musique;
        private WaveViewModelView modelView = new WaveViewModelView();
        private float _indicateurPosition;

        private Brush defaultBack = new SolidColorBrush(Color.FromRgb(40, 40, 40)); 
        private Brush warningBack = new SolidColorBrush(Color.FromRgb(250, 128, 114));

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
            cursorTranslateTransform.X = ActualWidth* indicateurPosition;
        }
        public void setMusique(Musique musique)
        {
            this.musique = musique;

            clearMusique();
            wafeFrome.Clear();
            modelView.getWave(musique);
        }

        public void clearMusique()
        {
            indicateurPosition = 0;
            TacheGetWaveHandler(this, new List<float>());
        }
        private void TacheGetWaveHandler(object sender, List<float> wave)
        {
            wafeFrome = wave;
            updateGraphBitmap();
            indicateurPosition = 0;

        }

        private void updateGraphBitmap()
        {
            waveCanvas.Children.Clear();
            if (wafeFrome == null || wafeFrome.Count < 1) return;
            

            int multiplicateur = 10;
            int nbrx = (int)(ActualWidth * multiplicateur);

            float[] tableauReduit = new float[nbrx];
            for (int i = 0; i < nbrx; i++)
            {
                int tauxReduc = wafeFrome.Count / nbrx;
                int nbr = i * tauxReduc;
                tableauReduit[i] = wafeFrome[nbr];
            }

            double yBase = (int) (ActualHeight / 2);
            int x = 0;

            for (int i = 0; i < tableauReduit.Length; i++)
            {
                float nbr = tableauReduit[i];

                Line line = new Line
                {
                    X1 = x,
                    Y1 = yBase,
                    X2 = x,
                    Y2 = yBase + nbr * yBase,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                waveCanvas.Children.Add(line);

                if (i % multiplicateur == 0)
                {
                    x++;
                }
            }
        }

        private void waveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updateGraphBitmap();
        }

        public void changeColorBack(double timeRight)
        {
            if (timeRight < SettingsManager.Instance.timeBeforBlinkSecond && timeRight != 0)
            {
                if (waveCanvas.Background == defaultBack)
                {
                    waveCanvas.Background = warningBack;
                }
                else
                {
                    waveCanvas.Background = defaultBack;
                }
            }
            else if (waveCanvas.Background != defaultBack)
            {
                waveCanvas.Background = defaultBack;
            }
        }
    }
}
