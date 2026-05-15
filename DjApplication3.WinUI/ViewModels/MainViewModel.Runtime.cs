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
                Status = $"{errorPrefix}: {ex.Message}";
            }
        }
    }
}
