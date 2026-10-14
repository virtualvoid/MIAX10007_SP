namespace NeuralNet;

/// <summary>
/// Konvolucna vrstva. Obrazok je ulozeny ako [kanal][riadok][stlpec] jeden za druhym,
/// takze davka je [obrazok][kanal][riadok][stlpec] v jednom dlhom poli.
/// Pouziva "same" konvoluciu (padding = k/2), cize vystup ma rovnaky rozmer ako vstup.
/// </summary>
public sealed class Conv2D : Layer
{
  private readonly int inC, k, pad;
  
  // vahy [outC][inC][k][k], bias [outC]
  private readonly Param wt, b;
  
  // vstup si pamatam na backprop
  private float[] x = [];
  
  /// <summary>
  /// Pocet vystupnych kanalov (feature map), ktore vizualizacia pouziva na zistenie rozmerov.
  /// </summary>
  public int OutChannels { get; }

  /// <summary>
  /// Vyska feature mapy (vstupu aj vystupu).
  /// </summary>
  public int Height { get; }

  /// <summary>
  /// Sirka feature mapy (vstupu aj vystupu).
  /// </summary>
  public int Width { get; }

  /// <inheritdoc />
  public override int InSize => inC * Height * Width;
  /// <inheritdoc />
  public override int OutSize => OutChannels * Height * Width;
  /// <inheritdoc />
  public override IEnumerable<Param> Params => [wt, b];

  /// <summary>
  /// Vytvori konvolucnu vrstvu a inicializuje vahy He inicializaciou.
  /// </summary>
  /// <param name="inC">Pocet vstupnych kanalov.</param>
  /// <param name="outC">Pocet vystupnych kanalov.</param>
  /// <param name="h">Vyska obrazku.</param>
  /// <param name="w">Sirka obrazku.</param>
  /// <param name="rng">Generator nahodnych cisel pre inicializaciu vah.</param>
  /// <param name="k">Velkost jadra (neparna), predvolene 3.</param>
  public Conv2D(int inC, int outC, int h, int w, Random rng, int k = 3)
  {
    this.inC = inC;
    OutChannels = outC;
    Height = h;
    Width = w;
    this.k = k;
    pad = k / 2;
    
    wt = new Param(outC * inC * k * k);
    b = new Param(outC);
    
    // He init, fan_in je pocet vstupov jedneho vystupneho pixelu = inC * k * k
    var std = Math.Sqrt(2.0 / (inC * k * k));
    for (var i = 0; i < wt.W.Length; i++)
    {
      wt.W[i] = (float)(Vec.Gauss(rng) * std);
    }
  }
  
  /// <summary>
  /// Pre posun jadra (dy, dx) urci rozsah vystupnych pixelov, ktore maju platny vstup (zvysok je padding s nulami).
  /// Vystupny pixel (y, x) sa vtedy pozera na vstupny pixel (y + dy, x + dx).
  /// </summary>
  /// <returns>Polootvorene rozsahy riadkov [y0, y1) a stlpcov [x0, x1).</returns>
  private (int y0, int y1, int x0, int x1) Range(int dy, int dx) => (Math.Max(0, -dy), Math.Min(Height, Height - dy), Math.Max(0, -dx), Math.Min(Width, Width - dx));
  
  /// <inheritdoc />
  public override float[] Forward(float[] x, int batch, bool training)
  {
    this.x = x;
    int hw = Height * Width, inSize = InSize, outSize = OutSize;
    var y = new float[batch * outSize];
    
    Parallel.For(0, batch, b =>
    {
      for (var oc = 0; oc < OutChannels; oc++)
      {
        var outPlane = y.AsSpan(b * outSize + oc * hw, hw);
        outPlane.Fill(this.b.W[oc]); // zacnem biasom a potom pripocitavam prispevky
        
        for (var ic = 0; ic < inC; ic++)
        {
          var inPlane = x.AsSpan(b * inSize + ic * hw, hw);
          for (var ky = 0; ky < k; ky++)
          {
            for (var kx = 0; kx < k; kx++)
            {
              // jedna hodnota jadra sa vynasobi s cely posunutym obrazkom naraz,
              // po riadkoch, aby sa dal pouzit SIMD (Axpy)
              var wv = wt.W[((oc * inC + ic) * k + ky) * k + kx];
              var dy = ky - pad;
              var dx = kx - pad;
              var (y0, y1, x0, x1) = Range(dy, dx);
              for (var yy = y0; yy < y1; yy++)
              {
                Vec.Axpy(wv, inPlane.Slice((yy + dy) * Width + x0 + dx, x1 - x0), outPlane.Slice(yy * Width + x0, x1 - x0));
              }
            }
          }
        }
      }
    });
    return y;
  }
  
  /// <inheritdoc />
  public override float[] Backward(float[] g, int batch)
  {
    int hw = Height * Width, inSize = InSize, outSize = OutSize;
    
    // 1) gradient vah a biasu, paralelne cez vystupne kanaly (kazdy pise len do svojich vah)
    Parallel.For(0, OutChannels, oc =>
    {
      float db = 0;
      for (var b = 0; b < batch; b++)
      {
        var gPlane = g.AsSpan(b * outSize + oc * hw, hw);
        for (var i = 0; i < hw; i++)
        {
          db += gPlane[i];
        }
        
        for (var ic = 0; ic < inC; ic++)
        {
          var inPlane = x.AsSpan(b * inSize + ic * hw, hw);
          for (var ky = 0; ky < k; ky++)
          {
            for (var kx = 0; kx < k; kx++)
            {
              var dy = ky - pad;
              var dx = kx - pad;
              var (y0, y1, x0, x1) = Range(dy, dx);
              float acc = 0;
              for (var yy = y0; yy < y1; yy++)
              {
                acc += Vec.Dot(gPlane.Slice(yy * Width + x0, x1 - x0), inPlane.Slice((yy + dy) * Width + x0 + dx, x1 - x0));
              }
              wt.G[((oc * inC + ic) * k + ky) * k + kx] += acc;
            }
          }
        }
      }
      b.G[oc] += db;
    });
    
    // 2) gradient pre predchadzajucu vrstvu, tu je to nezavisle po obrazkoch
    var dxArr = new float[batch * inSize];
    Parallel.For(0, batch, b =>
    {
      for (var oc = 0; oc < OutChannels; oc++)
      {
        var gPlane = g.AsSpan(b * outSize + oc * hw, hw);
        for (var ic = 0; ic < inC; ic++)
        {
          var dxPlane = dxArr.AsSpan(b * inSize + ic * hw, hw);
          for (var ky = 0; ky < k; ky++)
          {
            for (var kx = 0; kx < k; kx++)
            {
              var wv = wt.W[((oc * inC + ic) * k + ky) * k + kx];
              var dy = ky - pad;
              var dx = kx - pad;
              var (y0, y1, x0, x1) = Range(dy, dx);
              for (var yy = y0; yy < y1; yy++)
              {
                Vec.Axpy(wv, gPlane.Slice(yy * Width + x0, x1 - x0), dxPlane.Slice((yy + dy) * Width + x0 + dx, x1 - x0));
              }
            }
          }
        }
      }
    });
    return dxArr;
  }
}
