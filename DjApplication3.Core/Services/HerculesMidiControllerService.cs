using DjApplication3.outils;
using System;

namespace DjApplication3.Services
{
    public sealed class HerculesMidiControllerService : IMidiControllerService
    {
        public event EventHandler? PlayPauseLeft;
        public event EventHandler? PlayPauseRight;
        public event EventHandler? HeadphoneLeft;
        public event EventHandler? HeadphoneRight;
        public event EventHandler? NavigateUp;
        public event EventHandler? NavigateDown;
        public event EventHandler? NavigateLeft;
        public event EventHandler? NavigateRight;
        public event EventHandler? LoadLeft;
        public event EventHandler? LoadRight;
        public event EventHandler<int>? PisteLeft;
        public event EventHandler<int>? PisteRight;
        public event EventHandler<float>? VolumeLeft;
        public event EventHandler<float>? VolumeRight;
        public event EventHandler<float>? BassLeft;
        public event EventHandler<float>? BassRight;
        public event EventHandler<float>? MediumLeft;
        public event EventHandler<float>? MediumRight;
        public event EventHandler<float>? TrebleLeft;
        public event EventHandler<float>? TrebleRight;
        public event EventHandler<float>? Mix;
        public event EventHandler<int>? ScratchLeft;
        public event EventHandler<int>? ScratchRight;
        public event EventHandler<bool>? ScratchLeftPress;
        public event EventHandler<bool>? ScratchRightPress;
        public event EventHandler<int>? PitchLeft;
        public event EventHandler<int>? PitchRight;
        public event EventHandler<int>? PitchNudgeLeft;
        public event EventHandler<int>? PitchNudgeRight;
        public event EventHandler? PitchResetLeft;
        public event EventHandler? PitchResetRight;
        public event EventHandler? SyncLeft;
        public event EventHandler? SyncRight;
        public event EventHandler? VolumeUpHeadPhone;
        public event EventHandler? VolumeDownHeadPhone;
        public event EventHandler? PreviewPlayPause;

        public void Start()
        {
            var controller = HerculesDJ.Instance;
            controller.Dispose();
            controller = HerculesDJ.Instance;
            controller.eventPlayPauseLeft += (_, e) => PlayPauseLeft?.Invoke(this, e);
            controller.eventPlayPauseRight += (_, e) => PlayPauseRight?.Invoke(this, e);
            controller.eventCasqueLeft += (_, e) => HeadphoneLeft?.Invoke(this, e);
            controller.eventCasqueRight += (_, e) => HeadphoneRight?.Invoke(this, e);
            controller.eventButtonUp += (_, e) => NavigateUp?.Invoke(this, e);
            controller.eventButtonDown += (_, e) => NavigateDown?.Invoke(this, e);
            controller.eventButtonLeft += (_, e) => NavigateLeft?.Invoke(this, e);
            controller.eventButtonRight += (_, e) => NavigateRight?.Invoke(this, e);
            controller.eventButtonLoadLeft += (_, e) => LoadLeft?.Invoke(this, e);
            controller.eventButtonLoadRight += (_, e) => LoadRight?.Invoke(this, e);
            controller.eventPisteLeft += (_, e) => PisteLeft?.Invoke(this, e);
            controller.eventPisteRight += (_, e) => PisteRight?.Invoke(this, e);
            controller.eventVolumeLeft += (_, e) => VolumeLeft?.Invoke(this, e);
            controller.eventVolumeRight += (_, e) => VolumeRight?.Invoke(this, e);
            controller.eventBassLeft += (_, e) => BassLeft?.Invoke(this, e);
            controller.eventBassRight += (_, e) => BassRight?.Invoke(this, e);
            controller.eventMediumLeft += (_, e) => MediumLeft?.Invoke(this, e);
            controller.eventMediumRight += (_, e) => MediumRight?.Invoke(this, e);
            controller.eventTrebleLeft += (_, e) => TrebleLeft?.Invoke(this, e);
            controller.eventTrebleRight += (_, e) => TrebleRight?.Invoke(this, e);
            controller.eventMixe += (_, e) => Mix?.Invoke(this, e);
            controller.eventScratchLeft += (_, e) => ScratchLeft?.Invoke(this, e);
            controller.eventScratchRight += (_, e) => ScratchRight?.Invoke(this, e);
            controller.eventScratchLeftPress += (_, e) => ScratchLeftPress?.Invoke(this, e);
            controller.eventScratchRightPress += (_, e) => ScratchRightPress?.Invoke(this, e);
            controller.eventPitchLeft += (_, e) => PitchLeft?.Invoke(this, e);
            controller.eventPitchRight += (_, e) => PitchRight?.Invoke(this, e);
            controller.eventPitchNudgeLeft += (_, e) => PitchNudgeLeft?.Invoke(this, e);
            controller.eventPitchNudgeRight += (_, e) => PitchNudgeRight?.Invoke(this, e);
            controller.eventPitchResetLeft += (_, e) => PitchResetLeft?.Invoke(this, e);
            controller.eventPitchResetRight += (_, e) => PitchResetRight?.Invoke(this, e);
            controller.eventSyncLeft += (_, e) => SyncLeft?.Invoke(this, e);
            controller.eventSyncRight += (_, e) => SyncRight?.Invoke(this, e);
            controller.eventPreviewPlayPause += (_, e) => PreviewPlayPause?.Invoke(this, e);
            controller.eventVolumeUpHeadPhone += (_, e) => VolumeUpHeadPhone?.Invoke(this, e);
            controller.eventVolumeDownHeadPhone += (_, e) => VolumeDownHeadPhone?.Invoke(this, e);
            controller.start();
        }

        public void SetPlayLeft(bool isOn) => HerculesDJ.Instance.playLeft(isOn);
        public void SetPlayRight(bool isOn) => HerculesDJ.Instance.playRight(isOn);
        public void SetPreviewLeft(bool isOn) => HerculesDJ.Instance.PreviewLeft(isOn);
        public void SetPreviewRight(bool isOn) => HerculesDJ.Instance.PreviewRight(isOn);
        public void SetPreviewPlayPause(bool isOn) => HerculesDJ.Instance.PreviewPlayPause(isOn);
        public void SetLoadedLeft(bool isOn) => HerculesDJ.Instance.loadedLeft(isOn);
        public void SetLoadedRight(bool isOn) => HerculesDJ.Instance.loadedRight(isOn);
        public void SetSelectedLeftDeck(int deckNumber) => HerculesDJ.Instance.selectPisteLeft(deckNumber);
        public void SetSelectedRightDeck(int deckNumber) => HerculesDJ.Instance.selectPisteRight(deckNumber);
        public void Dispose() => HerculesDJ.Instance.Dispose();
    }
}
