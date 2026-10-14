using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using DigitApp.Controls;
using DigitApp.Infrastructure;
using DigitApp.Services;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Zalozka s trenovanim: nastavenie, spustenie a zastavenie, priebeh v grafoch a v zazname.
/// </summary>
public sealed class TrainingViewModel : ViewModelBase
{
  private readonly ModelRepository models;
  private readonly DatasetService datasets;

  // hodnoty po epochach, z nich sa skladaju rady grafov
  private readonly List<double> trainAccuracy = [];
  private readonly List<double> testAccuracy = [];
  private readonly List<double> trainLoss = [];
  private readonly List<double> testLoss = [];

  private CancellationTokenSource? cancellation;

  /// <summary>
  /// Vytvori view model a pripravi prikazy na trenovanie.
  /// </summary>
  /// <param name="models">Uloziste modelov, do ktoreho sa uklada natrenovana siet.</param>
  /// <param name="datasets">Sluzba poskytujuca dataset MNIST.</param>
  public TrainingViewModel(ModelRepository models, DatasetService datasets)
  {
    this.models = models;
    this.datasets = datasets;
    DatasetDirectory = datasets.DirectoryPath;

    StartCommand = new AsyncRelayCommand(StartAsync, () => IsIdle);
    StopCommand = new RelayCommand(() => cancellation?.Cancel(), () => IsRunning);
    ResetModelsCommand = new RelayCommand(ResetModels, () => IsIdle);
  }

  /// <summary>
  /// Prikaz, ktory spusti trenovanie.
  /// </summary>
  public ICommand StartCommand { get; }

  /// <summary>
  /// Prikaz, ktory zastavi prebiehajuce trenovanie.
  /// </summary>
  public ICommand StopCommand { get; }

  /// <summary>
  /// Prikaz, ktory obnovi povodne modely dodane s aplikaciou.
  /// </summary>
  public ICommand ResetModelsCommand { get; }

  /// <summary>
  /// Ponuka druhov modelov, ktore je mozne trenovat.
  /// </summary>
  public IReadOnlyList<Choice<ModelKind>> ModelChoices { get; } =
  [
    new(ModelKind.Mlp, "MLP (plne prepojená sieť)"),
    new(ModelKind.Cnn, "CNN (konvolučná sieť)")
  ];

  /// <summary>
  /// Ponuka optimalizatorov pouzitelnych pri trenovani.
  /// </summary>
  public IReadOnlyList<Choice<OptimizerKind>> OptimizerChoices { get; } =
  [
    new(OptimizerKind.Adam, "Adam"),
    new(OptimizerKind.Sgd, "SGD")
  ];

  /// <summary>
  /// Druh modelu zvoleny na trenovanie.
  /// </summary>
  public ModelKind SelectedModel { get; set => SetField(ref field, value); } = ModelKind.Cnn;

  /// <summary>
  /// Optimalizator zvoleny na trenovanie.
  /// </summary>
  public OptimizerKind SelectedOptimizer { get; set => SetField(ref field, value); } = OptimizerKind.Adam;

  /// <summary>
  /// Pocet epoch trenovania.
  /// </summary>
  public int Epochs { get; set => SetField(ref field, value); } = 10;

  /// <summary>
  /// Velkost davky vzoriek pri trenovani.
  /// </summary>
  public int BatchSize { get; set => SetField(ref field, value); } = 64;

  /// <summary>
  /// Rychlost ucenia (learning rate) optimalizatora.
  /// </summary>
  public double LearningRate { get; set => SetField(ref field, value); } = 0.001;

  /// <summary>
  /// Priecinok so subormi datasetu MNIST.
  /// </summary>
  public string DatasetDirectory
  {
    get;
    set
    {
      if (SetField(ref field, value))
      {
        datasets.SetDirectory(value);
      }
    }
  } = string.Empty;

  /// <summary>
  /// Udava, ci prave prebieha trenovanie.
  /// </summary>
  public bool IsRunning
  {
    get;
    private set
    {
      if (SetField(ref field, value))
      {
        OnPropertyChanged(nameof(IsIdle));
      }
    }
  }

  /// <summary>
  /// Udava, ci trenovanie neprebieha a je mozne menit nastavenia.
  /// </summary>
  public bool IsIdle => !IsRunning;

