namespace Fuzzy;

/// <summary>
/// Fuzzy hodnotenie spolahlivosti rozpoznania cislice. Vstupy (z pravdepodobnosti neuronovych sieti):
/// istota (pravdepodobnost predikovanej triedy), rozdiel prvej a druhej najpravdepodobnejsej triedy
/// a zhoda druheho modelu (jeho pravdepodobnost pre tu istu triedu). Vystupom je spolahlivost 0..100.
/// Ak je k dispozicii len jeden model, pouzije sa pravidlova baza bez zhody.
/// </summary>
public sealed class ReliabilityEstimator
{
  /// <summary>
  /// Nazov vstupu istoty (pravdepodobnost predikovanej triedy).
  /// </summary>
  public const string ConfidenceInput = "Istota";

  /// <summary>
  /// Nazov vstupu rozdielu medzi prvou a druhou najpravdepodobnejsou triedou.
  /// </summary>
  public const string MarginInput = "Rozdiel";

  /// <summary>
  /// Nazov vstupu zhody druheho modelu.
  /// </summary>
  public const string AgreementInput = "Zhoda";

  /// <summary>
  /// Dolna hranica skore spolahlivosti.
  /// </summary>
  public const double ScoreMin = 0;

  /// <summary>
  /// Horna hranica skore spolahlivosti.
  /// </summary>
  public const double ScoreMax = 100;

  // nazvy fuzzy mnozin
  private const string Low = "Nízka";
  private const string Medium = "Stredná";
  private const string High = "Vysoká";
  private const string Small = "Malý";
  private const string Moderate = "Stredný";
  private const string Large = "Veľký";
  private const string Disagree = "Nesúhlas";
  private const string Agree = "Súhlas";

  /// <summary>
  /// Fuzzy system pre dvojicu modelov (so vstupom zhody).
  /// </summary>
  private readonly MamdaniSystem dual;

  /// <summary>
  /// Fuzzy system pre jediny model (bez vstupu zhody).
  /// </summary>
  private readonly MamdaniSystem single;

  /// <summary>
  /// Vytvori odhadovac so zabudovanymi premennymi a pravidlovou bazou.
  /// </summary>
  public ReliabilityEstimator()
  {
    var confidence = new LinguisticVariable(ConfidenceInput, 0, 1,
    [
      new FuzzySet(Low, new TrapezoidalMembership(0, 0, 0.5, 0.8)),
      new FuzzySet(Medium, new TriangularMembership(0.5, 0.8, 0.95)),
      new FuzzySet(High, new TrapezoidalMembership(0.8, 0.95, 1, 1))
    ]);
    var margin = new LinguisticVariable(MarginInput, 0, 1,
    [
      new FuzzySet(Small, new TrapezoidalMembership(0, 0, 0.15, 0.45)),
      new FuzzySet(Moderate, new TriangularMembership(0.15, 0.45, 0.8)),
      new FuzzySet(Large, new TrapezoidalMembership(0.45, 0.8, 1, 1))
    ]);
    var agreement = new LinguisticVariable(AgreementInput, 0, 1,
    [
      new FuzzySet(Disagree, new TrapezoidalMembership(0, 0, 0.15, 0.5)),
      new FuzzySet(Agree, new TrapezoidalMembership(0.15, 0.6, 1, 1))
    ]);
    var score = new LinguisticVariable("Spoľahlivosť", ScoreMin, ScoreMax,
    [
      new FuzzySet(Low, new TrapezoidalMembership(0, 0, 15, 40)),
      new FuzzySet(Medium, new TriangularMembership(25, 50, 75)),
      new FuzzySet(High, new TrapezoidalMembership(60, 85, 100, 100))
    ]);

    dual = new MamdaniSystem([confidence, margin, agreement], score,
    [
      Rule(High, Is(ConfidenceInput, High), Is(MarginInput, Large), Is(AgreementInput, Agree)),
      Rule(High, Is(ConfidenceInput, High), Is(MarginInput, Moderate), Is(AgreementInput, Agree)),
      Rule(Medium, Is(ConfidenceInput, High), Is(MarginInput, Large), Is(AgreementInput, Disagree)),
      Rule(Low, Is(ConfidenceInput, High), Is(MarginInput, Moderate), Is(AgreementInput, Disagree)),
      Rule(Medium, Is(ConfidenceInput, Medium), Is(AgreementInput, Agree)),
      Rule(Low, Is(ConfidenceInput, Medium), Is(AgreementInput, Disagree)),
      Rule(Low, Is(ConfidenceInput, Low)),
      Rule(Low, Is(MarginInput, Small))
    ]);

    single = new MamdaniSystem([confidence, margin], score,
    [
      Rule(High, Is(ConfidenceInput, High), Is(MarginInput, Large)),
      Rule(Medium, Is(ConfidenceInput, High), Is(MarginInput, Moderate)),
      Rule(Medium, Is(ConfidenceInput, Medium)),
      Rule(Low, Is(ConfidenceInput, Low)),
      Rule(Low, Is(MarginInput, Small))
    ]);

    Variables = [confidence, margin, agreement, score];
  }

