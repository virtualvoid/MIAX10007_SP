namespace Fuzzy;

/// <summary>
/// Lichobeznikova funkcia: stupa na intervale A..B, medzi B a C je rovna 1, klesa na intervale C..D.
/// Pre A == B (alebo C == D) je prislusna hrana kolma, cim vzniknu "ramenove" mnoziny na okraji univerza.
/// </summary>
public sealed record TrapezoidalMembership : IMembershipFunction
{
  /// <summary>
  /// Vytvori lichobeznik z styroch bodov, ktore musia splnat a &lt;= b &lt;= c &lt;= d.
  /// </summary>
  /// <param name="a">Zaciatok stupania (prislusnost 0).</param>
  /// <param name="b">Koniec stupania (prislusnost 1).</param>
  /// <param name="c">Zaciatok klesania (prislusnost 1).</param>
  /// <param name="d">Koniec klesania (prislusnost 0).</param>
  public TrapezoidalMembership(double a, double b, double c, double d)
  {
    if (!(a <= b && b <= c && c <= d))
    {
      throw new ArgumentException("Body lichobeznika musia splnat a <= b <= c <= d.");
    }
    A = a;
    B = b;
    C = c;
    D = d;
  }

  /// <summary>
  /// Zaciatok stupania (prislusnost 0).
  /// </summary>
  public double A { get; }

  /// <summary>
  /// Koniec stupania (prislusnost 1).
  /// </summary>
  public double B { get; }

  /// <summary>
  /// Zaciatok klesania (prislusnost 1).
  /// </summary>
  public double C { get; }

  /// <summary>
  /// Koniec klesania (prislusnost 0).
  /// </summary>
  public double D { get; }

  /// <inheritdoc />
  public double Evaluate(double x)
  {
    if (x < A || x > D)
    {
      return 0;
    }
    if (x >= B && x <= C)
    {
      return 1;
    }
    return x < B ? (x - A) / (B - A) : (D - x) / (D - C);
  }
}
