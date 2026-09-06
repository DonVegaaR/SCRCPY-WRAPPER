using System.Windows.Input;

namespace ScrcpyLauncher.App.Infrastructure;

public sealed class RelayCommand : ICommand
{
    private readonly Action? execute;
    private readonly Action<object?>? executeWithParameter;
    private readonly Func<bool>? canExecute;
    private readonly Func<object?, bool>? canExecuteWithParameter;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        executeWithParameter = execute;
        canExecuteWithParameter = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (canExecuteWithParameter is not null)
        {
            return canExecuteWithParameter(parameter);
        }

        return canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        if (executeWithParameter is not null)
        {
            executeWithParameter(parameter);
            return;
        }

        execute?.Invoke();
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
