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

namespace DjApplication3.view.composentPerso
{
    /// <summary>
    /// Logique d'interaction pour TrackBarPerso.xaml
    /// </summary>
    public partial class TrackBarPerso : UserControl
    {

        private int _default = 100;
        private int _value = 0;
        private int _minimum = 0;
        private int _maximum = 100;

        private int targetValue;
        private System.Windows.Forms.Timer animationTimer;

        private bool isLeftDown = false;

        public event EventHandler<int> ValueChanged;

        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(_minimum, Math.Min(_maximum, value));
                OnValueChanged(_value);
            }
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                Value = Math.Max(_minimum, Value);
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                Value = Math.Min(_maximum, Value);
            }
        }
        public int Default
        {
            get { return _default; }
            set
            {
                _default = value;
                Value = _default;
            }
        }
        public TrackBarPerso()
        {
            InitializeComponent();
            Loaded += TrackBarPerso_Loaded;
        }
        private void TrackBarPerso_Loaded(object sender, RoutedEventArgs e)
        {
            Value = Default;
        }
        protected virtual void OnValueChanged(int value)
        {
            ValueChanged?.Invoke(this, value);

            // Calculer la nouvelle position en pixels en fonction de la valeur
            double pos = ((double)(value - Minimum) / (Maximum - Minimum)) * ActualWidth - cursorRectangle.ActualWidth/2;

            // Mettre à jour la position du rectangle du curseur
            Dispatcher.Invoke(() =>
            {
                translateTransform.X = pos;
            });
            
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton==MouseButton.Left)
            {
                double trackBarWidth = ActualWidth;

                // La plage totale de valeurs du TrackBar (Maximum - Minimum)
                int valueRange = Maximum - Minimum;

                // Calculer la nouvelle valeur en fonction des coordonnées de la souris
                int newPosition = (int)(e.GetPosition(this).X / trackBarWidth * valueRange);

                targetValue = Math.Max(Minimum, Math.Min(Maximum, newPosition));
                StartAnimation();
            }
            if (e.ChangedButton == MouseButton.Right)
            {
                targetValue = Default;
                StartAnimation();
            }
            
        }

        private void StartAnimation()
        {
            if (animationTimer != null)
            {
                animationTimer.Stop();
                animationTimer.Dispose();
                animationTimer = null;
            }
            // Démarrer le timer pour effectuer l'animation
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 10; // Ajustez l'intervalle selon vos besoins
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            const int stepSize = 1; // Ajustez la taille de l'incrément selon vos besoins

            if (Value < targetValue)
            {
                Value = Math.Min(Value + stepSize, targetValue);
            }
            else if (Value > targetValue)
            {
                Value = Math.Max(Value - stepSize, targetValue);
            }

            // Arrêtez le timer lorsque la valeur cible est atteinte
            if (Value == targetValue)
            {
                animationTimer.Stop();
                animationTimer.Dispose();
            }
        }

        private void cursorRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isLeftDown)
            {
                double trackBarWidth = ActualWidth;

                // La plage totale de valeurs du TrackBar (Maximum - Minimum)
                int valueRange = Maximum - Minimum;

                // Calculer la nouvelle valeur en fonction des coordonnées de la souris
                int newPosition = (int)(e.GetPosition(this).X / trackBarWidth * valueRange);

                Value = Math.Max(Minimum, Math.Min(Maximum, newPosition));
            }
        }

        private void cursorRectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isLeftDown = false;
            }
        }

        private void cursorRectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton==MouseButton.Left)
            {
                isLeftDown = true;
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Value = Value;
        }

        private void cursorRectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            isLeftDown = false;
        }
    }
}
