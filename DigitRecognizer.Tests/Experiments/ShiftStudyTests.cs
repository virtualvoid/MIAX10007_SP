using DigitRecognizer.Experiments;
using NeuralNet;

namespace DigitRecognizer.Tests.Experiments;

/// <summary>
/// Testy posunu obrazkov v triede <see cref="ShiftStudy"/>, ktory sa pouziva pri skuske odolnosti modelov.
/// </summary>
public sealed class ShiftStudyTests
{
  /// <summary>
  /// Strana obrazka v pixeloch.
  /// </summary>
  private const int Side = MnistPreprocessor.Side;

  /// <summary>
  /// Vytvori mnozinu s jednym obrazkom, v ktorom je jediny biely pixel na zadanej pozicii.
  /// </summary>
  /// <param name="x">Stlpec pixelu.</param>
  /// <param name="y">Riadok pixelu.</param>
  /// <returns>Mnozina s jednym obrazkom a popiskom 7.</returns>
  private static Dataset SinglePixel(int x, int y)
  {
    var images = new float[Side * Side];
    images[y * Side + x] = 1f;
    return new Dataset(images, [7]);
  }

  /// <summary>
  /// Over, ze posun o (dx, dy) presunie pixel na ocakavanu poziciu.
  /// </summary>
  [Fact]
  public void ShiftMovesPixelByOffset()
  {
    var shifted = ShiftStudy.Shift(SinglePixel(10, 12), 3, -2);

    Assert.Equal(1f, shifted.Images[10 * Side + 13]);
    Assert.Equal(1f, shifted.Images.Sum());
  }

  /// <summary>
  /// Over, ze pixel posunuty mimo obrazok zmizne a uvolnene miesta ostanu cierne (nuly).
  /// </summary>
  [Fact]
  public void ShiftDropsPixelsLeavingTheImage()
  {
    var shifted = ShiftStudy.Shift(SinglePixel(26, 5), 3, 0);

    Assert.Equal(0f, shifted.Images.Sum());
  }

  /// <summary>
  /// Over, ze nulovy posun vrati rovnaky obsah obrazkov a popisky ostanu nezmenene.
  /// </summary>
  [Fact]
  public void ZeroShiftKeepsImagesAndLabels()
  {
    var original = SinglePixel(4, 9);

    var shifted = ShiftStudy.Shift(original, 0, 0);

    Assert.Equal(original.Images, shifted.Images);
    Assert.Equal(original.Labels, shifted.Labels);
  }
}
