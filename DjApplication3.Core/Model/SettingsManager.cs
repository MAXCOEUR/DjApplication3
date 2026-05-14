using CSCore.CoreAudioAPI;
using Microsoft.Win32;
using NAudio.Midi;
using System;
using System.Collections.Generic;

namespace DjApplication3.model
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static readonly object _lockObject = new object();

        // Déclare ici les propriétés de tes paramètres

        public string APP_NAME { get;}
        public int timeBeforBlinkSecond { get; set; }

        public int nbrHeadPhone { get; set; }
        public int nbrOut { get; set; }

        public int nbrMidi { get; set; }

        public int nbrPiste { get; set; }
        public List<MidiInCapabilities> listMidi = new List<MidiInCapabilities>();


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
            nbrMidi = 0;
            APP_NAME = "DjApplication 3";


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
                    return _instance;
                }
            }
        }

        public void RefreshDevices()
        {
            updateMMDeviceCollection();
            updateListMidi();

            if (dispositifsAudio == null || dispositifsAudio.Count == 0)
            {
                nbrOut = 0;
                nbrHeadPhone = 0;
            }
            else
            {
                nbrOut = Math.Clamp(nbrOut, 0, dispositifsAudio.Count - 1);
                nbrHeadPhone = Math.Clamp(nbrHeadPhone, 0, dispositifsAudio.Count - 1);
            }

            nbrMidi = Math.Clamp(nbrMidi, 0, Math.Max(0, listMidi.Count - 1));
        }

        private void updateMMDeviceCollection()
        {
            try
            {
                enumerator = new MMDeviceEnumerator();
                dispositifsAudio = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Impossible de charger les peripheriques audio: {ex.Message}");
            }
        }
        private void updateListMidi()
        {
            listMidi.Clear();
            try
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    listMidi.Add(MidiIn.DeviceInfo(device));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Impossible de charger les peripheriques MIDI: {ex.Message}");
            }
        }
    }
}
