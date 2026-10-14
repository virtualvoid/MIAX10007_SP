using System.Diagnostics;

namespace NeuralNet;

/// <summary>
/// Trenovanie siete na datasete po epochach.
/// </summary>
public static class NetworkTrainer
{
  /// <summary>
  /// Po kolkych davkach sa hlasi priebezny postup.
  /// </summary>
  private const int ReportEveryBatches = 25;

  /// <summary>
  /// Natrenuje siet na trenovacej mnozine a po kazdej epoche ju vyhodnoti na testovacej.
  /// Je synchronna (zatazuje volajuce vlakno), UI ju ma spustat cez Task.Run. Zrusenie vyhodi OperationCanceledException.
  /// </summary>
  /// <param name="network">Trenovana siet.</param>
  /// <param name="train">Trenovacia mnozina.</param>
  /// <param name="test">Testovacia mnozina, na ktorej sa vyhodnoti kazda epocha.</param>
  /// <param name="options">Nastavenia trenovania.</param>
  /// <param name="progress">Volitelny prijemca priebezneho postupu.</param>
  /// <param name="cancellationToken">Token na zrusenie trenovania.</param>
  /// <returns>Vysledky vsetkych epoch.</returns>
  public static IReadOnlyList<EpochReport> Train(
    Network network,
    Dataset train,
    Dataset test,
    TrainingOptions options,
    IProgress<TrainingProgress>? progress = null,
    CancellationToken cancellationToken = default)
  {
    Validate(options, train);

    var rng = new Random(options.Seed);
    var optimizer = Optimizers.Create(options.Optimizer, options.LearningRate);
    var size = network.InSize;
    var batchSize = options.BatchSize;
    var batches = train.Count / batchSize; // posledna nekompletna davka sa zahadzuje
    var order = Enumerable.Range(0, train.Count).ToArray();
    var bx = new float[batchSize * size];
    var by = new byte[batchSize];
    var reports = new List<EpochReport>();

    for (var epoch = 1; epoch <= options.Epochs; epoch++)
    {
      var stopwatch = Stopwatch.StartNew();

      // kazdu epochu inak pomiesat poradie obrazkov
      rng.Shuffle(order);

      var lossSum = 0f;
      var correct = 0;
      for (var b = 0; b < batches; b++)
      {
        cancellationToken.ThrowIfCancellationRequested();

        // poskladam davku z pomiesanych indexov
        for (var i = 0; i < batchSize; i++)
        {
          var idx = order[b * batchSize + i];
          Array.Copy(train.Images, idx * size, bx, i * size, size);
          by[i] = train.Labels[idx];
        }

        var (loss, ok) = network.TrainBatch(bx, by, batchSize, optimizer);
        lossSum += loss;
        correct += ok;

        if ((b + 1) % ReportEveryBatches == 0)
        {
          progress?.Report(new TrainingProgress(epoch, options.Epochs, (double)(b + 1) / batches, null));
        }
      }

      var evaluation = network.Evaluate(test);
      var report = new EpochReport(
        epoch,
        lossSum / batches,
        (float)correct / (batches * batchSize),
        evaluation.Loss,
        evaluation.Accuracy,
        stopwatch.Elapsed);
      reports.Add(report);
      progress?.Report(new TrainingProgress(epoch, options.Epochs, 1, report));
    }
    return reports;
  }

  /// <summary>
  /// Overi, ze nastavenia trenovania davaju zmysel pre dany dataset.
  /// </summary>
  private static void Validate(TrainingOptions options, Dataset train)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(options.Epochs, 1);
    ArgumentOutOfRangeException.ThrowIfLessThan(options.BatchSize, 1);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(options.BatchSize, train.Count);
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.LearningRate, 0f);
  }
}
