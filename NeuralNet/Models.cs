namespace NeuralNet;

/// <summary>
/// Hotove architektury na jednom mieste, aby aplikacia vsade pouzivala presne tie iste.
/// </summary>
public static class Models
{
  /// <summary>
  /// Vytvori novu siet zadanej architektury s nahodne inicializovanymi vahami.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <param name="rng">Generator nahodnych cisel pre inicializaciu vah a dropout.</param>
  /// <returns>Nova siet s nahodne inicializovanymi vahami.</returns>
  public static Network Create(ModelKind kind, Random rng) => kind switch
  {
    ModelKind.Mlp => Mlp(rng),
    ModelKind.Cnn => Cnn(rng),
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
  };

  /// <summary>
  /// Viacvrstvovy perceptron s architekturou 784 -&gt; 256 -&gt; 128 -&gt; 10.
  /// </summary>
  /// <returns>Nova siet architektury MLP.</returns>
  private static Network Mlp(Random rng) => new Network()
    .Add(new Dense(784, 256, rng)).Add(new ReLU(256)).Add(new Dropout(256, 0.2f, rng))
    .Add(new Dense(256, 128, rng)).Add(new ReLU(128))
    .Add(new Dense(128, 10, rng));

  /// <summary>
  /// Konvolucna siet: 1x28x28 -&gt; conv 8 -&gt; pool -&gt; 8x14x14 -&gt; conv 16 -&gt; pool -&gt; 16x7x7 (784) -&gt; 128 -&gt; 10.
  /// </summary>
  /// <returns>Nova siet architektury CNN.</returns>
  private static Network Cnn(Random rng) => new Network()
    .Add(new Conv2D(1, 8, 28, 28, rng)).Add(new ReLU(8 * 28 * 28)).Add(new MaxPool2D(8, 28, 28))
    .Add(new Conv2D(8, 16, 14, 14, rng)).Add(new ReLU(16 * 14 * 14)).Add(new MaxPool2D(16, 14, 14))
    .Add(new Dense(16 * 7 * 7, 128, rng)).Add(new ReLU(128)).Add(new Dropout(128, 0.3f, rng))
    .Add(new Dense(128, 10, rng));
}
