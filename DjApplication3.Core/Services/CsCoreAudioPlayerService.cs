using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using DjApplication3.Infrastructure;
using DjApplication3.model;
using System.Diagnostics;
using System;

namespace DjApplication3.Services
{
    public sealed class CsCoreAudioPlayerService : IAudioPlayerService
    {
        private readonly ISettingsService _settings;
        private readonly System.Timers.Timer _timer = new(500);
        private IWaveSource? _waveSource;
        private WasapiOut? _audioPlayer;
        private Equalizer? _equalizer;
        private VariablePlaybackRateSampleSource? _playbackRateSource;
        private string? _loadedPath;
        private float _masterVolume = 1;
        private float _trackVolume = 1;
        private float _headphoneVolume = 1;
        private float _bassDb;
        private float _midDb;
        private float _trebleDb;
        private float _playbackRate = 1;
        private bool _headphoneEnabled;
        private bool _isReinitializing;

        public event EventHandler? PositionChanged;
        public event EventHandler? PlaybackStopped;

        public CsCoreAudioPlayerService(ISettingsService settings)
        {
            _settings = settings;
            _timer.Elapsed += (_, _) =>
            {
                try
                {
                    PositionChanged?.Invoke(this, EventArgs.Empty);
                    Debug.WriteLine($"[CsCoreAudioPlayerService] Timer tick. IsPlaying={IsPlaying}, PositionRatio={PositionRatio}");
                }
                catch (Exception ex)
                {
                    AppLogger.Warning(ex, "Audio position timer failed");
                    Debug.WriteLine($"[CsCoreAudioPlayerService] Timer handler exception: {ex}");
                }
            };
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
            var fallbackDevice = _audioPlayer?.Device;
            Stop();
            _loadedPath = musique.url;
            InitializePlayer(0, fallbackDevice);
            if (wasPlaying)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_audioPlayer == null || _audioPlayer.DebuggingId == -1 || _audioPlayer.WaveSource == null)
            {
                throw new InvalidOperationException("Aucun lecteur audio initialise.");
            }

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
            DisposePlayer();
            _audioPlayer = null;
            _waveSource?.Dispose();
            _waveSource = null;
            _equalizer = null;
            _playbackRateSource = null;
            _loadedPath = null;
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
            if (_headphoneEnabled == enabled)
            {
                return;
            }

            var previous = _headphoneEnabled;
            _headphoneEnabled = enabled;
            try
            {
                UpdateOutputDevice();
            }
            catch (Exception ex)
            {
                // If switching output fails, restore previous state and log the error.
                _headphoneEnabled = previous;
                AppLogger.Error(ex, "Output switch failed");
                Debug.WriteLine($"SetHeadphoneEnabled failed: {ex}");
            }
        }

        public void SetEqualizer(float bassDb, float midDb, float trebleDb)
        {
            _bassDb = Math.Clamp(bassDb, -12, 12);
            _midDb = Math.Clamp(midDb, -12, 12);
            _trebleDb = Math.Clamp(trebleDb, -12, 12);
            ApplyEqualizerGains();
        }

        public void SetPlaybackRate(float rate)
        {
            var boundedRate = Math.Clamp(rate, 0.75f, 1.25f);
            if (Math.Abs(_playbackRate - boundedRate) < 0.001f)
            {
                return;
            }

            _playbackRate = boundedRate;
            if (_playbackRateSource != null)
            {
                _playbackRateSource.PlaybackRate = boundedRate;
            }
        }

        public void UpdateOutputDevice()
        {
            var currentDevice = _audioPlayer?.Device;
            var targetDevice = ResolveDevice(currentDevice);

            if (targetDevice == null)
            {
                UpdateVolume();
                return;
            }

            if (currentDevice != null && AreSameDevice(currentDevice, targetDevice))
            {
                UpdateVolume();
                return;
            }

            ReinitializeKeepingState();
        }

        public void Dispose()
        {
            _timer.Dispose();
            DisposePlayer();
            _waveSource?.Dispose();
        }

        private void ReinitializeKeepingState()
        {
            if (string.IsNullOrWhiteSpace(_loadedPath)) return;
            var wasPlaying = IsPlaying;
            var currentPosition = PositionRatio;
            var fallbackDevice = _audioPlayer?.Device;
            _isReinitializing = true;
            try
            {
                if (wasPlaying)
                {
                    Pause();
                }

                InitializePlayer(currentPosition, fallbackDevice);
                if (wasPlaying)
                {
                    Play();
                }
            }
            finally
            {
                _isReinitializing = false;
            }
        }

