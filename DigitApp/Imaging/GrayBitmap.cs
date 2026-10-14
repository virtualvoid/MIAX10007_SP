using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitApp.Imaging;

/// <summary>
/// Pomocne metody na vytvaranie sivotonovych obrazkov.
/// </summary>
public static class GrayBitmap
{
  /// <summary>
  /// Sivotonovy obrazok z hodnot 0..1 (po riadkoch).
  /// </summary>
  /// <param name="values">Intenzity pixelov v rozsahu 0..1.</param>
  /// <param name="width">Sirka obrazka v pixeloch.</param>
  /// <param name="height">Vyska obrazka v pixeloch.</param>
  /// <returns>Zmrazeny obrazok, pouzitelny aj z inych vlakien.</returns>
  public static BitmapSource Create(ReadOnlySpan<float> values, int width, int height)
  {
    var bytes = new byte[values.Length];
    for (var i = 0; i < bytes.Length; i++)
    {
      bytes[i] = (byte)Math.Clamp(values[i] * 255, 0, 255);
    }
    var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, bytes, width);
    bitmap.Freeze();
    return bitmap;
  }
}
