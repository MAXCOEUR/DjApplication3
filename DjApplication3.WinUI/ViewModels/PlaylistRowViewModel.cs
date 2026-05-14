using DjApplication3.Infrastructure;
using DjApplication3.model;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed class PlaylistRowViewModel : ObservableObject, ILibrarySelectableItem
    {
        private bool _isSelected;
        private bool _isOpened;

        public PlaylistRowViewModel(PlayListe playlist)
        {
            Playlist = playlist;
        }

        public PlayListe Playlist { get; }

        public string Id => Playlist.id;

        public string Name => Playlist.name;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                }
            }
        }

        public bool IsOpened
        {
            get => _isOpened;
            set
            {
                if (SetProperty(ref _isOpened, value))
                {
                }
            }
        }
    }
}