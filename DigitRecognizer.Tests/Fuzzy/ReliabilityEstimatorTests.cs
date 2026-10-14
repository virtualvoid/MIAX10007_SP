using Fuzzy;

namespace DigitRecognizer.Tests.Fuzzy;

/// <summary>
/// Testy triedy <see cref="ReliabilityEstimator"/>, ktora fuzzy systemom odhaduje spolahlivost predikcie,
/// a triedy <see cref="ReliabilityAnalyzer"/>, ktora vyhodnocuje spolahlivost nad celou mnozinou predikcii.
/// </summary>
public sealed class ReliabilityEstimatorTests
{
  /// <summary>
  /// Odhadovac spolahlivosti pouzivany vo vsetkych testoch.
  /// </summary>
  private readonly ReliabilityEstimator estimator = new();

  /// <summary>
  /// Vytvori vektor 10 pravdepodobnosti tried: zadana trieda dostane zadanu pravdepodobnost, volitelne
  /// druha trieda tiez a zvysok pravdepodobnosti sa rozdeli rovnomerne medzi ostatne triedy.
  /// </summary>
  /// <param name="top">Index najpravdepodobnejsej triedy.</param>
  /// <param name="topValue">Pravdepodobnost najpravdepodobnejsej triedy.</param>
  /// <param name="second">Index druhej triedy, alebo -1, ak sa druha trieda nepouzije.</param>
  /// <param name="secondValue">Pravdepodobnost druhej triedy.</param>
  /// <returns>Pole 10 pravdepodobnosti so suctom 1.</returns>
  private static float[] Probabilities(int top, float topValue, int second = -1, float secondValue = 0)
  {
    var others = Enumerable.Range(0, 10).Where(i => i != top && i != second).ToArray();
    var rest = (1 - topValue - (second < 0 ? 0 : secondValue)) / others.Length;
    var p = new float[10];
    foreach (var i in others)
    {
      p[i] = rest;
    }
    p[top] = topValue;
    if (second >= 0)
    {
      p[second] = secondValue;
    }
    return p;
  }

  /// <summary>
  /// Over, ze ak su oba modely isty a zhoduju sa, spolahlivost je vysoka (uroven High, skore nad 80)
  /// a predikovana trieda je spravna.
  /// </summary>
  [Fact]
  public void ConfidentAndAgreeingModelsAreHighlyReliable()
  {
    var p = Probabilities(7, 0.99f);

    var result = estimator.Assess(p, p);

    Assert.Equal(ReliabilityLevel.High, result.Level);
    Assert.True(result.Score > 80, $"skore {result.Score}");
    Assert.Equal(7, result.PredictedClass);
  }

  /// <summary>
  /// Over, ze siet, ktora sa nevie rozhodnut medzi dvoma triedami s podobnou pravdepodobnostou,
  /// ma nizku spolahlivost (uroven Low, skore pod 30).
  /// </summary>
  [Fact]
  public void UndecidedNetworkIsUnreliable()
  {
    var p = Probabilities(3, 0.40f, 5, 0.35f);

    var result = estimator.Assess(p, p);

    Assert.Equal(ReliabilityLevel.Low, result.Level);
    Assert.True(result.Score < 30, $"skore {result.Score}");
  }

  /// <summary>
  /// Over, ze nezhoda druheho modelu znizi skore spolahlivosti o viac nez 20 bodov oproti zhode
  /// a uroven spolahlivosti uz nie je High.
  /// </summary>
  [Fact]
  public void DisagreementOfSecondModelLowersReliability()
  {
    var primary = Probabilities(4, 0.99f);
    var agreeing = Probabilities(4, 0.95f);
    var disagreeing = Probabilities(9, 0.97f, 4, 0.01f);

    var withAgreement = estimator.Assess(primary, agreeing);
    var withDisagreement = estimator.Assess(primary, disagreeing);

    Assert.True(withDisagreement.Score < withAgreement.Score - 20);
    Assert.NotEqual(ReliabilityLevel.High, withDisagreement.Level);
  }

