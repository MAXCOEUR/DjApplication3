using CSCore.CoreAudioAPI;
using NAudio.Midi;
using System.Collections.Generic;

namespace DjApplication3.model
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static readonly object _lockObject = new object();

        // Déclare ici les propriétés de tes paramètres
        public int timeBeforBlinkSecond { get; set; }

        public int nbrHeadPhone { get; set; }
        public int nbrOut { get; set; }

        public int nbrMidi { get; set; }

        public int nbrPiste { get; set; }
        public List<MidiInCapabilities> listMidi = new List<MidiInCapabilities>();

        public int browser { get; set; }
        public string browserName { get; set; }

        MMDeviceEnumerator enumerator;
        public MMDeviceCollection dispositifsAudio;

        // Constructeur privé pour empêcher l'instanciation directe
        private SettingsManager()
        {
            // Initialise les paramètres par défaut ici
            timeBeforBlinkSecond = 30;
            updateMMDeviceCollection();
            updateListMidi();
            nbrHeadPhone = 0;
            nbrOut = 0;
            nbrPiste = 2;
            browser = 0;
            browserName = "edge";
            nbrMidi = 0;
        }

        // Méthode pour obtenir l'instance unique de la classe
        public static SettingsManager Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new SettingsManager();
                    }
                    else
                    {
                        _instance.updateMMDeviceCollection();
                        _instance.updateListMidi();
                    }
                    return _instance;
                }
            }
        }

        private void updateMMDeviceCollection()
        {
            enumerator = new MMDeviceEnumerator();
            dispositifsAudio = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
        }
        private void updateListMidi()
        {
            listMidi.Clear();
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                listMidi.Add(MidiIn.DeviceInfo(device));
            }
        }
    }
}
