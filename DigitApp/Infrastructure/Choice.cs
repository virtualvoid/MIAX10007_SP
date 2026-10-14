namespace DigitApp.Infrastructure;

/// <summary>
/// Polozka vyberu (ComboBox): hodnota a text, ktory sa zobrazi.
/// </summary>
/// <typeparam name="T">Typ hodnoty polozky.</typeparam>
/// <param name="Value">Hodnota polozky.</param>
/// <param name="Label">Text zobrazeny pouzivatelovi.</param>
public sealed record Choice<T>(T Value, string Label);