  /// <summary>
  /// Over, ze pri posudzovani jedneho modelu sa pouziju pravidla bez vstupu zhody modelov
  /// (zhoda nie je urcena) a isty model ma uroven High.
  /// </summary>
  [Fact]
  public void SingleModelUsesRulesWithoutAgreement()
  {
    var result = estimator.Assess(Probabilities(2, 0.99f));

    Assert.Null(result.Agreement);
    Assert.False(result.Detail.InputDegrees.ContainsKey(ReliabilityEstimator.AgreementInput));
    Assert.Equal(ReliabilityLevel.High, result.Level);
  }

  /// <summary>
  /// Over, ze vysledok posudenia obsahuje spravne vstupy fuzzy systemu: istotu (najvyssia pravdepodobnost),
  /// odstup (rozdiel prvych dvoch pravdepodobnosti) a zhodu modelov.
  /// </summary>
  [Fact]
  public void AssessmentReportsInputsOfTheSystem()
  {
    var primary = Probabilities(1, 0.80f, 7, 0.10f);
    var secondary = Probabilities(1, 0.60f);

    var result = estimator.Assess(primary, secondary);

    Assert.Equal(0.80, result.Confidence, 1e-6);
    Assert.Equal(0.70, result.Margin, 1e-6);
    Assert.Equal(0.60, result.Agreement!.Value, 1e-6);
  }

  /// <summary>
  /// Over, ze skore spolahlivosti rastie s istotou modelu.
  /// </summary>
  [Fact]
  public void ScoreGrowsWithConfidence()
  {
    double Score(float confidence) => estimator.Assess(Probabilities(0, confidence)).Score;

    Assert.True(Score(0.99f) > Score(0.85f));
    Assert.True(Score(0.85f) > Score(0.55f));
  }

  /// <summary>
  /// Over obmedzenie navrhu opisane v praci: ak su vsetky tri vstupy v plochej casti funkcii prislusnosti
  /// (istota nad 0,95, odstup nad 0,8, zhoda nad 0,6), skore je vzdy rovnake (85,4), bez ohladu na to,
  /// ci je predikcia velmi alebo extremne ista. Fuzzy skore v tejto oblasti neposkytuje ziadne poradie.
  /// </summary>
  [Fact]
  public void ScoreSaturatesForVeryConfidentAgreeingPredictions()
  {
    var veryConfident = Probabilities(5, 0.96f);
    var extremelyConfident = Probabilities(5, 0.99999f);

    var first = estimator.Assess(veryConfident, veryConfident);
    var second = estimator.Assess(extremelyConfident, extremelyConfident);

    Assert.Equal(first.Score, second.Score, 1e-9);
    Assert.Equal(85.4, first.Score, 0.1);
  }

  /// <summary>
  /// Over, ze analyzator spravne rozlisi spravne a chybne predikcie: vrati celkovy pocet, celkovu presnost,
  /// presnost a pokrytie pre uroven aspon Medium a pocet predikcii s urovnou Low.
  /// </summary>
  [Fact]
  public void AnalyzerSeparatesCorrectFromWrongPredictions()
  {
    // 8 istych a spravnych predikcii a 2 neiste a chybne
    var confident = Enumerable.Range(0, 8).Select(i => Probabilities(i, 0.99f)).ToList();
    var unsure = Enumerable.Range(0, 2).Select(_ => Probabilities(0, 0.30f, 1, 0.28f)).ToList();
    var primary = confident.Concat(unsure).ToList();
    byte[] labels = [0, 1, 2, 3, 4, 5, 6, 7, 5, 5]; // posledne dve su chybne, lebo predikcia je 0

    var report = ReliabilityAnalyzer.Analyze(estimator, primary, null, labels);

    Assert.Equal(10, report.Total);
    Assert.Equal(0.8, report.OverallAccuracy, 1e-9);
    Assert.Equal(1.0, report.AccuracyAtLeast(ReliabilityLevel.Medium), 1e-9);
    Assert.Equal(0.8, report.CoverageAtLeast(ReliabilityLevel.Medium), 1e-9);
    Assert.Equal(2, report.Levels.Single(l => l.Level == ReliabilityLevel.Low).Count);
  }
}
