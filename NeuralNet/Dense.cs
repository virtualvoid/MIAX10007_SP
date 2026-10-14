namespace NeuralNet;

/// <summary>
/// Plne prepojena (dense) vrstva, v ktorej je kazdy vstup spojeny s kazdym vystupnym neuronom.
/// </summary>
public sealed class Dense : Layer
{
  private readonly Param w, b;

  private float[] x = []; // vstup si pamatam, bude treba pri backprope

  /// <inheritdoc />
  public override int InSize { get; }

  /// <inheritdoc />
  public override int OutSize { get; }

  /// <inheritdoc />
  public override IEnumerable<Param> Params => [w, b];

  /// <summary>
  /// Vytvori vrstvu a inicializuje vahy He inicializaciou.
  /// </summary>
  /// <param name="inSize">Pocet vstupov.</param>
  /// <param name="outSize">Pocet vystupnych neuronov.</param>
  /// <param name="rng">Generator nahodnych cisel pre inicializaciu vah.</param>
  public Dense(int inSize, int outSize, Random rng)
  {
    InSize = inSize;
    OutSize = outSize;
    w = new Param(inSize * outSize);
    b = new Param(outSize);
    
    // He init: gauss so smerodajnou odchylkou sqrt(2/n)
    var std = Math.Sqrt(2.0 / inSize);
    for (var i = 0; i < w.W.Length; i++)
    {
      w.W[i] = (float)(Vec.Gauss(rng) * std);
    }
  }
  
  /// <inheritdoc />
  public override float[] Forward(float[] x, int batch, bool training)
  {
    this.x = x;
    var y = new float[batch * OutSize];
    
    // kazdy obrazok z davky je nezavisly, tak ich pustim paralelne
    Parallel.For(0, batch, b =>
    {
      var xi = x.AsSpan(b * InSize, InSize);
      for (var o = 0; o < OutSize; o++)
      {
        y[b * OutSize + o] = Vec.Dot(xi, w.W.AsSpan(o * InSize, InSize)) + this.b.W[o];
      }
    });
    return y;
  }
  
  /// <inheritdoc />
  public override float[] Backward(float[] g, int batch)
  {
    // gradient vah: dW[o,:] = sucet cez davku g[b,o] * x[b,:]
    // paralelizujem cez vystupne neurony, kazdy pise do svojho riadku, takze zadne zamky netreba
    Parallel.For(0, OutSize, o =>
    {
      var dw = w.G.AsSpan(o * InSize, InSize);
      float db = 0;
      for (var b = 0; b < batch; b++)
      {
        var gv = g[b * OutSize + o];
        db += gv; // bias gradient je len sucet gradientov
        Vec.Axpy(gv, x.AsSpan(b * InSize, InSize), dw);
      }
      b.G[o] += db;
    });
    
    // gradient pre predchadzajucu vrstvu: dx[b,:] = sucet cez o g[b,o] * W[o,:]
    var dx = new float[batch * InSize];
    Parallel.For(0, batch, b =>
    {
      var d = dx.AsSpan(b * InSize, InSize);
      for (var o = 0; o < OutSize; o++)
      {
        Vec.Axpy(g[b * OutSize + o], w.W.AsSpan(o * InSize, InSize), d);
      }
    });
    return dx;
  }
}
