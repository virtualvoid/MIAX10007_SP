namespace Fuzzy;

/// <summary>
/// Trojuholnikova funkcia s vrcholom v bode B (specialny pripad lichobeznika).
/// </summary>
/// <param name="A">Lavy okraj, v ktorom prislusnost zacina stupat z 0.</param>
/// <param name="B">Vrchol trojuholnika s prislusnostou 1.</param>
/// <param name="C">Pravy okraj, v ktorom prislusnost klesne na 0.</param>
public sealed record TriangularMembership(double A, double B, double C) : IMembershipFunction
{
  private readonly TrapezoidalMembership trapezoid = new(A, B, B, C);

  /// <inheritdoc />
  public double Evaluate(double x) => trapezoid.Evaluate(x);
}
