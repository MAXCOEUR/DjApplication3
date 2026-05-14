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
        public event EventHandler<float>? Mix;
        public event EventHandler<int>? ScratchLeft;
        public event EventHandler<int>? ScratchRight;
        public event EventHandler<bool>? ScratchLeftPress;
        public event EventHandler<bool>? ScratchRightPress;
        public event EventHandler? VolumeUpHeadPhone;
        public event EventHandler? VolumeDownHeadPhone;

        public void Start()
        {
            HerculesDJ.Instance?.Dispose();
            HerculesDJ.Instance.eventPlayPauseLeft += (_, e) => PlayPauseLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventPlayPauseRight += (_, e) => PlayPauseRight?.Invoke(this, e);
            HerculesDJ.Instance.eventCasqueLeft += (_, e) => HeadphoneLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventCasqueRight += (_, e) => HeadphoneRight?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonUp += (_, e) => NavigateUp?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonDown += (_, e) => NavigateDown?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonLeft += (_, e) => NavigateLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonRight += (_, e) => NavigateRight?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonLoadLeft += (_, e) => LoadLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventButtonLoadRight += (_, e) => LoadRight?.Invoke(this, e);
            HerculesDJ.Instance.eventPisteLeft += (_, e) => PisteLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventPisteRight += (_, e) => PisteRight?.Invoke(this, e);
            HerculesDJ.Instance.eventVolumeLeft += (_, e) => VolumeLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventVolumeRight += (_, e) => VolumeRight?.Invoke(this, e);
            HerculesDJ.Instance.eventMixe += (_, e) => Mix?.Invoke(this, e);
            HerculesDJ.Instance.eventScratchLeft += (_, e) => ScratchLeft?.Invoke(this, e);
            HerculesDJ.Instance.eventScratchRight += (_, e) => ScratchRight?.Invoke(this, e);
            HerculesDJ.Instance.eventScratchLeftPress += (_, e) => ScratchLeftPress?.Invoke(this, e);
            HerculesDJ.Instance.eventScratchRightPress += (_, e) => ScratchRightPress?.Invoke(this, e);
            HerculesDJ.Instance.eventVolumeUpHeadPhone += (_, e) => VolumeUpHeadPhone?.Invoke(this, e);
            HerculesDJ.Instance.eventVolumeDownHeadPhone += (_, e) => VolumeDownHeadPhone?.Invoke(this, e);
            HerculesDJ.Instance.start();
        }

        public void SetPlayLeft(bool isOn) => HerculesDJ.Instance.playLeft(isOn);
        public void SetPlayRight(bool isOn) => HerculesDJ.Instance.playRight(isOn);
        public void SetPreviewLeft(bool isOn) => HerculesDJ.Instance.PreviewLeft(isOn);
        public void SetPreviewRight(bool isOn) => HerculesDJ.Instance.PreviewRight(isOn);
        public void SetLoadedLeft(bool isOn) => HerculesDJ.Instance.loadedLeft(isOn);
        public void SetLoadedRight(bool isOn) => HerculesDJ.Instance.loadedRight(isOn);
        public void SetSelectedLeftDeck(int deckNumber) => HerculesDJ.Instance.selectPisteLeft(deckNumber);
        public void SetSelectedRightDeck(int deckNumber) => HerculesDJ.Instance.selectPisteRight(deckNumber);
        public void Dispose() => HerculesDJ.Instance.Dispose();
    }
}
