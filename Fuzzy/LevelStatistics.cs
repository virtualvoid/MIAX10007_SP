namespace Fuzzy;

/// <summary>
/// Pocet predikcii v jednej urovni spolahlivosti a kolko z nich bolo spravnych.
/// </summary>
/// <param name="Level">Uroven spolahlivosti.</param>
/// <param name="Count">Pocet predikcii v tejto urovni.</param>
/// <param name="Correct">Pocet spravnych predikcii v tejto urovni.</param>
public sealed record LevelStatistics(ReliabilityLevel Level, int Count, int Correct)
{
  /// <summary>
  /// Podiel spravnych predikcii v tejto urovni, 0 ak je uroven prazdna.
  /// </summary>
  public double Accuracy => Count == 0 ? 0 : (double)Correct / Count;
}