  /// <summary>
  /// Postup celeho trenovania v percentach.
  /// </summary>
  public double ProgressPercent { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Textovy stav trenovania zobrazeny pouzivatelovi.
  /// </summary>
  public string Status { get; private set => SetField(ref field, value); } = "Pripravené.";

  /// <summary>
  /// Pocet epoch aktualneho behu, od ktoreho zavisi rozsah osi grafov.
  /// </summary>
  public int ExpectedEpochs { get; private set => SetField(ref field, value); } = 10;

  /// <summary>
  /// Rady grafu presnosti po epochach (trenovacia a testovacia).
  /// </summary>
  public IReadOnlyList<ChartSeries> AccuracySeries { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Rady grafu stratovej funkcie po epochach (trenovacia a testovacia).
  /// </summary>
  public IReadOnlyList<ChartSeries> LossSeries { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Zaznam o priebehu trenovania, jeden riadok na epochu.
  /// </summary>
  public ObservableCollection<string> Log { get; } = [];

  /// <summary>
  /// Nacita dataset, natrenuje novu siet a po uspesnom dokonceni ju ulozi do uloziska modelov.
  /// </summary>
  private async Task StartAsync()
  {
    if (Validate() is { } error)
    {
      Status = error;
      return;
    }

    IsRunning = true;
    ResetProgress();
    cancellation = new CancellationTokenSource();
    var token = cancellation.Token;
    try
    {
      Status = "Načítavam dataset…";
      var train = await datasets.LoadTrainAsync();
      var test = await datasets.LoadTestAsync();

      var options = new TrainingOptions(Epochs, BatchSize, (float)LearningRate, SelectedOptimizer);
      var kind = SelectedModel;

      // trenuje sa nova siet, takze model v zalozke Rozpoznavanie ostava pouzitelny, kym trenovanie neskonci
      var network = Models.Create(kind, new Random(options.Seed));
      var progress = new Progress<TrainingProgress>(OnProgress);

      Status = "Trénujem…";
      await Task.Run(() => NetworkTrainer.Train(network, train, test, options, progress, token), token);

      models.Set(kind, network);
      Status = $"Hotovo. Model {kind.DisplayName} bol uložený a používa sa v záložke Rozpoznávanie.";
    }
    catch (OperationCanceledException)
    {
      Status = "Trénovanie zastavené, model nebol uložený.";
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
    {
      Status = $"Chyba: {ex.Message}";
    }
    finally
    {
      cancellation.Dispose();
      cancellation = null;
      IsRunning = false;
    }
  }

  /// <summary>
  /// Skontroluje nastavenia trenovania a dostupnost datasetu.
  /// </summary>
  /// <returns>Text chyby, alebo null ak je vsetko v poriadku.</returns>
  private string? Validate()
  {
    if (Epochs is < 1 or > 500)
    {
      return "Počet epoch musí byť od 1 do 500.";
    }
    if (BatchSize is < 1 or > 4096)
    {
      return "Veľkosť dávky musí byť od 1 do 4096.";
    }
    if (LearningRate is <= 0 or > 1)
    {
      return "Learning rate musí byť väčší ako 0 a najviac 1.";
    }
    if (!Mnist.IsAvailable(DatasetDirectory))
    {
      return "Zvolený priečinok neobsahuje súbory datasetu MNIST (train-images.idx3-ubyte a ďalšie).";
    }
    return null;
  }

  /// <summary>
  /// Spracuje hlasenie o postupe, aktualizuje stav, grafy a zaznam po dokonceni epochy.
  /// </summary>
  /// <param name="progress">Aktualny postup trenovania.</param>
  private void OnProgress(TrainingProgress progress)
  {
    ProgressPercent = progress.Overall * 100;
    Status = $"Trénujem, epocha {progress.Epoch}/{progress.Epochs}…";
    if (progress.Completed is not { } report)
    {
      return;
    }

    trainAccuracy.Add(report.TrainAccuracy * 100);
    testAccuracy.Add(report.TestAccuracy * 100);
    trainLoss.Add(report.TrainLoss);
    testLoss.Add(report.TestLoss);
    AccuracySeries =
    [
      new ChartSeries("trénovacia", Brushes.SteelBlue, [.. trainAccuracy]),
      new ChartSeries("testovacia", Brushes.OrangeRed, [.. testAccuracy])
    ];
    LossSeries =
    [
      new ChartSeries("trénovacia", Brushes.SteelBlue, [.. trainLoss]),
      new ChartSeries("testovacia", Brushes.OrangeRed, [.. testLoss])
    ];

    Log.Add($"Epocha {report.Epoch,2}/{progress.Epochs}  train loss {report.TrainLoss:F4}  train acc {report.TrainAccuracy:P2}" +
            $"  |  test loss {report.TestLoss:F4}  test acc {report.TestAccuracy:P2}  ({report.Duration.TotalSeconds:F1} s)");
  }

  /// <summary>
  /// Vynuluje grafy, zaznam a ukazovatel postupu pred novym trenovanim.
  /// </summary>
  private void ResetProgress()
  {
    trainAccuracy.Clear();
    testAccuracy.Clear();
    trainLoss.Clear();
    testLoss.Clear();
    AccuracySeries = [];
    LossSeries = [];
    ExpectedEpochs = Epochs;
    ProgressPercent = 0;
    Log.Clear();
  }

  /// <summary>
  /// Obnovi povodne modely dodane s aplikaciou.
  /// </summary>
  private void ResetModels()
  {
    models.ResetToBundled();
    Status = "Obnovené pôvodné modely, ktoré sú súčasťou aplikácie.";
  }
}
