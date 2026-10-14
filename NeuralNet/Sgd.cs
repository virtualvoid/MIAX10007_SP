namespace NeuralNet;

/// <summary>
/// Obycajny gradient descent: w = w - lr * g.
/// </summary>
/// <param name="learningRate">Rychlost ucenia, teda velkost kroku v smere gradientu.</param>
public sealed class Sgd(float learningRate) : IOptimizer
{
  /// <inheritdoc />
  public void Step(IReadOnlyList<Param> parameters)
  {
    foreach (var p in parameters)
    {
      Vec.Axpy(-learningRate, p.G, p.W);
    }
  }
}
