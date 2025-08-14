using System.Windows.Input;

namespace SpectriumLive.Wpf.Commands;

public class RelayCommand(
    Action<object?> execute,
    Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Action<object?> execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<object?>? canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return this.canExecute == null || this.canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        this.execute(parameter);
    }
}
