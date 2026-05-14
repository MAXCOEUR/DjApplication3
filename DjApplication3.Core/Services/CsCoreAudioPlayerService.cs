using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using DjApplication3.model;
using System;

namespace DjApplication3.Services
{
    public sealed class CsCoreAudioPlayerService : IAudioPlayerService
    {
        private readonly ISettingsService _settings;
        private readonly System.Timers.Timer _timer = new(500);
        private IWaveSource? _waveSource;
        private WasapiOut? _audioPlayer;
        private float _masterVolume = 1;
        private float _trackVolume = 1;
        private float _headphoneVolume = 1;
        private bool _headphoneEnabled;

        public event EventHandler? PositionChanged;

        public CsCoreAudioPlayerService(ISettingsService settings)
        {
            _settings = settings;
            _timer.Elapsed += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsPlaying => _audioPlayer?.PlaybackState == PlaybackState.Playing;
        public float PositionRatio => _audioPlayer?.WaveSource == null || _audioPlayer.WaveSource.Length == 0
            ? 0
            : (float)_audioPlayer.WaveSource.Position / _audioPlayer.WaveSource.Length;
        public TimeSpan Duration => _audioPlayer?.WaveSource?.GetLength() ?? TimeSpan.Zero;
        public TimeSpan Position => _audioPlayer?.WaveSource?.GetPosition() ?? TimeSpan.Zero;

        public void Load(Musique musique)
        {
            var wasPlaying = IsPlaying;
            Stop();
            _waveSource = CodecFactory.Instance.GetCodec(musique.url);
            InitializePlayer(0);
            if (wasPlaying)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_audioPlayer == null || _audioPlayer.DebuggingId == -1) return;
            _audioPlayer.Play();
            _timer.Start();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            if (_audioPlayer == null || _audioPlayer.DebuggingId == -1) return;
            _audioPlayer.Pause();
            _timer.Stop();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            _timer.Stop();
            _audioPlayer?.Stop();
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            _waveSource?.Dispose();
            _waveSource = null;
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Seek(double positionRatio)
        {
            if (_audioPlayer?.WaveSource == null) return;
            var bounded = Math.Clamp(positionRatio, 0, 1);
            _audioPlayer.WaveSource.Position = (long)(_audioPlayer.WaveSource.Length * bounded);
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ChangePosition(bool isForward)
        {
            Seek(PositionRatio + (isForward ? 0.001 : -0.001));
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = volume;
            UpdateVolume();
        }

        public void SetTrackVolume(float volume)
        {
            _trackVolume = volume;
            UpdateVolume();
        }

        public void SetHeadphoneVolume(float volume)
        {
            _headphoneVolume = volume / 100.0f;
            UpdateVolume();
        }

        public void SetHeadphoneEnabled(bool enabled)
        {
            _headphoneEnabled = enabled;
            UpdateOutputDevice();
        }

        public void UpdateOutputDevice()
        {
            if (_waveSource == null) return;
            var wasPlaying = IsPlaying;
            var currentPosition = PositionRatio;
            if (wasPlaying)
            {
                Pause();
            }
            InitializePlayer(currentPosition);
            if (wasPlaying)
            {
                Play();
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            _audioPlayer?.Dispose();
            _waveSource?.Dispose();
        }

        private void InitializePlayer(float positionRatio)
        {
            if (_waveSource == null) return;
            _audioPlayer ??= new WasapiOut();
            _audioPlayer.Stop();
            if (_audioPlayer.WaveSource != null)
            {
                _audioPlayer.WaveSource.Dispose();
            }
            _audioPlayer.Device = _headphoneEnabled
                ? _settings.AudioDevices[_settings.HeadphoneDeviceIndex]
                : _settings.AudioDevices[_settings.OutputDeviceIndex];
            _audioPlayer.Initialize(_waveSource);
            if (_audioPlayer.WaveSource != null)
            {
                _audioPlayer.WaveSource.Position = (long)(_audioPlayer.WaveSource.Length * positionRatio);
            }
            UpdateVolume();
        }

        private void UpdateVolume()
        {
            if (_audioPlayer == null || _audioPlayer.DebuggingId == -1) return;
            _audioPlayer.Volume = _headphoneEnabled ? _headphoneVolume : _trackVolume * _masterVolume;
        }
    }
}
