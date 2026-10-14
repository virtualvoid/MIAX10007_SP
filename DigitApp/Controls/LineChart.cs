using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DigitApp.Controls;

/// <summary>
/// Jednoduchy ciarovy graf: vykresluje viacero radov hodnot po epochach (os x je 1..ExpectedPoints).
/// </summary>
public sealed class LineChart : FrameworkElement
{
  /// <summary>
  /// Dependency property pre <see cref="Series"/>.
  /// </summary>
  public static readonly DependencyProperty SeriesProperty = Register<IReadOnlyList<ChartSeries>?>(nameof(Series), null);

  /// <summary>
  /// Dependency property pre <see cref="Title"/>.
  /// </summary>
  public static readonly DependencyProperty TitleProperty = Register(nameof(Title), string.Empty);

  /// <summary>
  /// Dependency property pre <see cref="ExpectedPoints"/>.
  /// </summary>
  public static readonly DependencyProperty ExpectedPointsProperty = Register(nameof(ExpectedPoints), 2);

  /// <summary>
  /// Dependency property pre <see cref="ValueFormat"/>.
  /// </summary>
  public static readonly DependencyProperty ValueFormatProperty = Register(nameof(ValueFormat), "0.0");

  /// <summary>
  /// Okraj plochy grafu vlavo (miesto pre popisky osi y).
  /// </summary>
  private const double Left = 52;

  /// <summary>
  /// Okraj plochy grafu vpravo.
  /// </summary>
  private const double Right = 14;

  /// <summary>
  /// Okraj plochy grafu hore (miesto pre nadpis a legendu).
  /// </summary>
  private const double Top = 34;

  /// <summary>
  /// Okraj plochy grafu dole (miesto pre popisky osi x).
  /// </summary>
  private const double Bottom = 26;

  /// <summary>
  /// Pocet vodorovnych deliacich ciar mriezky.
  /// </summary>
  private const int GridLines = 4;

  /// <summary>
  /// Pismo pouzite pre vsetky texty grafu.
  /// </summary>
  private static readonly Typeface Face = new("Segoe UI");

