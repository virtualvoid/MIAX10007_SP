namespace Fuzzy;

/// <summary>
/// Pravidlo "AK (podmienky spojene operatorom) POTOM vystup JE mnozina".
/// </summary>
/// <param name="Operator">Operator, ktorym su spojene podmienky pravidla.</param>
/// <param name="If">Podmienky predpokladovej casti pravidla.</param>
/// <param name="ThenSet">Nazov vystupnej mnoziny, ktoru pravidlo aktivuje.</param>
/// <param name="Weight">Vaha pravidla 0..1, ktorou sa prenasobi sila pravidla.</param>
public sealed record FuzzyRule(
  RuleOperator Operator,
  IReadOnlyList<FuzzyCondition> If,
  string ThenSet,
  double Weight = 1.0)
{
  /// <inheritdoc />
  public override string ToString()
  {
    var joiner = Operator == RuleOperator.And ? " A ZAROVEN " : " ALEBO ";
    return $"AK {string.Join(joiner, If.Select(c => $"{c.Variable} je {c.Set}"))} POTOM {ThenSet}";
  }
}
