using System.Globalization;
using System.Text;
using System.Security;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Jednoduchy ciarovy graf vykreslovany ako SVG (vektorovy obrazok, vhodny do vedeckej prace). Bez externych kniznic.
/// </summary>
internal sealed class SvgPlot(string title, string xLabel, string yLabel)
{
  private const double Left = 66;
  private const double Right = 18;
  private const double Top = 38;
  private const double Bottom = 50;
  private const int TargetTicks = 5;

  private static readonly CultureInfo Culture = new("sk-SK");

  /// <summary>
  /// Rady hodnot v poradi vykreslenia.
  /// </summary>
  public List<PlotSeries> Series { get; } = [];

  /// <summary>
  /// Pevna dolna hranica osi x, alebo null pre automaticky rozsah.
  /// </summary>
  public double? XMin { get; init; }

  /// <summary>
  /// Pevna horna hranica osi x, alebo null pre automaticky rozsah.
  /// </summary>
  public double? XMax { get; init; }

  /// <summary>
  /// Pevna dolna hranica osi y, alebo null pre automaticky rozsah.
  /// </summary>
  public double? YMin { get; init; }

  /// <summary>
  /// Pevna horna hranica osi y, alebo null pre automaticky rozsah.
  /// </summary>
  public double? YMax { get; init; }

  /// <summary>
  /// Formatovaci retazec cisel na osi y.
  /// </summary>
  public string YFormat { get; init; } = "0.##";

  /// <summary>
  /// Formatovaci retazec cisel na osi x.
  /// </summary>
  public string XFormat { get; init; } = "0.##";

  /// <summary>
  /// Vykresli graf ako samostatny SVG dokument.
  /// </summary>
  /// <param name="width">Sirka v pixeloch.</param>
  /// <param name="height">Vyska v pixeloch.</param>
  /// <returns>Text SVG dokumentu.</returns>
  public string ToSvg(int width = 720, int height = 420) => Wrap(width, height, ToGroup(0, 0, width, height));

  /// <summary>
  /// Obali obsah do SVG dokumentu s bielym pozadim.
  /// </summary>
  /// <param name="width">Sirka v pixeloch.</param>
  /// <param name="height">Vyska v pixeloch.</param>
  /// <param name="content">Vnutorny obsah SVG.</param>
  /// <returns>Text SVG dokumentu.</returns>
  public static string Wrap(int width, int height, string content) =>
    $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" " +
    $"font-family=\"Segoe UI, Arial, sans-serif\">\n<rect width=\"{width}\" height=\"{height}\" fill=\"white\"/>\n{content}</svg>\n";

  /// <summary>
  /// Vykresli graf ako skupinu prvkov na zadanej pozicii, aby sa dalo viac grafov zlozit do jedneho obrazka.
  /// </summary>
  /// <param name="x">Poloha laveho horneho rohu.</param>
  /// <param name="y">Poloha laveho horneho rohu.</param>
  /// <param name="width">Sirka grafu.</param>
  /// <param name="height">Vyska grafu.</param>
  /// <returns>Prvky SVG grafu.</returns>
  public string ToGroup(double x, double y, double width, double height)
  {
    var plot = new Plot(Left, Top, width - Left - Right, height - Top - Bottom);
    var (xMin, xMax) = Range(XMin, XMax, Series.SelectMany(s => s.Points.Select(p => p.X)), 0.0);
    var (yMin, yMax) = Range(YMin, YMax, Series.SelectMany(Extent), 0.08);

    var svg = new StringBuilder();
    svg.Append(CultureInfo.InvariantCulture, $"<g transform=\"translate({x:0.##},{y:0.##})\">\n");
    svg.Append(Text(width / 2, 22, title, 15, "middle", bold: true));
    DrawAxes(svg, plot, xMin, xMax, yMin, yMax);
    foreach (var series in Series)
    {
      DrawSeries(svg, series, plot, xMin, xMax, yMin, yMax);
    }
    DrawLegend(svg, plot);
    svg.Append("</g>\n");
    return svg.ToString();
  }

  /// <summary>
  /// Prevedie cislo na text s desatinnou ciarkou podla formatovacieho retazca.
  /// </summary>
  /// <param name="value">Cislo.</param>
  /// <param name="format">Formatovaci retazec.</param>
  /// <returns>Naformatovany text.</returns>
  public static string Format(double value, string format) => value.ToString(format, Culture);

