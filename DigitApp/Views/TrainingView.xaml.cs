using System.Windows;
using System.Windows.Controls;
using DigitApp.ViewModels;
using Microsoft.Win32;

namespace DigitApp.Views;

/// <summary>
/// Pohlad na trenovanie modelov nad datasetom MNIST.
/// </summary>
public partial class TrainingView : UserControl
{
  /// <summary>
  /// Inicializuje pohlad a nacita jeho XAML definiciu.
  /// </summary>
  public TrainingView() => InitializeComponent();

  /// <summary>
  /// Otvori dialog na vyber priecinka datasetu a vybranu cestu ulozi do view modelu.
  /// </summary>
  /// <remarks>Vyber priecinka je zalezitost okna, preto je tu a nie vo view modeli.</remarks>
  private void Browse_Click(object sender, RoutedEventArgs e)
  {
    if (DataContext is not TrainingViewModel viewModel)
    {
      return;
    }

    var dialog = new OpenFolderDialog
    {
      Title = "Priečinok so súbormi datasetu MNIST",
      InitialDirectory = viewModel.DatasetDirectory
    };
    if (dialog.ShowDialog() == true)
    {
      viewModel.DatasetDirectory = dialog.FolderName;
    }
  }
}
