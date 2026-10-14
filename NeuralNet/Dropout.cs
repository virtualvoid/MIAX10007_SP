namespace NeuralNet;

/// <summary>
/// Dropout: pri trenovani nahodne vypne cast neuronov, aby sa siet neprecvicila (overfitting).
/// Pouziva "inverted" verziu, cize hodnoty sa hned pri trenovani vydelia pravdepodobnostou ponechania
/// a pri testovani nie je potrebna ziadna uprava.
/// </summary>
/// <param name="size">Pocet hodnot jedneho obrazku (vstup aj vystup maju rovnaky rozmer).</param>
/// <param name="rate">Pravdepodobnost vypnutia neuronu (0..1).</param>
/// <param name="rng">Generator nahodnych cisel pre vyber vypnutych neuronov.</param>
public sealed class Dropout(int size, float rate, Random rng) : Layer
{
  private float[] mask = [];

  /// <inheritdoc />
  public override int InSize => size;
  /// <inheritdoc />
  public override int OutSize => size;

  /// <inheritdoc />
  public override float[] Forward(float[] x, int batch, bool training)
  {
    // pri testovani (alebo rate 0) sa nic nedeje
    if (!training || rate <= 0)
    {
      mask = [];
      return x;
    }

    mask = new float[x.Length];
    var y = new float[x.Length];
    var keep = 1 - rate;
    for (var i = 0; i < x.Length; i++)
    {
      mask[i] = rng.NextSingle() < keep ? 1f / keep : 0f;
      y[i] = x[i] * mask[i];
    }
    return y;
  }

  /// <inheritdoc />
  public override float[] Backward(float[] g, int batch)
  {
    if (mask.Length == 0)
    {
      return g;
    }

    // rovnaka maska ako v forwarde, vypnute neurony nedostanu ziadny gradient
    var dx = new float[g.Length];
    for (var i = 0; i < g.Length; i++)
    {
      dx[i] = g[i] * mask[i];
    }
    return dx;
  }
}
