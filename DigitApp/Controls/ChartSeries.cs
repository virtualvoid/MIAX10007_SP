using System.Windows.Media;

namespace DigitApp.Controls;

/// <summary>
/// Jedna ciara v grafe: hodnota pre kazdu epochu v poradi.
/// </summary>
/// <param name="Name">Nazov ciary zobrazeny v legende.</param>
/// <param name="Stroke">Farba ciary.</param>
/// <param name="Values">Hodnoty ciary, jedna pre kazdu epochu.</param>
public sealed record ChartSeries(string Name, Brush Stroke, IReadOnlyList<double> Values);
