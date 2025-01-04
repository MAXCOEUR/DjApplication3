using DjApplication3.model;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Threading;

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
        public event EventHandler eventButtonUp;
        public event EventHandler eventButtonDown;
        public event EventHandler eventButtonLeft;
        public event EventHandler eventButtonRight;
        public event EventHandler eventButtonLoadLeft;
        public event EventHandler eventButtonLoadRight;
        public event EventHandler<int> eventPisteLeft;
        public event EventHandler<int> eventPisteRight;
        public event EventHandler<float> eventVolumeLeft;
        public event EventHandler<float> eventVolumeRight;
        public event EventHandler<float> eventMixe;
        public event EventHandler<int> eventScratchLeft;
        public event EventHandler<int> eventScratchRight;
        public event EventHandler<bool> eventScratchLeftPress;
        public event EventHandler<bool> eventScratchRightPress;
        public event EventHandler eventVolumeUpHeadPhone;
        public event EventHandler eventVolumeDownHeadPhone;

        MidiIn midiIn;
        MidiOut midiOut;

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

        private void initOut()
        {
            List<MidiInCapabilities> listMidiIn = new List<MidiInCapabilities>();
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                listMidiIn.Add(MidiIn.DeviceInfo(device));
            }
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                foreach (var item in listMidiIn)
                {
                    if (item.ProductName == MidiOut.DeviceInfo(device).ProductName)
                    {
                        Console.WriteLine(device);
                        midiOut = new MidiOut(device);
                    }
                }
            }

            playLeft(false);
            playRight(false);

            PreviewLeft(false);
            PreviewRight(false);

            lightButton();
        }
        public void start()
        {
            try
            {
                midiIn = new MidiIn(SettingsManager.Instance.nbrMidi);

                initOut();

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
            _instance = null;
            if (midiIn == null) return;
            midiIn.Stop();
            midiIn.Dispose();
            midiIn = null;

            if (midiOut == null) return;
            midiOut.Close();
            midiOut.Dispose();
            midiOut = null;
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
                    case 54:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonUp?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonUp " + noteOnEvent.Velocity);
                        break;
                    case 55:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonDown?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonDown " + noteOnEvent.Velocity);
                        break;
                    case 56:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonRight?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonRight " + noteOnEvent.Velocity);
                        break;
                    case 57:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonLeft?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonLeft " + noteOnEvent.Velocity);
                        break;
                    case 25:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonLoadLeft?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonLoadLeft " + noteOnEvent.Velocity);
                        break;
                    case 51:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventButtonLoadRight?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventButtonLoadRight " + noteOnEvent.Velocity);
                        break;
                    case 64:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventVolumeDownHeadPhone?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventVolumeDownHeadPhone " + noteOnEvent.Velocity);
                        break;
                    case 65:
                        if (noteOnEvent.Velocity == 127)
                        {
                            eventVolumeUpHeadPhone?.Invoke(this, EventArgs.Empty);
                        }
                        Console.WriteLine("eventVolumeUpHeadPhone " + noteOnEvent.Velocity);
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


        public void playLeft(bool isOn)
        {
            if (isOn)
            {
                SendNoteOn(22, 127);
            }
            else
            {
                SendNoteOn(22, 0);
            }
        }
        public void playRight(bool isOn)
        {
            if (isOn)
            {
                SendNoteOn(48, 127);
            }
            else
            {
                SendNoteOn(48, 0);
            }
        }

        public void PreviewLeft(bool isOn)
        {
            if (isOn)
            {
                SendNoteOn(24, 127);
            }
            else
            {
                SendNoteOn(24, 0);
            }
        }
        public void PreviewRight(bool isOn)
        {
            if (isOn)
            {
                SendNoteOn(50, 127);
            }
            else
            {
                SendNoteOn(50, 0);
            }
        }
        private void SendNoteOn(int noteNumber, int velocity)
        {
            if (midiOut == null) return;

            midiOut.Send(MidiMessage.StartNote(noteNumber, velocity, 1).RawData);
            Console.WriteLine($"NoteOn sent: Note {noteNumber}, Velocity {velocity}, Channel {1}");
        }

        private void lightButton()
        {
            if (midiOut == null) return;

            midiOut.Send(MidiMessage.StartNote(13, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(14, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(15, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(16, 127, 1).RawData);


            midiOut.Send(MidiMessage.StartNote(42, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(39, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(40, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(41, 127, 1).RawData);

            midiOut.Send(MidiMessage.StartNote(56, 127, 1).RawData);
            midiOut.Send(MidiMessage.StartNote(57, 127, 1).RawData);
        }
    }
}
