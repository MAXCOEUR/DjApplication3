using DjApplication3.Infrastructure;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed class LocalFolderViewModel : ObservableObject, ILibrarySelectableItem
    {
        private bool _isSelected;
        private bool _isOpened;

        public LocalFolderViewModel(string name, string path, bool isParent = false)
        {
            Name = name;
            Path = path;
            IsParent = isParent;
        }

        public string Name { get; }

        public string Path { get; }

        public bool IsParent { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(RowBackground));
                    OnPropertyChanged(nameof(RowBorderBrush));
                    OnPropertyChanged(nameof(OpenMarkerBrush));
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
                    OnPropertyChanged(nameof(RowBackground));
                    OnPropertyChanged(nameof(RowBorderBrush));
                    OnPropertyChanged(nameof(OpenMarkerBrush));
                }
            }
        }

        public SolidColorBrush RowBackground
        {
            get
            {
                if (IsSelected && IsOpened)
                {
                    return new SolidColorBrush(Color.FromArgb(130, 0, 150, 180));
                }

                if (IsSelected)
                {
                    return new SolidColorBrush(Color.FromArgb(95, 70, 120, 160));
                }

                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        public SolidColorBrush RowBorderBrush
        {
            get
            {
                if (IsOpened)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 0, 229, 255));
                }

                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        public SolidColorBrush OpenMarkerBrush
        {
            get
            {
                if (IsOpened)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 0, 229, 255));
                }

                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }
    }
}