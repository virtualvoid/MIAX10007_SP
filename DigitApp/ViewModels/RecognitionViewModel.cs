using System.Windows.Input;
using System.Windows.Media;
using DigitApp.Controls;
using DigitApp.Imaging;
using DigitApp.Infrastructure;
using DigitApp.Services;
using Fuzzy;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Zalozka s kreslenim: porovnanie modelov, fuzzy spolahlivost, feature mapy a hra.
/// </summary>
public sealed class RecognitionViewModel : ViewModelBase
{
  private readonly ModelRepository models;
  private readonly ReliabilityEstimator estimator;

  private float[]? lastInput; // posledna kresba vo formate MNIST, null ak je platno prazdne

  /// <summary>
  /// Vytvori view model, pripravi hru a zaregistruje reakciu na zmenu dostupnych modelov.
  /// </summary>
  /// <param name="models">Uloziste natrenovanych modelov.</param>
  /// <param name="estimator">Fuzzy odhadca spolahlivosti rozpoznania.</param>
  public RecognitionViewModel(ModelRepository models, ReliabilityEstimator estimator)
  {
    this.models = models;
    this.estimator = estimator;

    ClearCommand = new RelayCommand(Clear);
    Game = new GameViewModel(models, () => lastInput, Clear);

    models.ModelsChanged += (_, _) => ResetView();
    ResetView();
  }

  /// <summary>
  /// View na to zareaguje vymazanim platna.
  /// </summary>
  public event EventHandler? ClearRequested;

  /// <summary>
  /// Prikaz, ktory vymaze platno a vrati zobrazenie do vychodzieho stavu.
  /// </summary>
  public ICommand ClearCommand { get; }

  /// <summary>
  /// View model hry nad kreslenim cislic.
  /// </summary>
  public GameViewModel Game { get; }

  /// <summary>
  /// Co vidi siet na vstupe (28 x 28 po predspracovani).
  /// </summary>
  public ImageSource? Preview { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Panely s vysledkom rozpoznania pre jednotlive modely.
  /// </summary>
  public IReadOnlyList<ModelPanelViewModel> Panels { get; private set => SetField(ref field, value); } = [];

  /// <summary>
  /// Fuzzy hodnotenie spolahlivosti aktualneho rozpoznania.
  /// </summary>
  public ReliabilityViewModel Reliability { get; private set => SetField(ref field, value); } = ReliabilityViewModel.Idle;

  /// <summary>
  /// Feature mapy prvej konvolucnej vrstvy, alebo null ak nie su k dispozicii.
  /// </summary>
  public IReadOnlyList<ImageSource>? FirstConvMaps { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Feature mapy druhej konvolucnej vrstvy, alebo null ak nie su k dispozicii.
  /// </summary>
  public IReadOnlyList<ImageSource>? SecondConvMaps { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Upozornenie, ked nie je k dispozicii ziadny model.
  /// </summary>
  public string Notice { get; private set => SetField(ref field, value); } = string.Empty;

  /// <summary>
  /// Predspracuje kresbu, spusti ju cez vsetky dostupne modely a aktualizuje zobrazenie.
  /// </summary>
  /// <param name="pixels">Nakreslene ciary ako sivotonovy obrazok <see cref="DrawingPad.Size"/> x <see cref="DrawingPad.Size"/>.</param>
  public void Recognize(float[] pixels)
  {
    var input = MnistPreprocessor.Normalize(pixels, DrawingPad.Size);
    lastInput = input;
    Preview = GrayBitmap.Create(input, MnistPreprocessor.Side, MnistPreprocessor.Side);

    var probabilities = new Dictionary<ModelKind, float[]>();
    var panels = new List<ModelPanelViewModel>();
    IReadOnlyList<IReadOnlyList<ImageSource>> maps = [];
    foreach (var kind in models.Available.Order())
    {
      var network = models.Get(kind)!;

      // vystupy vsetkych vrstiev: z posledneho su pravdepodobnosti, ostatne ide na feature mapy
      var outputs = network.ForwardAll(input);
      probabilities[kind] = Network.Probabilities(outputs[^1]);
      panels.Add(ModelPanelViewModel.Create(kind, network, probabilities[kind]));
      if (maps.Count == 0)
      {
        maps = FeatureMapRenderer.Render(network, outputs);
      }
    }

    Panels = panels;
    FirstConvMaps = maps.ElementAtOrDefault(0);
    SecondConvMaps = maps.ElementAtOrDefault(1);
    Reliability = AssessReliability(probabilities);
  }

  /// <summary>
  /// Ohodnoti spolahlivost rozpoznania z pravdepodobnosti hlavneho a pripadne pomocneho modelu.
  /// </summary>
  /// <param name="probabilities">Pravdepodobnosti tried podla druhu modelu.</param>
  /// <returns>Zobrazenie spolahlivosti, alebo prazdny stav ak nie je k dispozicii hlavny model.</returns>
  private ReliabilityViewModel AssessReliability(Dictionary<ModelKind, float[]> probabilities)
  {
    if (ModelRoles.From(probabilities.Keys) is not { } roles)
    {
      return ReliabilityViewModel.Idle;
    }
    var secondary = roles.Secondary is { } kind ? probabilities[kind] : null;
    var assessment = estimator.Assess(probabilities[roles.Primary], secondary);
    return ReliabilityViewModel.From(assessment, roles.Primary.DisplayName, roles.Secondary?.DisplayName);
  }

  /// <summary>
  /// Vrati zobrazenie do vychodzieho stavu a vyziada vymazanie platna.
  /// </summary>
  private void Clear()
  {
    ResetView();
    ClearRequested?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Zahodi vysledky rozpoznania a znovu vytvori prazdne panely podla dostupnych modelov.
  /// </summary>
  private void ResetView()
  {
    lastInput = null;
    Preview = null;
    FirstConvMaps = null;
    SecondConvMaps = null;
    Reliability = ReliabilityViewModel.Idle;
    Panels = [.. models.Available.Order().Select(kind => ModelPanelViewModel.Create(kind, models.Get(kind)!, null))];
    Notice = Panels.Count == 0
      ? "Nie je k dispozícii žiadny model. Natrénuj ho v záložke Trénovanie."
      : string.Empty;
  }
}
