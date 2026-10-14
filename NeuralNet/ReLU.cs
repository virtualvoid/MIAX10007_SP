namespace NeuralNet;

/// <summary>
/// Aktivacna vrstva ReLU, ktora zaporne hodnoty nahradi nulou.
/// </summary>
/// <param name="size">Pocet hodnot jedneho obrazku (vstup aj vystup maju rovnaky rozmer).</param>
public sealed class ReLU(int size) : Layer
{
  private float[] x = [];

  /// <inheritdoc />
  public override int InSize => size;
  /// <inheritdoc />
  public override int OutSize => size;

  /// <inheritdoc />
  public override float[] Forward(float[] x, int batch, bool training)
  {
    this.x = x; // treba vediet, ktore neurony boli zaporne
    var y = new float[x.Length];
    for (var i = 0; i < x.Length; i++)
    {
      y[i] = x[i] > 0 ? x[i] : 0;
    }
    return y;
  }
  
  /// <inheritdoc />
  public override float[] Backward(float[] g, int batch)
  {
    // gradient prejde len tam kde bol vstup kladny, inde je to nula
    var dx = new float[g.Length];
    for (var i = 0; i < g.Length; i++)
    {
      dx[i] = x[i] > 0 ? g[i] : 0;
    }
    return dx;
  }
}
