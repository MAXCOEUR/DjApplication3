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
using System.Windows.Threading;

namespace DjApplication3.view.windows
{
    public partial class ToastMessage : Window
    {
        public enum ToastType { Info, Error }

        public ToastMessage(string message, ToastType type = ToastType.Info, int durationMs = 5000)
        {
            InitializeComponent();
            txtMessage.Text = message;

            // Définition du style selon le type
            switch (type)
            {
                case ToastType.Info:
                    border.Background = new SolidColorBrush(Color.FromRgb(30, 144, 255)); // Bleu
                    txtIcon.Text = "ℹ️"; // Emoji information
                    break;
                case ToastType.Error:
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 69, 58)); // Rouge
                    txtIcon.Text = "❌"; // Emoji erreur
                    break;
            }


            // Position en haut à droite de l'écran
            var screen = System.Windows.SystemParameters.WorkArea;
            this.Left = screen.Right - 400;
            this.Top = screen.Top + 20;

            // Timer pour fermer automatiquement
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(durationMs);
            timer.Tick += (s, e) => { timer.Stop(); this.Close(); };
            timer.Start();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close(); // Ferme immédiatement si on clique dessus
        }
    }
}
