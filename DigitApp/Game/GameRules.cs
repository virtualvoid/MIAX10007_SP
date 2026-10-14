using NeuralNet;

namespace DigitApp.Game;

/// <summary>
/// Pravidla hry: aplikacia zada cislicu, hrac ju nakresli tak, aby ju siet rozpoznala co najistejsie.
/// Body = istota hlavneho modelu v percentach (ak trafil), plus bonus, ked sa druhy model pomylil.
/// </summary>
public static class GameRules
{
  /// <summary>
  /// Pocet kol v jednej hre.
  /// </summary>
  public const int Rounds = 10;

  /// <summary>
  /// Bonus bodov za to, ze sa druhy model pomylil, kym hlavny trafil.
  /// </summary>
  public const int FooledOpponentBonus = 25;

  /// <summary>
  /// Vyhodnoti jedno kolo hry.
  /// </summary>
  /// <param name="target">Cislica zadana hracovi.</param>
  /// <param name="mainProbabilities">Pravdepodobnosti hlavneho modelu pre jednotlive cislice.</param>
  /// <param name="opponentProbabilities">Pravdepodobnosti druheho modelu, alebo null ak sa bonus neudeluje.</param>
  /// <returns>Vysledok kola s bodmi a informaciami o tipe modelu.</returns>
  public static RoundResult Score(int target, float[] mainProbabilities, float[]? opponentProbabilities)
  {
    var predicted = mainProbabilities.ArgMax();
    var confidence = mainProbabilities[predicted];
    if (predicted != target)
    {
      return new RoundResult(0, false, false, predicted, confidence);
    }

    var fooled = opponentProbabilities is not null && opponentProbabilities.ArgMax() != target;
    var points = (int)Math.Round(confidence * 100) + (fooled ? FooledOpponentBonus : 0);
    return new RoundResult(points, true, fooled, predicted, confidence);
  }
}
