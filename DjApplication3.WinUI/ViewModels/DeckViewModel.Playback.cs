using DjApplication3.model;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class DeckViewModel
    {
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
                Debug.WriteLine($"UpdatePosition crash évité: {ex}");
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

                if (IsAutoNext && nextMusic is not null)
                {
                    var next = _nextDownloadedMusic ?? nextMusic;
                    _nextDownloadedMusic = null;

                    if (!File.Exists(next.url))
                    {
                        next = await _library.DownloadYtMusicAsync(next);
                    }

                    next.musiquesInPlayliste = playlist;
                    if (playlist != null)
                    {
                        MusicIdentity.ReplaceInPlaylist(playlist, nextMusic, next);
                    }

                    await SetMusicAsync(next);
                    Play();
                    return;
                }

                if (currentMusic is not null)
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
                Debug.WriteLine($"Erreur fin de musique: {ex}");
            }
            finally
            {
                _isHandlingTrackEnd = false;
            }
        }

        private static string Format(TimeSpan value)
            => $"{(int)value.TotalHours:D2}h {value.Minutes:D2}m {value.Seconds:D2}s";

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
                Debug.WriteLine($"{errorMessage}: {ex}");
            }
        }
    }
}
