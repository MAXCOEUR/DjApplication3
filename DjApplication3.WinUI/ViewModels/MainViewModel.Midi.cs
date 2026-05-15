using System;
using System.Linq;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
        public void RefreshDevicesForOptions()
        {
            try
            {
                _settings.RefreshDevices();
                _lastSeenMidiDeviceCount = _settings.MidiDevices.Count;
                if (!string.IsNullOrWhiteSpace(_settings.MidiDeviceRefreshError))
                {
                    Status = $"Mise a jour MIDI impossible: {_settings.MidiDeviceRefreshError}";
                }
                else if (_settings.MidiDevices.Count == 0)
                {
                    Status = "Aucun p\u00e9riph\u00e9rique MIDI d\u00e9tect\u00e9";
                }
                else
                {
                    Status = $"{_settings.MidiDevices.Count} peripherique(s) MIDI detecte(s)";
                }
            }
            catch (Exception ex)
            {
                Status = $"Mise a jour peripheriques impossible: {ex.Message}";
            }
        }

        public void RestartMidiController()
        {
            try
            {
                _midi.Start();
                SyncControllerState();
                _lastSeenMidiDeviceCount = _settings.MidiDevices.Count;
                Status = "Controleur MIDI reconnecte";
            }
            catch (Exception ex)
            {
                Status = $"Controleur MIDI indisponible: {ex.Message}";
            }
        }

        private void SyncControllerState()
        {
            var leftDeck = Decks.ElementAtOrDefault(LeftDeckIndex);
            var rightDeck = Decks.ElementAtOrDefault(RightDeckIndex);

            _midi.SetSelectedLeftDeck(LeftDeckNumber);
            _midi.SetSelectedRightDeck(RightDeckNumber);
            _midi.SetPlayLeft(leftDeck?.IsPlaying == true);
            _midi.SetPlayRight(rightDeck?.IsPlaying == true);
            _midi.SetPreviewLeft(leftDeck?.IsHeadphone == true);
            _midi.SetPreviewRight(rightDeck?.IsHeadphone == true);
            _midi.SetLoadedLeft(leftDeck?.HasMusic == true);
            _midi.SetLoadedRight(rightDeck?.HasMusic == true);
        }

        private void HandleScratchPress(bool isLeft, bool isPressed)
        {
            var deck = Decks.ElementAtOrDefault(isLeft ? LeftDeckIndex : RightDeckIndex);
            if (deck == null) return;

            if (isPressed)
            {
                if (isLeft)
                {
                    _leftWasPlayingBeforeScratch = deck.IsPlaying;
                }
                else
                {
                    _rightWasPlayingBeforeScratch = deck.IsPlaying;
                }
                deck.Pause();
                return;
            }

            if ((isLeft && _leftWasPlayingBeforeScratch) || (!isLeft && _rightWasPlayingBeforeScratch))
            {
                deck.Play();
            }
        }

        private void StartMidi()
        {
            _midi.PlayPauseLeft += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.TogglePlayPause());
            _midi.PlayPauseRight += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.TogglePlayPause());
            _midi.HeadphoneLeft += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.ToggleHeadphone());
            _midi.HeadphoneRight += (_, _) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.ToggleHeadphone());
            _midi.NavigateUp += (_, _) => Enqueue(() => MoveSelection(-1));
            _midi.NavigateDown += (_, _) => Enqueue(() => MoveSelection(1));
            _midi.NavigateLeft += (_, _) => Enqueue(() => _ = RunSafeAsync(NavigateLibraryLeftAsync(), "Navigation impossible"));
            _midi.NavigateRight += (_, _) => Enqueue(() => _ = RunSafeAsync(HandleMidiNavigateRightAsync(), "Navigation impossible"));
            _midi.LoadLeft += (_, _) => Enqueue(() => _ = RunSafeAsync(LoadSelectedAsync(LeftDeckIndex), "Chargement piste gauche impossible"));
            _midi.LoadRight += (_, _) => Enqueue(() => _ = RunSafeAsync(LoadSelectedAsync(RightDeckIndex), "Chargement piste droite impossible"));
            _midi.PisteLeft += (_, piste) => Enqueue(() => LeftDeckNumber = Math.Clamp(piste, 1, Decks.Count));
            _midi.PisteRight += (_, piste) => Enqueue(() => RightDeckNumber = Math.Clamp(piste, 1, Decks.Count));
            _midi.VolumeLeft += (_, volume) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(LeftDeckIndex);
                if (deck != null) deck.Volume = (int)(volume * 100);
            });
            _midi.VolumeRight += (_, volume) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(RightDeckIndex);
                if (deck != null) deck.Volume = (int)(volume * 100);
            });
            _midi.BassLeft += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(LeftDeckIndex);
                if (deck != null) deck.BassDb = value;
            });
            _midi.MediumLeft += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(LeftDeckIndex);
                if (deck != null) deck.MidDb = value;
            });
            _midi.TrebleLeft += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(LeftDeckIndex);
                if (deck != null) deck.TrebleDb = value;
            });
            _midi.BassRight += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(RightDeckIndex);
                if (deck != null) deck.BassDb = value;
            });
            _midi.MediumRight += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(RightDeckIndex);
                if (deck != null) deck.MidDb = value;
            });
            _midi.TrebleRight += (_, value) => Enqueue(() =>
            {
                var deck = Decks.ElementAtOrDefault(RightDeckIndex);
                if (deck != null) deck.TrebleDb = value;
            });
            _midi.Mix += (_, mix) => Enqueue(() => Crossfade = (int)(mix * 100));
            _midi.ScratchLeft += (_, value) => Enqueue(() => Decks.ElementAtOrDefault(LeftDeckIndex)?.ChangePosition(value != 127));
            _midi.ScratchRight += (_, value) => Enqueue(() => Decks.ElementAtOrDefault(RightDeckIndex)?.ChangePosition(value != 127));
            _midi.ScratchLeftPress += (_, isPressed) => Enqueue(() => HandleScratchPress(true, isPressed));
            _midi.ScratchRightPress += (_, isPressed) => Enqueue(() => HandleScratchPress(false, isPressed));
            _midi.VolumeUpHeadPhone += (_, _) => Enqueue(() => HeadphoneVolume += 5);
            _midi.VolumeDownHeadPhone += (_, _) => Enqueue(() => HeadphoneVolume -= 5);
            _midi.Start();
            SyncControllerState();
        }

        private void StartMidiAutoDetection()
        {
            if (_midiAutoDetectionTimer != null)
            {
                return;
            }

            try
            {
                _settings.RefreshDevices();
                _lastSeenMidiDeviceCount = _settings.MidiDevices.Count;
            }
            catch
            {
                _lastSeenMidiDeviceCount = -1;
            }

            _midiAutoDetectionTimer = _dispatcherQueue.CreateTimer();
            _midiAutoDetectionTimer.Interval = TimeSpan.FromSeconds(2);
            _midiAutoDetectionTimer.Tick += (_, _) => CheckMidiDevicesForAutoConnect();
            _midiAutoDetectionTimer.Start();
        }

        private void StopMidiAutoDetection()
        {
            if (_midiAutoDetectionTimer == null)
            {
                return;
            }

            _midiAutoDetectionTimer.Stop();
            _midiAutoDetectionTimer = null;
        }

        private void CheckMidiDevicesForAutoConnect()
        {
            if (_isCheckingMidiDevices)
            {
                return;
            }

            _isCheckingMidiDevices = true;
            try
            {
                var previousCount = _lastSeenMidiDeviceCount;
                _settings.RefreshDevices();

                if (!string.IsNullOrWhiteSpace(_settings.MidiDeviceRefreshError))
                {
                    return;
                }

                var currentCount = _settings.MidiDevices.Count;
                if (currentCount == previousCount)
                {
                    return;
                }

                if (currentCount == 1 && previousCount <= 0)
                {
                    _settings.MidiDeviceIndex = 0;
                    _midi.Start();
                    SyncControllerState();
                    _lastSeenMidiDeviceCount = currentCount;
                    Status = $"Controleur MIDI detecte: {_settings.MidiDevices[0].ProductName}";
                }
                else if (currentCount == 0)
                {
                    _lastSeenMidiDeviceCount = currentCount;
                    Status = "Aucun p\u00e9riph\u00e9rique MIDI d\u00e9tect\u00e9";
                }
                else if (previousCount <= 0)
                {
                    _lastSeenMidiDeviceCount = currentCount;
                    Status = $"{currentCount} peripherique(s) MIDI detecte(s), selectionne le bon dans les options";
                }
                else
                {
                    _lastSeenMidiDeviceCount = currentCount;
                }
            }
            catch (Exception ex)
            {
                Status = $"Detection MIDI impossible: {ex.Message}";
            }
            finally
            {
                _isCheckingMidiDevices = false;
            }
        }

        private async Task HandleMidiNavigateRightAsync()
        {
            if (_libraryFocus == LibraryFocus.Musics
                && SelectedMusicIndex >= 0
                && SelectedMusicIndex < Musics.Count)
            {
                await TogglePreviewAsync(Musics[SelectedMusicIndex]);
                return;
            }

            await NavigateLibraryRightAsync();
        }

        private void Enqueue(Action action) => _dispatcherQueue.TryEnqueue(() =>
        {
            try { action(); }
            catch (Exception ex) { Status = ex.Message; }
        });
    }
}
