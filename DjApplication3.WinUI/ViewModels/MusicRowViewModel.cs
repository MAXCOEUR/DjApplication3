using DjApplication3.model;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed class MusicRowViewModel : ObservableObject
    {
        private Musique _musique;
        private int? _bpm;
        private bool _downloaded;
        private bool _played;
        private bool _isDownloading;

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
                }
            }
        }

        public string DownloadText => IsDownloading ? "..." : Downloaded ? "OK" : "";
        public string PlayedText => Played ? "Oui" : "";

        public void UseResolvedMusic(Musique musique)
        {
            Musique = musique;
            Downloaded = true;
        }
    }
}
