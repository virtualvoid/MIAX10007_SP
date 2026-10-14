using System.Windows.Media;
using NeuralNet;

namespace DigitApp.Imaging;

/// <summary>
/// Vykresluje feature mapy konvolucnych vrstiev ako obrazky.
/// </summary>
public static class FeatureMapRenderer
{
  /// <summary>
  /// Feature mapy = vystupy konvolucnych vrstiev po ReLU. Kazdy filter vyrobi jeden mensi obrazok a vidno na nom,
  /// na co filter reaguje (hrany, oblucky, rohy). Vracia jeden zoznam obrazkov pre kazdu konvolucnu vrstvu.
  /// </summary>
  /// <param name="network">Siet, ktorej konvolucne vrstvy sa vykresluju.</param>
  /// <param name="outputs">Vystupy vsetkych vrstiev (<see cref="Network.ForwardAll"/>).</param>
  /// <returns>Pre kazdu konvolucnu vrstvu zoznam obrazkov jej feature map, jeden obrazok na filter.</returns>
  public static IReadOnlyList<IReadOnlyList<ImageSource>> Render(Network network, float[][] outputs)
  {
    var layers = new List<IReadOnlyList<ImageSource>>();
    for (var i = 0; i < network.Layers.Count; i++)
    {
      if (network.Layers[i] is not Conv2D conv)
      {
        continue;
      }

      // hned za konvoluciou je ReLU, beriem jeho vystup (i + 1), vyzera to zrozumitelnejsie
      var data = outputs[i + 1];
      var size = conv.Height * conv.Width;
      layers.Add([
        .. Enumerable.Range(0, conv.OutChannels)
          .Select(c => GrayBitmap.Create(Normalize(data.AsSpan(c * size, size)), conv.Width, conv.Height))
      ]);
    }
    return layers;
  }

  /// <summary>
  /// Normalizuje jednu mapu na rozsah 0..1 samostatne, aby slabe filtre neboli uplne cierne.
  /// </summary>
  /// <param name="plane">Hodnoty jednej mapy.</param>
  /// <returns>Mapa s hodnotami v rozsahu 0 az 1.</returns>
  private static float[] Normalize(ReadOnlySpan<float> plane)
  {
    var max = 0f;
    foreach (var v in plane)
    {
      max = Math.Max(max, v);
    }
    var result = new float[plane.Length];
    if (max > 1e-6f)
    {
      for (var i = 0; i < plane.Length; i++)
      {
        result[i] = plane[i] / max;
      }
    }
    return result;
  }
}
