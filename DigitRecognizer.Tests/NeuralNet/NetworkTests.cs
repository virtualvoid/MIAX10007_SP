using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy triedy <see cref="Network"/> a <see cref="NetworkTrainer"/>: trenovanie, ukladanie a nacitanie vah,
/// kopirovanie vah, skladanie vrstiev, vypocet pravdepodobnosti a vyhodnotenie.
/// </summary>
public sealed class NetworkTests
{
  /// <summary>
  /// Pocet vstupnych priznakov v testovacich datach.
  /// </summary>
  private const int Features = 4;

  /// <summary>
  /// Pocet tried, medzi ktorymi testovacia siet rozlisuje.
  /// </summary>
  private const int Classes = 3;

  /// <summary>
  /// Vytvori nahodny dataset, v ktorom je trieda dana indexom najvacsej z prvych troch hodnot priznakov
  /// a zvysny priznak je sum.
  /// </summary>
  /// <param name="count">Pocet vzoriek.</param>
  /// <param name="seed">Zrno generatora nahodnych cisel pre reprodukovatelnost.</param>
  /// <returns>Dataset s obrazmi a ich oznaceniami.</returns>
  private static Dataset CreateDataset(int count, int seed)
  {
    var rng = new Random(seed);
    var images = new float[count * Features];
    var labels = new byte[count];
    for (var i = 0; i < count; i++)
    {
      for (var f = 0; f < Features; f++)
      {
        images[i * Features + f] = rng.NextSingle();
      }
      labels[i] = (byte)images.AsSpan(i * Features, Classes).ToArray().ArgMax();
    }
    return new Dataset(images, labels);
  }

  /// <summary>
  /// Vytvori malu plne prepojenu siet (vstup, skryta vrstva so 16 neuronmi a funkciou ReLU, vystup) s nahodne
  /// inicializovanymi vahami.
  /// </summary>
  /// <param name="seed">Zrno generatora nahodnych cisel pre inicializaciu vah.</param>
  /// <returns>Novo vytvorena siet.</returns>
  private static Network CreateNetwork(int seed)
  {
    var rng = new Random(seed);
    return new Network()
      .Add(new Dense(Features, 16, rng)).Add(new ReLU(16))
      .Add(new Dense(16, Classes, rng));
  }

  /// <summary>
  /// Over, ze trener nauci siet jednoduche pravidlo: po 40 epochach je presnost na testovacich datach
  /// vyssia ako 90 percent a trenovacia chyba klesne oproti prvej epoche.
  /// </summary>
  [Fact]
  public void TrainerLearnsSimpleRule()
  {
    var network = CreateNetwork(1);
    var train = CreateDataset(600, 2);
    var test = CreateDataset(200, 3);

    var reports = NetworkTrainer.Train(network, train, test, new TrainingOptions(Epochs: 40, BatchSize: 32, LearningRate: 0.01f));

    Assert.Equal(40, reports.Count);
    Assert.True(reports[^1].TestAccuracy > 0.9f, $"presnost {reports[^1].TestAccuracy:P1}");
    Assert.True(reports[^1].TrainLoss < reports[0].TrainLoss);
  }

  /// <summary>
  /// Over, ze trener nahlasi dokoncenie kazdej epochy, a to v poradi 1, 2, 3.
  /// </summary>
  [Fact]
  public void TrainerReportsProgressForEveryEpoch()
  {
    var completed = new List<int>();
    var progress = new SynchronousProgress<TrainingProgress>(p =>
    {
      if (p.Completed is not null)
      {
        completed.Add(p.Completed.Epoch);
      }
    });

    NetworkTrainer.Train(CreateNetwork(1), CreateDataset(100, 2), CreateDataset(50, 3),
      new TrainingOptions(Epochs: 3, BatchSize: 20), progress);

    Assert.Equal([1, 2, 3], completed);
  }

  /// <summary>
  /// Over, ze trenovanie sa pri uz zrusenom tokene zrusenia ukonci vynimkou OperationCanceledException.
  /// </summary>
  [Fact]
  public void TrainerStopsWhenCancelled()
  {
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    Assert.Throws<OperationCanceledException>(() => NetworkTrainer.Train(
      CreateNetwork(1), CreateDataset(100, 2), CreateDataset(50, 3), new TrainingOptions(Epochs: 3, BatchSize: 20),
      cancellationToken: cts.Token));
  }

