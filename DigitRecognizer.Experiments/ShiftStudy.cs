using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Studia odolnosti voci posunu cislice: natrenovane modely sa vyhodnotia na testovacich obrazkoch posunutych o niekolko
/// pixelov. Sieti sa pocas trenovania posunuty obrazok nikdy nepredlozil, takze ide o skusku zovseobecnenia.
/// </summary>
internal static class ShiftStudy
{
  private const int MaxShift = 3;

  /// <summary>
  /// Vyhodnoti kazdy model na obrazkoch posunutych vodorovne a zvisle v oboch smeroch a zapise presnost.
  /// </summary>
  /// <param name="options">Nastavenie experimentov.</param>
  /// <param name="models">Natrenovane modely.</param>
  /// <param name="test">Testovacia mnozina.</param>
  public static void Run(ExperimentOptions options, IReadOnlyList<TrainedModel> models, Dataset test)
  {
    var rows = new List<IReadOnlyList<string>>();
    foreach (var (dx, dy) in Directions())
    {
      var shifted = Shift(test, dx, dy);
      foreach (var model in models)
      {
        var accuracy = model.Network.Evaluate(shifted).Accuracy;
        rows.Add([
          model.Kind.ToString(), CsvFile.Number(model.Seed), CsvFile.Number(Math.Abs(dx) + Math.Abs(dy)),
          CsvFile.Number(dx), CsvFile.Number(dy), CsvFile.Number(accuracy)
        ]);
      }
      Console.WriteLine($"[shift] posun ({dx}, {dy}) hotovy");
    }
    CsvFile.Write(
      Path.Combine(options.OutputDirectory, "shift_study.csv"),
      ["model", "seed", "shift", "dx", "dy", "test_accuracy"],
      rows);
  }

  // posun 0 jednou, potom pre kazdu velkost posunu styri smery (vpravo, vlavo, dole, hore)
  private static IEnumerable<(int Dx, int Dy)> Directions()
  {
    yield return (0, 0);
    for (var k = 1; k <= MaxShift; k++)
    {
      yield return (k, 0);
      yield return (-k, 0);
      yield return (0, k);
      yield return (0, -k);
    }
  }

  /// <summary>
  /// Posunie vsetky obrazky o (dx, dy) pixelov, pricom uvolnene miesta sa dopln nulami (cierna).
  /// </summary>
  /// <param name="data">Povodna mnozina.</param>
  /// <param name="dx">Posun vpravo v pixeloch (zaporny vlavo).</param>
  /// <param name="dy">Posun nadol v pixeloch (zaporny nahor).</param>
  /// <returns>Nova mnozina s posunutymi obrazkami a rovnakymi popiskami.</returns>
  public static Dataset Shift(Dataset data, int dx, int dy)
  {
    const int side = MnistPreprocessor.Side;
    const int size = side * side;
    var images = new float[data.Images.Length];
    for (var i = 0; i < data.Count; i++)
    {
      for (var y = 0; y < side; y++)
      {
        var sy = y - dy;
        if (sy is < 0 or >= side)
        {
          continue;
        }
        for (var x = 0; x < side; x++)
        {
          var sx = x - dx;
          if (sx is >= 0 and < side)
          {
            images[i * size + y * side + x] = data.Images[i * size + sy * side + sx];
          }
        }
      }
    }
    return new Dataset(images, data.Labels);
  }
}
