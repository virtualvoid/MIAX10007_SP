using System.Windows.Input;

namespace DigitApp.Infrastructure;

/// <summary>
/// Asynchronny prikaz, ktory sa pocas behu nedovoli spustit znova. Delegat si musi osetrit vynimky sam
/// (inak by pri "async void" zhodili aplikaciu).
/// </summary>
/// <param name="execute">Asynchronna operacia, ktora sa vykona pri spusteni prikazu.</param>
/// <param name="canExecute">Podmienka dostupnosti prikazu, alebo null ak je vzdy dostupny.</param>
public sealed class AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
  /// <summary>
  /// Ci prave prebieha vykonavanie prikazu.
  /// </summary>
  private bool running;

  /// <inheritdoc />
  public event EventHandler? CanExecuteChanged
  {
    add => CommandManager.RequerySuggested += value;
    remove => CommandManager.RequerySuggested -= value;
  }

  /// <inheritdoc />
  public bool CanExecute(object? parameter) => !running && (canExecute?.Invoke() ?? true);

  /// <inheritdoc />
  public async void Execute(object? parameter)
  {
    running = true;
    CommandManager.InvalidateRequerySuggested();
    try
    {
      await execute();
    }
    finally
    {
      running = false;
      CommandManager.InvalidateRequerySuggested();
    }
  }
}
