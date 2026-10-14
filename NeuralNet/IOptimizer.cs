namespace NeuralNet;

/// <summary>
/// Pravidlo, podla ktoreho sa z gradientov upravuju vahy.
/// </summary>
public interface IOptimizer
{
  /// <summary>
  /// Upravi vahy vsetkych parametrov podla ich aktualnych gradientov.
  /// </summary>
  /// <param name="parameters">Parametre siete s vyplnenymi gradientmi; poradie musi byt pri kazdom volani rovnake.</param>
  void Step(IReadOnlyList<Param> parameters);
}
