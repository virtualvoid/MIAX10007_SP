namespace NeuralNet;

/// <summary>
/// Tovaren na vytvaranie optimalizatorov.
/// </summary>
public static class Optimizers
{
  /// <summary>
  /// Vytvori optimalizator zadaneho druhu s danou rychlostou ucenia.
  /// </summary>
  /// <param name="kind">Druh optimalizatora.</param>
  /// <param name="learningRate">Rychlost ucenia.</param>
  /// <returns>Novy optimalizator s nulovym stavom.</returns>
  public static IOptimizer Create(OptimizerKind kind, float learningRate) => kind switch
  {
    OptimizerKind.Sgd => new Sgd(learningRate),
    OptimizerKind.Adam => new Adam(learningRate),
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
  };
}
