namespace NeuralNet;

/// <summary>
/// Prevedie sivotonovy obrazok kresby (biele ciary na cierno) do formatu MNIST: cislica zmensena tak, aby jej
/// vacsi rozmer mal 20 px (pomer stran sa zachova), vycentrovana podla tazista v poli 28x28.
/// Sieti trenovanej na MNIST treba dat vstup pripraveny rovnako, inak tipuje zle aj pri dobre citatelnej cislici.
/// </summary>
public static class MnistPreprocessor
{
  /// <summary>
  /// Dlzka strany vystupneho obrazku v pixeloch.
  /// </summary>
  public const int Side = 28;

  /// <summary>
  /// Velkost vacsieho rozmeru cislice po zmenseni v pixeloch.
  /// </summary>
  private const int DigitSize = 20;
  /// <summary>
  /// Minimalna jasnost pixelu, od ktorej sa povazuje za cast kresby.
  /// </summary>
  private const float InkThreshold = 0.1f;

  /// <summary>
  /// Prevedie sivotonovy obrazok kresby do formatu MNIST.
  /// </summary>
  /// <param name="gray">Pixely 0..1 po riadkoch, velkost size x size.</param>
  /// <param name="size">Dlzka strany vstupneho stvorcoveho obrazku v pixeloch.</param>
  /// <returns>Vektor 28 * 28 hodnot 0..1 (same nuly, ak nie je nic nakreslene).</returns>
  public static float[] Normalize(float[] gray, int size)
  {
    var result = new float[Side * Side];
    if (FindInk(gray, size) is not { } bounds)
    {
      return result;
    }

    var (small, width, height) = Downscale(gray, size, bounds);
    var (cx, cy) = CenterOfMass(small, width, height);

    // posun tak, aby tazisko skoncilo v strede (14, 14)
    var offsetX = (int)Math.Round(Side / 2.0 - cx);
    var offsetY = (int)Math.Round(Side / 2.0 - cy);

    // vlozim do 28x28, co by pretieklo za okraj sa zahodi
    for (var y = 0; y < height; y++)
    {
      for (var x = 0; x < width; x++)
      {
        int tx = x + offsetX, ty = y + offsetY;
        if (tx is >= 0 and < Side && ty is >= 0 and < Side)
        {
          result[ty * Side + tx] = small[y * width + x];
        }
      }
    }
    return result;
  }

  /// <summary>
  /// Najde ohranicujuci obdlznik kresby, alebo null, ak je plocha prazdna.
  /// </summary>
  /// <returns>Ohranicujuci obdlznik kresby, alebo null, ak je plocha prazdna.</returns>
  private static Bounds? FindInk(float[] gray, int size)
  {
    int minX = size, minY = size, maxX = -1, maxY = -1;
    for (var y = 0; y < size; y++)
    {
      for (var x = 0; x < size; x++)
      {
        if (gray[y * size + x] <= InkThreshold)
        {
          continue;
        }
        minX = Math.Min(minX, x);
        maxX = Math.Max(maxX, x);
        minY = Math.Min(minY, y);
        maxY = Math.Max(maxY, y);
      }
    }
    return maxX < 0 ? null : new Bounds(minX, minY, maxX, maxY);
  }

  /// <summary>
  /// Zmensi vyrez kresby na najviac 20 px priemerovanim okolitych pixelov (box filter), co vyzera lepsie ako zmensenie bez vyhladenia.
  /// </summary>
  /// <returns>Zmenseny obrazok (pixely po riadkoch) a jeho rozmery.</returns>
  private static (float[] Pixels, int Width, int Height) Downscale(float[] gray, int size, Bounds b)
  {
    var scale = (double)DigitSize / Math.Max(b.Width, b.Height);
    var width = Math.Max(1, (int)Math.Round(b.Width * scale));
    var height = Math.Max(1, (int)Math.Round(b.Height * scale));
    var pixels = new float[width * height];

    for (var y = 0; y < height; y++)
    {
      for (var x = 0; x < width; x++)
      {
        var x0 = b.MinX + (int)(x / scale);
        var x1 = Math.Min(Math.Max(x0 + 1, b.MinX + (int)((x + 1) / scale)), b.MaxX + 1);
        var y0 = b.MinY + (int)(y / scale);
        var y1 = Math.Min(Math.Max(y0 + 1, b.MinY + (int)((y + 1) / scale)), b.MaxY + 1);

        float sum = 0;
        var count = 0;
        for (var yy = y0; yy < y1; yy++)
        {
          for (var xx = x0; xx < x1; xx++)
          {
            sum += gray[yy * size + xx];
            count++;
          }
        }
        pixels[y * width + x] = count > 0 ? sum / count : 0;
      }
    }
    return (pixels, width, height);
  }

  /// <summary>
  /// Vypocita tazisko obrazku vazene jasnostou pixelov; pri nulovej hmotnosti vrati stred obrazku.
  /// </summary>
  /// <returns>Suradnice taziska (x, y) v pixeloch obrazka.</returns>
  private static (double X, double Y) CenterOfMass(float[] pixels, int width, int height)
  {
    double mass = 0, cx = 0, cy = 0;
    for (var y = 0; y < height; y++)
    {
      for (var x = 0; x < width; x++)
      {
        var v = pixels[y * width + x];
        mass += v;
        cx += x * v;
        cy += y * v;
      }
    }
    return mass > 0 ? (cx / mass, cy / mass) : (width / 2.0, height / 2.0);
  }

  /// <summary>
  /// Ohranicujuci obdlznik kresby so zahrnutymi hranicnymi pixelmi.
  /// </summary>
  private readonly record struct Bounds(int MinX, int MinY, int MaxX, int MaxY)
  {
    /// <summary>
    /// Sirka obdlznika v pixeloch.
    /// </summary>
    public int Width => MaxX - MinX + 1;
    /// <summary>
    /// Vyska obdlznika v pixeloch.
    /// </summary>
    public int Height => MaxY - MinY + 1;
  }
}