  /// <summary>
  /// Vytvori SVG prvok textu so znakmi bezpecne zapisanymi pre XML.
  /// </summary>
  /// <param name="x">Poloha textu.</param>
  /// <param name="y">Poloha zakladnej ciary textu.</param>
  /// <param name="text">Text.</param>
  /// <param name="size">Velkost pisma.</param>
  /// <param name="anchor">Zarovnanie (start, middle, end).</param>
  /// <param name="bold">Ci je text tucny.</param>
  /// <param name="color">Farba textu.</param>
  /// <returns>Text SVG prvku zakonceny novym riadkom.</returns>
  public static string Text(double x, double y, string text, int size, string anchor = "start", bool bold = false, string color = "#222")
    => string.Create(CultureInfo.InvariantCulture,
      $"<text x=\"{x:0.##}\" y=\"{y:0.##}\" font-size=\"{size}\" text-anchor=\"{anchor}\" fill=\"{color}\"{(bold ? " font-weight=\"600\"" : string.Empty)}>{SecurityElement.Escape(text)}</text>\n");

  /// <summary>
  /// Zvoli pekne zaokruhlene hodnoty na osi v zadanom rozsahu.
  /// </summary>
  /// <param name="min">Dolna hranica.</param>
  /// <param name="max">Horna hranica.</param>
  /// <returns>Hodnoty, na ktorych sa vykresli znacka osi.</returns>
  public static IReadOnlyList<double> NiceTicks(double min, double max)
  {
    var rough = (max - min) / TargetTicks;
    var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
    var fraction = rough / magnitude;
    var step = (fraction < 1.5 ? 1 : fraction < 3.5 ? 2 : fraction < 7.5 ? 5 : 10) * magnitude;

    var ticks = new List<double>();
    for (var v = Math.Ceiling(min / step - 1e-9) * step; v <= max + step * 1e-9; v += step)
    {
      var tick = Math.Round(v / step) * step;
      ticks.Add(tick == 0 ? 0 : tick); // bez zapornej nuly ("-0,0") po zaokruhleni
    }
    return ticks;
  }

  // rozsah osi z pevnych hranic alebo z dat s rezervou, aby ciary neliepali na okraj
  private static (double Min, double Max) Range(double? fixedMin, double? fixedMax, IEnumerable<double> values, double margin)
  {
    var list = values.ToList();
    var min = fixedMin ?? (list.Count > 0 ? list.Min() : 0);
    var max = fixedMax ?? (list.Count > 0 ? list.Max() : 1);
    if (max - min < 1e-12)
    {
      max = min + 1;
    }
    var pad = (max - min) * margin;
    return (fixedMin ?? min - pad, fixedMax ?? max + pad);
  }

  // hodnoty rady spolu s odchylkou, aby sa pas aj usecky zmestili do grafu
  private static IEnumerable<double> Extent(PlotSeries series)
  {
    for (var i = 0; i < series.Points.Count; i++)
    {
      var error = series.Error is null || series.ErrorStyle == ErrorStyle.None ? 0 : series.Error[i];
      yield return series.Points[i].Y - error;
      yield return series.Points[i].Y + error;
    }
  }

  private void DrawAxes(StringBuilder svg, Plot plot, double xMin, double xMax, double yMin, double yMax)
  {
    foreach (var tick in NiceTicks(yMin, yMax))
    {
      var y = plot.Y(tick, yMin, yMax);
      svg.Append(Line(plot.Left, y, plot.Right, y, "#e3e3e3", 1));
      svg.Append(Text(plot.Left - 8, y + 4, Format(tick, YFormat), 11, "end", color: "#555"));
    }
    foreach (var tick in NiceTicks(xMin, xMax))
    {
      var x = plot.X(tick, xMin, xMax);
      svg.Append(Line(x, plot.Top, x, plot.Bottom, "#f0f0f0", 1));
      svg.Append(Text(x, plot.Bottom + 17, Format(tick, XFormat), 11, "middle", color: "#555"));
    }
    svg.Append(Line(plot.Left, plot.Bottom, plot.Right, plot.Bottom, "#444", 1.2));
    svg.Append(Line(plot.Left, plot.Top, plot.Left, plot.Bottom, "#444", 1.2));
    svg.Append(Text((plot.Left + plot.Right) / 2, plot.Bottom + 38, xLabel, 12, "middle"));
    svg.Append(string.Create(CultureInfo.InvariantCulture,
      $"<text transform=\"translate(16,{(plot.Top + plot.Bottom) / 2:0.##}) rotate(-90)\" font-size=\"12\" text-anchor=\"middle\" fill=\"#222\">{SecurityElement.Escape(yLabel)}</text>\n"));
  }

  private static void DrawSeries(StringBuilder svg, PlotSeries series, Plot plot, double xMin, double xMax, double yMin, double yMax)
  {
    if (series.Points.Count == 0)
    {
      return;
    }
    var xs = series.Points.Select(p => plot.X(p.X, xMin, xMax)).ToArray();
    var ys = series.Points.Select(p => plot.Y(p.Y, yMin, yMax)).ToArray();

    if (series.Error is not null && series.ErrorStyle == ErrorStyle.Band)
    {
      var upper = series.Points.Select((p, i) => $"{F(xs[i])},{F(plot.Y(p.Y + series.Error[i], yMin, yMax))}");
      var lower = series.Points.Select((p, i) => $"{F(xs[i])},{F(plot.Y(p.Y - series.Error[i], yMin, yMax))}").Reverse();
      svg.Append($"<polygon points=\"{string.Join(' ', upper.Concat(lower))}\" fill=\"{series.Color}\" fill-opacity=\"0.16\" stroke=\"none\"/>\n");
    }
    if (series.Error is not null && series.ErrorStyle == ErrorStyle.Bars)
    {
      for (var i = 0; i < xs.Length; i++)
      {
        var top = plot.Y(series.Points[i].Y + series.Error[i], yMin, yMax);
        var bottom = plot.Y(series.Points[i].Y - series.Error[i], yMin, yMax);
        svg.Append(Line(xs[i], top, xs[i], bottom, series.Color, 1.4));
        svg.Append(Line(xs[i] - 4, top, xs[i] + 4, top, series.Color, 1.4));
        svg.Append(Line(xs[i] - 4, bottom, xs[i] + 4, bottom, series.Color, 1.4));
      }
    }

    var dash = series.Dashed ? " stroke-dasharray=\"6 4\"" : string.Empty;
    var path = string.Join(' ', xs.Select((x, i) => $"{F(x)},{F(ys[i])}"));
    svg.Append($"<polyline points=\"{path}\" fill=\"none\" stroke=\"{series.Color}\" stroke-width=\"2\" stroke-linejoin=\"round\"{dash}/>\n");
    if (series.Markers)
    {
      for (var i = 0; i < xs.Length; i++)
      {
        svg.Append($"<circle cx=\"{F(xs[i])}\" cy=\"{F(ys[i])}\" r=\"3.5\" fill=\"{series.Color}\"/>\n");
      }
    }
  }

  private void DrawLegend(StringBuilder svg, Plot plot)
  {
    var entries = Series.Where(s => !string.IsNullOrEmpty(s.Name)).ToList();
    if (entries.Count == 0)
    {
      return;
    }
    var width = entries.Max(s => s.Name.Length) * 6.4 + 34;
    var x = plot.Right - width - 4;
    var y = plot.Top + 8;
    svg.Append(string.Create(CultureInfo.InvariantCulture,
      $"<rect x=\"{x - 6:0.##}\" y=\"{y - 8:0.##}\" width=\"{width + 10:0.##}\" height=\"{entries.Count * 18 + 6}\" fill=\"white\" fill-opacity=\"0.85\" stroke=\"#ddd\"/>\n"));
    foreach (var s in entries)
    {
      svg.Append(Line(x, y + 4, x + 22, y + 4, s.Color, 2.5));
      svg.Append(Text(x + 28, y + 8, s.Name, 11));
      y += 18;
    }
  }

  private static string Line(double x1, double y1, double x2, double y2, string color, double width)
    => string.Create(CultureInfo.InvariantCulture,
      $"<line x1=\"{x1:0.##}\" y1=\"{y1:0.##}\" x2=\"{x2:0.##}\" y2=\"{y2:0.##}\" stroke=\"{color}\" stroke-width=\"{width}\"/>\n");

  private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

  // oblast vykreslovania grafu a prevod hodnot na suradnice
  private sealed record Plot(double Left, double Top, double Width, double Height)
  {
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public double X(double value, double min, double max) => Left + Width * (value - min) / (max - min);
    public double Y(double value, double min, double max) => Bottom - Height * (value - min) / (max - min);
  }
}
