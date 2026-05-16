using DjApplication3.Infrastructure;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using WinRT.Interop;

namespace DjApplication3.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppPaths.EnsureRuntimeDirectories();
            Title = $"DjApplication 3 WinUI {GetAppVersion()}";
            MaximizeWindow();
            Closed += OnClosed;
        }

        private static string GetAppVersion()
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(MainWindow).Assembly.Location).FileVersion;
            return version != null ? $"v{version}" : "v2.0";
        }

        private void MaximizeWindow()
        {
            try
            {
                var windowHandle = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static void OnClosed(object sender, WindowEventArgs args)
        {
            try
            {
                AppPaths.CleanupTempMusicDirectory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
