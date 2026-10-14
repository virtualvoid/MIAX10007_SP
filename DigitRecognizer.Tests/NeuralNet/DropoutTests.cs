using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy vrstvy <see cref="Dropout"/>: pri trenovani nahodne vypina neurony a zvysok skaluje, pri inferencii nerobi nic.
/// </summary>
public sealed class DropoutTests
{
  /// <summary>
  /// Pocet hodnot vo vstupe, dostatocne velky na to, aby podiel vypnutych neuronov zodpovedal pravdepodobnosti.
  /// </summary>
  private const int Size = 20_000;

  /// <summary>
  /// Pravdepodobnost vypnutia neuronu pouzita v testoch.
  /// </summary>
  private const float Rate = 0.3f;

  /// <summary>
  /// Vytvori vstup zlozeny zo samych jednotiek.
  /// </summary>
  /// <returns>Pole jednotiek dlzky <see cref="Size"/>.</returns>
  private static float[] Ones() => [.. Enumerable.Repeat(1f, Size)];

  /// <summary>
  /// Over, ze pri inferencii (training = false) vrstva vrati vstup nezmeneny.
  /// </summary>
  [Fact]
  public void InferenceKeepsInputUnchanged()
  {
    var dropout = new Dropout(Size, Rate, new Random(1));
    var input = Ones();

    var output = dropout.Forward(input, 1, false);

    Assert.Equal(input, output);
  }

  /// <summary>
  /// Over, ze pri trenovani sa vypne priblizne zadany podiel neuronov a ostatne sa prenasobia hodnotou 1 / (1 - rate),
  /// takze ocakavana hodnota vystupu ostane rovnaka ako vstupu.
  /// </summary>
  [Fact]
  public void TrainingDropsFractionAndRescalesSurvivors()
  {
    var dropout = new Dropout(Size, Rate, new Random(1));

    var output = dropout.Forward(Ones(), 1, true);

    var dropped = output.Count(v => v == 0f) / (double)Size;
    Assert.InRange(dropped, Rate - 0.02, Rate + 0.02);
    Assert.All(output.Where(v => v != 0f), v => Assert.Equal(1f / (1 - Rate), v, 1e-5f));
    Assert.Equal(1.0, output.Average(), 0.03);
  }

  /// <summary>
  /// Over, ze spatny prechod pouzije rovnaku masku ako dopredny: gradient prejde len cez neurony, ktore ostali aktivne.
  /// </summary>
  [Fact]
  public void BackwardUsesTheSameMaskAsForward()
  {
    var dropout = new Dropout(Size, Rate, new Random(1));
    var output = dropout.Forward(Ones(), 1, true);

    var gradient = dropout.Backward(Ones(), 1);

    Assert.Equal(output, gradient); // pri jednotkovom vstupe aj gradiente je vystup rovny maske
  }

  /// <summary>
  /// Over, ze po inferencii spatny prechod vrati gradient nezmeneny, lebo sa ziaden neuron nevypol.
  /// </summary>
  [Fact]
  public void BackwardAfterInferencePassesGradientThrough()
  {
    var dropout = new Dropout(Size, Rate, new Random(1));
    dropout.Forward(Ones(), 1, false);
    var gradient = Ones();

    Assert.Same(gradient, dropout.Backward(gradient, 1));
  }
}
