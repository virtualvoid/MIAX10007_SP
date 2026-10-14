namespace NeuralNet;

/// <summary>
/// Obrazky ulozene za sebou v jednom poli (n * pocet pixelov, hodnoty 0..1) a k nim popisky.
/// </summary>
public sealed record Dataset(float[] Images, byte[] Labels)
{
  /// <summary>
  /// Pocet obrazkov v datasete.
  /// </summary>
  public int Count => Labels.Length;
}
