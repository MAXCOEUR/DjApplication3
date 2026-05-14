using CSCore.CoreAudioAPI;
using DjApplication3.model;
using NAudio.Midi;
using System.Collections.Generic;

namespace DjApplication3.Services
{
    public sealed class SettingsService : ISettingsService
    {
        public string AppName => SettingsManager.Instance.APP_NAME;
        public int TimeBeforeBlinkSecond
        {
            get => SettingsManager.Instance.timeBeforBlinkSecond;
            set => SettingsManager.Instance.timeBeforBlinkSecond = value;
        }
        public int HeadphoneDeviceIndex
        {
            get => SettingsManager.Instance.nbrHeadPhone;
            set => SettingsManager.Instance.nbrHeadPhone = value;
        }
        public int OutputDeviceIndex
        {
            get => SettingsManager.Instance.nbrOut;
            set => SettingsManager.Instance.nbrOut = value;
        }
        public int MidiDeviceIndex
        {
            get => SettingsManager.Instance.nbrMidi;
            set => SettingsManager.Instance.nbrMidi = value;
        }
        public int TrackCount
        {
            get => SettingsManager.Instance.nbrPiste;
            set => SettingsManager.Instance.nbrPiste = value;
        }
        public IReadOnlyList<MidiInCapabilities> MidiDevices => SettingsManager.Instance.listMidi;
        public MMDeviceCollection AudioDevices => SettingsManager.Instance.dispositifsAudio;
        public void RefreshDevices()
        {
            SettingsManager.Instance.RefreshDevices();
        }
    }
}
