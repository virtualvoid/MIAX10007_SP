using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy hotovych architektur v triede <see cref="Models"/>, ktore pouziva aplikacia aj experimenty.
/// </summary>
public sealed class ModelsTests
{
  /// <summary>
  /// Over, ze architektury maju pocet trenovatelnych parametrov uvedeny v praci
  /// (MLP 235 146, CNN 103 018) a ze vstup ma 784 hodnot a vystup 10 tried.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <param name="parameters">Ocakavany pocet parametrov.</param>
  [Theory]
  [InlineData(ModelKind.Mlp, 235_146)]
  [InlineData(ModelKind.Cnn, 103_018)]
  public void ArchitectureHasDocumentedShape(ModelKind kind, int parameters)
  {
    var network = Models.Create(kind, new Random(1));

    Assert.Equal(parameters, network.ParameterCount);
    Assert.Equal(784, network.InSize);
    Assert.Equal(10, network.Classes);
  }

  /// <summary>
  /// Over, ze kazda architektura spracuje jeden obrazok a vrati rozdelenie pravdepodobnosti s suctom 1.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  [Theory]
  [InlineData(ModelKind.Mlp)]
  [InlineData(ModelKind.Cnn)]
  public void ForwardGivesProbabilityDistribution(ModelKind kind)
  {
    var network = Models.Create(kind, new Random(1));

    var probabilities = network.Predict(new float[784]);

    Assert.Equal(10, probabilities.Length);
    Assert.Equal(1f, probabilities.Sum(), 1e-5f);
  }

  /// <summary>
  /// Over, ze rovnaky seed vytvori rovnaku sietu (rovnake vahy), co je zaklad reprodukovatelnosti experimentov.
  /// </summary>
  [Fact]
  public void SameSeedGivesSameWeights()
  {
    var first = Models.Create(ModelKind.Cnn, new Random(7));
    var second = Models.Create(ModelKind.Cnn, new Random(7));

    for (var i = 0; i < first.Parameters.Count; i++)
    {
      Assert.Equal(first.Parameters[i].W, second.Parameters[i].W);
    }
  }

  /// <summary>
  /// Over, ze ina hodnota seedu vytvori siet s inymi vahami.
  /// </summary>
  [Fact]
  public void DifferentSeedGivesDifferentWeights()
  {
    var first = Models.Create(ModelKind.Mlp, new Random(1));
    var second = Models.Create(ModelKind.Mlp, new Random(2));

    Assert.NotEqual(first.Parameters[0].W, second.Parameters[0].W);
  }
}
