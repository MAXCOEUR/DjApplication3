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
                throw;
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
            e.Handled = true;
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
    }
}
