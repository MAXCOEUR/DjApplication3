using System;

namespace DjApplication3.Services
{
    public interface IMidiControllerService : IDisposable
    {
        event EventHandler? PlayPauseLeft;
        event EventHandler? PlayPauseRight;
        event EventHandler? HeadphoneLeft;
        event EventHandler? HeadphoneRight;
        event EventHandler? NavigateUp;
        event EventHandler? NavigateDown;
        event EventHandler? NavigateLeft;
        event EventHandler? NavigateRight;
        event EventHandler? LoadLeft;
        event EventHandler? LoadRight;
        event EventHandler<int>? PisteLeft;
        event EventHandler<int>? PisteRight;
        event EventHandler<float>? VolumeLeft;
        event EventHandler<float>? VolumeRight;
        event EventHandler<float>? BassLeft;
        event EventHandler<float>? BassRight;
        event EventHandler<float>? MediumLeft;
        event EventHandler<float>? MediumRight;
        event EventHandler<float>? TrebleLeft;
        event EventHandler<float>? TrebleRight;
        event EventHandler<float>? Mix;
        event EventHandler<int>? ScratchLeft;
        event EventHandler<int>? ScratchRight;
        event EventHandler<bool>? ScratchLeftPress;
        event EventHandler<bool>? ScratchRightPress;
        event EventHandler? VolumeUpHeadPhone;
        event EventHandler? VolumeDownHeadPhone;
        event EventHandler? PreviewPlayPause;
        void Start();
        void SetPlayLeft(bool isOn);
        void SetPlayRight(bool isOn);
        void SetPreviewLeft(bool isOn);
        void SetPreviewRight(bool isOn);
        void SetPreviewPlayPause(bool isOn);
        void SetLoadedLeft(bool isOn);
        void SetLoadedRight(bool isOn);
        void SetSelectedLeftDeck(int deckNumber);
        void SetSelectedRightDeck(int deckNumber);
    }
}
