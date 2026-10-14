using System.Numerics;
namespace NeuralNet;

/// <summary>
/// Pomocne funkcie nad vektormi, ktore vyuzivaju SIMD instrukcie pre vyssi vykon.
/// </summary>
internal static class Vec
{
  /// <summary>
  /// Vypocita skalarny sucin dvoch vektorov rovnakej dlzky.
  /// </summary>
  /// <returns>Skalarny sucin oboch poli.</returns>
  public static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    int n = a.Length, i = 0, w = Vector<float>.Count;
    var acc = Vector<float>.Zero;
    
    // idem po kusoch, kolko floatov sa vojde do jedneho SIMD registra
    for (; i <= n - w; i += w)
    {
      acc += new Vector<float>(a[i..]) * new Vector<float>(b[i..]);
    }
    
    var s = Vector.Sum(acc);
    
    // zvysok co sa uz do registra nevosiel dorobime klasicky
    for (; i < n; i++)
    {
      s += a[i] * b[i];
    }
    
    return s;
  }
  
  /// <summary>
  /// Vygeneruje nahodne cislo so standardnym normalnym rozdelenim pomocou Box-Mullerovej transformacie.
  /// </summary>
  /// <returns>Nahodna hodnota zo standardneho normalneho rozdelenia.</returns>
  public static double Gauss(Random r) => Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());
  
  /// <summary>
  /// Pripocita k vektoru y nasobok vektora x, teda y += alpha * x (operacia axpy z BLAS).
  /// </summary>
  /// <param name="alpha">Skalarny nasobok.</param>
  /// <param name="x">Vstupny vektor.</param>
  /// <param name="y">Cielovy vektor, ktory sa modifikuje.</param>
  public static void Axpy(float alpha, ReadOnlySpan<float> x, Span<float> y)
  {
    var n = x.Length;
    var i = 0;
    var w = Vector<float>.Count;
    var va = new Vector<float>(alpha);
    for (; i <= n - w; i += w)
    {
      (new Vector<float>(y[i..]) + va * new Vector<float>(x[i..])).CopyTo(y[i..]);
    }
    for (; i < n; i++)
    {
      y[i] += alpha * x[i];
    }
  }
}
