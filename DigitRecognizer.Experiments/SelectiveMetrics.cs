namespace DigitRecognizer.Experiments;

/// <summary>
/// Metriky kvality odhadu spolahlivosti: dokaze skore oddelit spravne predikcie od chybnych? Skore je tym vyssie,
/// cim viac sa predikcii da verit. Zhody skore (napriklad nasytena pravdepodobnost 1,0) sa riesia ferovo, bez
/// zavislosti od poradia obrazkov v datasete.
/// </summary>
internal sealed class SelectiveMetrics
{
  private readonly double[] groupScores;
  private readonly int[] groupCounts;
  private readonly int[] groupErrors;
  private readonly int total;

  /// <summary>
  /// Pripravi metriky pre dane skore a priznaky chybnych predikcii.
  /// </summary>
  /// <param name="scores">Skore spolahlivosti pre kazdy obrazok.</param>
  /// <param name="isError">True, ak bola predikcia pre obrazok chybna.</param>
  public SelectiveMetrics(IReadOnlyList<double> scores, IReadOnlyList<bool> isError)
  {
    total = scores.Count;
    var groups = Enumerable.Range(0, total)
      .GroupBy(i => scores[i])
      .OrderByDescending(g => g.Key)
      .ToList();
    groupScores = [.. groups.Select(g => g.Key)];
    groupCounts = [.. groups.Select(g => g.Count())];
    groupErrors = [.. groups.Select(g => g.Count(i => isError[i]))];
    ErrorCount = groupErrors.Sum();
  }

  /// <summary>
  /// Pocet chybnych predikcii.
  /// </summary>
  public int ErrorCount { get; }

  /// <summary>
  /// Pocet navzajom roznych hodnot skore. Mensi pocet znamena hrubsie rozlisenie, teda viac predikcii s rovnakym skore.
  /// </summary>
  public int DistinctScores => groupScores.Length;

  /// <summary>
  /// Podiel obrazkov v najvacsej skupine s rovnakym skore. Obrazky v nej sa nedaju od seba rozlisit, takze metoda
  /// medzi nimi nevie oddelit spravne predikcie od chybnych.
  /// </summary>
  public double LargestTieShare => (double)groupCounts.Max() / total;

  /// <summary>
  /// Plocha pod ROC krivkou pre odhalenie chyb: pravdepodobnost, ze nahodne vybrana spravna predikcia ma vyssie
  /// skore ako nahodne vybrana chybna (pri rovnosti polovica). 0,5 je nahodny odhad, 1 dokonaly.
  /// </summary>
  /// <returns>Hodnota v intervale 0 az 1, alebo NaN, ak chyba spravna alebo chybna predikcia.</returns>
  public double Auroc()
  {
    var correct = total - ErrorCount;
    if (correct == 0 || ErrorCount == 0)
    {
      return double.NaN;
    }

    // poradie od najvyssieho skore, preto sa pocitaju dvojice (spravna, chybna) so spravnou nad chybnou
    double wins = 0;
    var errorsAbove = 0;
    for (var g = 0; g < groupScores.Length; g++)
    {
      var correctInGroup = groupCounts[g] - groupErrors[g];
      wins += correctInGroup * (ErrorCount - errorsAbove - groupErrors[g]); // chyby s nizsim skore
      wins += 0.5 * correctInGroup * groupErrors[g]; // zhoda skore
      errorsAbove += groupErrors[g];
    }
    return wins / ((double)correct * ErrorCount);
  }

  /// <summary>
  /// Miera chyby medzi predikciami, ktore by sa prijali pri danom pokryti (podiele prijatych predikcii).
  /// Ak hranica pokrytia pretina skupinu s rovnakym skore, zapocita sa jej pomerna cast.
  /// </summary>
  /// <param name="coverage">Podiel prijatych predikcii v intervale 0 az 1.</param>
  /// <returns>Podiel chybnych predikcii medzi prijatymi.</returns>
  public double RiskAtCoverage(double coverage)
  {
    var target = coverage * total;
    double accepted = 0, errors = 0;
    for (var g = 0; g < groupScores.Length && accepted < target; g++)
    {
      var take = Math.Min(groupCounts[g], target - accepted);
      accepted += take;
      errors += groupErrors[g] * take / groupCounts[g];
    }
    return accepted > 0 ? errors / accepted : 0;
  }

  /// <summary>
  /// Plocha pod krivkou riziko-pokrytie (AURC): priemerna miera chyby cez vsetky pokrytia. Mensia je lepsia.
  /// </summary>
  /// <param name="steps">Pocet krokov pokrytia pri integrovani.</param>
  /// <returns>Priemerne riziko.</returns>
  public double Aurc(int steps = 1000)
    => Enumerable.Range(1, steps).Average(k => RiskAtCoverage((double)k / steps));
}
