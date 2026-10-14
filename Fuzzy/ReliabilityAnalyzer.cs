namespace Fuzzy;

/// <summary>
/// Hromadne vyhodnotenie spolahlivosti predikcii nad datasetom.
/// </summary>
public static class ReliabilityAnalyzer
{
  /// <summary>
  /// Vyhodnoti spolahlivost pre kazdy obrazok a spocita, kolko predikcii hlavneho modelu bolo v ktorej urovni spravnych.
  /// </summary>
  /// <param name="estimator">Odhadovac spolahlivosti.</param>
  /// <param name="primary">Pravdepodobnosti hlavneho modelu pre kazdy obrazok.</param>
  /// <param name="secondary">Pravdepodobnosti druheho modelu pre kazdy obrazok, alebo null.</param>
  /// <param name="labels">Spravne triedy obrazkov.</param>
  /// <returns>Report s poctami predikcii a spravnych predikcii podla urovne.</returns>
  public static ReliabilityReport Analyze(
    ReliabilityEstimator estimator,
    IReadOnlyList<float[]> primary,
    IReadOnlyList<float[]>? secondary,
    IReadOnlyList<byte> labels)
  {
    var levels = Enum.GetValues<ReliabilityLevel>();
    var counts = new int[levels.Length];
    var correct = new int[levels.Length];

    for (var i = 0; i < labels.Count; i++)
    {
      var assessment = estimator.Assess(primary[i], secondary?[i]);
      var index = (int)assessment.Level;
      counts[index]++;
      if (assessment.PredictedClass == labels[i])
      {
        correct[index]++;
      }
    }
    return new ReliabilityReport([.. levels.Select(l => new LevelStatistics(l, counts[(int)l], correct[(int)l]))]);
  }
}
