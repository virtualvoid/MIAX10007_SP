using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy spravnosti spatneho sirenia chyby v triede <see cref="Network"/>. Gradient z backpropu sa porovnava
/// s numerickym gradientom (f(w+e) - f(w-e)) / 2e. Ak sa zhoduju, spatny prechod vsetkych typov vrstiev
/// je naprogramovany spravne.
/// </summary>
public sealed class GradientCheckTests
{
  /// <summary>
  /// Velkost poruchy vahy e pouzita pri vypocte numerickeho gradientu.
  /// </summary>
  private const float Epsilon = 1e-2f;

  /// <summary>
  /// Pocet vzoriek v jednej davke pouzitej pri kontrole gradientu.
  /// </summary>
  private const int Batch = 4;

  /// <summary>
  /// Najvacsia pripustna relativna odchylka medzi analytickym a numerickym gradientom.
  /// </summary>
  private const double Tolerance = 0.05;

  /// <summary>
  /// Pocet nahodne vybranych vah z kazdeho parametra, pre ktore sa gradient kontroluje.
  /// </summary>
  private const int SamplesPerParameter = 15;

  /// <summary>
  /// Over, ze gradienty vypocitane spatnym sirenim chyby sa zhoduju s numerickymi gradientmi
  /// v ramci povolenej tolerancie pre vahy vsetkych vrstiev siete.
  /// </summary>
  [Fact]
  public void BackpropagationMatchesNumericalGradient()
  {
    var rng = new Random(1);

    // mala siet so vsetkymi typmi vrstiev (bez dropoutu, ten je nahodny)
    var net = new Network()
      .Add(new Conv2D(1, 3, 8, 8, rng)).Add(new ReLU(3 * 8 * 8)).Add(new MaxPool2D(3, 8, 8))
      .Add(new Conv2D(3, 4, 4, 4, rng)).Add(new ReLU(4 * 4 * 4)).Add(new MaxPool2D(4, 4, 4))
      .Add(new Dense(4 * 2 * 2, 10, rng));

    var x = new float[Batch * 64];
    for (var i = 0; i < x.Length; i++)
    {
      x[i] = rng.NextSingle();
    }
    byte[] labels = [1, 4, 7, 2];

    float Loss() => net.ComputeGradients(x, labels, Batch).loss;

    foreach (var p in net.Parameters)
    {
      Loss();
      var analytic = (float[])p.G.Clone(); // dalsie volania Loss() gradienty prepisu

      foreach (var i in Enumerable.Range(0, p.W.Length).OrderBy(_ => rng.Next()).Take(SamplesPerParameter))
      {
        var original = p.W[i];
        p.W[i] = original + Epsilon;
        var plus = Loss();
        p.W[i] = original - Epsilon;
        var minus = Loss();
        p.W[i] = original;

        var numeric = (plus - minus) / (2 * Epsilon);
        var error = Math.Abs(numeric - analytic[i]) / Math.Max(1e-3, Math.Abs(numeric) + Math.Abs(analytic[i]));
        Assert.True(error <= Tolerance, $"param {i}: analyticky {analytic[i]:E3}, numericky {numeric:E3}, chyba {error:P2}");
      }
    }
  }
}
