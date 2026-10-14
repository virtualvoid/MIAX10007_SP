using System.Windows.Input;
using DigitApp.Infrastructure;
using DigitApp.Services;
using Fuzzy;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Zalozka s vyhodnotenim modelov na testovacej mnozine vratane fuzzy analyzy spolahlivosti.
/// </summary>
public sealed class EvaluationViewModel : ViewModelBase
{
  private readonly ModelRepository models;
  private readonly DatasetService datasets;
  private readonly ReliabilityEstimator estimator;

  private IReadOnlyDictionary<ModelKind, EvaluationResult> results = new Dictionary<ModelKind, EvaluationResult>();

  /// <summary>
  /// Vytvori view model a pripravi prikaz na vyhodnotenie.
  /// </summary>
  /// <param name="models">Uloziste natrenovanych modelov.</param>
  /// <param name="datasets">Sluzba poskytujuca testovaciu mnozinu.</param>
  /// <param name="estimator">Fuzzy odhadca spolahlivosti predikcii.</param>
  public EvaluationViewModel(ModelRepository models, DatasetService datasets, ReliabilityEstimator estimator)
  {
    this.models = models;
    this.datasets = datasets;
    this.estimator = estimator;
    RunCommand = new AsyncRelayCommand(RunAsync, () => models.Available.Count > 0);
  }

  /// <summary>
  /// Prikaz, ktory spusti vyhodnotenie modelov.
  /// </summary>
  public ICommand RunCommand { get; }

