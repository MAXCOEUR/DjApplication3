﻿using CSCore.CoreAudioAPI;
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

        public int browserIndice { get; set; }
        public List<string> browsers {  get; }

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
            browsers = GetInstalledBrowsers();
            browserIndice = 0;
            APP_NAME = "DjApplication 3";


        }
        private List<string> GetInstalledBrowsers()
        {
            List<string> browsers = new List<string>();

            // Les navigateurs sont souvent enregistrés dans le registre
            // Sous la clé "SOFTWARE\Clients\StartMenuInternet"
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet"))
            {
                if (key != null)
                {
                    // Obtenez les noms des sous-clés (qui représentent les navigateurs)
                    string[] browserNames = key.GetSubKeyNames();

                    foreach (string browserName in browserNames)
                    {
                        // Ajoutez le nom du navigateur à la liste
                        browsers.Add(browserName);
                    }
                }
            }

            return browsers;
        }
        private string GetDefaultBrowser()
        {
            string defaultBrowser = string.Empty;

            // Accédez à la clé du registre qui stocke l'application par défaut pour HTTP
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"))
            {
                if (key != null)
                {
                    // Obtenez le nom de l'application par défaut
                    object progId = key.GetValue("Progid");

                    if (progId != null)
                    {
                        defaultBrowser = progId.ToString();
                    }
                }
            }

            return defaultBrowser;
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
