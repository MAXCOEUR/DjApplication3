using CSCore.CoreAudioAPI;
using NAudio.Midi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

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

        private string _pathTShark;

        public string pathTShark
        {
            get
            {
                if (File.Exists("settings.json"))
                {
                    // Charger la valeur depuis le fichier JSON
                    string json = File.ReadAllText("settings.json");
                    JObject settingsJson = JObject.Parse(json);
                    string loadedSettings = (string)settingsJson["pathTShark"];
                    return loadedSettings ?? "";
                }

                return "C:\\Program Files\\Wireshark\\tshark.exe";
            }
            set
            {
                // Mettre à jour la valeur et sauvegarder dans le fichier JSON
                _pathTShark = value;
                SaveSettings();
            }
        }
        public string numeroUSB { get; set; }

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
            numeroUSB = "";
        }

        private void SaveSettings()
        {
            // Sérialiser l'objet Settings en format JSON
            string json = JsonConvert.SerializeObject(new { pathTShark = _pathTShark }, Formatting.Indented);

            // Écrire le JSON dans un fichier
            File.WriteAllText("settings.json", json);
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