  /// <summary>
  /// Pero na kreslenie mriezky.
  /// </summary>
  private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xE4)), 1);

  /// <summary>
  /// Pero na kreslenie obrysu grafu.
  /// </summary>
  private static readonly Pen FramePen = new(Brushes.LightGray, 1);

  /// <summary>
  /// Rady hodnot, ktore sa v grafe vykresluju.
  /// </summary>
  public IReadOnlyList<ChartSeries>? Series
  {
    get => (IReadOnlyList<ChartSeries>?)GetValue(SeriesProperty);
    set => SetValue(SeriesProperty, value);
  }

  /// <summary>
  /// Nadpis zobrazeny v lavom hornom rohu grafu.
  /// </summary>
  public string Title
  {
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
  }

  /// <summary>
  /// Kolko bodov sa ma zmestit na os x, aby sa graf pocas trenovania neprepocitaval.
  /// </summary>
  public int ExpectedPoints
  {
    get => (int)GetValue(ExpectedPointsProperty);
    set => SetValue(ExpectedPointsProperty, value);
  }

  /// <summary>
  /// Format cisel na popiskach osi y (format retazca pre cisla).
  /// </summary>
  public string ValueFormat
  {
    get => (string)GetValue(ValueFormatProperty);
    set => SetValue(ValueFormatProperty, value);
  }

  /// <inheritdoc />
  protected override void OnRender(DrawingContext dc)
  {
    var width = ActualWidth;
    var height = ActualHeight;
    if (width < 120 || height < 100)
    {
      return;
    }

    dc.DrawRectangle(Brushes.White, FramePen, new Rect(0.5, 0.5, width - 1, height - 1));
    var plot = new Rect(Left, Top, width - Left - Right, height - Top - Bottom);
    var series = Series ?? [];

    DrawText(dc, Title, 13, Brushes.Black, new Point(10, 7), FontWeights.SemiBold);
    DrawLegend(dc, series, width);

    var (min, max) = Range(series);
    DrawGrid(dc, plot, min, max);
    DrawXAxis(dc, plot);

    foreach (var s in series)
    {
      DrawSeries(dc, s, plot, min, max);
    }
  }

  /// <summary>
  /// Vypocita rozsah osi y zo vsetkych hodnot s malou rezervou, aby ciary neliepali na okraj.
  /// </summary>
  /// <param name="series">Rady hodnot grafu.</param>
  /// <returns>Dolna a horna hranica osi y.</returns>
  private static (double Min, double Max) Range(IReadOnlyList<ChartSeries> series)
  {
    var values = series.SelectMany(s => s.Values).ToList();
    if (values.Count == 0)
    {
      return (0, 1);
    }
    double min = values.Min(), max = values.Max();
    if (max - min < 1e-9)
    {
      max = min + 1;
    }
    var margin = (max - min) * 0.1;
    return (min - margin, max + margin);
  }

  /// <summary>
  /// Vykresli vodorovnu mriezku s popiskami osi y.
  /// </summary>
  /// <param name="dc">Kresliaci kontext.</param>
  /// <param name="plot">Plocha grafu.</param>
  /// <param name="min">Dolna hranica osi y.</param>
  /// <param name="max">Horna hranica osi y.</param>
  private void DrawGrid(DrawingContext dc, Rect plot, double min, double max)
  {
    for (var i = 0; i <= GridLines; i++)
    {
      var y = plot.Bottom - plot.Height * i / GridLines;
      dc.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));

      var label = (min + (max - min) * i / GridLines).ToString(ValueFormat, CultureInfo.CurrentCulture);
      var text = CreateText(label, 11, Brushes.Gray);
      dc.DrawText(text, new Point(plot.Left - 6 - text.Width, y - text.Height / 2));
    }
  }

  /// <summary>
  /// Vykresli popisky osi x (prva a posledna epocha a nazov osi).
  /// </summary>
  /// <param name="dc">Kresliaci kontext.</param>
  /// <param name="plot">Plocha grafu.</param>
  private void DrawXAxis(DrawingContext dc, Rect plot)
  {
    var last = Math.Max(1, ExpectedPoints);
    DrawText(dc, "1", 11, Brushes.Gray, new Point(plot.Left - 3, plot.Bottom + 5));
    if (last > 1)
    {
      var text = CreateText(last.ToString(CultureInfo.CurrentCulture), 11, Brushes.Gray);
      dc.DrawText(text, new Point(plot.Right - text.Width / 2, plot.Bottom + 5));
    }
    var caption = CreateText("epocha", 11, Brushes.Gray);
    dc.DrawText(caption, new Point(plot.Left + (plot.Width - caption.Width) / 2, plot.Bottom + 5));
  }

  /// <summary>
  /// Vykresli legendu radov v pravom hornom rohu grafu.
  /// </summary>
  /// <param name="dc">Kresliaci kontext.</param>
  /// <param name="series">Rady hodnot grafu.</param>
  /// <param name="width">Sirka celeho prvku.</param>
  private void DrawLegend(DrawingContext dc, IReadOnlyList<ChartSeries> series, double width)
  {
    var x = width - Right;
    foreach (var s in series.Reverse())
    {
      var text = CreateText(s.Name, 11, Brushes.DimGray);
      x -= text.Width;
      dc.DrawText(text, new Point(x, 9));
      x -= 18;
      dc.DrawLine(new Pen(s.Stroke, 2.5), new Point(x, 16), new Point(x + 12, 16));
      x -= 12;
    }
  }

  /// <summary>
  /// Vykresli jeden rad hodnot ako lomenu ciaru s bodmi.
  /// </summary>
  /// <param name="dc">Kresliaci kontext.</param>
  /// <param name="series">Vykreslovany rad.</param>
  /// <param name="plot">Plocha grafu.</param>
  /// <param name="min">Dolna hranica osi y.</param>
  /// <param name="max">Horna hranica osi y.</param>
  private void DrawSeries(DrawingContext dc, ChartSeries series, Rect plot, double min, double max)
  {
    if (series.Values.Count == 0)
    {
      return;
    }

    var step = plot.Width / Math.Max(1, ExpectedPoints - 1);
    var points = series.Values
      .Select((v, i) => new Point(plot.Left + step * i, plot.Bottom - plot.Height * (v - min) / (max - min)))
      .ToList();

    var pen = new Pen(series.Stroke, 2) { LineJoin = PenLineJoin.Round };
    for (var i = 1; i < points.Count; i++)
    {
      dc.DrawLine(pen, points[i - 1], points[i]);
    }
    foreach (var p in points)
    {
      dc.DrawEllipse(series.Stroke, null, p, 3, 3);
    }
  }

  /// <summary>
  /// Vykresli text na danu poziciu.
  /// </summary>
  private void DrawText(DrawingContext dc, string text, double size, Brush brush, Point origin, FontWeight? weight = null)
    => dc.DrawText(CreateText(text, size, brush, weight), origin);

  /// <summary>
  /// Vytvori formatovany text s pismom grafu a aktualnym DPI.
  /// </summary>
  /// <returns>Text pripraveny na vykreslenie.</returns>
  private FormattedText CreateText(string text, double size, Brush brush, FontWeight? weight = null) => new(
    text,
    CultureInfo.CurrentCulture,
    FlowDirection.LeftToRight,
    new Typeface(Face.FontFamily, FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
    size,
    brush,
    VisualTreeHelper.GetDpi(this).PixelsPerDip);

  /// <summary>
  /// Zaregistruje dependency property, ktorej zmena vyvola prekreslenie.
  /// </summary>
  /// <param name="name">Nazov vlastnosti.</param>
  /// <param name="defaultValue">Predvolena hodnota.</param>
  /// <returns>Zaregistrovana dependency property.</returns>
  private static DependencyProperty Register<T>(string name, T defaultValue) => DependencyProperty.Register(
    name,
    typeof(T),
    typeof(LineChart),
    new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));
}
