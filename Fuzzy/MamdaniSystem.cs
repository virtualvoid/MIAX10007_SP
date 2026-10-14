namespace Fuzzy;

/// <summary>
/// Fuzzy system typu Mamdani: fuzzifikacia vstupov, vyhodnotenie pravidiel (And = min, Or = max),
/// implikacia orezanim (min), agregacia pravidiel maximom a defuzzifikacia metodou tazista (centroid).
/// Objekt je po vytvoreni nemenny, takze vyhodnocovanie je bezpecne aj z viacerych vlakien.
/// </summary>
public sealed class MamdaniSystem
{
  /// <summary>
  /// Vstupne lingvisticke premenne indexovane nazvom.
  /// </summary>
  private readonly IReadOnlyDictionary<string, LinguisticVariable> inputs;

  /// <summary>
  /// Pocet vzoriek agregovanej vystupnej krivky.
  /// </summary>
  private readonly int resolution;

  /// <summary>
  /// Vytvori a overi system z premennych a pravidiel.
  /// </summary>
  /// <param name="inputs">Vstupne lingvisticke premenne.</param>
  /// <param name="output">Vystupna lingvisticka premenna.</param>
  /// <param name="rules">Pravidlova baza.</param>
  /// <param name="resolution">Pocet vzoriek pri defuzzifikacii, minimalne 2.</param>
  public MamdaniSystem(
    IEnumerable<LinguisticVariable> inputs,
    LinguisticVariable output,
    IReadOnlyList<FuzzyRule> rules,
    int resolution = 201)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(resolution, 2);
    this.inputs = inputs.ToDictionary(v => v.Name);
    this.resolution = resolution;
    Output = output;
    Rules = rules;
    Validate();
  }

  /// <summary>
  /// Vystupna lingvisticka premenna.
  /// </summary>
  public LinguisticVariable Output { get; }

  /// <summary>
  /// Pravidlova baza systemu.
  /// </summary>
  public IReadOnlyList<FuzzyRule> Rules { get; }

  /// <summary>
  /// Vstupne lingvisticke premenne systemu.
  /// </summary>
  public IEnumerable<LinguisticVariable> Inputs => inputs.Values;

  /// <summary>
  /// Vyhodnoti system pre ostre vstupy a vrati defuzzifikovany vystup s medzivysledkami.
  /// </summary>
  /// <param name="crispInputs">Ostre hodnoty vstupov podla nazvu premennej, musia obsahovat vsetky vstupy.</param>
  /// <returns>Vysledok vyhodnotenia.</returns>
  public FuzzyResult Evaluate(IReadOnlyDictionary<string, double> crispInputs)
  {
    // 1) fuzzifikacia
    var degrees = inputs.ToDictionary(
      kv => kv.Key,
      kv => crispInputs.TryGetValue(kv.Key, out var x)
        ? kv.Value.Fuzzify(x)
        : throw new ArgumentException($"Chyba hodnota vstupu '{kv.Key}'.", nameof(crispInputs)));

    // 2) sila kazdeho pravidla a 3) agregacia: aktivacia vystupnej mnoziny = maximum cez jej pravidla
    var strengths = Rules.Select(rule => Strength(rule, degrees)).ToArray();
    var activation = Output.Sets.ToDictionary(s => s.Name, _ => 0.0);
    for (var i = 0; i < Rules.Count; i++)
    {
      activation[Rules[i].ThenSet] = Math.Max(activation[Rules[i].ThenSet], strengths[i]);
    }

    // 4) agregovana vystupna mnozina a 5) defuzzifikacia
    var curve = SampleAggregate(activation);
    return new FuzzyResult(
      Centroid(curve),
      activation,
      curve,
      strengths,
      degrees.ToDictionary(kv => kv.Key, kv => kv.Value));
  }

  /// <summary>
  /// Vypocita silu pravidla z fuzzifikovanych vstupov vratane jeho vahy.
  /// </summary>
  /// <returns>Sila pravidla v intervale 0 az 1 po prenasobeni jeho vahou.</returns>
  private static double Strength(FuzzyRule rule, Dictionary<string, IReadOnlyDictionary<string, double>> degrees)
  {
    var values = rule.If.Select(c => degrees[c.Variable][c.Set]);
    var combined = rule.Operator == RuleOperator.And ? values.Min() : values.Max();
    return combined * rule.Weight;
  }

  /// <summary>
  /// Vzorkuje agregovanu vystupnu mnozinu: mu(y) je maximum cez vystupne mnoziny z minima aktivacie a prislusnosti mnoziny v bode y.
  /// </summary>
  /// <returns>Vzorkovana agregovana vystupna mnozina, jeden bod na kazdy vzorok univerza.</returns>
  private List<CurvePoint> SampleAggregate(IReadOnlyDictionary<string, double> activation)
  {
    var curve = new List<CurvePoint>(resolution);
    for (var i = 0; i < resolution; i++)
    {
      var y = Output.Min + (Output.Max - Output.Min) * i / (resolution - 1);
      var mu = Output.Sets.Max(s => Math.Min(activation[s.Name], s.Membership.Evaluate(y)));
      curve.Add(new CurvePoint(y, mu));
    }
    return curve;
  }

  /// <summary>
  /// Vypocita taziste krivky ako suma(y * mu) / suma(mu), ak sa nespustilo ziadne pravidlo, vrati stred univerza.
  /// </summary>
  /// <returns>Ostra hodnota vystupu, alebo stred univerza, ak sa nespustilo ziadne pravidlo.</returns>
  private double Centroid(IReadOnlyList<CurvePoint> curve)
  {
    var area = curve.Sum(p => p.Mu);
    return area > 0 ? curve.Sum(p => p.X * p.Mu) / area : (Output.Min + Output.Max) / 2;
  }

  /// <summary>
  /// Overi, ze kazde pravidlo ma podmienky a odkazuje len na existujuce premenne a mnoziny.
  /// </summary>
  private void Validate()
  {
    foreach (var rule in Rules)
    {
      if (rule.If.Count == 0)
      {
        throw new ArgumentException($"Pravidlo '{rule}' nema ziadnu podmienku.");
      }
      foreach (var condition in rule.If)
      {
        if (!inputs.TryGetValue(condition.Variable, out var variable))
        {
          throw new ArgumentException($"Pravidlo '{rule}' pouziva neznamy vstup '{condition.Variable}'.");
        }
        _ = variable[condition.Set];
      }
      _ = Output[rule.ThenSet];
    }
  }
}
