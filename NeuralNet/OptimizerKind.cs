namespace NeuralNet;

/// <summary>
/// Druh optimalizatora pouzivaneho pri trenovani siete.
/// </summary>
public enum OptimizerKind
{
  /// <summary>
  /// Obycajny gradientny zostup (SGD).
  /// </summary>
  Sgd,
  /// <summary>
  /// Adaptivny optimalizator Adam.
  /// </summary>
  Adam
}
