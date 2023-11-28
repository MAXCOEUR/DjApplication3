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
using System.Windows.Shapes;

namespace DjApplication3.view.windows
{
    /// <summary>
    /// Logique d'interaction pour ConnectedYtMusic.xaml
    /// </summary>
    public partial class ConnectedYtMusic : Window
    {
        Process? process;
        public ConnectedYtMusic()
        {
            InitializeComponent();
            connected();
            tv_label.Content = "Connectez-vous avec votre compte YouTube Music via le navigateur qui se lancera automatiquement. Si ce n’est pas le cas, c’est que c’est déjà fait. Une fois cette étape accomplie, appuyez sur le bouton ‘Continuer’.";
        }

        private async void connected()
        {
            process = await YtMusicDataSource.Connected();
        }
        private async void CloseForm()
        {
            if (process != null)
            {
                process.StandardInput.WriteLine();
                await Task.Run(() => process.WaitForExit());
                process.Dispose();
            }
            Close();
        }
        private void bt_continuer_Click(object sender, RoutedEventArgs e)
        {
            CloseForm();
        }
    }
}
