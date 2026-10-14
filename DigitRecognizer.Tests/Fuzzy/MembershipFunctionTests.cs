using Fuzzy;

namespace DigitRecognizer.Tests.Fuzzy;

/// <summary>
/// Testy funkcii prislusnosti <see cref="TrapezoidalMembership"/> a <see cref="TriangularMembership"/>
/// a fuzzifikacie v triede <see cref="LinguisticVariable"/>.
/// </summary>
public sealed class MembershipFunctionTests
{
  /// <summary>
  /// Over, ze lichobeznikova funkcia prislusnosti (0, 2, 3, 4) vrati ocakavany stupen prislusnosti.
  /// Vstupne data obsahuju hodnotu x a ocakavany stupen prislusnosti: mimo nosica 0, na narastajucej a klesajucej
  /// hrane hodnoty medzi 0 a 1, na plato 1.
  /// </summary>
  /// <param name="x">Vstupna hodnota.</param>
  /// <param name="expected">Ocakavany stupen prislusnosti.</param>
  [Theory]
  [InlineData(-1, 0)]
  [InlineData(0, 0)]
  [InlineData(1, 0.5)]
  [InlineData(2, 1)]
  [InlineData(3, 1)]
  [InlineData(3.5, 0.5)]
  [InlineData(4, 0)]
  [InlineData(5, 0)]
  public void TrapezoidReturnsExpectedDegrees(double x, double expected)
  {
    var trapezoid = new TrapezoidalMembership(0, 2, 3, 4);

    Assert.Equal(expected, trapezoid.Evaluate(x), 1e-9);
  }

  /// <summary>
  /// Over, ze lichobeznik so zvislou hranou sa sprava ako rameno, teda na zvislej hrane ma stupen prislusnosti 1.
  /// </summary>
  [Fact]
  public void TrapezoidWithVerticalEdgesActsAsShoulder()
  {
    var leftShoulder = new TrapezoidalMembership(0, 0, 0.5, 0.8);
    var rightShoulder = new TrapezoidalMembership(0.8, 0.95, 1, 1);

    Assert.Equal(1, leftShoulder.Evaluate(0), 1e-9);
    Assert.Equal(1, rightShoulder.Evaluate(1), 1e-9);
  }

  /// <summary>
  /// Over, ze trojuholnikova funkcia prislusnosti (0, 50, 100) vrati ocakavany stupen prislusnosti.
  /// Vstupne data obsahuju hodnotu x a ocakavany stupen: 0 v krajnych bodoch, 0.5 v polovici hran, 1 vo vrchole.
  /// </summary>
  /// <param name="x">Vstupna hodnota.</param>
  /// <param name="expected">Ocakavany stupen prislusnosti.</param>
  [Theory]
  [InlineData(0, 0)]
  [InlineData(25, 0.5)]
  [InlineData(50, 1)]
  [InlineData(75, 0.5)]
  [InlineData(100, 0)]
  public void TriangleReturnsExpectedDegrees(double x, double expected)
  {
    var triangle = new TriangularMembership(0, 50, 100);

    Assert.Equal(expected, triangle.Evaluate(x), 1e-9);
  }

  /// <summary>
  /// Over, ze lichobeznik s nezoradenymi bodmi skonci vynimkou ArgumentException.
  /// </summary>
  [Fact]
  public void TrapezoidRejectsUnorderedPoints()
  {
    Assert.Throws<ArgumentException>(() => new TrapezoidalMembership(0, 3, 2, 4));
  }

  /// <summary>
  /// Over, ze linguisticka premenna spravne fuzzifikuje hodnotu vo vnutri univerza a hodnotu mimo univerza
  /// obmedzi na jeho hranicu.
  /// </summary>
  [Fact]
  public void VariableFuzzifiesAndClampsToUniverse()
  {
    var variable = new LinguisticVariable("x", 0, 10,
    [
      new FuzzySet("Low", new TrapezoidalMembership(0, 0, 2, 6)),
      new FuzzySet("High", new TrapezoidalMembership(4, 8, 10, 10))
    ]);

    var inside = variable.Fuzzify(5);
    var outside = variable.Fuzzify(1000);

    Assert.Equal(0.25, inside["Low"], 1e-9);
    Assert.Equal(0.25, inside["High"], 1e-9);
    Assert.Equal(1, outside["High"], 1e-9);
  }
}
