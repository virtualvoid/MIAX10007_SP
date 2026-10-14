using System.Windows;
using System.Windows.Media;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Jedna bunka tabulky confusion matrix (vratane hlaviciek).
/// </summary>
/// <param name="Text">Text bunky.</param>
/// <param name="Background">Farba pozadia bunky.</param>
/// <param name="Weight">Hrubka pisma textu.</param>
public sealed record ConfusionCell(string Text, Brush Background, FontWeight Weight)
{
  /// <summary>
  /// Zdielany priehladny stetec pre bunky bez zvyraznenia.
  /// </summary>
  private static readonly Brush Transparent = Brushes.Transparent;

  /// <summary>
  /// Riadok = skutocna cislica, stlpec = predikcia. Uhlopriecka je zelena, chyby cervene podla poctu.
  /// </summary>
  /// <param name="result">Vysledok vyhodnotenia obsahujuci confusion matrix.</param>
  /// <returns>Bunky v poradi po riadkoch, vratane hlavickoveho riadka a stlpca.</returns>
  public static IReadOnlyList<ConfusionCell> Build(EvaluationResult result)
  {
    var n = result.Confusion.GetLength(0);
    var maxError = 1;
    for (var r = 0; r < n; r++)
    {
      for (var c = 0; c < n; c++)
      {
        if (r != c)
        {
          maxError = Math.Max(maxError, result.Confusion[r, c]);
        }
      }
    }

    var cells = new List<ConfusionCell> { new(string.Empty, Transparent, FontWeights.Normal) };
    cells.AddRange(Enumerable.Range(0, n).Select(c => Header(c.ToString())));
    for (var r = 0; r < n; r++)
    {
      cells.Add(Header(r.ToString()));
      var rowTotal = Math.Max(1, Enumerable.Range(0, n).Sum(c => result.Confusion[r, c]));
      for (var c = 0; c < n; c++)
      {
        var count = result.Confusion[r, c];
        cells.Add(r == c
          ? new ConfusionCell(count.ToString(), Tint(Colors.SeaGreen, 0.15 + 0.6 * count / rowTotal), FontWeights.SemiBold)
          : new ConfusionCell(count.ToString(), count == 0 ? Transparent : Tint(Colors.Firebrick, 0.15 + 0.85 * count / maxError), FontWeights.Normal));
      }
    }
    return cells;
  }

  /// <summary>
  /// Vytvori hlavickovu bunku s tucnym pismom.
  /// </summary>
  /// <returns>Bunka hlavicky tabulky.</returns>
  private static ConfusionCell Header(string text) => new(text, Brushes.Transparent, FontWeights.Bold);

  /// <summary>
  /// Vytvori zmrazeny stetec danej farby s polopriehladnostou podla zadanej nepriehladnosti.
  /// </summary>
  /// <returns>Zmrazeny stetec zadanej farby s danou nepriehladnostou.</returns>
  private static Brush Tint(Color color, double opacity)
  {
    var brush = new SolidColorBrush(Color.FromArgb((byte)(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B));
    brush.Freeze();
    return brush;
  }
}
