using System.Diagnostics;
using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Spusta reprodukovatelne experimenty z prace a zapisuje ich vysledky ako CSV pre dalsiu analyzu (docs/scripts/analyze.py).
/// Pouzitie: <c>--data priecinok --out priecinok --seeds 5 --epochs 10 --studies seeds,shift,reliability,optimizers</c>.
/// </summary>
internal static class Program
{
  /// <summary>
  /// Vstupny bod programu.
  /// </summary>
  /// <param name="args">Argumenty prikazoveho riadku.</param>
  /// <returns>Navratovy kod procesu (0 = uspech).</returns>
  private static int Main(string[] args)
  {
    ExperimentOptions options;
    try
    {
      options = ExperimentOptions.Parse(args);
    }
    catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or FormatException)
    {
      Console.Error.WriteLine(ex.Message);
      return 1;
    }

    var stopwatch = Stopwatch.StartNew();
    Console.WriteLine($"Dataset: {options.DataDirectory}");
    Console.WriteLine($"Vystup:  {options.OutputDirectory}");

    // samotna analyza cita len hotove CSV, dataset nepotrebuje
    var onlyAnalysis = options.Studies.SetEquals([ExperimentOptions.AnalyzeStudy]);
    var train = onlyAnalysis ? new Dataset([], []) : Mnist.LoadTrain(options.DataDirectory);
    var test = onlyAnalysis ? new Dataset([], []) : Mnist.LoadTest(options.DataDirectory);

    IReadOnlyList<TrainedModel> models = [];
    if (options.NeedsModels)
    {
      models = SeedStudy.Run(options, train, test);
    }
    if (options.Studies.Contains(ExperimentOptions.ShiftStudy))
    {
      ShiftStudy.Run(options, models, test);
    }
    if (options.Studies.Contains(ExperimentOptions.ReliabilityStudy))
    {
      ReliabilityStudy.Run(options, models, test);
    }
    if (options.Studies.Contains(ExperimentOptions.OptimizersStudy))
    {
      OptimizerStudy.Run(options, train, test);
    }

    if (options.Studies.Contains(ExperimentOptions.AnalyzeStudy))
    {
      AnalysisStudy.Run(options);
    }

    Console.WriteLine($"Hotovo za {stopwatch.Elapsed.TotalMinutes:F1} min.");
    return 0;
  }
}