        private void InitializePlayer(float positionRatio, object? fallbackDevice = null)
        {
            if (string.IsNullOrWhiteSpace(_loadedPath))
            {
                throw new InvalidOperationException("Aucun fichier audio charge.");
            }

            if (_audioPlayer == null)
            {
                _audioPlayer = new WasapiOut();
                _audioPlayer.Stopped += AudioPlayer_Stopped;
            }
            _audioPlayer.Stop();
            if (_audioPlayer.WaveSource != null)
            {
                _audioPlayer.WaveSource.Dispose();
            }
            _waveSource?.Dispose();
            _waveSource = CodecFactory.Instance.GetCodec(_loadedPath);
            var device = ResolveDevice(fallbackDevice);
            if (device == null)
            {
                throw new InvalidOperationException("Aucun peripherique audio disponible.");
            }

            SetPlayerDevice(device);
            var outputSource = BuildOutputSource(_waveSource);
            _audioPlayer.Initialize(outputSource);
            if (_audioPlayer.WaveSource == null)
            {
                throw new InvalidOperationException("Initialisation du lecteur audio impossible.");
            }

            if (_audioPlayer.WaveSource != null)
            {
                _audioPlayer.WaveSource.Position = (long)(_audioPlayer.WaveSource.Length * positionRatio);
            }
            UpdateVolume();
        }

        private IWaveSource BuildOutputSource(IWaveSource source)
        {
            var sampleSource = source.ToSampleSource();
            _equalizer = new Equalizer(sampleSource);
            var sampleRate = _equalizer.WaveFormat.SampleRate;
            var channels = Math.Max(1, _equalizer.WaveFormat.Channels);
            _equalizer.SampleFilters.Add(new EqualizerFilter(channels, new EqualizerChannelFilter(sampleRate, 100, 0.8, _bassDb)));
            _equalizer.SampleFilters.Add(new EqualizerFilter(channels, new EqualizerChannelFilter(sampleRate, 1000, 0.8, _midDb)));
            _equalizer.SampleFilters.Add(new EqualizerFilter(channels, new EqualizerChannelFilter(sampleRate, 10000, 0.8, _trebleDb)));
            ApplyEqualizerGains();

            _playbackRateSource = new VariablePlaybackRateSampleSource(_equalizer, _playbackRate);
            return _playbackRateSource.ToWaveSource();
        }

        private void ApplyEqualizerGains()
        {
            if (_equalizer == null || _equalizer.SampleFilters.Count < 3)
            {
                return;
            }

            _equalizer.SampleFilters[0].AverageGainDB = _bassDb;
            _equalizer.SampleFilters[1].AverageGainDB = _midDb;
            _equalizer.SampleFilters[2].AverageGainDB = _trebleDb;
        }

        private void DisposePlayer()
        {
            if (_audioPlayer == null)
            {
                return;
            }

            _audioPlayer.Stopped -= AudioPlayer_Stopped;
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
        }

        private void AudioPlayer_Stopped(object? sender, PlaybackStoppedEventArgs e)
        {
            if (_isReinitializing || IsPlaying)
            {
                return;
            }

            _timer.Stop();
            PositionChanged?.Invoke(this, EventArgs.Empty);
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        private object? ResolveDevice(object? fallbackDevice = null)
        {
            var preferredDeviceId = _headphoneEnabled
                ? _settings.HeadphoneDeviceId
                : _settings.OutputDeviceId;

            var resolved = FindDeviceById(preferredDeviceId);
            if (resolved != null)
            {
                return resolved;
            }

            if (fallbackDevice != null)
            {
                return fallbackDevice;
            }

            if (_settings.AudioDevices == null || _settings.AudioDevices.Count == 0)
            {
                return null;
            }

            var fallbackIndex = _headphoneEnabled
                ? _settings.HeadphoneDeviceIndex
                : _settings.OutputDeviceIndex;

            if (fallbackIndex >= 0 && fallbackIndex < _settings.AudioDevices.Count)
            {
                return _settings.AudioDevices[fallbackIndex];
            }

            return _settings.AudioDevices[0];
        }

        private object? FindDeviceById(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId) || _settings.AudioDevices == null)
            {
                return null;
            }

            foreach (var device in _settings.AudioDevices)
            {
                if (string.Equals(GetDeviceIdentifier(device), deviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }
            }

            return null;
        }

        private void SetPlayerDevice(object device)
        {
            var property = _audioPlayer?.GetType().GetProperty("Device");
            property?.SetValue(_audioPlayer, device);
        }

        private static string? GetDeviceIdentifier(object device)
        {
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

        private static bool AreSameDevice(object left, object right)
        {
            var leftId = GetDeviceIdentifier(left);
            var rightId = GetDeviceIdentifier(right);

            if (!string.IsNullOrWhiteSpace(leftId) && !string.IsNullOrWhiteSpace(rightId))
            {
                return string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateVolume()
        {
            if (_audioPlayer == null || _audioPlayer.DebuggingId == -1) return;
            _audioPlayer.Volume = _headphoneEnabled ? _headphoneVolume : _trackVolume * _masterVolume;
        }
    }
}
