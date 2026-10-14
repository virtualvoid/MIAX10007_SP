using Fuzzy;
using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Studia fuzzy hodnotenia spolahlivosti: porovna, ako dobre fuzzy skore odlisuje spravne predikcie CNN od chybnych
/// v porovnani s jednoduchymi zakladnymi metodami (maximalna pravdepodobnost, rozdiel, entropia, priemer dvoch modelov).
/// </summary>
internal static class ReliabilityStudy
{
  private static readonly double[] Coverages = [0.90, 0.95, 0.98, 0.99];

  /// <summary>
  /// Pre kazdu dvojicu modelov (CNN ako hlavny a MLP ako druhy s rovnakym seedom) vyhodnoti vsetky metody
  /// odhadu spolahlivosti a zapise metriky, rozdelenie podla urovni fuzzy a krivky riziko-pokrytie.
  /// </summary>
  /// <param name="options">Nastavenie experimentov.</param>
  /// <param name="models">Natrenovane modely.</param>
  /// <param name="test">Testovacia mnozina.</param>
  public static void Run(ExperimentOptions options, IReadOnlyList<TrainedModel> models, Dataset test)
  {
    var estimator = new ReliabilityEstimator();
    var metricRows = new List<IReadOnlyList<string>>();
    var levelRows = new List<IReadOnlyList<string>>();
    var curveRows = new List<IReadOnlyList<string>>();

    foreach (var seed in models.Select(m => m.Seed).Distinct().Order())
    {
      var cnn = models.Single(m => m.Kind == ModelKind.Cnn && m.Seed == seed).Network.PredictAll(test);
      var mlp = models.Single(m => m.Kind == ModelKind.Mlp && m.Seed == seed).Network.PredictAll(test);
      var isError = Enumerable.Range(0, test.Count).Select(i => cnn[i].ArgMax() != test.Labels[i]).ToArray();

      var scores = Scores(estimator, cnn, mlp);
      foreach (var (method, values) in scores)
      {
        var metrics = new SelectiveMetrics(values, isError);
        metricRows.Add([
          CsvFile.Number(seed), method, CsvFile.Number(metrics.ErrorCount), CsvFile.Number(metrics.Auroc()), CsvFile.Number(metrics.Aurc(), 8),
          .. Coverages.Select(c => CsvFile.Number(metrics.RiskAtCoverage(c), 8)),
          CsvFile.Number(metrics.DistinctScores), CsvFile.Number(metrics.LargestTieShare, 6)
        ]);
        if (seed == 1)
        {
          for (var coverage = 0.5; coverage <= 1.0 + 1e-9; coverage += 0.005)
          {
            curveRows.Add([method, CsvFile.Number(coverage, 3), CsvFile.Number(metrics.RiskAtCoverage(coverage), 8)]);
          }
        }
      }

      var report = ReliabilityAnalyzer.Analyze(estimator, cnn, mlp, test.Labels);
      foreach (var level in report.Levels)
      {
        levelRows.Add([CsvFile.Number(seed), level.Level.ToString(), CsvFile.Number(level.Count), CsvFile.Number(level.Correct)]);
      }
      Console.WriteLine($"[reliability] seed {seed}: AUROC fuzzy {new SelectiveMetrics(scores["fuzzy_dual"], isError).Auroc():F4}, " +
                        $"max_softmax {new SelectiveMetrics(scores["max_softmax"], isError).Auroc():F4}");
    }

    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "reliability_metrics.csv"),
      ["seed", "method", "errors", "auroc", "aurc", .. Coverages.Select(c => $"risk_at_{CsvFile.Number(c, 2)}"), "distinct_scores", "largest_tie_share"],
      metricRows);
    CsvFile.Write(Path.Combine(options.OutputDirectory, "reliability_levels.csv"), ["seed", "level", "count", "correct"], levelRows);
    CsvFile.Write(Path.Combine(options.OutputDirectory, "risk_coverage_seed1.csv"), ["method", "coverage", "risk"], curveRows);
  }

  // skore spolahlivosti pre kazdy obrazok podla vsetkych porovnavanych metod (vyssie = viac istoty)
  private static Dictionary<string, double[]> Scores(ReliabilityEstimator estimator, float[][] cnn, float[][] mlp)
  {
    var count = cnn.Length;
    var scores = new Dictionary<string, double[]>
    {
      ["fuzzy_dual"] = new double[count],
      ["fuzzy_single"] = new double[count],
      ["max_softmax"] = new double[count],
      ["margin"] = new double[count],
      ["neg_entropy"] = new double[count],
      ["agreement_only"] = new double[count],
      ["ensemble_mean"] = new double[count]
    };

    for (var i = 0; i < count; i++)
    {
      var predicted = cnn[i].ArgMax();
      var top = cnn[i][predicted];
      var second = cnn[i].Where((_, k) => k != predicted).Max();
      var other = mlp[i][predicted];

      scores["fuzzy_dual"][i] = estimator.Assess(cnn[i], mlp[i]).Score;
      scores["fuzzy_single"][i] = estimator.Assess(cnn[i]).Score;
      scores["max_softmax"][i] = top;
      scores["margin"][i] = top - second;
      scores["neg_entropy"][i] = -Entropy(cnn[i]);
      scores["agreement_only"][i] = other;
      scores["ensemble_mean"][i] = (top + other) / 2.0;
    }
    return scores;
  }

  // Shannonova entropia rozdelenia v nateoch, 0 pre uplne istu predikciu
  private static double Entropy(float[] probabilities)
    => -probabilities.Where(p => p > 0).Sum(p => p * Math.Log(p));
}