  /// <summary>
  /// Udava, ci prave prebieha vyhodnotenie.
  /// </summary>
  public bool IsRunning { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Textovy stav vyhodnotenia zobrazeny pouzivatelovi.
  /// </summary>
  public string Status { get; private set => SetField(ref field, value); } = "Spusti vyhodnotenie.";

  /// <summary>
  /// Riadky tabulky s vysledkami jednotlivych modelov.
  /// </summary>
  public IReadOnlyList<ModelResultRow> ModelRows { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Ponuka modelov, pre ktore je mozne zobrazit confusion matrix.
  /// </summary>
  public IReadOnlyList<Choice<ModelKind>> ConfusionChoices { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Model, ktoreho confusion matrix sa zobrazuje; jeho zmena prekresli tabulku.
  /// </summary>
  public ModelKind? SelectedConfusionModel
  {
    get;
    set
    {
      if (SetField(ref field, value))
      {
        ConfusionCells = value is { } kind && results.TryGetValue(kind, out var result) ? ConfusionCell.Build(result) : [];
      }
    }
  }

  /// <summary>
  /// Bunky confusion matrix zvoleneho modelu.
  /// </summary>
  public IReadOnlyList<ConfusionCell> ConfusionCells { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Riadky tabulky s presnostou podla urovne fuzzy spolahlivosti.
  /// </summary>
  public IReadOnlyList<FuzzyLevelRow> FuzzyRows { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Slovne zhrnutie dosledkov fuzzy analyzy spolahlivosti.
  /// </summary>
  public string FuzzySummary { get; private set => SetField(ref field, value); } = string.Empty;

  /// <summary>
  /// Nacita testovaciu mnozinu, vyhodnoti kopie modelov a zobrazi vysledky.
  /// </summary>
  private async Task RunAsync()
  {
    IsRunning = true;
    try
    {
      Status = "Načítavam testovaciu množinu…";
      var test = await datasets.LoadTestAsync();

      // vyhodnocuje sa na kopiach modelov, originaly pouziva zalozka Rozpoznavanie
      var networks = models.Available.Order().ToDictionary(kind => kind, models.CreateIsolatedCopy);

      Status = "Vyhodnocujem…";
      var outcome = await Task.Run(() => Compute(networks, test));
      Show(outcome, networks);
      Status = $"Hotovo, vyhodnotených {test.Count:N0} obrázkov.";
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
    {
      Status = $"Chyba: {ex.Message}";
    }
    finally
    {
      IsRunning = false;
    }
  }

  /// <summary>
  /// Vypocita vysledky vyhodnotenia modelov a fuzzy analyzu spolahlivosti na testovacej mnozine.
  /// </summary>
  /// <param name="networks">Izolovane kopie sieti podla druhu modelu.</param>
  /// <param name="test">Testovacia mnozina.</param>
  /// <returns>Vysledky modelov spolu s analyzou spolahlivosti, ak je k dispozicii hlavny model.</returns>
  private Outcome Compute(Dictionary<ModelKind, Network> networks, Dataset test)
  {
    var probabilities = networks.ToDictionary(kv => kv.Key, kv => kv.Value.PredictAll(test));
    var evaluations = probabilities.ToDictionary(
      kv => kv.Key,
      kv => EvaluationResult.From(kv.Value, test.Labels, networks[kv.Key].Classes));

    if (ModelRoles.From(networks.Keys) is not { } roles)
    {
      return new Outcome(evaluations, null, null);
    }
    var secondary = roles.Secondary is { } kind ? probabilities[kind] : null;
    var report = ReliabilityAnalyzer.Analyze(estimator, probabilities[roles.Primary], secondary, test.Labels);
    return new Outcome(evaluations, report, roles);
  }

  /// <summary>
  /// Premietne vypocitane vysledky do tabuliek, confusion matrix a fuzzy zhrnutia.
  /// </summary>
  /// <param name="outcome">Vysledok vypoctu.</param>
  /// <param name="networks">Siete, z ktorych sa berie pocet parametrov.</param>
  private void Show(Outcome outcome, Dictionary<ModelKind, Network> networks)
  {
    results = outcome.Results;
    ModelRows =
    [
      .. outcome.Results.OrderBy(kv => kv.Key).Select(kv => new ModelResultRow(
        kv.Key.DisplayName,
        $"{kv.Value.Accuracy:P2}",
        $"{kv.Value.Loss:F4}",
        $"{kv.Value.Errors:N0} z {kv.Value.Count:N0}",
        $"{networks[kv.Key].ParameterCount:N0}"))
    ];
    ConfusionChoices = [.. outcome.Results.Keys.Order().Select(kind => new Choice<ModelKind>(kind, kind.DisplayName))];

    // nastavenie vyberu zaroven prekresli confusion matrix
    SelectedConfusionModel = null;
    SelectedConfusionModel = outcome.Roles?.Primary ?? outcome.Results.Keys.Order().First();

    if (outcome.Report is { } report && outcome.Roles is { } roles)
    {
      ShowFuzzy(report, roles);
    }
  }

  /// <summary>
  /// Naplni tabulku urovni spolahlivosti a slovne zhrnutie fuzzy analyzy.
  /// </summary>
  private void ShowFuzzy(ReliabilityReport report, ModelRoles roles)
  {
    FuzzyRows =
    [
      .. report.Levels.OrderByDescending(l => l.Level).Select(l => new FuzzyLevelRow(
        LevelName(l.Level),
        $"{l.Count:N0}",
        $"{(double)l.Count / report.Total:P1}",
        l.Count == 0 ? "–" : $"{l.Accuracy:P2}"))
    ];

    var name = roles.Primary.DisplayName;
    FuzzySummary =
      $"Presnosť modelu {name} na všetkých obrázkoch: {report.OverallAccuracy:P2}. " +
      $"Ak by sa prijali len predikcie so strednou alebo vysokou spoľahlivosťou, presnosť by bola " +
      $"{report.AccuracyAtLeast(ReliabilityLevel.Medium):P2} pri pokrytí {report.CoverageAtLeast(ReliabilityLevel.Medium):P1}; " +
      $"len s vysokou spoľahlivosťou {report.AccuracyAtLeast(ReliabilityLevel.High):P2} pri pokrytí {report.CoverageAtLeast(ReliabilityLevel.High):P1}.";
  }

  /// <summary>
  /// Vrati slovensky nazov urovne spolahlivosti.
  /// </summary>
  /// <returns>Slovensky nazov urovne spolahlivosti.</returns>
  private static string LevelName(ReliabilityLevel level) => level switch
  {
    ReliabilityLevel.High => "Vysoká",
    ReliabilityLevel.Medium => "Stredná",
    _ => "Nízka"
  };

  /// <summary>
  /// Medzivysledok vypoctu vyhodnotenia predavany z pracovneho vlakna do zobrazenia.
  /// </summary>
  /// <param name="Results">Vysledky vyhodnotenia podla druhu modelu.</param>
  /// <param name="Report">Analyza spolahlivosti, alebo null ak nie je k dispozicii hlavny model.</param>
  /// <param name="Roles">Rozdelenie roli modelov, alebo null ak nie je k dispozicii hlavny model.</param>
  private sealed record Outcome(
    IReadOnlyDictionary<ModelKind, EvaluationResult> Results,
    ReliabilityReport? Report,
    ModelRoles? Roles);
}
