using DigitRecognizer.Experiments;

namespace DigitRecognizer.Tests.Experiments;

/// <summary>
/// Testy statistickych funkcii v triede <see cref="Statistics"/>. Ocakavane hodnoty boli nezavisle vypocitane
/// kniznicou scipy (referencna implementacia), cim sa overuje spravnost vlastnej implementacie.
/// </summary>
public sealed class StatisticsTests
{
  /// <summary>
  /// Vysledky piatich opakovani trenovania prveho modelu (presnost v percentach).
  /// </summary>
  private static readonly double[] GroupA = [98.1, 98.3, 98.2, 98.4, 98.0];

  /// <summary>
  /// Vysledky piatich opakovani trenovania druheho modelu (presnost v percentach).
  /// </summary>
  private static readonly double[] GroupB = [98.9, 99.0, 98.8, 99.1, 98.95];

  /// <summary>
  /// Over, ze priemer a vyberova smerodajna odchylka suhlasia s rucne vypocitanymi hodnotami.
  /// </summary>
  [Fact]
  public void MeanAndStdDevMatchManualComputation()
  {
    double[] values = [2, 4, 4, 4, 5, 5, 7, 9];

    Assert.Equal(5.0, Statistics.Mean(values), 1e-12);
    Assert.Equal(Math.Sqrt(32.0 / 7.0), Statistics.StdDev(values), 1e-12);
    Assert.Equal(0.0, Statistics.StdDev([3.0]), 1e-12);
  }

  /// <summary>
  /// Over, ze Welchov t-test vrati rovnaku statistiku, stupne volnosti, p-hodnotu a interval spolahlivosti
  /// ako referencna implementacia.
  /// </summary>
  [Fact]
  public void WelchTTestMatchesReference()
  {
    var result = Statistics.WelchTTest(GroupB, GroupA);

    Assert.Equal(0.75, result.Difference, 1e-9);
    Assert.Equal(8.6602540378, result.T, 1e-8);
    Assert.Equal(7.2, result.Df, 1e-9);
    Assert.Equal(4.640484736480e-05, result.P, 1e-11);
    Assert.Equal(0.5463650512, result.CiLow, 1e-8);
    Assert.Equal(0.9536349488, result.CiHigh, 1e-8);
  }

  /// <summary>
  /// Over, ze parovy t-test vrati rovnaku statistiku a p-hodnotu ako referencna implementacia.
  /// </summary>
  [Fact]
  public void PairedTTestMatchesReference()
  {
    var result = Statistics.PairedTTest(GroupB, GroupA);

    Assert.Equal(12.6773138209, result.T, 1e-8);
    Assert.Equal(4.0, result.Df, 1e-12);
    Assert.Equal(2.229659371723e-04, result.P, 1e-11);
  }

  /// <summary>
  /// Over, ze kriticke hodnoty t-rozdelenia (aj pre necely pocet stupnov volnosti) zodpovedaju referencnym hodnotam.
  /// </summary>
  [Fact]
  public void CriticalValueMatchesReference()
  {
    Assert.Equal(2.7764451052, Statistics.CriticalValue(0.95, 4), 1e-8);
    Assert.Equal(2.2138402929, Statistics.CriticalValue(0.95, 10.5), 1e-8);
  }

  /// <summary>
  /// Over, ze obojstranna p-hodnota Studentovho rozdelenia zodpoveda referencnym hodnotam.
  /// </summary>
  [Fact]
  public void StudentTwoSidedPMatchesReference()
  {
    Assert.Equal(0.101939478830, Statistics.StudentTwoSidedP(2.0, 5), 1e-10);
    Assert.Equal(0.009343138528, Statistics.StudentTwoSidedP(3.5, 7.3), 1e-10);
  }

  /// <summary>
  /// Over, ze presny McNemarov test vrati rovnake p-hodnoty ako referencny binomicky test,
  /// vratane okrajoveho pripadu rovnakeho poctu chyb a prazdneho porovnania.
  /// </summary>
  [Fact]
  public void McNemarExactPMatchesReference()
  {
    Assert.Equal(1.015847194125e-03, Statistics.McNemarExactP(40, 15), 1e-12);
    Assert.Equal(0.34375, Statistics.McNemarExactP(7, 3), 1e-12);
    Assert.Equal(1.0, Statistics.McNemarExactP(5, 5), 1e-12);
    Assert.Equal(1.0, Statistics.McNemarExactP(0, 0), 1e-12);
  }

  /// <summary>
  /// Over, ze McNemarov test nezavisi od poradia klasifikatorov (je symetricky v b a c).
  /// </summary>
  [Fact]
  public void McNemarIsSymmetric()
  {
    Assert.Equal(Statistics.McNemarExactP(40, 15), Statistics.McNemarExactP(15, 40), 1e-15);
  }

  /// <summary>
  /// Over, ze Wilsonov interval pre 9895 uspechov z 10000 zodpoveda referencnym hodnotam.
  /// </summary>
  [Fact]
  public void WilsonIntervalMatchesReference()
  {
    var (low, high) = Statistics.WilsonInterval(9895, 10000);

    Assert.Equal(0.9873057978, low, 1e-9);
    Assert.Equal(0.9913182678, high, 1e-9);
  }

  /// <summary>
  /// Over, ze specialne funkcie (logaritmus gama a regularizovana neuplna beta) zodpovedaju referencnym hodnotam.
  /// </summary>
  [Fact]
  public void SpecialFunctionsMatchReference()
  {
    Assert.Equal(3.957813967619, Statistics.LogGamma(5.5), 1e-10);
    Assert.Equal(0.572364942925, Statistics.LogGamma(0.5), 1e-10);
    Assert.Equal(0.5248, Statistics.RegularizedIncompleteBeta(2, 3, 0.4), 1e-10);
    Assert.Equal(0.0, Statistics.RegularizedIncompleteBeta(2, 3, 0), 1e-12);
    Assert.Equal(1.0, Statistics.RegularizedIncompleteBeta(2, 3, 1), 1e-12);
  }
}
