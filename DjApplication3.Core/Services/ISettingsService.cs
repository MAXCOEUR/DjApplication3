using CSCore.CoreAudioAPI;
using NAudio.Midi;
using System.Collections.Generic;

namespace DjApplication3.Services
{
    public interface ISettingsService
    {
        string AppName { get; }
        int TimeBeforeBlinkSecond { get; set; }
        int HeadphoneDeviceIndex { get; set; }
        int OutputDeviceIndex { get; set; }
        string? HeadphoneDeviceId { get; set; }
        string? OutputDeviceId { get; set; }
        int MidiDeviceIndex { get; set; }
        int TrackCount { get; set; }
        IReadOnlyList<MidiInCapabilities> MidiDevices { get; }
        MMDeviceCollection AudioDevices { get; }
        void RefreshDevices();
    }
}
