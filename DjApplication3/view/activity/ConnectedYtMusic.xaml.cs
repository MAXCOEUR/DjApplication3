using DjApplication3.DataSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Logique d'interaction pour ConnectedYtMusic.xaml
    /// </summary>
    public partial class ConnectedYtMusic : UserControl
    {
        Process? process;
        public event EventHandler Closing;
        public ConnectedYtMusic()
        {
            InitializeComponent();
            TextBlock.Text = "Connectez-vous avec votre compte YouTube Music via le navigateur qui se lancera automatiquement. Si ce n’est pas le cas, c’est que c’est déjà fait. Une fois cette étape accomplie, appuyez sur le bouton ‘Continuer’.";
        }
        public async void OpenPage()
        {
            Visibility = Visibility.Visible;
            process = await YtMusicDataSource.Connected();
        }
        private async void ClosePage()
        {
            if (process != null)
            {
                process.StandardInput.WriteLine();
                await Task.Run(() => process.WaitForExit());
                process.Dispose();
            }
            Closing.Invoke(this, EventArgs.Empty);
            Visibility = Visibility.Hidden;
        }
        private void bt_continuer_Click(object sender, RoutedEventArgs e)
        {
            ClosePage();
        }
    }
}
