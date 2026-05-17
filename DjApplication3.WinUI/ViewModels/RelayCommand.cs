using DjApplication3.Infrastructure;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DjApplication3.WinUI.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _execute;
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            try
            {
                if (_execute != null)
                {
                    _execute(parameter);
                    return;
                }

                if (_executeAsync != null)
                {
                    await _executeAsync(parameter).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Command execution failed");
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
