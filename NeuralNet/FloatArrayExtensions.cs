namespace NeuralNet;

/// <summary>
/// Rozsirujuce metody pre polia typu float.
/// </summary>
public static class FloatArrayExtensions
{
  extension(float[] values)
  {
    /// <summary>
    /// Index najvacsej hodnoty (pri rovnosti prvy).
    /// </summary>
    /// <returns>Index najvacsej hodnoty v poli.</returns>
    public int ArgMax()
    {
      var best = 0;
      for (var i = 1; i < values.Length; i++)
      {
        if (values[i] > values[best])
        {
          best = i;
        }
      }
      return best;
    }
  }
}
