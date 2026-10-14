using System.Windows.Input;

namespace DigitApp.Infrastructure;

/// <summary>
/// Prikaz, ktory vola delegata. O dostupnosti sa WPF pyta samo (CommandManager.RequerySuggested).
/// </summary>
/// <param name="execute">Operacia, ktora sa vykona pri spusteni prikazu.</param>
/// <param name="canExecute">Podmienka dostupnosti prikazu, alebo null ak je vzdy dostupny.</param>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
  /// <inheritdoc />
  public event EventHandler? CanExecuteChanged
  {
    add => CommandManager.RequerySuggested += value;
    remove => CommandManager.RequerySuggested -= value;
  }

  /// <inheritdoc />
  public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

  /// <inheritdoc />
  public void Execute(object? parameter) => execute();
}
