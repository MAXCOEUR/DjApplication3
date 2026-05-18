using DjApplication3.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
using Windows.System;

namespace DjApplication3.WinUI.Controls
{
    public sealed partial class DeckControl : UserControl
    {
        public DeckControl()
        {
            InitializeComponent();
        }

        private void Waveform_SeekRequested(object? sender, double e)
        {
            if (DataContext is DeckViewModel vm)
            {
                vm.Seek(e);
            }
        }

        private void PitchSlider_Commit(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeckViewModel vm)
            {
                vm.CommitPitch();
            }
        }

        private void PitchSlider_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key is VirtualKey.Left
                or VirtualKey.Right
                or VirtualKey.Up
                or VirtualKey.Down
                or VirtualKey.Home
                or VirtualKey.End
                or VirtualKey.PageUp
                or VirtualKey.PageDown
                or VirtualKey.Enter
                or VirtualKey.Space)
            {
                if (DataContext is DeckViewModel vm)
                {
                    vm.CommitPitch();
                }
            }
        }

        private void PitchSlider_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (DataContext is DeckViewModel vm)
            {
                vm.ResetPitch();
                e.Handled = true;
            }
        }
    }
}
