using DjApplication3.DataSource;
using DjApplication3.model;
using DjApplication3.outils;
using DjApplication3.repository;
using DjApplication3.view.activity;
using DjApplication3.view.fragment;
using DjApplication3.view.windows;
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
        private MainPageMixage mainPageMixage = new MainPageMixage();
        public MainWindow()
        {
            //test treeViewExample = new test();
            //treeViewExample.ShowDialog();
            new FFmpegGestion();
            InitializeComponent();
            MainContent.Content = mainPageMixage;
            mainPageMixage.eventOptionButton += eventOptionButton;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainPageMixage.Dispose();
            cleanTmp();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            
        }

        private void eventOptionButton(object? sender, EventArgs e)
        {
            ParametresForm parametresForm = new ParametresForm();
            parametresForm.Closing += ParametresForm_Closing;
            MainContent.Content = parametresForm;
        }

        private void ParametresForm_Closing(object? sender, EventArgs e)
        {
            MainContent.Content = mainPageMixage;
            mainPageMixage.ParametresForm_Closing(sender, e);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            Title = SettingsManager.Instance.APP_NAME;
        }
    }
}
