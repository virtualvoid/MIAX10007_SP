namespace DigitApp.ViewModels;

/// <summary>
/// Jeden riadok tabulky s presnostou modelu pri danej urovni fuzzy istoty.
/// </summary>
/// <param name="Level">Nazov urovne istoty.</param>
/// <param name="Count">Pocet vzoriek v tejto urovni.</param>
/// <param name="Share">Podiel vzoriek v tejto urovni.</param>
/// <param name="Accuracy">Presnost na vzorkach tejto urovne.</param>
public sealed record FuzzyLevelRow(string Level, string Count, string Share, string Accuracy);
