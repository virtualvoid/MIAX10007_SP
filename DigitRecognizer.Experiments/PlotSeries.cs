namespace DigitRecognizer.Experiments;

/// <summary>
/// Sposob zobrazenia nepresnosti (smerodajnej odchylky) pri rade hodnot v grafe.
/// </summary>
internal enum ErrorStyle
{
  /// <summary>
  /// Nezobrazuje sa.
  /// </summary>
  None,

  /// <summary>
  /// Polopriehladny pas okolo ciary.
  /// </summary>
  Band,

  /// <summary>
  /// Zvisle usecky s koncovkami pri jednotlivych bodoch.
  /// </summary>
  Bars
}

/// <summary>
/// Jeden rad hodnot v ciarovom grafe.
/// </summary>
/// <param name="Name">Nazov v legende.</param>
/// <param name="Color">Farba ciary vo formate CSS.</param>
/// <param name="Points">Body (x, y) v poradi.</param>
/// <param name="Error">Odchylka (plus aj minus) pre kazdy bod, alebo null.</param>
/// <param name="ErrorStyle">Ako sa odchylka zobrazi.</param>
/// <param name="Markers">Ci sa maju vykreslit body.</param>
/// <param name="Dashed">Ci ma byt ciara ciarkovana.</param>
internal sealed record PlotSeries(
  string Name,
  string Color,
  IReadOnlyList<(double X, double Y)> Points,
  IReadOnlyList<double>? Error = null,
  ErrorStyle ErrorStyle = ErrorStyle.None,
  bool Markers = false,
  bool Dashed = false);
