using DjApplication3.Infrastructure;
using DjApplication3.model;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.UI;
using System.Windows.Input;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class DeckViewModel : ObservableObject, IDisposable
    {
        private readonly IMusicLibraryService _library;
        private readonly IAudioPlayerService _audio;
        private readonly DispatcherQueue _dispatcherQueue;
        private Musique? _currentMusic;
        private Musique? _nextDownloadedMusic;
        private string _title = "Aucune musique";
        private string _bpm = "000 BPM";
        private string _currentTime = "00h 00m 00s";
        private string _totalTime = "00h 00m 00s";
        private string _remainingTime = "00h 00m 00s";
        private float _positionRatio;
        private bool _isPlaying;
        private bool _isHeadphone;
        private bool _isAutoNext;
        private bool _hasMusic;
        private int _volume = 100;
        private double _bassDb;
        private double _midDb;
        private double _trebleDb;
        private double _pitchPercent;
        private double _pitchPreviewPercent;
        private int? _baseBpm;
        private double _deckHeight = 260;
        private sbyte[] _waveform = Array.Empty<sbyte>();
        private bool _isWaveformLoading;
        private bool _isEndingSoon;
        private bool _isHandlingTrackEnd;
        private bool _playedEnoughReported;
        private DateTime? _lastPlaybackUpdateUtc;
        private readonly DispatcherQueueTimer _pitchCommitTimer;
        private TimeSpan _listenedDuration = TimeSpan.Zero;
        private string _nextMusicPreview = "Aucune musique suivante";
        private int _waveformLoadVersion;

        public event EventHandler<int>? BpmCalculated;
        public event EventHandler<Musique>? PlayedEnough;

        // Commands for UI bindings
        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ToggleHeadphoneCommand { get; }
        public ICommand ResetEqualizerCommand { get; }
        public ICommand RandomCommand { get; }
        public ICommand SeekCommand { get; }

        public DeckViewModel(int trackNumber, IMusicLibraryService library, ISettingsService settings, DispatcherQueue dispatcherQueue)
        {
            TrackNumber = trackNumber;
            _library = library;
            _dispatcherQueue = dispatcherQueue;
            _audio = new CsCoreAudioPlayerService(settings);
            _pitchCommitTimer = dispatcherQueue.CreateTimer();
            _pitchCommitTimer.Interval = TimeSpan.FromMilliseconds(140);
            _pitchCommitTimer.Tick += (_, _) =>
            {
                _pitchCommitTimer.Stop();
                CommitPitch();
            };
            _audio.PositionChanged += (_, _) =>
            {
                try
                {
                    var enqueued = _dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, UpdatePosition);
                    if (!enqueued)
                    {
                        AppLogger.Warning(new InvalidOperationException("DispatcherQueue.TryEnqueue returned false."), $"Position update enqueue failed on deck {TrackNumber}");
                        Debug.WriteLine($"[DeckViewModel] Dispatcher.TryEnqueue(High) failed for deck {TrackNumber}");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning(ex, $"Position changed handler failed on deck {TrackNumber}");
                    Debug.WriteLine($"[DeckViewModel] PositionChanged handler exception: {ex}");
                }
            };
            _audio.PlaybackStopped += (_, _) => _dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, UpdatePosition);

            PlayPauseCommand = new RelayCommand(_ => TogglePlayPause());
            StopCommand = new RelayCommand(_ => Stop());
            ToggleHeadphoneCommand = new RelayCommand(_ => ToggleHeadphone());
            ResetEqualizerCommand = new RelayCommand(_ => ResetEqualizer());
            RandomCommand = new RelayCommand(async _ => await ShufflePlaylistAsync());
            SeekCommand = new RelayCommand(param =>
            {
                if (param is double d) Seek(d);
                else if (param != null && double.TryParse(param.ToString(), out var v)) Seek(v);
            });
        }

        public int TrackNumber { get; }
        public Musique? CurrentMusic => _currentMusic;
        public string Title { get => _title; private set => SetProperty(ref _title, value); }
        public string Bpm { get => _bpm; private set => SetProperty(ref _bpm, value); }
        public string CurrentTime { get => _currentTime; private set => SetProperty(ref _currentTime, value); }
        public string TotalTime { get => _totalTime; private set => SetProperty(ref _totalTime, value); }
        public string RemainingTime { get => _remainingTime; private set => SetProperty(ref _remainingTime, value); }
        public float PositionRatio { get => _positionRatio; private set => SetProperty(ref _positionRatio, value); }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    OnPropertyChanged(nameof(PlayStateLabel));
                    OnPropertyChanged(nameof(PlayButtonBackground));
                    OnPropertyChanged(nameof(PlayButtonForeground));
                }
            }
        }

        public bool IsHeadphone
        {
            get => _isHeadphone;
            set
            {
                if (SetProperty(ref _isHeadphone, value))
                {
                    Debug.WriteLine($"[DeckViewModel] IsHeadphone set to {value} on deck {TrackNumber}");
                    TryAudio(() => _audio.SetHeadphoneEnabled(IsHeadphone), "Sortie casque impossible");
                    OnPropertyChanged(nameof(HeadphoneLabel));
                }
            }
        }

        public bool IsAutoNext { get => _isAutoNext; set { if (SetProperty(ref _isAutoNext, value)) _ = PreloadNextMusicAsync(); } }
        public bool HasMusic { get => _hasMusic; private set => SetProperty(ref _hasMusic, value); }
        public int Volume { get => _volume; set { if (SetProperty(ref _volume, value)) TryAudio(() => _audio.SetTrackVolume(value / 100.0f), "Volume indisponible"); } }
        public double BassDb { get => _bassDb; set { if (SetProperty(ref _bassDb, Math.Clamp(value, -12, 12))) ApplyEqualizer(); } }
        public double MidDb { get => _midDb; set { if (SetProperty(ref _midDb, Math.Clamp(value, -12, 12))) ApplyEqualizer(); } }
        public double TrebleDb { get => _trebleDb; set { if (SetProperty(ref _trebleDb, Math.Clamp(value, -12, 12))) ApplyEqualizer(); } }
        public double PitchPercent
        {
            get => _pitchPercent;
            private set
            {
                var bounded = Math.Clamp(value, -25, 25);
                if (SetProperty(ref _pitchPercent, bounded))
                {
                    TryAudio(() => _audio.SetPlaybackRate((float)(1.0 + bounded / 100.0)), "Pitch indisponible");
                }
            }
        }

        public double PitchPreviewPercent
        {
            get => _pitchPreviewPercent;
            set
            {
                var bounded = Math.Clamp(value, -25, 25);
                if (SetProperty(ref _pitchPreviewPercent, bounded))
                {
                    OnPropertyChanged(nameof(PitchText));
                    UpdatePitchAdjustedBpm();
                }
            }
        }

        public double DeckHeight { get => _deckHeight; set => SetProperty(ref _deckHeight, value); }
        public sbyte[] Waveform { get => _waveform; private set => SetProperty(ref _waveform, value); }
        public bool IsWaveformLoading { get => _isWaveformLoading; private set => SetProperty(ref _isWaveformLoading, value); }
        public string HeadphoneLabel => IsHeadphone ? "Casque ON" : "Casque OFF";
        public string PlayStateLabel => IsPlaying ? "Pause" : "Play";
        public string PitchText => PitchPreviewPercent >= 0
            ? $"+{PitchPreviewPercent:0.#}%"
            : $"{PitchPreviewPercent:0.#}%";
        public double? EffectiveBpm => _baseBpm.HasValue
            ? Math.Max(1, _baseBpm.Value * (1.0 + PitchPreviewPercent / 100.0))
            : null;

        public string NextMusicPreview
        {
            get => _nextMusicPreview;
            private set => SetProperty(ref _nextMusicPreview, value);
        }

        public SolidColorBrush PlayButtonBackground => IsPlaying
            ? new SolidColorBrush(Color.FromArgb(255, 0, 170, 80))
            : new SolidColorBrush(Color.FromArgb(255, 190, 45, 45));

        public SolidColorBrush PlayButtonForeground =>
            new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public bool IsEndingSoon
        {
            get => _isEndingSoon;
            private set => SetProperty(ref _isEndingSoon, value);
        }

        public void Dispose()
        {
            _pitchCommitTimer.Stop();
            _audio.Dispose();
        }

        public void ResetEqualizer()
        {
            BassDb = 0;
            MidDb = 0;
            TrebleDb = 0;
        }

        public void CommitPitch()
        {
            PitchPercent = PitchPreviewPercent;
        }

        public void ResetPitch()
        {
            PitchPreviewPercent = 0;
            CommitPitch();
        }

        public void AdjustPitchFromMidi(int midiValue)
        {
            var delta = GetPitchDeltaFromMidi(midiValue);
            if (Math.Abs(delta) < 0.001)
            {
                return;
            }

            PitchPreviewPercent += delta;
            QueuePitchCommit();
        }

        public void NudgePitchFromButton(int direction)
        {
            PitchPreviewPercent += Math.Sign(direction) * 0.5;
            CommitPitch();
        }

        public void SyncPitchTo(DeckViewModel? targetDeck)
        {
            if (!_baseBpm.HasValue || targetDeck?.EffectiveBpm is not double targetBpm)
            {
                return;
            }

            PitchPreviewPercent = Math.Clamp((targetBpm / _baseBpm.Value - 1.0) * 100.0, -25, 25);
            CommitPitch();
        }

        private void SetBaseBpm(int bpm)
        {
            _baseBpm = bpm;
            UpdatePitchAdjustedBpm();
        }

        private void ClearBaseBpm(string fallbackText)
        {
            _baseBpm = null;
            Bpm = fallbackText;
        }

        private void UpdatePitchAdjustedBpm()
        {
            if (!_baseBpm.HasValue)
            {
                return;
            }

            var adjustedBpm = Math.Max(1, (int)Math.Round(_baseBpm.Value * (1.0 + PitchPreviewPercent / 100.0)));
            Bpm = $"{adjustedBpm} BPM";
        }

        private void QueuePitchCommit()
        {
            _pitchCommitTimer.Stop();
            _pitchCommitTimer.Start();
        }

        private static double GetPitchDeltaFromMidi(int midiValue)
        {
            if (midiValue <= 0)
            {
                return 0;
            }

            if (midiValue <= 63)
            {
                return GetPitchStep(midiValue);
            }

            return -GetPitchStep(128 - midiValue);
        }

        private static double GetPitchStep(int strength)
        {
            var normalized = Math.Clamp(strength, 1, 63) / 63.0;
            return 0.08 + Math.Pow(normalized, 1.35) * 1.15;
        }

        private void ApplyEqualizer()
            => TryAudio(() => _audio.SetEqualizer((float)BassDb, (float)MidDb, (float)TrebleDb), "EQ indisponible");
    }
}
