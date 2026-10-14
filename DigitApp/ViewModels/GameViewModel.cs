using System.Windows.Input;
using DigitApp.Game;
using DigitApp.Infrastructure;
using DigitApp.Services;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Hra, v ktorej pouzivatel kresli zadane cislice a body dostava podla istoty siete.
/// </summary>
public sealed class GameViewModel : ViewModelBase
{
  /// <summary>
  /// Vychodzi text tlacidla na spustenie hry.
  /// </summary>
  private const string StartText = "Hra: nakresli zadanú číslicu";

  private readonly ModelRepository models;
  private readonly Func<float[]?> getInput;
  private readonly Action clearCanvas;
  private readonly Random random = new();

  private int target;
  private int round;
  private int score;
  private int best;

  /// <summary>
  /// Vytvori hru a pripravi jej prikazy.
  /// </summary>
  /// <param name="models">Uloziste natrenovanych modelov.</param>
  /// <param name="getInput">Posledna rozpoznavana kresba vo formate MNIST, alebo null ak je platno prazdne.</param>
  /// <param name="clearCanvas">Vymaze platno (po kazdom kole).</param>
  public GameViewModel(ModelRepository models, Func<float[]?> getInput, Action clearCanvas)
  {
    this.models = models;
    this.getInput = getInput;
    this.clearCanvas = clearCanvas;

    StartCommand = new RelayCommand(Start, () => !IsRunning && models.Available.Count > 0);
    SubmitCommand = new RelayCommand(Submit, () => IsRunning);
    EndCommand = new RelayCommand(End);
  }

  /// <summary>
  /// Prikaz, ktory spusti novu hru.
  /// </summary>
  public ICommand StartCommand { get; }

  /// <summary>
  /// Prikaz, ktory odovzda aktualnu kresbu na vyhodnotenie v danom kole.
  /// </summary>
  public ICommand SubmitCommand { get; }

  /// <summary>
  /// Prikaz, ktory predcasne ukonci hru.
  /// </summary>
  public ICommand EndCommand { get; }

  /// <summary>
  /// Udava, ci prave prebieha hra.
  /// </summary>
  public bool IsRunning { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Udava, ci sa ma zobrazit panel hry.
  /// </summary>
  public bool IsPanelVisible { get; private set => SetField(ref field, value); }

  /// <summary>
  /// Text tlacidla na spustenie hry.
  /// </summary>
  public string StartButtonText { get; private set => SetField(ref field, value); } = StartText;

  /// <summary>
  /// Informacia o kole, skore a najlepsom vysledku.
  /// </summary>
  public string Info { get; private set => SetField(ref field, value); } = string.Empty;

  /// <summary>
  /// Cislica, ktoru ma pouzivatel v aktualnom kole nakreslit.
  /// </summary>
  public string Target { get; private set => SetField(ref field, value); } = string.Empty;

  /// <summary>
  /// Sprava o vysledku posledneho kola alebo vyzva pre pouzivatela.
  /// </summary>
  public string Message { get; private set => SetField(ref field, value); } = string.Empty;

  /// <summary>
  /// Spusti novu hru od prveho kola s nulovym skore.
  /// </summary>
  private void Start()
  {
    round = 1;
    score = 0;
    IsRunning = true;
    IsPanelVisible = true;
    NextTarget();

    var roles = ModelRoles.From(models.Available);
    Message = roles is { Primary: ModelKind.Cnn, Secondary: not null }
      ? $"Body = istota CNN. Bonus +{GameRules.FooledOpponentBonus}, ak sa MLP pomýli."
      : "Body = istota siete.";
  }

  /// <summary>
  /// Ukonci hru a skryje jej panel.
  /// </summary>
  private void End()
  {
    IsRunning = false;
    IsPanelVisible = false;
    StartButtonText = StartText;
  }

  /// <summary>
  /// Nahodne vyberie dalsiu cislicu, aktualizuje informacie o kole a vymaze platno.
  /// </summary>
  private void NextTarget()
  {
    target = random.Next(10);
    Target = target.ToString();
    Info = $"Kolo {round}/{GameRules.Rounds}  ·  skóre {score}  ·  najlepšie {best}";
    clearCanvas();
  }

  /// <summary>
  /// Vyhodnoti aktualnu kresbu, pripise body a posunie hru na dalsie kolo alebo ju ukonci.
  /// </summary>
  private void Submit()
  {
    if (getInput() is not { } input)
    {
      Message = "Najprv nakresli číslicu.";
      return;
    }
    if (ModelRoles.From(models.Available) is not { } roles)
    {
      End();
      return;
    }

    var main = models.Get(roles.Primary)!.Predict(input);
    // bonus za oklamanie sa udeluje len pri hre proti CNN, kedy je MLP "slabsi" supiter
    var opponent = roles is { Primary: ModelKind.Cnn, Secondary: { } secondary }
      ? models.Get(secondary)!.Predict(input)
      : null;
    var result = GameRules.Score(target, main, opponent);
    var name = roles.Primary.DisplayName;

    var message = result.Hit
      ? $"+{result.Points} b: {name} rozpoznala {result.Predicted} s istotou {result.Confidence:P0}."
        + (result.FooledOpponent ? $" Zahŕňa bonus +{GameRules.FooledOpponentBonus}: oklamal si MLP!" : string.Empty)
      : $"0 b: {name} videla {result.Predicted} ({result.Confidence:P0}), nie {target}.";

    score += result.Points;
    round++;
    if (round > GameRules.Rounds)
    {
      Finish(message);
      return;
    }

    NextTarget();
    Message = message;
  }

  /// <summary>
  /// Dokonci hru, aktualizuje najlepsie skore a zobrazi zaverecnu spravu.
  /// </summary>
  /// <param name="lastRoundMessage">Sprava o vysledku posledneho kola.</param>
  private void Finish(string lastRoundMessage)
  {
    var record = score > best;
    best = Math.Max(best, score);
    IsRunning = false;
    Info = $"Koniec hry  ·  skóre {score}  ·  najlepšie {best}";
    Message = lastRoundMessage + (record ? " Nový rekord!" : string.Empty);
    Target = "–";
    StartButtonText = "Hrať znova";
    clearCanvas();
  }
}
