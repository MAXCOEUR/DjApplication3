using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.outils;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace DjApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //new YtMusicDataSource().search("vald vlad");
            new FFmpegGestion();
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainPageMixage.Dispose();
            cleanTmp();

        }

        void cleanTmp()
        {
            try
            {
                string[] allFiles = Directory.GetFiles("musique/tmp");

                // Filtrer les fichiers avec l'extension .mp3
                List<string> mp3Files = allFiles
                    .Where(file => System.IO.Path.GetExtension(file).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (string mp3File in mp3Files)
                {
                    File.Delete(mp3File);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
