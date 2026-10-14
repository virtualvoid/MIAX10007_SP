using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy optimalizatorov <see cref="Sgd"/> a <see cref="Adam"/> a tovarne <see cref="Optimizers"/>.
/// </summary>
public sealed class OptimizerTests
{
  /// <summary>
  /// Vytvori parameter s jednou vahou a jednym gradientom so zadanymi hodnotami.
  /// </summary>
  /// <param name="weight">Pociatocna hodnota vahy.</param>
  /// <param name="gradient">Hodnota gradientu vahy.</param>
  /// <returns>Parameter pripraveny na krok optimalizatora.</returns>
  private static Param CreateParam(float weight, float gradient)
  {
    var p = new Param(1);
    p.W[0] = weight;
    p.G[0] = gradient;
    return p;
  }

  /// <summary>
  /// Over, ze krok optimalizatora Sgd posunie vahu proti smeru gradientu o learning rate nasobok gradientu.
  /// </summary>
  [Fact]
  public void SgdMovesWeightsAgainstGradient()
  {
    var p = CreateParam(1f, 0.5f);

    new Sgd(0.1f).Step([p]);

    Assert.Equal(0.95f, p.W[0], 1e-6f);
  }

  /// <summary>
  /// Over, ze prvy krok optimalizatora Adam ma velkost priblizne rovnu learning rate v smere proti gradientu,
  /// nezavisle od velkosti gradientu (kladny maly, kladny velky aj zaporny).
  /// </summary>
  [Fact]
  public void AdamFirstStepIsApproximatelyLearningRate()
  {
    // po bias korekcii je prvy krok Adamu priblizne lr v smere proti gradientu, nezavisle od jeho velkosti
    foreach (var gradient in new[] { 0.5f, 50f, -0.01f })
    {
      var p = CreateParam(1f, gradient);

      new Adam(0.01f).Step([p]);

      Assert.Equal(1f - 0.01f * MathF.Sign(gradient), p.W[0], 1e-4f);
    }
  }

  /// <summary>
  /// Over, ze optimalizator Adam vyhodi vynimku InvalidOperationException, ak dostane iny pocet parametrov,
  /// nez s akym zacal.
  /// </summary>
  [Fact]
  public void AdamRejectsDifferentNetworkThanItStartedWith()
  {
    var adam = new Adam(0.01f);
    adam.Step([CreateParam(1f, 1f)]);

    Assert.Throws<InvalidOperationException>(() => adam.Step([CreateParam(1f, 1f), CreateParam(1f, 1f)]));
  }

  /// <summary>
  /// Over, ze tovaren vytvori optimalizator typu zodpovedajuceho zadanemu druhu. Vstupne data obsahuju
  /// druh optimalizatora a ocakavany typ vytvorenej instancie.
  /// </summary>
  /// <param name="kind">Pozadovany druh optimalizatora.</param>
  /// <param name="expected">Ocakavany typ vytvorenej instancie.</param>
  [Theory]
  [InlineData(OptimizerKind.Sgd, typeof(Sgd))]
  [InlineData(OptimizerKind.Adam, typeof(Adam))]
  public void FactoryCreatesRequestedOptimizer(OptimizerKind kind, Type expected)
  {
    Assert.IsType(expected, Optimizers.Create(kind, 0.01f));
  }
}
