using DjApplication3.Infrastructure;
using System;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
        private async Task RunSafeAsync(Task task, string errorPrefix)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Status = "Operation annulee";
            }
            catch (Exception ex)
            {
                ReportError($"{errorPrefix}: {ex.Message}", ex, errorPrefix);
            }
        }

        private void ReportError(string statusMessage, Exception exception, string context)
        {
            Status = statusMessage;
            AppLogger.Error(exception, context);
        }
    }
}
