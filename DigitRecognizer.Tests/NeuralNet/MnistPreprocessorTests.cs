using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy triedy <see cref="MnistPreprocessor"/>, ktora upravuje nakreslenu cislicu do formatu MNIST (28 x 28 pixelov).
/// </summary>
public sealed class MnistPreprocessorTests
{
  /// <summary>
  /// Strana stvorcoveho platna v pixeloch, z ktoreho sa cislica normalizuje.
  /// </summary>
  private const int Size = 280;

  /// <summary>
  /// Vytvori obraz platna v odtienoch sivej, na ktorom je vyplneny biely obdlznik so zadanymi suradnicami.
  /// </summary>
  /// <param name="x0">Lavy okraj obdlznika (vratane).</param>
  /// <param name="y0">Horny okraj obdlznika (vratane).</param>
  /// <param name="x1">Pravy okraj obdlznika (vratane).</param>
  /// <param name="y1">Dolny okraj obdlznika (vratane).</param>
  /// <returns>Pole hodnot sivej so zhodnou velkostou ako platno, kde obdlznik ma hodnotu 1 a pozadie 0.</returns>
  private static float[] DrawRectangle(int x0, int y0, int x1, int y1)
  {
    var gray = new float[Size * Size];
    for (var y = y0; y <= y1; y++)
    {
      for (var x = x0; x <= x1; x++)
      {
        gray[y * Size + x] = 1f;
      }
    }
    return gray;
  }

  /// <summary>
  /// Over, ze prazdne platno sa normalizuje na cierny obraz 28 x 28 pixelov so vsetkymi hodnotami rovnymi 0.
  /// </summary>
  [Fact]
  public void EmptyCanvasGivesBlackImage()
  {
    var result = MnistPreprocessor.Normalize(new float[Size * Size], Size);

    Assert.Equal(MnistPreprocessor.Side * MnistPreprocessor.Side, result.Length);
    Assert.All(result, v => Assert.Equal(0f, v));
  }

  /// <summary>
  /// Over, ze vacsi rozmer cislice sa zmensi na 20 pixelov a pomer stran sa zachova.
  /// </summary>
  [Fact]
  public void LargerDimensionIsScaledToTwentyPixels()
  {
    // obdlznik 80 x 160 px, vyska je vacsi rozmer
    var result = MnistPreprocessor.Normalize(DrawRectangle(100, 60, 179, 219), Size);

    var rows = Enumerable.Range(0, 28).Count(y => Enumerable.Range(0, 28).Any(x => result[y * 28 + x] > 0.1f));
    var columns = Enumerable.Range(0, 28).Count(x => Enumerable.Range(0, 28).Any(y => result[y * 28 + x] > 0.1f));

    Assert.InRange(rows, 20, 21);
    Assert.InRange(columns, 9, 11); // pomer stran sa zachova
  }

  /// <summary>
  /// Over, ze tazisko cislice lezi po normalizacii v strede obrazu (priblizne v bode 14, 14) bez ohladu
  /// na to, kde na platne bola cislica nakreslena. Vstupne data su suradnice obdlznika (lavy, horny, pravy
  /// a dolny okraj) v roznych castiach platna.
  /// </summary>
  /// <param name="x0">Lavy okraj obdlznika.</param>
  /// <param name="y0">Horny okraj obdlznika.</param>
  /// <param name="x1">Pravy okraj obdlznika.</param>
  /// <param name="y1">Dolny okraj obdlznika.</param>
  [Theory]
  [InlineData(10, 10, 60, 90)]
  [InlineData(200, 150, 260, 270)]
  public void DigitIsCenteredByCenterOfMass(int x0, int y0, int x1, int y1)
  {
    var result = MnistPreprocessor.Normalize(DrawRectangle(x0, y0, x1, y1), Size);

    double mass = result.Sum(), cx = 0, cy = 0;
    for (var i = 0; i < result.Length; i++)
    {
      cx += i % 28 * result[i];
      cy += i / 28 * result[i];
    }

    Assert.InRange(cx / mass, 13.4, 14.6);
    Assert.InRange(cy / mass, 13.4, 14.6);
  }
}
