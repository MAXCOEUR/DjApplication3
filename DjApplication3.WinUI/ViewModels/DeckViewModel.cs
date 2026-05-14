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
            _audio.PlaybackStopped += (_, _) => _dispatcherQueue.TryEnqueue(UpdatePosition);
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

        public Task<int> SetMusicAsync(Musique? musique)
        {
            if (musique == null)
            {
                return Task.FromResult(2);
            }

            try
            {
                if (_audio.IsPlaying)
                {
                    _audio.Stop();
                    IsPlaying = false;
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
                _ = PreloadNextMusicAsync();
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                IsEndingSoon = false;
                NextMusicPreview = $"Chargement impossible: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Chargement musique impossible: {ex}");
                return Task.FromResult(1);
            }
        }

        public void Play()
        {
            TryAudio(() =>
            {
                _audio.Play();
                IsPlaying = _audio.IsPlaying;
            }, "Lecture impossible");
        }

        public void Pause()
        {
            TryAudio(_audio.Pause, "Pause impossible");
            IsPlaying = false;
        }

        public void TogglePlayPause()
        {
            if (_audio.IsPlaying) Pause();
            else Play();
        }

        public void Stop()
        {
            TryAudio(_audio.Stop, "Arret impossible");
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

        public void SetMasterVolume(float volume) => TryAudio(() => _audio.SetMasterVolume(volume), "Volume master indisponible");

        public void SetHeadphoneVolume(float volume) => TryAudio(() => _audio.SetHeadphoneVolume(volume), "Volume casque indisponible");

        public void Seek(double ratio) => TryAudio(() => _audio.Seek(ratio), "Deplacement impossible");

        public void ChangePosition(bool isForward)
        {
            if (IsPlaying) return;
            TryAudio(() => _audio.ChangePosition(isForward), "Scratch impossible");
        }

        public async Task ShufflePlaylistAsync()
        {
            try
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
                await PreloadNextMusicAsync();
                UpdateNextMusicPreview();
            }
            catch (Exception ex)
            {
                NextMusicPreview = $"Shuffle impossible: {ex.Message}";
            }
        }

        private async Task LoadBpmAsync(Musique musique)
        {
            try
            {
                var bpm = await _library.GetBpmAsync(musique);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Bpm = $"{bpm} BPM";
                    BpmCalculated?.Invoke(this, bpm);
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => Bpm = "BPM --");
                System.Diagnostics.Debug.WriteLine($"BPM impossible: {ex}");
            }
        }

        private async Task LoadWaveAsync(Musique musique)
        {
            try
            {
                var waveform = await _library.GetWaveAsync(musique);
                _dispatcherQueue.TryEnqueue(() => Waveform = waveform);
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => Waveform = Array.Empty<sbyte>());
                System.Diagnostics.Debug.WriteLine($"Waveform impossible: {ex}");
            }
        }

        private async Task DownloadNextMusicAsync()
        {
            var playlist = _currentMusic?.musiquesInPlayliste;
            if (!IsAutoNext || playlist == null)
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
                _nextDownloadedMusic.musiquesInPlayliste = playlist;
                ReplacePlaylistMusic(playlist, next, _nextDownloadedMusic);
            }

            UpdateNextMusicPreview();
        }

        private async Task PreloadNextMusicAsync()
        {
            try
            {
                await DownloadNextMusicAsync();
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    NextMusicPreview = $"Prechargement impossible: {ex.Message}";
                });
                System.Diagnostics.Debug.WriteLine($"Prechargement auto impossible: {ex}");
            }
        }

        private Musique? GetNextMusic()
        {
            if (_currentMusic?.musiquesInPlayliste == null) return null;
            var playlist = _currentMusic.musiquesInPlayliste;
            var currentIndex = FindMusicIndex(playlist, _currentMusic);
            return currentIndex >= 0 && currentIndex < playlist.Count - 1
                ? playlist[currentIndex + 1]
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
                var currentMusic = _currentMusic;
                var playlist = _currentMusic?.musiquesInPlayliste;
                var nextMusic = GetNextMusic();
                _audio.Stop();
                IsPlaying = false;
                PositionRatio = 0f;

                if (IsAutoNext && nextMusic != null)
                {
                    var next = IsAutoNext && _nextDownloadedMusic != null
                        ? _nextDownloadedMusic
                        : nextMusic;

                    _nextDownloadedMusic = null;

                    if (next != null && !System.IO.File.Exists(next.url))
                    {
                        next = await _library.DownloadYtMusicAsync(next);
                    }

                    if (next != null)
                    {
                        next.musiquesInPlayliste = playlist;
                        if (playlist != null && nextMusic != null)
                        {
                            ReplacePlaylistMusic(playlist, nextMusic, next);
                        }
                        await SetMusicAsync(next);
                        Play();
                        return;
                    }
                }

                if (currentMusic != null)
                {
                    _nextDownloadedMusic = null;
                    await SetMusicAsync(currentMusic);
                    IsPlaying = false;
                    PositionRatio = 0f;
                    IsEndingSoon = false;
                    return;
                }

                Stop();
            }
            catch (Exception ex)
            {
                NextMusicPreview = $"Auto impossible: {ex.Message}";
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
            var currentIndex = FindMusicIndex(playlist, _currentMusic);

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

        private static void ReplacePlaylistMusic(IList<Musique> playlist, Musique oldMusic, Musique newMusic)
        {
            var index = FindMusicIndex(playlist, oldMusic);
            if (index >= 0)
            {
                newMusic.musiquesInPlayliste = playlist as List<Musique> ?? playlist.ToList();
                playlist[index] = newMusic;
            }
        }

        private static int FindMusicIndex(IList<Musique> playlist, Musique music)
        {
            for (var i = 0; i < playlist.Count; i++)
            {
                if (ReferenceEquals(playlist[i], music)
                    || playlist[i] == music
                    || SameTrack(playlist[i], music))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool SameTrack(Musique first, Musique second)
            => string.Equals(first.title, second.title, StringComparison.OrdinalIgnoreCase)
               && string.Equals(first.author, second.author, StringComparison.OrdinalIgnoreCase);

        private void TryAudio(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                NextMusicPreview = $"{errorMessage}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"{errorMessage}: {ex}");
            }
        }

        public void Dispose() => _audio.Dispose();
    }
}
