namespace Fuzzy;

/// <summary>
/// Pomenovana fuzzy mnozina (napr. "Vysoka") dana funkciou prislusnosti.
/// </summary>
/// <param name="Name">Nazov mnoziny.</param>
/// <param name="Membership">Funkcia prislusnosti definujuca tvar mnoziny.</param>
public sealed record FuzzySet(string Name, IMembershipFunction Membership);
