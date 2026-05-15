using DjApplication3.model;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using System;
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
        private double _deckHeight = 260;
        private sbyte[] _waveform = Array.Empty<sbyte>();
        private bool _isEndingSoon;
        private bool _isHandlingTrackEnd;
        private bool _playedEnoughReported;
        private DateTime? _lastPlaybackUpdateUtc;
        private TimeSpan _listenedDuration = TimeSpan.Zero;
        private string _nextMusicPreview = "Aucune musique suivante";

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
            _audio.PositionChanged += (_, _) => _dispatcherQueue.TryEnqueue(UpdatePosition);
            _audio.PlaybackStopped += (_, _) => _dispatcherQueue.TryEnqueue(UpdatePosition);

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
        public double DeckHeight { get => _deckHeight; set => SetProperty(ref _deckHeight, value); }
        public sbyte[] Waveform { get => _waveform; private set => SetProperty(ref _waveform, value); }
        public string HeadphoneLabel => IsHeadphone ? "Casque ON" : "Casque OFF";
        public string PlayStateLabel => IsPlaying ? "Pause" : "Play";

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

        public void Dispose() => _audio.Dispose();

        public void ResetEqualizer()
        {
            BassDb = 0;
            MidDb = 0;
            TrebleDb = 0;
        }

        private void ApplyEqualizer()
            => TryAudio(() => _audio.SetEqualizer((float)BassDb, (float)MidDb, (float)TrebleDb), "EQ indisponible");
    }
}
