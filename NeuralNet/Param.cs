namespace NeuralNet;

/// <summary>
/// Trenovatelny parameter: hodnoty a gradient rovnakej dlzky.
/// </summary>
public sealed class Param(int size)
{
  /// <summary>
  /// Aktualne hodnoty parametra (vahy alebo biasy).
  /// </summary>
  public readonly float[] W = new float[size];
  /// <summary>
  /// Gradient straty podla jednotlivych hodnot parametra, akumulovany pocas spatneho prechodu.
  /// </summary>
  public readonly float[] G = new float[size];
}
