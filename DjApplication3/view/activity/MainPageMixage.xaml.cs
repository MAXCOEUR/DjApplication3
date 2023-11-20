using DjApplication3.model;
using DjApplication3.view.page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DjApplication3.view.activity
{
    /// <summary>
    /// Logique d'interaction pour MainPageMixage.xaml
    /// </summary>
    public partial class MainPageMixage : UserControl
    {

        List<LecteurMusique> lecteurMusiques = new List<LecteurMusique>();
        public MainPageMixage()
        {
            InitializeComponent();

            exploLocal.eventMusiqueSlected += eventMusiqueSlected;
            exploLocal.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploYoutube.eventMusiqueSlected += eventMusiqueSlected;
            exploYoutube.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;

            exploSpotify.eventMusiqueSlected += eventMusiqueSlected;
            exploSpotify.eventMusiqueSlectedWithPiste += ExploLocal_eventMusiqueSlectedWithPiste;
        }

        private void ExploLocal_eventMusiqueSlectedWithPiste(object? sender, (Musique, int) e)
        {
            lecteurMusiques[e.Item2].setMusique(e.Item1);
        }
        private void eventMusiqueSlected(object? sender, Musique musique)
        {
            foreach (LecteurMusique lecteur in lecteurMusiques)
            {
                if (lecteur.setMusique(musique) == 0)
                {
                    return;
                }

            }
            Console.WriteLine("Toutes les pistes sont prises");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_nbrPiste.SelectedItem is ComboBoxItem selectedItem)
            {
                SettingsManager.Instance.nbrPiste = int.Parse(selectedItem.Content.ToString());
                updatePiste();
            }
        }

        private void updatePiste()
        {
            AjouterLecteursMusique(SettingsManager.Instance.nbrPiste);
            //mixage2Pistes.updatePiste();
        }

        private void AjouterLecteursMusique(int nombreDePistes)
        {
            // Supprime les lecteurs de musique excédentaires
            //while (lecteurMusiques.Count > nombreDePistes)
            //{
            //    LecteurMusique lecteurASupprimer = lecteurMusiques[lecteurMusiques.Count - 1];
            //    lecteurASupprimer.Dispose();
            //    //table_lecteur.RowCount--;
            //    //table_lecteur.Controls.Remove(lecteurASupprimer);
            //    lecteurMusiques.RemoveAt(lecteurMusiques.Count - 1);
            //}

            // Ajoute les nouveaux lecteurs de musique nécessaires
            //for (int i = lecteurMusiques.Count; i < nombreDePistes; i++)
            //{
            //    LecteurMusique lecteurMusique = new LecteurMusique();
            //    lecteurMusique.Dock = DockStyle.Fill;
            //    lecteurMusique.setNbrPiste(i + 1);
            //    //table_lecteur.RowCount++; // Ajoute une nouvelle ligne
            //    table_lecteur.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            //    table_lecteur.Controls.Add(lecteurMusique, 0, i + 1);
            //    lecteurMusique.LecteurMusiqueViewModel.TacheGetBPM += LecteurMusiqueViewModel_TacheGetBPM;
            //    lecteurMusiques.Add(lecteurMusique);
            //}
        }
    }
}
