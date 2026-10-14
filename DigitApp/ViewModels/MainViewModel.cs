using DigitApp.Services;
using Fuzzy;

namespace DigitApp.ViewModels;

/// <summary>
/// Koren aplikacie: vytvori sluzby a view modely jednotlivych zalozok.
/// </summary>
public sealed class MainViewModel
{
  /// <summary>
  /// Vytvori sluzby a view modely vsetkych zalozok.
  /// </summary>
  public MainViewModel()
  {
    var settings = new SettingsStore();
    var datasets = new DatasetService(settings);
    var models = new ModelRepository();
    var estimator = new ReliabilityEstimator();

    Recognition = new RecognitionViewModel(models, estimator);
    Training = new TrainingViewModel(models, datasets);
    Evaluation = new EvaluationViewModel(models, datasets, estimator);
  }

  /// <summary>
  /// View model zalozky Rozpoznavanie.
  /// </summary>
  public RecognitionViewModel Recognition { get; }

  /// <summary>
  /// View model zalozky Trenovanie.
  /// </summary>
  public TrainingViewModel Training { get; }

  /// <summary>
  /// View model zalozky Vyhodnotenie.
  /// </summary>
  public EvaluationViewModel Evaluation { get; }
}
