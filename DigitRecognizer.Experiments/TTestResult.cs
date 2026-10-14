namespace DigitRecognizer.Experiments;

/// <summary>
/// Vysledok t-testu pre rozdiel dvoch skupin hodnot.
/// </summary>
/// <param name="Difference">Rozdiel priemerov (prva skupina minus druha).</param>
/// <param name="T">Hodnota testovej statistiky.</param>
/// <param name="Df">Pocet stupnov volnosti (pri Welchovom teste necely).</param>
/// <param name="P">Obojstranna p-hodnota.</param>
/// <param name="CiLow">Dolna hranica intervalu spolahlivosti rozdielu.</param>
/// <param name="CiHigh">Horna hranica intervalu spolahlivosti rozdielu.</param>
internal sealed record TTestResult(double Difference, double T, double Df, double P, double CiLow, double CiHigh);