  /// <summary>
  /// Over, ze trener odmietne davku vacsiu nez pocet vzoriek datasetu vynimkou ArgumentOutOfRangeException.
  /// </summary>
  [Fact]
  public void TrainerRejectsBatchLargerThanDataset()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => NetworkTrainer.Train(
      CreateNetwork(1), CreateDataset(10, 2), CreateDataset(10, 3), new TrainingOptions(BatchSize: 64)));
  }

  /// <summary>
  /// Over, ze po ulozeni vah jednej siete a ich nacitani do druhej siete obe siete vratia rovnaku predikciu.
  /// </summary>
  [Fact]
  public void SaveAndLoadPreserveWeights()
  {
    var original = CreateNetwork(1);
    var copy = CreateNetwork(2);
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.weights");
    var input = new float[] { 0.1f, 0.9f, 0.4f, 0.3f };

    try
    {
      original.Save(path);
      copy.Load(path);
    }
    finally
    {
      File.Delete(path);
    }

    Assert.Equal(original.Predict(input), copy.Predict(input));
  }

  /// <summary>
  /// Over, ze nacitanie vah ulozenych z inej architektury siete skonci vynimkou InvalidDataException.
  /// </summary>
  [Fact]
  public void LoadRejectsDifferentArchitecture()
  {
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.weights");
    var other = new Network().Add(new Dense(Features, 5, new Random(1)));

    try
    {
      other.Save(path);
      Assert.Throws<InvalidDataException>(() => CreateNetwork(1).Load(path));
    }
    finally
    {
      File.Delete(path);
    }
  }

  /// <summary>
  /// Over, ze po skopirovani vah zo zdrojovej siete do cielovej obe siete vratia rovnaku predikciu.
  /// </summary>
  [Fact]
  public void CopyWeightsFromMakesNetworksPredictTheSame()
  {
    var source = CreateNetwork(1);
    var target = CreateNetwork(2);
    var input = new float[] { 0.1f, 0.9f, 0.4f, 0.3f };

    target.CopyWeightsFrom(source);

    Assert.Equal(source.Predict(input), target.Predict(input));
  }

  /// <summary>
  /// Over, ze pridanie vrstvy, ktorej vstupna velkost nezodpoveda vystupu predchadzajucej vrstvy,
  /// skonci vynimkou ArgumentException.
  /// </summary>
  [Fact]
  public void AddRejectsLayersWithMismatchedSizes()
  {
    var network = new Network().Add(new Dense(4, 8, new Random(1)));

    Assert.Throws<ArgumentException>(() => network.Add(new Dense(9, 3, new Random(1))));
  }

  /// <summary>
  /// Over, ze pravdepodobnosti vypocitane z logitov davaju sucet 1 a ze velke logity nevyvolaju pretecenie,
  /// pricom najvyssia pravdepodobnost zostane pri najvacsom logite.
  /// </summary>
  [Fact]
  public void ProbabilitiesSumToOne()
  {
    var p = Network.Probabilities([2f, -1f, 0.5f, 1000f]);

    Assert.Equal(1f, p.Sum(), 1e-5f);
    Assert.Equal(3, p.ArgMax()); // velke logity nesmu pretect
  }

  /// <summary>
  /// Over, ze vyhodnotenie siete vrati spravny pocet vzoriek a ze pocet chyb zodpoveda poctu vzoriek
  /// mimo diagonaly matice zamien.
  /// </summary>
  [Fact]
  public void EvaluateBuildsConfusionMatrix()
  {
    var network = CreateNetwork(1);
    var data = CreateDataset(120, 5);

    var result = network.Evaluate(data);

    Assert.Equal(120, result.Count);
    Assert.Equal(result.Errors, 120 - Enumerable.Range(0, Classes).Sum(c => result.Confusion[c, c]));
  }

  /// <summary>
  /// Implementacia rozhrania IProgress, ktora vola obsluznu funkciu synchronne. Standardna trieda Progress
  /// by funkciu volala asynchronne, v testoch vsak je potrebne mat poradie hlaseni deterministicke.
  /// </summary>
  /// <typeparam name="T">Typ hlaseneho stavu.</typeparam>
  /// <param name="handler">Funkcia volana pri kazdom hlaseni.</param>
  private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
  {
    public void Report(T value) => handler(value);
  }
}
