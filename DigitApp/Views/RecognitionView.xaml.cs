using System.Windows;
using System.Windows.Controls;
using DigitApp.ViewModels;

namespace DigitApp.Views;

/// <summary>
/// Pohlad na rozpoznavanie cislic z kresby pouzivatela.
/// </summary>
public partial class RecognitionView : UserControl
{
  /// <summary>
  /// Inicializuje pohlad a zaregistruje sledovanie zmeny data contextu.
  /// </summary>
  public RecognitionView()
  {
    InitializeComponent();
    DataContextChanged += OnDataContextChanged;
  }

  /// <summary>
  /// Prepoji odber udalosti vymazania plochy zo stareho view modelu na novy.
  /// </summary>
  private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (e.OldValue is RecognitionViewModel old)
    {
      old.ClearRequested -= OnClearRequested;
    }
    if (e.NewValue is RecognitionViewModel current)
    {
      current.ClearRequested += OnClearRequested;
    }
  }

  /// <summary>
  /// Vymaze kresliacu plochu na poziadanie view modelu.
  /// </summary>
  private void OnClearRequested(object? sender, EventArgs e) => Pad.Clear();

  /// <summary>
  /// Po zmene ciar odovzda aktualne pixely kresby view modelu na rozpoznanie.
  /// </summary>
  private void Pad_StrokesChanged(object? sender, EventArgs e)
  {
    if (DataContext is RecognitionViewModel viewModel)
    {
      viewModel.Recognize(Pad.GetPixels());
    }
  }
}
