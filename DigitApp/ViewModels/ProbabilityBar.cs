using System.Windows.Media;

namespace DigitApp.ViewModels;

/// <summary>
/// Jeden riadok v grafe s pravdepodobnostami jednotlivych cislic.
/// </summary>
/// <param name="Digit">Cislica, ktorej riadok patri.</param>
/// <param name="Probability">Pravdepodobnost cislice v rozsahu 0 az 1.</param>
/// <param name="Text">Pravdepodobnost naformatovana na zobrazenie.</param>
/// <param name="Brush">Farba stlpca v grafe.</param>
public sealed record ProbabilityBar(int Digit, double Probability, string Text, Brush Brush);