  /// <summary>
  /// Lingvisticke premenne systemu v poradi istota, rozdiel, zhoda (vstupy) a spolahlivost (vystup),
  /// napriklad na vykreslenie funkcii prislusnosti.
  /// </summary>
  public IReadOnlyList<LinguisticVariable> Variables { get; }

  /// <summary>
  /// Pravidla systemu s dvoma modelmi (na zobrazenie a do dokumentacie).
  /// </summary>
  public IReadOnlyList<FuzzyRule> Rules => dual.Rules;

  /// <summary>
  /// Vyhodnoti spolahlivost predikcie hlavneho modelu, volitelne s ohladom na druhy model.
  /// </summary>
  /// <param name="primary">Pravdepodobnosti hlavneho modelu (napr. CNN).</param>
  /// <param name="secondary">Pravdepodobnosti druheho modelu (napr. MLP), alebo null.</param>
  /// <returns>Hodnotenie so skore, urovnou a vstupmi fuzzy systemu.</returns>
  public ReliabilityAssessment Assess(IReadOnlyList<float> primary, IReadOnlyList<float>? secondary = null)
  {
    var (predicted, confidence, margin) = Top(primary);
    double? agreement = secondary?[predicted];

    var inputs = new Dictionary<string, double>
    {
      [ConfidenceInput] = confidence,
      [MarginInput] = margin
    };
    if (agreement is { } a)
    {
      inputs[AgreementInput] = a;
    }

    var detail = (agreement is null ? single : dual).Evaluate(inputs);
    return new ReliabilityAssessment(detail.Value, LevelOf(detail), predicted, confidence, margin, agreement, detail);
  }

  /// <summary>
  /// Najde triedu s najvyssou pravdepodobnostou, jej pravdepodobnost a rozdiel oproti druhej v poradi.
  /// </summary>
  /// <returns>Index predikovanej triedy, jej pravdepodobnost a rozdiel oproti druhej v poradi.</returns>
  private static (int Index, double Top, double Margin) Top(IReadOnlyList<float> probabilities)
  {
    var best = 0;
    var second = -1;
    for (var i = 1; i < probabilities.Count; i++)
    {
      if (probabilities[i] > probabilities[best])
      {
        second = best;
        best = i;
      }
      else if (second < 0 || probabilities[i] > probabilities[second])
      {
        second = i;
      }
    }
    var secondValue = second < 0 ? 0 : probabilities[second];
    return (best, probabilities[best], probabilities[best] - secondValue);
  }

  /// <summary>
  /// Prevedie dominantnu vystupnu mnozinu na uroven spolahlivosti, pri nejasnosti vrati strednu.
  /// </summary>
  /// <returns>Uroven spolahlivosti podla dominantnej vystupnej mnoziny, Stredna ak sa nespustilo ziadne pravidlo.</returns>
  private static ReliabilityLevel LevelOf(FuzzyResult detail) => detail.DominantSet switch
  {
    High => ReliabilityLevel.High,
    Low => ReliabilityLevel.Low,
    _ => ReliabilityLevel.Medium
  };

  /// <summary>
  /// Skratka na vytvorenie podmienky "premenna JE mnozina".
  /// </summary>
  /// <returns>Podmienka pre danu premennu a mnozinu.</returns>
  private static FuzzyCondition Is(string variable, string set) => new(variable, set);

  /// <summary>
  /// Skratka na vytvorenie pravidla s podmienkami spojenymi operatorom A.
  /// </summary>
  /// <returns>Pravidlo, ktore spaja podmienky operatorom AND a nastavuje zadanu vystupnu mnozinu.</returns>
  private static FuzzyRule Rule(string then, params FuzzyCondition[] conditions) =>
    new(RuleOperator.And, conditions, then);
}
