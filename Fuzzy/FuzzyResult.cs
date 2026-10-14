namespace Fuzzy;

/// <summary>
/// Vysledok vyhodnotenia fuzzy systemu vratane medzivysledkov (fuzzifikacia, sily pravidiel, agregovana krivka).
/// </summary>
/// <param name="Value">Defuzzifikovana (ostra) hodnota vystupu.</param>
/// <param name="OutputActivation">Stupen aktivacie kazdej vystupnej mnoziny (po agregacii pravidiel).</param>
/// <param name="Curve">Vzorkovana agregovana vystupna fuzzy mnozina, z ktorej sa pocitalo taziste.</param>
/// <param name="RuleStrengths">Sila kazdeho pravidla v poradi, v akom boli zadane.</param>
/// <param name="InputDegrees">Fuzzifikacia vstupov: premenna, mnozina, stupen prislusnosti.</param>
public sealed record FuzzyResult(
  double Value,
  IReadOnlyDictionary<string, double> OutputActivation,
  IReadOnlyList<CurvePoint> Curve,
  IReadOnlyList<double> RuleStrengths,
  IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> InputDegrees)
{
  /// <summary>
  /// Vystupna mnozina s najvyssou aktivaciou, null ak sa nespustilo ziadne pravidlo.
  /// </summary>
  public string? DominantSet =>
    OutputActivation.MaxBy(kv => kv.Value) is { Value: > 0 } best ? best.Key : null;
}
