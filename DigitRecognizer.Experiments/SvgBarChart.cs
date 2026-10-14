using System.Globalization;
using System.Text;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Vodorovny stlpcovy graf so smerodajnou odchylkou, vykresleny ako SVG.
/// </summary>
internal sealed class SvgBarChart(string title, string valueLabel)
{
  private const double Left = 190;
  private const double Right = 40;
  private const double Top = 38;
  private const double Bottom = 50;
  private const double RowHeight = 34;

  private readonly List<(string Label, double Value, double Error, string Color)> bars = [];

  /// <summary>
  /// Dolna hranica osi hodnot (os nemusi zacinat nulou, ak su hodnoty blizko seba).
  /// </summary>
  public double Min { get; init; }

  /// <summary>
  /// Horna hranica osi hodnot.
  /// </summary>
  public double Max { get; init; } = 1;

  /// <summary>
  /// Formatovaci retazec cisel na osi aj pri stlpcoch.
  /// </summary>
  public string Format { get; init; } = "0.000";

  /// <summary>
  /// Prida stlpec.
  /// </summary>
  /// <param name="label">Popis stlpca.</param>
  /// <param name="value">Hodnota (priemer).</param>
  /// <param name="error">Smerodajna odchylka zobrazena ako usecka.</param>
  /// <param name="color">Farba stlpca.</param>
  public void Add(string label, double value, double error, string color) => bars.Add((label, value, error, color));

  /// <summary>
  /// Vykresli graf ako SVG dokument.
  /// </summary>
  /// <param name="width">Sirka v pixeloch.</param>
  /// <returns>Text SVG dokumentu, vyska sa prisposobi poctu stlpcov.</returns>
  public string ToSvg(int width = 720)
  {
    var height = (int)(Top + Bottom + bars.Count * RowHeight);
    var plotLeft = Left;
    var plotRight = width - Right;
    double X(double v) => plotLeft + (plotRight - plotLeft) * (Math.Clamp(v, Min, Max) - Min) / (Max - Min);

    var svg = new StringBuilder();
    svg.Append(SvgPlot.Text(width / 2.0, 22, title, 15, "middle", bold: true));
    foreach (var tick in SvgPlot.NiceTicks(Min, Max))
    {
      var x = X(tick);
      svg.Append(string.Create(CultureInfo.InvariantCulture,
        $"<line x1=\"{x:0.##}\" y1=\"{Top}\" x2=\"{x:0.##}\" y2=\"{height - Bottom}\" stroke=\"#e6e6e6\"/>\n"));
      svg.Append(SvgPlot.Text(x, height - Bottom + 17, SvgPlot.Format(tick, Format), 11, "middle", color: "#555"));
    }
    svg.Append(SvgPlot.Text((plotLeft + plotRight) / 2, height - 10, valueLabel, 12, "middle"));

    for (var i = 0; i < bars.Count; i++)
    {
      var (label, value, error, color) = bars[i];
      var y = Top + i * RowHeight + 6;
      var cy = y + (RowHeight - 12) / 2;
      svg.Append(SvgPlot.Text(plotLeft - 8, cy + 4, label, 12, "end"));
      svg.Append(string.Create(CultureInfo.InvariantCulture,
        $"<rect x=\"{plotLeft:0.##}\" y=\"{y:0.##}\" width=\"{X(value) - plotLeft:0.##}\" height=\"{RowHeight - 12}\" fill=\"{color}\" fill-opacity=\"0.85\"/>\n"));
      var lo = X(value - error);
      var hi = X(value + error);
      svg.Append(string.Create(CultureInfo.InvariantCulture,
        $"<line x1=\"{lo:0.##}\" y1=\"{cy:0.##}\" x2=\"{hi:0.##}\" y2=\"{cy:0.##}\" stroke=\"#222\" stroke-width=\"1.5\"/>\n" +
        $"<line x1=\"{lo:0.##}\" y1=\"{cy - 4:0.##}\" x2=\"{lo:0.##}\" y2=\"{cy + 4:0.##}\" stroke=\"#222\" stroke-width=\"1.5\"/>\n" +
        $"<line x1=\"{hi:0.##}\" y1=\"{cy - 4:0.##}\" x2=\"{hi:0.##}\" y2=\"{cy + 4:0.##}\" stroke=\"#222\" stroke-width=\"1.5\"/>\n"));
      svg.Append(SvgPlot.Text(Math.Max(hi + 6, X(value) + 6), cy + 4, SvgPlot.Format(value, Format), 11, color: "#333"));
    }
    svg.Append(string.Create(CultureInfo.InvariantCulture,
      $"<line x1=\"{plotLeft:0.##}\" y1=\"{Top}\" x2=\"{plotLeft:0.##}\" y2=\"{height - Bottom}\" stroke=\"#444\" stroke-width=\"1.2\"/>\n"));
    return SvgPlot.Wrap(width, height, svg.ToString());
  }
}
