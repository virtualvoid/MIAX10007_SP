namespace NeuralNet;

/// <summary>
/// Max pooling 2x2 so krokom 2: z kazdej stvorice zoberie najvacsiu hodnotu,
/// takze rozmer sa zmensi na polovicu a siet je menej citliva na presnu polohu ciary.
/// </summary>
/// <param name="channels">Pocet kanalov.</param>
/// <param name="h">Vyska vstupnej feature mapy.</param>
/// <param name="w">Sirka vstupnej feature mapy.</param>
public sealed class MaxPool2D(int channels, int h, int w) : Layer
{
  private readonly int oh = h / 2, ow = w / 2;
  private int[] idx = []; // odkial sa zobralo maximum, treba pri backprope
  
  /// <inheritdoc />
  public override int InSize => channels * h * w;
  /// <inheritdoc />
  public override int OutSize => channels * oh * ow;

  /// <inheritdoc />
  public override float[] Forward(float[] x, int batch, bool training)
  {
    int inSize = InSize, outSize = OutSize;
    var y = new float[batch * outSize];
    idx = new int[batch * outSize];
    
    Parallel.For(0, batch, b =>
    {
      for (var c = 0; c < channels; c++)
      {
        for (var oy = 0; oy < oh; oy++)
        {
          for (var ox = 0; ox < ow; ox++)
          {
            // prejdem 4 pixely okienka a zapamatam si, ktory vyhral
            var best = b * inSize + (c * h + oy * 2) * w + ox * 2;
            for (var dy = 0; dy < 2; dy++)
            for (var dx = 0; dx < 2; dx++)
            {
              var i = b * inSize + (c * h + oy * 2 + dy) * w + ox * 2 + dx;
              if (!(x[i] > x[best]))
              {
                continue;
              }
              best = i;
            }
            var o = b * outSize + (c * oh + oy) * ow + ox;
            y[o] = x[best];
            idx[o] = best;
          }
        }
      }
    });
    return y;
  }
  
  /// <inheritdoc />
  public override float[] Backward(float[] g, int batch)
  {
    // gradient ide iba do toho pixelu, ktory vyhral, ostatne dostanu nulu
    // okienka sa neprekryvaju, takze sa nic neprepisuje
    var dx = new float[batch * InSize];
    for (var i = 0; i < g.Length; i++)
    {
      dx[idx[i]] += g[i];
    }
    return dx;
  }
}
