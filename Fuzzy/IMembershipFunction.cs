namespace Fuzzy;

/// <summary>
/// Funkcia prislusnosti: priradi hodnote stupen prislusnosti k fuzzy mnozine v intervale 0..1.
/// </summary>
public interface IMembershipFunction
{
  /// <summary>
  /// Vypocita stupen prislusnosti hodnoty <paramref name="x"/> k mnozine.
  /// </summary>
  /// <param name="x">Hodnota z univerza.</param>
  /// <returns>Stupen prislusnosti v intervale 0..1.</returns>
  double Evaluate(double x);
}
