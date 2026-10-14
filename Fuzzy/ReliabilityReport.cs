namespace Fuzzy;

/// <summary>
/// Suhrn toho, ci fuzzy spolahlivost naozaj oddeluje spravne predikcie od chybnych.
/// </summary>
/// <param name="Levels">Statistiky pre jednotlive urovne spolahlivosti.</param>
public sealed record ReliabilityReport(IReadOnlyList<LevelStatistics> Levels)
{
  /// <summary>
  /// Celkovy pocet vyhodnotenych predikcii.
  /// </summary>
  public int Total => Levels.Sum(l => l.Count);

  /// <summary>
  /// Presnost hlavneho modelu cez vsetky urovne, 0 ak nie su ziadne predikcie.
  /// </summary>
  public double OverallAccuracy => Total == 0 ? 0 : (double)Levels.Sum(l => l.Correct) / Total;

  /// <summary>
  /// Presnost, ak by sa prijali len predikcie s urovnou aspon <paramref name="minimum"/> (ostatne by sa odmietli).
  /// </summary>
  /// <param name="minimum">Minimalna prijata uroven spolahlivosti.</param>
  /// <returns>Presnost (0 az 1) prijatych predikcii, alebo 0, ak sa neprijala ziadna.</returns>
  public double AccuracyAtLeast(ReliabilityLevel minimum)
  {
    var accepted = Levels.Where(l => l.Level >= minimum).ToList();
    var count = accepted.Sum(l => l.Count);
    return count == 0 ? 0 : (double)accepted.Sum(l => l.Correct) / count;
  }

  /// <summary>
  /// Podiel predikcii, ktore by sa pri tom prijali.
  /// </summary>
  /// <param name="minimum">Minimalna prijata uroven spolahlivosti.</param>
  /// <returns>Podiel prijatych predikcii zo vsetkych (0 az 1).</returns>
  public double CoverageAtLeast(ReliabilityLevel minimum) =>
    Total == 0 ? 0 : (double)Levels.Where(l => l.Level >= minimum).Sum(l => l.Count) / Total;
}
