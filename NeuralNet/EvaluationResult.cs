namespace NeuralNet;

/// <summary>
/// Vysledok vyhodnotenia siete: presnost, strata a matica zamien.
/// </summary>
/// <param name="Accuracy">Podiel spravnych predikcii (0..1).</param>
/// <param name="Loss">Priemerna cross-entropy.</param>
/// <param name="Confusion">Riadok = skutocna trieda, stlpec = predikcia.</param>
public sealed record EvaluationResult(float Accuracy, float Loss, int[,] Confusion)
{
  /// <summary>
  /// Celkovy pocet vyhodnotenych obrazkov.
  /// </summary>
  public int Count { get; } = Confusion.Cast<int>().Sum();

  /// <summary>
  /// Pocet nespravne klasifikovanych obrazkov.
  /// </summary>
  public int Errors => Count - (int)MathF.Round(Accuracy * Count);

  /// <summary>
  /// Vyhodnoti pravdepodobnosti (jeden vektor na obrazok) voci skutocnym popiskom.
  /// </summary>
  /// <param name="probabilities">Predikovane pravdepodobnosti tried pre kazdy obrazok.</param>
  /// <param name="labels">Skutocne triedy obrazkov.</param>
  /// <param name="classes">Pocet tried.</param>
  /// <returns>Vysledok vyhodnotenia s presnostou, stratou a maticou zamien.</returns>
  public static EvaluationResult From(IReadOnlyList<float[]> probabilities, byte[] labels, int classes)
  {
    var confusion = new int[classes, classes];
    var correct = 0;
    var loss = 0f;
    for (var i = 0; i < labels.Length; i++)
    {
      var y = labels[i];
      var predicted = probabilities[i].ArgMax();
      loss -= MathF.Log(Math.Max(probabilities[i][y], 1e-12f)); // max() kvoli log(0)
      confusion[y, predicted]++;
      if (predicted == y)
      {
        correct++;
      }
    }
    return new EvaluationResult((float)correct / labels.Length, loss / labels.Length, confusion);
  }
}
