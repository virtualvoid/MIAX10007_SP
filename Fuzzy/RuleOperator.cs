namespace Fuzzy;

/// <summary>
/// Spojka podmienok pravidla: And = minimum (t-norma), Or = maximum (s-norma).
/// </summary>
public enum RuleOperator
{
  /// <summary>
  /// Logicky A, vyhodnoteny ako minimum stupnov prislusnosti.
  /// </summary>
  And,
  /// <summary>
  /// Logicky ALEBO, vyhodnoteny ako maximum stupnov prislusnosti.
  /// </summary>
  Or
}
