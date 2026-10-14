namespace Fuzzy;

/// <summary>
/// Lingvisticka premenna: univerzum [Min, Max] rozdeleny na pomenovane fuzzy mnoziny.
/// </summary>
public sealed class LinguisticVariable
{
  /// <summary>
  /// Vytvori lingvisticku premennu s danym univerzom a mnozinami.
  /// </summary>
  /// <param name="name">Nazov premennej.</param>
  /// <param name="min">Dolna hranica univerza.</param>
  /// <param name="max">Horna hranica univerza, musi byt vacsia ako dolna.</param>
  /// <param name="sets">Fuzzy mnoziny premennej.</param>
  public LinguisticVariable(string name, double min, double max, IReadOnlyList<FuzzySet> sets)
  {
    if (min >= max)
    {
      throw new ArgumentException("Univerzum musi mat min < max.");
    }
    Name = name;
    Min = min;
    Max = max;
    Sets = sets;
  }

  /// <summary>
  /// Nazov premennej.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Dolna hranica univerza.
  /// </summary>
  public double Min { get; }

  /// <summary>
  /// Horna hranica univerza.
  /// </summary>
  public double Max { get; }

  /// <summary>
  /// Fuzzy mnoziny, na ktore je univerzum rozdelene.
  /// </summary>
  public IReadOnlyList<FuzzySet> Sets { get; }

  /// <summary>
  /// Vrati mnozinu podla nazvu, ak neexistuje, vyhodi vynimku.
  /// </summary>
  /// <param name="setName">Nazov hladanej mnoziny.</param>
  public FuzzySet this[string setName] =>
    Sets.FirstOrDefault(s => s.Name == setName)
    ?? throw new KeyNotFoundException($"Premenna '{Name}' nema mnozinu '{setName}'.");

  /// <summary>
  /// Fuzzifikacia: stupen prislusnosti hodnoty ku kazdej mnozine (hodnota sa orezava na univerzum).
  /// </summary>
  /// <returns>Stupen prislusnosti hodnoty ku kazdej mnozine premennej, podla nazvu mnoziny.</returns>
  public IReadOnlyDictionary<string, double> Fuzzify(double x)
  {
    var clamped = Math.Clamp(x, Min, Max);
    return Sets.ToDictionary(s => s.Name, s => s.Membership.Evaluate(clamped));
  }
}
