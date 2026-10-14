using System.Windows.Media;
using DigitApp.Infrastructure;
using NeuralNet;

namespace DigitApp.ViewModels;

/// <summary>
/// Vsetky udaje potrebne na zobrazenie jedneho modelu (jeden stlpec v porovnani).
/// </summary>
/// <param name="Name">Zobrazovany nazov modelu.</param>
/// <param name="Info">Strucny popis modelu, napriklad pocet parametrov.</param>
/// <param name="Digit">Rozpoznana cislica, alebo pomlcka ak nic nie je nakreslene.</param>
/// <param name="Confidence">Text s istotou predikcie alebo vyzva na kreslenie.</param>
/// <param name="Bars">Pravdepodobnosti jednotlivych cislic pre graf.</param>
public sealed record ModelPanelViewModel(
  string Name,
  string Info,
  string Digit,
  string Confidence,
  IReadOnlyList<ProbabilityBar> Bars)
{
  /// <summary>
  /// Vytvori panel modelu z vystupu siete pre danu kresbu.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <param name="network">Siet, z ktorej sa berie pocet tried a parametrov.</param>
  /// <param name="probabilities">Vystup siete, alebo null pre prazdny stav (nic nenakreslene).</param>
  /// <returns>Panel modelu pripraveny na zobrazenie.</returns>
  public static ModelPanelViewModel Create(ModelKind kind, Network network, float[]? probabilities)
  {
    var best = probabilities?.ArgMax() ?? -1;
    var bars = Enumerable.Range(0, network.Classes).Select(i =>
    {
      var p = probabilities?[i] ?? 0f;
      return new ProbabilityBar(i, p, $"{p:P1}", i == best ? Brushes.SeaGreen : Brushes.SteelBlue);
    }).ToList();

    return new ModelPanelViewModel(
      kind.DisplayName,
      $"{network.ParameterCount:N0} parametrov",
      best < 0 ? "–" : best.ToString(),
      probabilities is null ? "Nakresli číslicu myšou" : $"istota {probabilities[best]:P1}",
      bars);
  }
}
