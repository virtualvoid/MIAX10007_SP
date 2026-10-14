using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;

namespace DigitRecognizer.Screenshots;

/// <summary>
/// Jednoduche ciary cislic na platne 280 x 280 px, ktore sa vkladaju do platna ako nakreslene mysou.
/// </summary>
internal static class DigitShapes
{
  private static readonly DrawingAttributes Pen = new()
  {
    Color = Colors.White,
    Width = 18,
    Height = 18,
    FitToCurve = false,
    StylusTip = StylusTip.Ellipse
  };

  /// <summary>
  /// Ciary nacrtnutej cislice, alebo prazdny zoznam pre cislicu, ktoru tvarovka nepozna.
  /// </summary>
  /// <param name="digit">Cislica 0 az 9.</param>
  /// <returns>Ciary, kazda ako zoznam bodov.</returns>
  public static IReadOnlyList<Stroke> Digit(int digit) => digit switch
  {
    0 => [ToStroke(Arc(140, 140, 62, 88, -90, 270))],
    1 => [ToStroke(Line(120, 80, 150, 50)), ToStroke(Line(150, 50, 150, 235))],
    2 => [ToStroke([.. Arc(140, 100, 62, 50, 200, 360 + 40), .. Line(190, 130, 80, 225), .. Line(80, 225, 215, 225)])],
    3 => [ToStroke([.. Arc(140, 95, 55, 48, -150, 90), .. Arc(140, 190, 62, 55, -90, 150)])],
    4 => [ToStroke([.. Line(175, 50, 70, 170), .. Line(70, 170, 220, 170)]), ToStroke(Line(175, 50, 175, 235))],
    5 => [ToStroke([.. Line(205, 55, 90, 55), .. Line(90, 55, 82, 125), .. Arc(130, 175, 62, 58, -125, 150)])],
    6 => [ToStroke([.. Arc(150, 120, 80, 90, -50, -200, 8), .. Arc(140, 185, 58, 55, 180, 540)])],
    7 => [ToStroke([.. Line(70, 60, 210, 60), .. Line(210, 60, 120, 235)])],
    8 => [ToStroke(Arc(140, 100, 50, 45, 90, 450)), ToStroke(Arc(140, 185, 60, 52, -90, 270))],
    9 => [ToStroke(Arc(140, 100, 55, 52, 0, 360)), ToStroke(Line(194, 100, 160, 235))],
    _ => []
  };

  /// <summary>
  /// Nejednoznacne alebo nedokoncene ciary, na ktorych sa overuje, ci siete a fuzzy hodnotenie vyjadria neistotu.
  /// </summary>
  public static IReadOnlyList<(string Name, IReadOnlyList<Stroke> Strokes)> Ambiguous { get; } =
  [
    ("otvoreny kruh", [ToStroke(Arc(140, 140, 75, 85, -50, 250))]),
    ("zlomena stvorka", [ToStroke(Line(190, 50, 70, 170)), ToStroke(Line(70, 170, 215, 170)), ToStroke(Line(175, 110, 175, 190))]),
    ("vlnovka", [ToStroke([.. Line(60, 150, 100, 90), .. Line(100, 90, 140, 190), .. Line(140, 190, 180, 90), .. Line(180, 90, 220, 150)])]),
    ("pol osmicky", [ToStroke(Arc(140, 100, 50, 45, 90, 450)), ToStroke(Line(100, 190, 190, 235))]),
    ("krizik", [ToStroke(Line(70, 70, 210, 210)), ToStroke(Line(210, 70, 70, 210))]),
    ("hacik", [ToStroke([.. Line(80, 80, 140, 60), .. Line(140, 60, 140, 200), .. Line(140, 200, 100, 230)])])
  ];

  private static Stroke ToStroke(IEnumerable<Point> points)
    => new(new StylusPointCollection(points.Select(p => new StylusPoint(p.X, p.Y))), Pen.Clone());

  private static List<Point> Line(double x1, double y1, double x2, double y2, int steps = 12)
    => [.. Enumerable.Range(0, steps + 1).Select(i => new Point(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps))];

  private static List<Point> Arc(double cx, double cy, double rx, double ry, double from, double to, double step = 8)
  {
    var points = new List<Point>();
    var direction = to >= from ? 1 : -1;
    for (var a = from; direction > 0 ? a <= to : a >= to; a += direction * step)
    {
      var radians = a * Math.PI / 180;
      points.Add(new Point(cx + rx * Math.Cos(radians), cy + ry * Math.Sin(radians)));
    }
    return points;
  }
}
