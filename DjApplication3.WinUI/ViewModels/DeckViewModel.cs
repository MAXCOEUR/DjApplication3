using DjApplication3.model;
using DjApplication3.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed class DeckViewModel : ObservableObject, IDisposable
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
        private double _deckHeight = 260;
        private sbyte[] _waveform = Array.Empty<sbyte>();
        private bool _isEndingSoon;
        private bool _isHandlingTrackEnd;
        private string _nextMusicPreview = "Aucune musique suivante";

        public event EventHandler<int>? BpmCalculated;

        public DeckViewModel(int trackNumber, IMusicLibraryService library, ISettingsService settings, DispatcherQueue dispatcherQueue)
        {
            TrackNumber = trackNumber;
            _library = library;
            _dispatcherQueue = dispatcherQueue;
            _audio = new CsCoreAudioPlayerService(settings);
            _audio.PositionChanged += (_, _) => _dispatcherQueue.TryEnqueue(UpdatePosition);
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
                    _audio.SetHeadphoneEnabled(IsHeadphone);
                    OnPropertyChanged(nameof(HeadphoneLabel));
                }
            }
        }

        public bool IsAutoNext { get => _isAutoNext; set { if (SetProperty(ref _isAutoNext, value)) _ = DownloadNextMusicAsync(); } }
        public bool HasMusic { get => _hasMusic; private set => SetProperty(ref _hasMusic, value); }
        public int Volume { get => _volume; set { if (SetProperty(ref _volume, value)) _audio.SetTrackVolume(value / 100.0f); } }
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
            ? new SolidColorBrush(Color.FromArgb(255, 0, 170, 80))      // vert quand ça joue
            : new SolidColorBrush(Color.FromArgb(255, 190, 45, 45));    // rouge quand ça ne joue pas

        public SolidColorBrush PlayButtonForeground =>
            new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public bool IsEndingSoon
        {
            get => _isEndingSoon;
            private set => SetProperty(ref _isEndingSoon, value);
        }

        public async Task<int> SetMusicAsync(Musique musique)
        {
            if (_audio.IsPlaying)
            {
                _audio.Stop();
                IsPlaying = false;
            }

            if (musique == null)
            {
                return 2;
            }

            _isHandlingTrackEnd = false;

            _currentMusic = musique;
            UpdateNextMusicPreview();
            _audio.Load(musique);
            HasMusic = true;
            Title = $"{musique.title} ({musique.author})";
            Bpm = "000 BPM";
            UpdatePosition();
            _ = LoadBpmAsync(musique);
            _ = LoadWaveAsync(musique);
            await DownloadNextMusicAsync();
            return 0;
        }

        public void Play()
        {
            _audio.Play();
            IsPlaying = _audio.IsPlaying;
        }

        public void Pause()
        {
            _audio.Pause();
            IsPlaying = false;
        }

        public void TogglePlayPause()
        {
            if (_audio.IsPlaying) Pause();
            else Play();
        }

        public void Stop()
        {
            _audio.Stop();
            _isHandlingTrackEnd = false;
            _currentMusic = null;
            Waveform = Array.Empty<sbyte>();
            Title = "Aucune musique";
            HasMusic = false;
            IsPlaying = false;
            IsEndingSoon = false;
            UpdatePosition();
        }

        public void ToggleHeadphone() => IsHeadphone = !IsHeadphone;

        public void SetMasterVolume(float volume) => _audio.SetMasterVolume(volume);

        public void SetHeadphoneVolume(float volume) => _audio.SetHeadphoneVolume(volume);

        public void Seek(double ratio) => _audio.Seek(ratio);

        public void ChangePosition(bool isForward)
        {
            if (IsPlaying) return;
            _audio.ChangePosition(isForward);
        }

        public async Task ShufflePlaylistAsync()
        {
            if (_currentMusic?.musiquesInPlayliste == null) return;
            var list = _currentMusic.musiquesInPlayliste;
            Shuffle(list);
            list.Remove(_currentMusic);
            if (_nextDownloadedMusic != null)
            {
                list.Remove(_nextDownloadedMusic);
                list.Insert(0, _nextDownloadedMusic);
            }
            list.Insert(0, _currentMusic);
            await DownloadNextMusicAsync();
            UpdateNextMusicPreview();
        }

        private async Task LoadBpmAsync(Musique musique)
        {
            var bpm = await _library.GetBpmAsync(musique);
            _dispatcherQueue.TryEnqueue(() =>
            {
                Bpm = $"{bpm} BPM";
                BpmCalculated?.Invoke(this, bpm);
            });
        }

        private async Task LoadWaveAsync(Musique musique)
        {
            var waveform = await _library.GetWaveAsync(musique);
            _dispatcherQueue.TryEnqueue(() => Waveform = waveform);
        }

        private async Task DownloadNextMusicAsync()
        {
            if (!IsAutoNext || _currentMusic?.musiquesInPlayliste == null)
            {
                UpdateNextMusicPreview();
                return;
            }

            var next = GetNextMusic();

            if (next == null)
            {
                UpdateNextMusicPreview();
                return;
            }

            _nextDownloadedMusic = System.IO.File.Exists(next.url)
                ? next
                : await _library.DownloadYtMusicAsync(next);

            if (_nextDownloadedMusic != null)
            {
                _nextDownloadedMusic.musiquesInPlayliste = _currentMusic.musiquesInPlayliste;
            }

            UpdateNextMusicPreview();
        }

        private Musique? GetNextMusic()
        {
            if (_currentMusic?.musiquesInPlayliste == null) return null;
            var currentIndex = _currentMusic.musiquesInPlayliste.IndexOf(_currentMusic);
            return currentIndex >= 0 && currentIndex < _currentMusic.musiquesInPlayliste.Count - 1
                ? _currentMusic.musiquesInPlayliste[currentIndex + 1]
                : null;
        }

        private void UpdatePosition()
        {
            try
            {
                var duration = _audio.Duration;
                var position = _audio.Position;

                if (duration < TimeSpan.Zero)
                {
                    duration = TimeSpan.Zero;
                }

                if (position < TimeSpan.Zero)
                {
                    position = TimeSpan.Zero;
                }

                if (position > duration && duration > TimeSpan.Zero)
                {
                    position = duration;
                }

                var remaining = duration - position;

                if (remaining < TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                }

                PositionRatio = Math.Clamp(_audio.PositionRatio, 0f, 1f);
                CurrentTime = Format(position);
                TotalTime = Format(duration);
                RemainingTime = Format(remaining);
                IsPlaying = _audio.IsPlaying;

                IsEndingSoon =
                    HasMusic &&
                    duration > TimeSpan.Zero &&
                    remaining.TotalSeconds <= 30 &&
                    remaining.TotalSeconds > 0;

                var isAtEnd =
                    HasMusic &&
                    duration > TimeSpan.Zero &&
                    remaining.TotalMilliseconds <= 250;

                if (isAtEnd && !_isHandlingTrackEnd)
                {
                    _isHandlingTrackEnd = true;
                    IsEndingSoon = false;
                    _ = HandleTrackEndAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePosition crash évité: {ex}");
            }
        }

        private async Task HandleTrackEndAsync()
        {
            try
            {
                _audio.Stop();
                IsPlaying = false;
                PositionRatio = 0f;

                if (IsAutoNext && _nextDownloadedMusic != null)
                {
                    var next = _nextDownloadedMusic;
                    _nextDownloadedMusic = null;

                    await SetMusicAsync(next);

                    if (IsAutoNext)
                    {
                        Play();
                    }
                }
                else
                {
                    await SetMusicAsync(_currentMusic);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur fin de musique: {ex}");
            }
            finally
            {
                _isHandlingTrackEnd = false;
            }
        }

        private static string Format(TimeSpan value)
            => $"{(int)value.TotalHours:D2}h {value.Minutes:D2}m {value.Seconds:D2}s";

        private static void Shuffle<T>(IList<T> list)
        {
            var random = new Random();
            for (var n = list.Count; n > 1;)
            {
                n--;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private void UpdateNextMusicPreview()
        {
            if (_currentMusic?.musiquesInPlayliste == null || _currentMusic.musiquesInPlayliste.Count == 0)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            var playlist = _currentMusic.musiquesInPlayliste;
            var currentIndex = playlist.IndexOf(_currentMusic);

            if (currentIndex < 0 || currentIndex >= playlist.Count - 1)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            var nextMusics = playlist
                .Skip(currentIndex + 1)
                .Take(5)
                .Select((music, index) =>
                {
                    var title = string.IsNullOrWhiteSpace(music.title) ? "Titre inconnu" : music.title;
                    var author = string.IsNullOrWhiteSpace(music.author) ? "Artiste inconnu" : music.author;

                    return $"{index + 1}. {title} - {author}";
                })
                .ToList();

            if (nextMusics.Count == 0)
            {
                NextMusicPreview = "Aucune musique suivante";
                return;
            }

            NextMusicPreview = "Musiques suivantes :\n" + string.Join("\n", nextMusics);
        }

        public void Dispose() => _audio.Dispose();
    }
}
