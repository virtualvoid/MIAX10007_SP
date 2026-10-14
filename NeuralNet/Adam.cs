namespace NeuralNet;

/// <summary>
/// Adam: kazdemu parametru prisposobuje krok podla klzavych priemerov gradientu (m) a jeho druhej mocniny (v).
/// Stav optimalizatora (m, v, pocitadlo krokov) patri sem, nie do siete.
/// </summary>
/// <param name="learningRate">Rychlost ucenia.</param>
/// <param name="beta1">Koeficient exponencialneho priemeru gradientu.</param>
/// <param name="beta2">Koeficient exponencialneho priemeru druhej mocniny gradientu.</param>
/// <param name="epsilon">Mala konstanta zabranujuca deleniu nulou.</param>
public sealed class Adam(float learningRate, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1e-8f) : IOptimizer
{
  private float[][]? m;
  private float[][]? v;
  private int step;

  /// <inheritdoc />
  public void Step(IReadOnlyList<Param> parameters)
  {
    // stav sa vytvori pri prvom kroku, podla velkosti parametrov siete
    m ??= [.. parameters.Select(p => new float[p.W.Length])];
    v ??= [.. parameters.Select(p => new float[p.W.Length])];
    if (m.Length != parameters.Count)
    {
      throw new InvalidOperationException("Optimalizator sa nemoze pouzit pre inu siet, ako s ktorou zacal.");
    }

    step++;
    var correction1 = 1 - MathF.Pow(beta1, step); // bias korekcia, v prvych krokoch su m a v podhodnotene
    var correction2 = 1 - MathF.Pow(beta2, step);

    for (var k = 0; k < parameters.Count; k++)
    {
      var p = parameters[k];
      var mk = m[k];
      var vk = v[k];
      for (var i = 0; i < p.W.Length; i++)
      {
        mk[i] = beta1 * mk[i] + (1 - beta1) * p.G[i];
        vk[i] = beta2 * vk[i] + (1 - beta2) * p.G[i] * p.G[i];
        p.W[i] -= learningRate * (mk[i] / correction1) / (MathF.Sqrt(vk[i] / correction2) + epsilon);
      }
    }
  }
}
