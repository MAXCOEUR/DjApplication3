using DjApplication3.model;
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
    /// Logique d'interaction pour Mixage2Pistes.xaml
    /// </summary>
    public partial class Mixage2Pistes : UserControl
    {
        public event EventHandler eventSetPiste;
        public int _nbrPisteGauche;
        public int _nbrPisteDroite;
        public int nbrPisteGauche => _nbrPisteGauche;
        public int nbrPisteDroite => _nbrPisteDroite;
        public Mixage2Pistes()
        {
            InitializeComponent();
            updatePiste();
        }

        public void updatePiste()
        {
            List<string> optionsListGauche = new List<string>();
            List<string> optionsListDroite = new List<string>();
            for (int i = 0; i < SettingsManager.Instance.nbrPiste; i++)
            {
                optionsListGauche.Add((i + 1).ToString());
                optionsListDroite.Add((i + 1).ToString());
            }
            cb_pisteGauche.ItemsSource = optionsListGauche;
            cb_pisteDroite.ItemsSource = optionsListDroite;

            cb_pisteGauche.SelectedIndex = 0;
            cb_pisteDroite.SelectedIndex = 1;
        }

        private void cb_pisteGauche_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_pisteGauche.SelectedIndex == _nbrPisteDroite)
            {
                cb_pisteGauche.SelectedIndex = _nbrPisteGauche;
                return;
            }
            _nbrPisteGauche = cb_pisteGauche.SelectedIndex;
            eventSetPiste?.Invoke(this, null);
        }

        private void cb_pisteDroite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_pisteDroite.SelectedIndex == _nbrPisteGauche)
            {
                cb_pisteDroite.SelectedIndex = _nbrPisteDroite;
                return;
            }
            _nbrPisteDroite = cb_pisteDroite.SelectedIndex;
            eventSetPiste?.Invoke(this, null);
        }
    }
}
