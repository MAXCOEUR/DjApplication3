using DjApplication3.model;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed class MusicRowViewModel : ObservableObject
    {
        private Musique _musique;
        private int? _bpm;
        private bool _downloaded;
        private bool _played;
        private bool _isDownloading;
        private bool _isPreviewing;
        private bool _isPreviewLoading;

        public MusicRowViewModel(Musique musique, int? bpm = null, bool downloaded = false)
        {
            _musique = musique;
            Title = musique.title;
            Author = musique.author;
            _bpm = bpm;
            _downloaded = downloaded;
        }

        public Musique Musique
        {
            get => _musique;
            private set => SetProperty(ref _musique, value);
        }

        public string Title { get; }
        public string Author { get; }

        public int? Bpm
        {
            get => _bpm;
            set
            {
                if (SetProperty(ref _bpm, value))
                {
                    OnPropertyChanged(nameof(BpmText));
                    OnPropertyChanged(nameof(HasBpm));
                }
            }
        }

        public string BpmText => Bpm?.ToString() ?? "";
        public bool HasBpm => Bpm.HasValue;

        public bool Downloaded
        {
            get => _downloaded;
            set
            {
                if (SetProperty(ref _downloaded, value))
                {
                    OnPropertyChanged(nameof(DownloadText));
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(DownloadText));
                }
            }
        }

        public bool Played
        {
            get => _played;
            set
            {
                if (SetProperty(ref _played, value))
                {
                    OnPropertyChanged(nameof(PlayedText));
                    OnPropertyChanged(nameof(RowBackground));
                }
            }
        }

        public string DownloadText => IsDownloading ? "..." : Downloaded ? "OK" : "";
        public string PlayedText => Played ? "Oui" : "";
        public SolidColorBrush RowBackground => Played
            ? new SolidColorBrush(Color.FromArgb(64, 220, 185, 55))
            : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public bool IsPreviewing
        {
            get => _isPreviewing;
            set
            {
                if (SetProperty(ref _isPreviewing, value))
                {
                    OnPropertyChanged(nameof(PreviewText));
                    OnPropertyChanged(nameof(PreviewButtonBackground));
                    OnPropertyChanged(nameof(PreviewButtonForeground));
                }
            }
        }

        public bool IsPreviewLoading
        {
            get => _isPreviewLoading;
            set
            {
                if (SetProperty(ref _isPreviewLoading, value))
                {
                    OnPropertyChanged(nameof(PreviewText));
                    OnPropertyChanged(nameof(PreviewButtonBackground));
                    OnPropertyChanged(nameof(PreviewButtonForeground));
                }
            }
        }

        public string PreviewText => IsPreviewLoading ? "..." : IsPreviewing ? "Stop" : "Cue";
        public SolidColorBrush PreviewButtonBackground => IsPreviewing
            ? new SolidColorBrush(Color.FromArgb(255, 0, 150, 80))
            : IsPreviewLoading
                ? new SolidColorBrush(Color.FromArgb(255, 64, 72, 82))
                : new SolidColorBrush(Color.FromArgb(255, 36, 40, 44));

        public SolidColorBrush PreviewButtonForeground => new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public void UseResolvedMusic(Musique musique)
        {
            Musique = musique;
            Downloaded = true;
        }
    }
}
