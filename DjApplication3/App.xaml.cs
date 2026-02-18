using DjApplication3.view.windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DjApplication3
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var errorToast = new ToastMessage(
                "Une erreur inattendue est survenue : " + e.Exception.Message,
                ToastMessage.ToastType.Error,
                e.Exception
            );
            errorToast.Show();

            e.Handled = true;
        }
    }
}
