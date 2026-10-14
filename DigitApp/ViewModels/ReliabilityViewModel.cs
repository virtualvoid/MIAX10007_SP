using System.Windows;
using System.Windows.Media;
using Fuzzy;

namespace DigitApp.ViewModels;

/// <summary>
/// Zobrazenie vysledku fuzzy hodnotenia spolahlivosti rozpoznania.
/// </summary>
/// <param name="Label">Slovne hodnotenie spolahlivosti.</param>
/// <param name="ScoreText">Skore spolahlivosti naformatovane na zobrazenie.</param>
/// <param name="Score">Ostre skore spolahlivosti po defuzzifikacii.</param>
/// <param name="Brush">Farba zodpovedajuca urovni spolahlivosti.</param>
/// <param name="Inputs">Vstupy fuzzy systemu s ich fuzzifikaciou.</param>
/// <param name="Curve">Body polygonu agregovanej vystupnej mnoziny pre vykreslenie.</param>
/// <param name="CentroidX">Vodorovna poloha taziska vystupnej mnoziny v suradniciach krivky.</param>
public sealed record ReliabilityViewModel(
  string Label,
  string ScoreText,
  double Score,
  Brush Brush,
  IReadOnlyList<FuzzyInputRow> Inputs,
  PointCollection Curve,
  double CentroidX)
{
  /// <summary>
  /// Sirka vykreslenej krivky vystupnej mnoziny.
  /// </summary>
  public const double CurveWidth = 300;
  /// <summary>
  /// Vyska vykreslenej krivky vystupnej mnoziny.
  /// </summary>
  public const double CurveHeight = 50;

  /// <summary>
  /// Prazdny stav zobrazeny, kym nie je nic nakreslene alebo chyba model.
  /// </summary>
  public static ReliabilityViewModel Idle { get; } = new(
    "Nakresli číslicu",
    string.Empty,
    0,
    Brushes.Gray,
    [],
    [],
    0);

  /// <summary>
  /// Vytvori zobrazenie z vysledku fuzzy hodnotenia spolahlivosti.
  /// </summary>
  /// <param name="assessment">Vysledok hodnotenia spolahlivosti.</param>
  /// <param name="primaryName">Nazov hlavneho modelu.</param>
  /// <param name="secondaryName">Nazov pomocneho modelu, alebo null ak sa nepouziva.</param>
  /// <returns>Zobrazenie vysledku hodnotenia spolahlivosti.</returns>
  public static ReliabilityViewModel From(ReliabilityAssessment assessment, string primaryName, string? secondaryName)
  {
    var (label, brush) = assessment.Level switch
    {
      ReliabilityLevel.High => ("Istý výsledok", Brushes.SeaGreen),
      ReliabilityLevel.Medium => ("Pravdepodobný výsledok", Brushes.DarkOrange),
      _ => ("Neistý výsledok", Brushes.Firebrick)
    };

    var inputs = new List<FuzzyInputRow>
    {
      Row($"Istota ({primaryName})", assessment.Confidence, assessment, ReliabilityEstimator.ConfidenceInput),
      Row("Rozdiel 1. a 2. triedy", assessment.Margin, assessment, ReliabilityEstimator.MarginInput)
    };
    if (assessment.Agreement is { } agreement)
    {
      inputs.Add(Row($"Zhoda ({secondaryName})", agreement, assessment, ReliabilityEstimator.AgreementInput));
    }

    return new ReliabilityViewModel(
      label,
      $"spoľahlivosť {assessment.Score:0.0} / {ReliabilityEstimator.ScoreMax:0}",
      assessment.Score,
      brush,
      inputs,
      Polygon(assessment.Detail),
      Scale(assessment.Score));
  }

  /// <summary>
  /// Vytvori riadok vstupu s ostrou hodnotou a mnozinami, do ktorych patri.
  /// </summary>
  /// <returns>Riadok tabulky vstupov fuzzy systemu.</returns>
  private static FuzzyInputRow Row(string name, double value, ReliabilityAssessment assessment, string variable)
  {
    var sets = assessment.Detail.InputDegrees[variable]
      .Where(kv => kv.Value > 0.001)
      .OrderByDescending(kv => kv.Value)
      .Select(kv => $"{kv.Key} {kv.Value:0.00}");
    return new FuzzyInputRow(name, $"{value:P1}", string.Join(", ", sets));
  }

  /// <summary>
  /// Prevedie agregovanu vystupnu mnozinu na uzavrety polygon (zaklad a krivka).
  /// </summary>
  /// <returns>Body uzavreteho polygonu na vykreslenie vystupnej mnoziny.</returns>
  private static PointCollection Polygon(FuzzyResult detail)
  {
    var points = new PointCollection { new(0, CurveHeight) };
    foreach (var p in detail.Curve)
    {
      points.Add(new Point(Scale(p.X), CurveHeight - p.Mu * CurveHeight));
    }
    points.Add(new Point(CurveWidth, CurveHeight));
    return points;
  }

  /// <summary>
  /// Prepocita skore spolahlivosti na vodorovnu suradnicu krivky.
  /// </summary>
  /// <returns>Vodorovna suradnica v rozsahu 0 az CurveWidth.</returns>
  private static double Scale(double score) =>
    (score - ReliabilityEstimator.ScoreMin) / (ReliabilityEstimator.ScoreMax - ReliabilityEstimator.ScoreMin) * CurveWidth;
}
