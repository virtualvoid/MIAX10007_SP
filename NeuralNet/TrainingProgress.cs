namespace NeuralNet;

/// <summary>
/// Informacia o priebehu trenovania, posielana volajucemu pocas behu.
/// </summary>
/// <param name="Epoch">Cislo prave bezacej epochy (od 1).</param>
/// <param name="Epochs">Celkovy pocet epoch.</param>
/// <param name="EpochFraction">Kolko z aktualnej epochy je hotove (0..1).</param>
/// <param name="Completed">Vysledky epochy, ak prave skoncila, inak null.</param>
public sealed record TrainingProgress(int Epoch, int Epochs, double EpochFraction, EpochReport? Completed)
{
  /// <summary>
  /// Postup celeho trenovania (0..1).
  /// </summary>
  public double Overall => (Epoch - 1 + EpochFraction) / Epochs;
}
