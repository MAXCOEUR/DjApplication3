using DjApplication3.DataSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Windows.UI.WebUI;

namespace DjApplication3.view.activity
{
    /// <summary>
    /// Logique d'interaction pour ConnectedYtMusic.xaml
    /// </summary>
    public partial class ConnectedYtMusic : UserControl
    {
        public event EventHandler Closing;
        private string sessionFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "outilsExtern", "session_cookies.txt");

        public ConnectedYtMusic()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            // Initialise l'environnement WebView2
            await webView.EnsureCoreWebView2Async();
        }

        public void OpenPage()
        {
            // On rend le composant visible par-dessus les autres
            this.Visibility = Visibility.Visible;

            // Optionnel : Recharger la page de login au cas où
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate("https://accounts.google.com/ServiceLogin?continue=https://music.youtube.com/");
            }
        }

        private async void bt_continuer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Récupérer tous les cookies du domaine youtube
                var cookieManager = webView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.youtube.com");

                if (cookies != null && cookies.Count > 0)
                {
                    var cookieData = cookies.Select(c => new {
                        c.Name,
                        c.Value,
                        c.Path,
                        c.Domain
                    }).ToList();

                    string jsonString = JsonSerializer.Serialize(cookieData);
                    File.WriteAllText(sessionFile, jsonString);

                    MessageBox.Show("Connexion réussie ! Votre bibliothèque est maintenant accessible.");
                }
                else
                {
                    MessageBox.Show("Aucun cookie trouvé. Connectez-vous d'abord.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la récupération de la session : " + ex.Message);
            }
            this.Visibility = Visibility.Collapsed;
            Closing?.Invoke(this, EventArgs.Empty);
        }
    }
}
