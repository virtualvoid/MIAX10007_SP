using System.Diagnostics;
using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Studia porovnania MLP a CNN: kazdy model sa natrenuje opakovane s inym seedom a rovnakymi hyperparametrami,
/// aby sa dala odhadnut variabilita vysledku a rozdiel modelov otestovat statisticky.
/// </summary>
internal static class SeedStudy
{
  private const int BatchSize = 64;
  private const float LearningRate = 0.001f;

  /// <summary>
  /// Natrenuje vsetky modely, zapise priebeh trenovania a konecne vysledky a vrati natrenovane siete
  /// pre dalsie studie.
  /// </summary>
  /// <param name="options">Nastavenie experimentov.</param>
  /// <param name="train">Trenovacia mnozina.</param>
  /// <param name="test">Testovacia mnozina.</param>
  /// <returns>Natrenovane modely, pre kazdy druh a seed jeden.</returns>
  public static IReadOnlyList<TrainedModel> Run(ExperimentOptions options, Dataset train, Dataset test)
  {
    var epochRows = new List<IReadOnlyList<string>>();
    var finalRows = new List<IReadOnlyList<string>>();
    var models = new List<TrainedModel>();

    foreach (var kind in new[] { ModelKind.Mlp, ModelKind.Cnn })
    {
      for (var seed = 1; seed <= options.Seeds; seed++)
      {
        var network = Models.Create(kind, new Random(seed));
        var trainingOptions = new TrainingOptions(options.Epochs, BatchSize, LearningRate, OptimizerKind.Adam, seed);

        var stopwatch = Stopwatch.StartNew();
        var reports = NetworkTrainer.Train(network, train, test, trainingOptions);
        stopwatch.Stop();

        foreach (var r in reports)
        {
          epochRows.Add([
            kind.ToString(), CsvFile.Number(seed), CsvFile.Number(r.Epoch), CsvFile.Number(r.TrainLoss),
            CsvFile.Number(r.TrainAccuracy), CsvFile.Number(r.TestLoss), CsvFile.Number(r.TestAccuracy)
          ]);
        }

        var evaluation = network.Evaluate(test);
        finalRows.Add([
          kind.ToString(), CsvFile.Number(seed), CsvFile.Number(evaluation.Accuracy), CsvFile.Number(evaluation.Errors),
          CsvFile.Number(evaluation.Loss), CsvFile.Number(network.ParameterCount), CsvFile.Number(stopwatch.Elapsed.TotalSeconds, 1)
        ]);
        models.Add(new TrainedModel(kind, seed, network, stopwatch.Elapsed.TotalSeconds));
        Console.WriteLine($"[seeds] {kind} seed {seed}: presnost {evaluation.Accuracy:P2}, chyb {evaluation.Errors}, {stopwatch.Elapsed.TotalSeconds:F0} s");
      }
    }

    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "seed_study_epochs.csv"),
      ["model", "seed", "epoch", "train_loss", "train_accuracy", "test_loss", "test_accuracy"],
      epochRows);
    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "seed_study_final.csv"),
      ["model", "seed", "test_accuracy", "errors", "test_loss", "parameters", "train_seconds"],
      finalRows);
    WriteMcNemarCounts(options, models, test);
    return models;
  }

  // pre kazdy seed zrata, kolko obrazkov trafili obe siete, len jedna z nich alebo ziadna (podklad pre McNemarov test)
  private static void WriteMcNemarCounts(ExperimentOptions options, IReadOnlyList<TrainedModel> models, Dataset test)
  {
    var rows = new List<IReadOnlyList<string>>();
    foreach (var seed in models.Select(m => m.Seed).Distinct())
    {
      var mlp = Predictions(models.Single(m => m.Kind == ModelKind.Mlp && m.Seed == seed), test);
      var cnn = Predictions(models.Single(m => m.Kind == ModelKind.Cnn && m.Seed == seed), test);
      int bothCorrect = 0, mlpOnly = 0, cnnOnly = 0, bothWrong = 0;
      for (var i = 0; i < test.Count; i++)
      {
        var mlpOk = mlp[i] == test.Labels[i];
        var cnnOk = cnn[i] == test.Labels[i];
        if (mlpOk && cnnOk)
        {
          bothCorrect++;
        }
        else if (mlpOk)
        {
          mlpOnly++;
        }
        else if (cnnOk)
        {
          cnnOnly++;
        }
        else
        {
          bothWrong++;
        }
      }
      rows.Add([CsvFile.Number(seed), CsvFile.Number(bothCorrect), CsvFile.Number(mlpOnly), CsvFile.Number(cnnOnly), CsvFile.Number(bothWrong)]);
    }
    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "mcnemar_counts.csv"),
      ["seed", "both_correct", "mlp_only", "cnn_only", "both_wrong"],
      rows);
  }

  // predikovana trieda pre kazdy obrazok testovacej mnoziny
  private static int[] Predictions(TrainedModel model, Dataset test)
    => [.. model.Network.PredictAll(test).Select(p => p.ArgMax())];
}
