using DjApplication3.model;
using NAudio.Midi;
using System;

namespace DjApplication3.outils
{
    internal class HerculesDJ
    {
        private static HerculesDJ _instance;
        private static readonly object _lockObject = new object();

        public event EventHandler eventPlayPauseLeft;
        public event EventHandler eventPlayPauseRight;
        public event EventHandler eventCasqueLeft;
        public event EventHandler eventCasqueRight;
        public event EventHandler<int> eventPisteLeft;
        public event EventHandler<int> eventPisteRight;
        public event EventHandler<float> eventVolumeLeft;
        public event EventHandler<float> eventVolumeRight;
        public event EventHandler<float> eventMixe;
        public event EventHandler<int> eventScratchLeft;
        public event EventHandler<int> eventScratchRight;
        public event EventHandler<bool> eventScratchLeftPress;
        public event EventHandler<bool> eventScratchRightPress;

        MidiIn midiIn;

        public static HerculesDJ Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new HerculesDJ();
                    }
                    return _instance;
                }
            }
        }
        private HerculesDJ()
        {

        }
        public void start()
        {
            try
            {
                midiIn = new MidiIn(SettingsManager.Instance.nbrMidi);
                midiIn.MessageReceived += midiIn_MessageReceived;
                midiIn.ErrorReceived += midiIn_ErrorReceived;
                midiIn.Start();

            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            return;

            
        }

        public void Dispose()
        {
            if (midiIn == null) return;
            midiIn.Stop();
            midiIn.Dispose();
            _instance = null;
        }

        private void midiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            if (midiIn == null) return;

            MidiEvent midiEvent = e.MidiEvent;

            if (midiEvent is NoteEvent)
            {
                NoteEvent noteOnEvent = midiEvent as NoteEvent;
                switch (noteOnEvent.NoteNumber)
                {
                    case 48:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPlayPauseRight?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("Droite playPause " + noteOnEvent.Velocity);
                        break;
                    case 22:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPlayPauseLeft?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("Gauche playPause " + noteOnEvent.Velocity);
                        break;
                    case 50:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventCasqueRight?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("Droite casque " + noteOnEvent.Velocity);
                        break;
                    case 24:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventCasqueLeft?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("Gauche casque " + noteOnEvent.Velocity);
                        break;
                    case 9:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteLeft?.Invoke(this, 1);
                        }
                        Console.WriteLine("Gauche piste1 " + noteOnEvent.Velocity);
                        break;
                    case 10:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteLeft?.Invoke(this, 2);
                        }
                        Console.WriteLine("Gauche piste2 " + noteOnEvent.Velocity);
                        break;
                    case 11:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteLeft?.Invoke(this, 3);
                        }
                        Console.WriteLine("Gauche piste3 " + noteOnEvent.Velocity);
                        break;
                    case 12:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteLeft?.Invoke(this, 4);
                        }
                        Console.WriteLine("Gauche piste4 " + noteOnEvent.Velocity);
                        break;
                    case 35:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteRight?.Invoke(this, 1);
                        }
                        Console.WriteLine("Droite piste1 " + noteOnEvent.Velocity);
                        break;
                    case 36:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteRight?.Invoke(this, 2);
                        }
                        Console.WriteLine("Droite piste2 " + noteOnEvent.Velocity);
                        break;
                    case 37:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteRight?.Invoke(this, 3);
                        }
                        Console.WriteLine("Droite piste3 " + noteOnEvent.Velocity);
                        break;
                    case 38:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventPisteRight?.Invoke(this, 4);
                        }
                        Console.WriteLine("Droite piste4 " + noteOnEvent.Velocity);
                        break;
                    case 26:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventScratchLeftPress?.Invoke(this, true);
                        }
                        else
                        {
                            eventScratchLeftPress?.Invoke(this, false);
                        }
                        Console.WriteLine("eventScratchLeftPress " + noteOnEvent.Velocity);
                        break;
                    case 52:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventScratchRightPress?.Invoke(this, true);
                        }
                        else
                        {
                            eventScratchRightPress?.Invoke(this, false);
                        }
                        Console.WriteLine("eventScratchRightPress " + noteOnEvent.Velocity);
                        break;
                }
            }
            else if (midiEvent is ControlChangeEvent)
            {
                ControlChangeEvent controlEvent = midiEvent as ControlChangeEvent;
                switch (((int)controlEvent.Controller))
                {
                    case 59:
                        eventVolumeRight?.Invoke(this, controlEvent.ControllerValue / 127.0F);
                        Console.WriteLine("Gauche volume " + controlEvent.ControllerValue / 127.0F);
                        break;
                    case 54:
                        eventVolumeLeft?.Invoke(this, controlEvent.ControllerValue / 127.0F);
                        Console.WriteLine("Droite volume " + controlEvent.ControllerValue / 127.0F);
                        break;
                    case 48:
                        eventScratchLeft?.Invoke(this, controlEvent.ControllerValue);
                        Console.WriteLine("Gauche scrach " + controlEvent.ControllerValue);
                        break;
                    case 49:
                        eventScratchRight?.Invoke(this, controlEvent.ControllerValue);
                        Console.WriteLine("Droite scrach " + controlEvent.ControllerValue);
                        break;
                    case 50:
                        eventScratchLeft?.Invoke(this, controlEvent.ControllerValue);
                        Console.WriteLine("Gauche scrach " + controlEvent.ControllerValue);
                        break;
                    case 51:
                        eventScratchRight?.Invoke(this, controlEvent.ControllerValue);
                        Console.WriteLine("Droite scrach " + controlEvent.ControllerValue);
                        break;
                    case 58:
                        eventMixe?.Invoke(this, controlEvent.ControllerValue / 127.0F);
                        Console.WriteLine("mixage " + controlEvent.ControllerValue / 127.0F);
                        break;
                }
            }
        }
        private void midiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            int rawMessage = e.RawMessage;
            Console.WriteLine(rawMessage);
        }
    }
}
