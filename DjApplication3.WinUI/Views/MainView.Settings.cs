using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DjApplication3.WinUI.Views
{
    public sealed partial class MainView
    {
        private void TrackCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (TrackCountCombo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out var count))
            {
                ViewModel.TrackCount = count;

                if (DeckScrollViewer != null)
                {
                    ViewModel.UpdateDeckHeights(DeckScrollViewer.ActualHeight);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.ToggleSettings();
                if (ViewModel.IsSettingsOpen)
                {
                    PopulateSettings(refreshDevices: true);
                }
                UpdateSettingsVisibility();
            }
            catch (System.Exception ex)
            {
                ViewModel.Status = $"Options indisponibles: {ex.Message}";
            }
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSettingsOpen = false;
            UpdateSettingsVisibility();
        }

        private void UpdateSettingsVisibility()
            => SettingsOverlay.Visibility = ViewModel.IsSettingsOpen ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateLibraryModeVisibility()
        {
            var hasNavigationColumn = ViewModel.IsLocalMode || ViewModel.IsYtMusicMode;

            LocalRootPanel.Visibility = ViewModel.IsLocalMode ? Visibility.Visible : Visibility.Collapsed;
            PlaylistTitlePanel.Visibility = ViewModel.IsYtMusicMode ? Visibility.Visible : Visibility.Collapsed;
            LocalFolderPanel.Visibility = ViewModel.IsLocalMode ? Visibility.Visible : Visibility.Collapsed;
            PlaylistPanel.Visibility = ViewModel.IsYtMusicMode ? Visibility.Visible : Visibility.Collapsed;
            LibraryResizeGrip.Visibility = hasNavigationColumn ? Visibility.Visible : Visibility.Collapsed;
            NavigationResizeColumn.Width = hasNavigationColumn
                ? new GridLength(6)
                : new GridLength(0);

            if (hasNavigationColumn)
            {
                ApplySavedLibraryNavigationWidth();
            }
            else
            {
                NavigationColumn.Width = new GridLength(0);
            }
        }

        private void PopulateSettings(bool refreshDevices = false)
        {
            if (refreshDevices)
            {
                ViewModel.RefreshDevicesForOptions();
            }

            AudioOutputCombo.Items.Clear();
            HeadphoneOutputCombo.Items.Clear();
            var audioDevices = ViewModel.Settings.AudioDevices;
            if (audioDevices != null)
            {
                foreach (var device in audioDevices)
                {
                    AudioOutputCombo.Items.Add(device);
                    HeadphoneOutputCombo.Items.Add(device);
                }
            }

            if (AudioOutputCombo.Items.Count > ViewModel.Settings.OutputDeviceIndex)
            {
                AudioOutputCombo.SelectedIndex = ViewModel.Settings.OutputDeviceIndex;
            }
            if (HeadphoneOutputCombo.Items.Count > ViewModel.Settings.HeadphoneDeviceIndex)
            {
                HeadphoneOutputCombo.SelectedIndex = ViewModel.Settings.HeadphoneDeviceIndex;
            }

            MidiCombo.Items.Clear();
            foreach (var midi in ViewModel.Settings.MidiDevices)
            {
                MidiCombo.Items.Add(midi.ProductName);
            }
            if (MidiCombo.Items.Count > ViewModel.Settings.MidiDeviceIndex)
            {
                MidiCombo.SelectedIndex = ViewModel.Settings.MidiDeviceIndex;
            }
        }

        private void AudioOutputCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (AudioOutputCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.OutputDeviceIndex = AudioOutputCombo.SelectedIndex;
            }
        }

        private void HeadphoneOutputCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (HeadphoneOutputCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.HeadphoneDeviceIndex = HeadphoneOutputCombo.SelectedIndex;
            }
        }

        private void MidiCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }
            if (MidiCombo.SelectedIndex >= 0)
            {
                ViewModel.Settings.MidiDeviceIndex = MidiCombo.SelectedIndex;
                ViewModel.RestartMidiController();
            }
        }
    }
}
