using Microsoft.UI.Xaml;

namespace DjApplication3.WinUI
{
    public partial class App : Application
    {
        private Window? _window;
        public static Window? MainAppWindow { get; private set; }

        public App()
        {
            InitializeComponent();
            RequestedTheme = ApplicationTheme.Dark;
            UnhandledException += App_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                _window = new MainWindow();
                MainAppWindow = _window;
                _window.Activate();
            }
            catch (System.Exception ex)
            {
                LogUnhandledException(ex);
                _window = CreateErrorWindow(ex);
                MainAppWindow = _window;
                _window.Activate();
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
            e.Handled = true;
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
            e.SetObserved();
        }

        private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is System.Exception exception)
            {
                LogUnhandledException(exception);
            }
        }

        private static void LogUnhandledException(System.Exception exception)
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "winui-crash.log");
                System.IO.File.AppendAllText(logPath, $"{System.DateTime.Now:u} {exception}\n");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        private static Window CreateErrorWindow(System.Exception exception)
        {
            var window = new Window
            {
                Title = "DjApplication 3 - erreur"
            };

            window.Content = new Microsoft.UI.Xaml.Controls.Grid
            {
                Padding = new Microsoft.UI.Xaml.Thickness(24),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 23, 26, 30)),
                Children =
                {
                    new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = $"Demarrage impossible, mais l'application n'a pas crashe.\n\n{exception.Message}\n\nVoir winui-crash.log pour le detail.",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 246, 247, 248)),
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    }
                }
            };

            return window;
        }
    }
}
