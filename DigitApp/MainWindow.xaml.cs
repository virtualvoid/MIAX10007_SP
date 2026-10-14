using System.Windows;

namespace DigitApp;

/// <summary>
/// Hlavne okno aplikacie, ktore hostuje jednotlive pohlady.
/// </summary>
public partial class MainWindow : Window
{
  /// <summary>
  /// Inicializuje okno a nacita jeho XAML definiciu.
  /// </summary>
  public MainWindow() => InitializeComponent();
}
