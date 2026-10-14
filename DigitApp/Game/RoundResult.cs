namespace DigitApp.Game;

/// <summary>
/// Vysledok jedneho kola hry.
/// </summary>
/// <param name="Points">Ziskane body (aj s bonusom).</param>
/// <param name="Hit">Ci hlavny model rozpoznal zadanu cislicu.</param>
/// <param name="FooledOpponent">Ci sa druhy model pomylil, kym hlavny trafil.</param>
/// <param name="Predicted">Cislica, ktoru hlavny model tipol.</param>
/// <param name="Confidence">Istota hlavneho modelu pre tip.</param>
public readonly record struct RoundResult(int Points, bool Hit, bool FooledOpponent, int Predicted, float Confidence);
