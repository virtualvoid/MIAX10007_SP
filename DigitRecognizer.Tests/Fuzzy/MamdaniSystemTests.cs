using Fuzzy;

namespace DigitRecognizer.Tests.Fuzzy;

/// <summary>
/// Testy fuzzy systemu <see cref="MamdaniSystem"/>: vyhodnotenie pravidiel, operatory AND a OR, vahy pravidiel,
/// defuzzifikacia a validacia vstupov.
/// </summary>
public sealed class MamdaniSystemTests
{
  /// <summary>
  /// Vytvori jednoduchy Mamdaniho system s jednym vstupom x (Small, Big) na intervale 0 az 1
  /// a vystupom y (Low, High) na intervale 0 az 100.
  /// </summary>
  /// <param name="rules">Pravidla systemu. Ak nie su zadane, pouzije sa Small na Low a Big na High.</param>
  /// <returns>Vytvoreny fuzzy system.</returns>
  private static MamdaniSystem CreateSystem(IReadOnlyList<FuzzyRule>? rules = null)
  {
    var x = new LinguisticVariable("x", 0, 1,
    [
      new FuzzySet("Small", new TriangularMembership(0, 0, 1)),
      new FuzzySet("Big", new TriangularMembership(0, 1, 1))
    ]);
    var y = new LinguisticVariable("y", 0, 100,
    [
      new FuzzySet("Low", new TriangularMembership(0, 0, 100)),
      new FuzzySet("High", new TriangularMembership(0, 100, 100))
    ]);

    return new MamdaniSystem([x], y, rules ??
    [
      new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Small")], "Low"),
      new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Big")], "High")
    ]);
  }

  /// <summary>
  /// Vytvori vstup systemu s jedinou premennou x s danou hodnotou.
  /// </summary>
  /// <param name="x">Hodnota vstupnej premennej x.</param>
  /// <returns>Slovnik nazvov vstupnych premennych a ich hodnot.</returns>
  private static Dictionary<string, double> Input(double x) => new() { ["x"] = x };

  /// <summary>
  /// Over, ze symetricky vstup (0.5) vedie k vystupu v strede univerza vystupnej premennej, teda priblizne 50.
  /// </summary>
  [Fact]
  public void SymmetricInputGivesCentreOfUniverse()
  {
    var result = CreateSystem().Evaluate(Input(0.5));

    Assert.Equal(50, result.Value, 0.5);
  }

  /// <summary>
  /// Over, ze vystup systemu s rastucim vstupom neklesa, pricom pre najvacsi vstup je nad 60 a pre najmensi pod 40.
  /// </summary>
  [Fact]
  public void OutputIncreasesWithInput()
  {
    var system = CreateSystem();

    var values = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }.Select(x => system.Evaluate(Input(x)).Value).ToArray();

    Assert.Equal(values.Order(), values);
    Assert.True(values[^1] > 60);
    Assert.True(values[0] < 40);
  }

  /// <summary>
  /// Over, ze vysledok obsahuje sily jednotlivych pravidiel, dominantnu vystupnu mnozinu
  /// a stupne prislusnosti vstupov.
  /// </summary>
  [Fact]
  public void ResultExposesRuleStrengthsAndDominantSet()
  {
    var result = CreateSystem().Evaluate(Input(0.8));

    Assert.Equal([0.2, 0.8], result.RuleStrengths.Select(s => Math.Round(s, 6)));
    Assert.Equal("High", result.DominantSet);
    Assert.Equal(0.8, result.InputDegrees["x"]["Big"], 1e-9);
  }

  /// <summary>
  /// Over, ze operator AND vyberie z podmienok minimum a operator OR maximum ich stupnov prislusnosti.
  /// </summary>
  [Fact]
  public void AndTakesMinimumAndOrTakesMaximum()
  {
    var conditions = new[] { new FuzzyCondition("x", "Small"), new FuzzyCondition("x", "Big") };

    var and = CreateSystem([new FuzzyRule(RuleOperator.And, conditions, "High")]).Evaluate(Input(0.25));
    var or = CreateSystem([new FuzzyRule(RuleOperator.Or, conditions, "High")]).Evaluate(Input(0.25));

    Assert.Equal(0.25, and.RuleStrengths[0], 1e-9); // min(0.75, 0.25)
    Assert.Equal(0.75, or.RuleStrengths[0], 1e-9); // max(0.75, 0.25)
  }

  /// <summary>
  /// Over, ze vaha pravidla vynasobi jeho silu (pri vahe 0.5 a plnej prislusnosti je sila 0.5).
  /// </summary>
  [Fact]
  public void RuleWeightScalesItsStrength()
  {
    var rule = new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Big")], "High", Weight: 0.5);

    var result = CreateSystem([rule]).Evaluate(Input(1));

    Assert.Equal(0.5, result.RuleStrengths[0], 1e-9);
  }

  /// <summary>
  /// Over, ze ak sa neaktivuje ziadne pravidlo, vystup je v strede univerza a dominantna mnozina nie je urcena.
  /// </summary>
  [Fact]
  public void NoFiredRuleGivesCentreAndNoDominantSet()
  {
    var rule = new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Big")], "High");

    var result = CreateSystem([rule]).Evaluate(Input(0));

    Assert.Equal(50, result.Value, 1e-9);
    Assert.Null(result.DominantSet);
  }

  /// <summary>
  /// Over, ze konstruktor odmietne pravidlo s neznamou premennou (ArgumentException) a pravidlo
  /// s neznamou vstupnou alebo vystupnou mnozinou (KeyNotFoundException).
  /// </summary>
  [Fact]
  public void ConstructorRejectsRuleWithUnknownVariableOrSet()
  {
    var unknownVariable = new FuzzyRule(RuleOperator.And, [new FuzzyCondition("z", "Big")], "High");
    var unknownSet = new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Huge")], "High");
    var unknownOutput = new FuzzyRule(RuleOperator.And, [new FuzzyCondition("x", "Big")], "Extreme");

    Assert.Throws<ArgumentException>(() => CreateSystem([unknownVariable]));
    Assert.Throws<KeyNotFoundException>(() => CreateSystem([unknownSet]));
    Assert.Throws<KeyNotFoundException>(() => CreateSystem([unknownOutput]));
  }

  /// <summary>
  /// Over, ze vyhodnotenie bez hodnoty pozadovanej vstupnej premennej skonci vynimkou ArgumentException.
  /// </summary>
  [Fact]
  public void EvaluateRejectsMissingInput()
  {
    Assert.Throws<ArgumentException>(() => CreateSystem().Evaluate(new Dictionary<string, double>()));
  }
}
