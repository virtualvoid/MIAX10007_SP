using NeuralNet;

namespace DigitApp.Infrastructure;

/// <summary>
/// Rozsirenia typu <see cref="ModelKind"/>.
/// </summary>
public static class ModelKindExtensions
{
  extension(ModelKind kind)
  {
    /// <summary>
    /// Nazov modelu urceny na zobrazenie pouzivatelovi.
    /// </summary>
    public string DisplayName => kind switch
    {
      ModelKind.Mlp => "MLP",
      ModelKind.Cnn => "CNN",
      _ => kind.ToString()
    };
  }
}
