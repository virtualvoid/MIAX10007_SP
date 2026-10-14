namespace Fuzzy;

/// <summary>
/// Podmienka "premenna JE mnozina", napr. Istota JE Vysoka.
/// </summary>
/// <param name="Variable">Nazov vstupnej lingvistickej premennej.</param>
/// <param name="Set">Nazov fuzzy mnoziny tejto premennej.</param>
public sealed record FuzzyCondition(string Variable, string Set);
