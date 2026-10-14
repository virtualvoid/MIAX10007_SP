namespace Fuzzy;

/// <summary>
/// Bod vzorkovanej krivky prislusnosti: hodnota na osi x a jej stupen prislusnosti.
/// </summary>
/// <param name="X">Hodnota na osi univerza.</param>
/// <param name="Mu">Stupen prislusnosti v intervale 0..1.</param>
public readonly record struct CurvePoint(double X, double Mu);
