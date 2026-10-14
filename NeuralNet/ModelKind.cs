namespace NeuralNet;

/// <summary>
/// Druh architektury modelu.
/// </summary>
public enum ModelKind
{
  /// <summary>
  /// Viacvrstvovy perceptron zlozeny z plne prepojenych vrstiev.
  /// </summary>
  Mlp,
  /// <summary>
  /// Konvolucna neuronova siet.
  /// </summary>
  Cnn
}
