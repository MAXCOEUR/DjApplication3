using CSCore.CoreAudioAPI;
using DjApplication3.Infrastructure;
using Microsoft.Win32;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DjApplication3.model
{
    public class SettingsManager
    {
        private const double DefaultLibraryNavigationWidth = 190;
        private const double MinLibraryNavigationWidth = 120;
        private const double MaxLibraryNavigationWidth = 420;
        private const double UnsetDeckAreaWidth = 0;
        private const double MinDeckAreaWidth = 420;
        private static SettingsManager _instance;
        private static readonly object _lockObject = new object();

        // Déclare ici les propriétés de tes paramètres

        public string APP_NAME { get;}
        public int timeBeforBlinkSecond { get; set; }

        public int nbrHeadPhone { get; set; }
        public int nbrOut { get; set; }
        public string? HeadphoneDeviceId { get; set; }
        public string? OutputDeviceId { get; set; }

        public int nbrMidi { get; set; }

        public int nbrPiste { get; set; }
        private double _deckAreaWidth = UnsetDeckAreaWidth;
        private double _libraryNavigationWidth = DefaultLibraryNavigationWidth;
        public List<MidiInCapabilities> listMidi = new List<MidiInCapabilities>();
        public string? MidiDeviceRefreshError { get; private set; }


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
            HeadphoneDeviceId = null;
            OutputDeviceId = null;
            nbrPiste = 2;
            nbrMidi = 0;
            APP_NAME = "DjApplication 3";
            LoadUiSettings();

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
                nbrOut = ResolveAudioDeviceIndex(OutputDeviceId, nbrOut);
                nbrHeadPhone = ResolveAudioDeviceIndex(HeadphoneDeviceId, nbrHeadPhone);
            }

            nbrMidi = listMidi.Count == 0
                ? 0
                : Math.Clamp(nbrMidi, 0, listMidi.Count - 1);
        }

        public string? GetAudioDeviceId(int index)
        {
            if (dispositifsAudio == null || index < 0 || index >= dispositifsAudio.Count)
            {
                return null;
            }

            return GetDeviceIdentifier(dispositifsAudio[index]);
        }

        public double LibraryNavigationWidth
        {
            get => _libraryNavigationWidth;
            set
            {
                _libraryNavigationWidth = Math.Clamp(value, MinLibraryNavigationWidth, MaxLibraryNavigationWidth);
                SaveUiSettings();
            }
        }

        public double DeckAreaWidth
        {
            get => _deckAreaWidth;
            set
            {
                _deckAreaWidth = value <= 0 ? UnsetDeckAreaWidth : Math.Max(MinDeckAreaWidth, value);
                SaveUiSettings();
            }
        }

        private int ResolveAudioDeviceIndex(string? deviceId, int fallbackIndex)
        {
            if (dispositifsAudio == null || dispositifsAudio.Count == 0)
            {
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                for (var index = 0; index < dispositifsAudio.Count; index++)
                {
                    if (string.Equals(GetDeviceIdentifier(dispositifsAudio[index]), deviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        return index;
                    }
                }

                return Math.Clamp(fallbackIndex, 0, dispositifsAudio.Count - 1);
            }

            return Math.Clamp(fallbackIndex, 0, dispositifsAudio.Count - 1);
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

        private static string? GetDeviceIdentifier(object? device)
        {
            if (device == null)
            {
                return null;
            }

            var type = device.GetType();
            foreach (var propertyName in new[] { "ID", "Id", "DeviceID", "DeviceId" })
            {
                var property = type.GetProperty(propertyName);
                if (property?.PropertyType == typeof(string))
                {
                    return property.GetValue(device) as string;
                }
            }

            return device.ToString();
        }

        private void updateListMidi()
        {
            listMidi.Clear();
            MidiDeviceRefreshError = null;
            try
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    listMidi.Add(MidiIn.DeviceInfo(device));
                }
            }
            catch (Exception ex)
            {
                MidiDeviceRefreshError = ex.Message;
                Console.WriteLine($"Impossible de charger les peripheriques MIDI: {ex.Message}");
            }
        }

        private void LoadUiSettings()
        {
            try
            {
                if (!File.Exists(AppPaths.SettingsFile))
                {
                    return;
                }

                var json = File.ReadAllText(AppPaths.SettingsFile);
                var settings = JsonSerializer.Deserialize<PersistedSettings>(json);
                if (settings?.LibraryNavigationWidth is double width)
                {
                    _libraryNavigationWidth = Math.Clamp(width, MinLibraryNavigationWidth, MaxLibraryNavigationWidth);
                }

                if (settings?.DeckAreaWidth is double deckWidth)
                {
                    _deckAreaWidth = deckWidth <= 0 ? UnsetDeckAreaWidth : Math.Max(MinDeckAreaWidth, deckWidth);
                }
            }
            catch
            {
                _deckAreaWidth = UnsetDeckAreaWidth;
                _libraryNavigationWidth = DefaultLibraryNavigationWidth;
            }
        }

        private void SaveUiSettings()
        {
            try
            {
                AppPaths.EnsureRuntimeDirectories();
                var settings = new PersistedSettings
                {
                    DeckAreaWidth = _deckAreaWidth > 0 ? _deckAreaWidth : null,
                    LibraryNavigationWidth = _libraryNavigationWidth
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppPaths.SettingsFile, json);
            }
            catch
            {
                // Saving UI preferences should never block audio or startup.
            }
        }

        private sealed class PersistedSettings
        {
            public double? DeckAreaWidth { get; set; }
            public double? LibraryNavigationWidth { get; set; }
        }
    }
}
