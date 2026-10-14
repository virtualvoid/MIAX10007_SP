using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Studia vplyvu optimalizatora a learning rate na ucenie MLP: porovna Adam a SGD pri niekolkych hodnotach learning rate.
/// </summary>
internal static class OptimizerStudy
{
  private const int BatchSize = 64;

  private static readonly (OptimizerKind Optimizer, float LearningRate)[] Configurations =
  [
    (OptimizerKind.Adam, 0.0003f),
    (OptimizerKind.Adam, 0.001f),
    (OptimizerKind.Adam, 0.003f),
    (OptimizerKind.Sgd, 0.01f),
    (OptimizerKind.Sgd, 0.1f)
  ];

  /// <summary>
  /// Natrenuje MLP pre kazdu konfiguraciu a seed a zapise vysledky po epochach.
  /// </summary>
  /// <param name="options">Nastavenie experimentov.</param>
  /// <param name="train">Trenovacia mnozina.</param>
  /// <param name="test">Testovacia mnozina.</param>
  public static void Run(ExperimentOptions options, Dataset train, Dataset test)
  {
    var rows = new List<IReadOnlyList<string>>();
    foreach (var (optimizer, learningRate) in Configurations)
    {
      for (var seed = 1; seed <= options.OptimizerSeeds; seed++)
      {
        var network = Models.Create(ModelKind.Mlp, new Random(seed));
        var trainingOptions = new TrainingOptions(options.OptimizerEpochs, BatchSize, learningRate, optimizer, seed);
        var reports = NetworkTrainer.Train(network, train, test, trainingOptions);

        foreach (var r in reports)
        {
          rows.Add([
            optimizer.ToString(), CsvFile.Number(learningRate, 4), CsvFile.Number(seed), CsvFile.Number(r.Epoch),
            CsvFile.Number(r.TrainLoss), CsvFile.Number(r.TestLoss), CsvFile.Number(r.TestAccuracy)
          ]);
        }
        Console.WriteLine($"[optimizers] {optimizer} lr {learningRate} seed {seed}: presnost {reports[^1].TestAccuracy:P2}");
      }
    }
    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "optimizer_study.csv"),
      ["optimizer", "learning_rate", "seed", "epoch", "train_loss", "test_loss", "test_accuracy"],
      rows);
  }
}
