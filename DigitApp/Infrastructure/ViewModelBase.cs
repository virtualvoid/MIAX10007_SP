using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DigitApp.Infrastructure;

/// <summary>
/// Zakladna trieda view modelov s podporou oznamovania zmien vlastnosti.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>
  /// Nastavi pole a oznami zmenu, ak sa hodnota naozaj zmenila.
  /// </summary>
  /// <param name="field">Pole, ktore sa nastavuje.</param>
  /// <param name="value">Nova hodnota.</param>
  /// <param name="name">Nazov vlastnosti, doplni sa automaticky.</param>
  /// <returns>True, ak sa hodnota zmenila.</returns>
  protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value))
    {
      return false;
    }
    field = value;
    OnPropertyChanged(name);
    return true;
  }

  /// <summary>
  /// Vyvola udalost <see cref="PropertyChanged"/> pre danu vlastnost.
  /// </summary>
  /// <param name="name">Nazov vlastnosti, doplni sa automaticky.</param>
  protected void OnPropertyChanged([CallerMemberName] string? name = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
