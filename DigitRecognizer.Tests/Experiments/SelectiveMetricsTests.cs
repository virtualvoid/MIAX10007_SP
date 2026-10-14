using DigitRecognizer.Experiments;

namespace DigitRecognizer.Tests.Experiments;

/// <summary>
/// Testy metrik kvality odhadu spolahlivosti v triede <see cref="SelectiveMetrics"/> (AUROC, riziko pri pokryti, AURC).
/// </summary>
public sealed class SelectiveMetricsTests
{
  /// <summary>
  /// Over, ze ak maju vsetky spravne predikcie vyssie skore nez chybne, je AUROC rovne 1.
  /// </summary>
  [Fact]
  public void PerfectSeparationGivesAurocOne()
  {
    var metrics = new SelectiveMetrics([0.9, 0.8, 0.7, 0.2, 0.1], [false, false, false, true, true]);

    Assert.Equal(1.0, metrics.Auroc(), 1e-12);
  }

  /// <summary>
  /// Over, ze ak maju vsetky chybne predikcie vyssie skore nez spravne, je AUROC rovne 0.
  /// </summary>
  [Fact]
  public void InvertedSeparationGivesAurocZero()
  {
    var metrics = new SelectiveMetrics([0.1, 0.2, 0.9, 0.8], [false, false, true, true]);

    Assert.Equal(0.0, metrics.Auroc(), 1e-12);
  }

  /// <summary>
  /// Over, ze ak maju vsetky predikcie rovnake skore, je AUROC 0,5 bez ohladu na poradie obrazkov.
  /// </summary>
  [Fact]
  public void TiedScoresGiveAurocOneHalf()
  {
    var metrics = new SelectiveMetrics([1.0, 1.0, 1.0, 1.0], [false, true, false, true]);

    Assert.Equal(0.5, metrics.Auroc(), 1e-12);
  }

  /// <summary>
  /// Over, ze AUROC zodpoveda definicii: podiel dvojic (spravna, chybna), v ktorych ma spravna vyssie skore.
  /// Vypocet na malom priklade s jednou zhodou skore sa porovna s priamym pocitanim dvojic.
  /// </summary>
  [Fact]
  public void AurocMatchesPairwiseDefinition()
  {
    double[] scores = [0.9, 0.6, 0.6, 0.4, 0.3, 0.8];
    bool[] isError = [false, true, false, true, false, false];

    double wins = 0;
    var pairs = 0;
    for (var c = 0; c < scores.Length; c++)
    {
      for (var e = 0; e < scores.Length; e++)
      {
        if (isError[c] || !isError[e])
        {
          continue;
        }
        pairs++;
        wins += scores[c] > scores[e] ? 1 : scores[c] == scores[e] ? 0.5 : 0;
      }
    }

    Assert.Equal(wins / pairs, new SelectiveMetrics(scores, isError).Auroc(), 1e-12);
  }

  /// <summary>
  /// Over, ze AUROC nie je definovane (NaN), ak nie je ziadna chybna alebo ziadna spravna predikcia.
  /// </summary>
  [Fact]
  public void AurocIsNaNWithoutBothClasses()
  {
    Assert.True(double.IsNaN(new SelectiveMetrics([0.9, 0.8], [false, false]).Auroc()));
    Assert.True(double.IsNaN(new SelectiveMetrics([0.9, 0.8], [true, true]).Auroc()));
  }

  /// <summary>
  /// Over, ze pri plnom pokryti je riziko rovne celkovej miere chyby a pri nizsom pokryti sa prijmu
  /// najprv predikcie s najvyssim skore.
  /// </summary>
  [Fact]
  public void RiskAtCoverageAcceptsHighestScoresFirst()
  {
    var metrics = new SelectiveMetrics([0.9, 0.8, 0.7, 0.2, 0.1], [false, false, false, true, true]);

    Assert.Equal(0.4, metrics.RiskAtCoverage(1.0), 1e-12);
    Assert.Equal(0.0, metrics.RiskAtCoverage(0.6), 1e-12);
    Assert.Equal(0.25, metrics.RiskAtCoverage(0.8), 1e-12); // 4 prijate, 1 chyba
  }

  /// <summary>
  /// Over, ze skupina predikcii s rovnakym skore, ktoru pretina hranica pokrytia, sa zapocita pomerne,
  /// takze vysledok nezavisi od poradia obrazkov.
  /// </summary>
  [Fact]
  public void RiskAtCoverageSplitsTiedGroupProportionally()
  {
    // styri obrazky s rovnakym skore, z nich jedna chyba: pri pokryti 50 % je ocakavane riziko 25 %
    var metrics = new SelectiveMetrics([1.0, 1.0, 1.0, 1.0], [false, false, true, false]);

    Assert.Equal(0.25, metrics.RiskAtCoverage(0.5), 1e-12);
  }

  /// <summary>
  /// Over, ze pocet roznych hodnot skore a podiel najvacsej skupiny rovnakeho skore zodpovedaju vstupu.
  /// </summary>
  [Fact]
  public void TieStatisticsDescribeScoreResolution()
  {
    var metrics = new SelectiveMetrics([0.9, 0.9, 0.9, 0.5, 0.1], [false, false, true, false, true]);

    Assert.Equal(3, metrics.DistinctScores);
    Assert.Equal(0.6, metrics.LargestTieShare, 1e-12);
  }

  /// <summary>
  /// Over, ze lepsie usporiadanie skore (chyby na konci) ma nizsie AURC nez horsie usporiadanie.
  /// </summary>
  [Fact]
  public void BetterRankingHasLowerAurc()
  {
    bool[] isError = [false, false, false, true, true];
    var good = new SelectiveMetrics([0.9, 0.8, 0.7, 0.2, 0.1], isError);
    var bad = new SelectiveMetrics([0.1, 0.2, 0.7, 0.8, 0.9], isError);

    Assert.True(good.Aurc() < bad.Aurc());
  }
}
