namespace DigitApp.ViewModels;

/// <summary>
/// Jeden riadok tabulky vstupov fuzzy systemu s ich fuzzifikaciou.
/// </summary>
/// <param name="Name">Nazov vstupu fuzzy systemu.</param>
/// <param name="Value">Ostra hodnota vstupu.</param>
/// <param name="Sets">Fuzzifikacia: ku ktorym mnozinam a v akom stupni hodnota patri.</param>
public sealed record FuzzyInputRow(string Name, string Value, string Sets);
