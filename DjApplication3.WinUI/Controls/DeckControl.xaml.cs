using DjApplication3.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;

namespace DjApplication3.WinUI.Controls
{
    public sealed partial class DeckControl : UserControl
    {
        private DeckViewModel? _subscribedViewModel;

        public DeckControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private DeckViewModel? ViewModel => DataContext as DeckViewModel;

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (args.NewValue is DeckViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
                _subscribedViewModel = newVm;
            }
            UpdatePlayPauseButton();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeckViewModel.IsPlaying))
            {
                UpdatePlayPauseButton();
            }
        }

        private void UpdatePlayPauseButton()
        {
            PlayPauseButton.Content = ViewModel?.IsPlaying == true ? "Pause" : "Play";
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => ViewModel?.TogglePlayPause();

        private void StopButton_Click(object sender, RoutedEventArgs e) => ViewModel?.Stop();

        private void HeadphoneButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleHeadphone();

        private async void RandomButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.ShufflePlaylistAsync();
            }
        }

        private void Waveform_SeekRequested(object? sender, double e) => ViewModel?.Seek(e);
    }
}
