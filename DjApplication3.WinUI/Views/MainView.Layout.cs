using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;

namespace DjApplication3.WinUI.Views
{
    public sealed partial class MainView
    {
        private const double DefaultLibraryNavigationWidth = 190;
        private const double MinLibraryNavigationWidth = 120;
        private const double MaxLibraryNavigationWidth = 420;
        private const double ResizeGripWidth = 6;
        private const double MinimumMusicColumnWidth = 220;
        private const double MinDeckAreaWidth = 420;
        private const double MinimumLibraryPanelWidth = 360;
        private bool _isLibraryResizeDragging;
        private double _libraryResizeStartX;
        private double _libraryResizeStartWidth;
        private bool _isMainResizeDragging;
        private double _mainResizeStartX;
        private double _mainResizeStartWidth;

        private void ApplySavedDeckAreaWidth()
        {
            if (ViewModel.Settings.DeckAreaWidth > 0)
            {
                SetDeckAreaWidth(ViewModel.Settings.DeckAreaWidth, persist: false);
            }
        }

        private void SetDeckAreaWidth(double width, bool persist)
        {
            var boundedWidth = ClampDeckAreaWidth(width);
            DeckAreaColumn.Width = new GridLength(boundedWidth);

            if (persist)
            {
                ViewModel.Settings.DeckAreaWidth = boundedWidth;
            }
        }

        private double ClampDeckAreaWidth(double requestedWidth)
        {
            var maxWidth = Math.Max(MinDeckAreaWidth, requestedWidth);
            if (MainContentGrid?.ActualWidth > 0)
            {
                maxWidth = Math.Max(
                    MinDeckAreaWidth,
                    MainContentGrid.ActualWidth - MinimumLibraryPanelWidth - ResizeGripWidth);
            }

            return Math.Clamp(requestedWidth, MinDeckAreaWidth, maxWidth);
        }

        private void ApplySavedLibraryNavigationWidth()
            => SetLibraryNavigationWidth(ViewModel.Settings.LibraryNavigationWidth, persist: false);

        private void SetLibraryNavigationWidth(double width, bool persist)
        {
            var boundedWidth = ClampLibraryNavigationWidth(width);
            NavigationColumn.Width = new GridLength(boundedWidth);

            if (persist)
            {
                ViewModel.Settings.LibraryNavigationWidth = boundedWidth;
            }
        }

        private double ClampLibraryNavigationWidth(double requestedWidth)
        {
            var maxWidth = MaxLibraryNavigationWidth;
            if (LibraryListsGrid?.ActualWidth > 0)
            {
                maxWidth = Math.Min(
                    MaxLibraryNavigationWidth,
                    Math.Max(MinLibraryNavigationWidth, LibraryListsGrid.ActualWidth - MinimumMusicColumnWidth - ResizeGripWidth));
            }

            return Math.Clamp(requestedWidth, MinLibraryNavigationWidth, maxWidth);
        }

        private void LibraryListsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel.IsLocalMode || ViewModel.IsYtMusicMode)
            {
                SetLibraryNavigationWidth(NavigationColumn.ActualWidth > 0
                    ? NavigationColumn.ActualWidth
                    : ViewModel.Settings.LibraryNavigationWidth, persist: false);
            }
        }

        private void MainContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DeckAreaColumn.Width.IsAbsolute)
            {
                SetDeckAreaWidth(DeckAreaColumn.ActualWidth > 0
                    ? DeckAreaColumn.ActualWidth
                    : ViewModel.Settings.DeckAreaWidth, persist: false);
            }
        }

        private void MainResizeGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isMainResizeDragging = true;
            _mainResizeStartX = e.GetCurrentPoint(MainContentGrid).Position.X;
            _mainResizeStartWidth = DeckAreaColumn.ActualWidth > 0
                ? DeckAreaColumn.ActualWidth
                : MainContentGrid.ActualWidth * 0.68;

            MainResizeGrip.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void MainResizeGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isMainResizeDragging)
            {
                return;
            }

            var currentX = e.GetCurrentPoint(MainContentGrid).Position.X;
            SetDeckAreaWidth(_mainResizeStartWidth + currentX - _mainResizeStartX, persist: false);
            e.Handled = true;
        }

        private void MainResizeGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isMainResizeDragging)
            {
                return;
            }

            _isMainResizeDragging = false;
            MainResizeGrip.ReleasePointerCapture(e.Pointer);
            SetDeckAreaWidth(DeckAreaColumn.ActualWidth, persist: true);
            e.Handled = true;
        }

        private void MainResizeGrip_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _isMainResizeDragging = false;
            MainResizeGrip.ReleasePointerCapture(e.Pointer);
        }

        private void LibraryResizeGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!ViewModel.IsLocalMode && !ViewModel.IsYtMusicMode)
            {
                return;
            }

            _isLibraryResizeDragging = true;
            _libraryResizeStartX = e.GetCurrentPoint(LibraryListsGrid).Position.X;
            _libraryResizeStartWidth = NavigationColumn.ActualWidth > 0
                ? NavigationColumn.ActualWidth
                : DefaultLibraryNavigationWidth;

            LibraryResizeGrip.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void LibraryResizeGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isLibraryResizeDragging)
            {
                return;
            }

            var currentX = e.GetCurrentPoint(LibraryListsGrid).Position.X;
            SetLibraryNavigationWidth(_libraryResizeStartWidth + currentX - _libraryResizeStartX, persist: false);
            e.Handled = true;
        }

        private void LibraryResizeGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isLibraryResizeDragging)
            {
                return;
            }

            _isLibraryResizeDragging = false;
            LibraryResizeGrip.ReleasePointerCapture(e.Pointer);
            SetLibraryNavigationWidth(NavigationColumn.ActualWidth, persist: true);
            e.Handled = true;
        }

        private void LibraryResizeGrip_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _isLibraryResizeDragging = false;
            LibraryResizeGrip.ReleasePointerCapture(e.Pointer);
        }
    }
}
