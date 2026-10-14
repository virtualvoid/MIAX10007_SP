namespace Fuzzy;

/// <summary>
/// Vysledok fuzzy hodnotenia spolahlivosti jednej predikcie.
/// </summary>
/// <param name="Score">Spolahlivost 0..100 (defuzzifikovany vystup).</param>
/// <param name="Level">Slovna uroven spolahlivosti odvodena z dominantnej vystupnej mnoziny.</param>
/// <param name="PredictedClass">Trieda s najvyssou pravdepodobnostou podla hlavneho modelu.</param>
/// <param name="Confidence">Pravdepodobnost predikovanej triedy (vstup systemu).</param>
/// <param name="Margin">Rozdiel medzi najpravdepodobnejsou a druhou najpravdepodobnejsou triedou (vstup systemu).</param>
/// <param name="Agreement">Pravdepodobnost predikovanej triedy podla druheho modelu, null ak je len jeden (vstup systemu).</param>
/// <param name="Detail">Podrobnosti vyhodnotenia fuzzy systemu.</param>
public sealed record ReliabilityAssessment(
  double Score,
  ReliabilityLevel Level,
  int PredictedClass,
  double Confidence,
  double Margin,
  double? Agreement,
  FuzzyResult Detail);
