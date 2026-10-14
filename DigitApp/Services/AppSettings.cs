namespace DigitApp.Services;

/// <summary>
/// Nastavenia aplikacie ukladane medzi spusteniami.
/// </summary>
/// <param name="DatasetDirectory">Priecinok so subormi MNIST, null = hladat automaticky.</param>
public sealed record AppSettings(string? DatasetDirectory = null);
