namespace NeuralNet;

/// <summary>
/// Vysledky jednej dokoncenej epochy (presnosti su 0..1).
/// </summary>
/// <param name="Epoch">Cislo epochy (od 1).</param>
/// <param name="TrainLoss">Priemerna strata na trenovacej mnozine.</param>
/// <param name="TrainAccuracy">Presnost na trenovacej mnozine.</param>
/// <param name="TestLoss">Strata na testovacej mnozine.</param>
/// <param name="TestAccuracy">Presnost na testovacej mnozine.</param>
/// <param name="Duration">Doba trvania epochy.</param>
public sealed record EpochReport(
  int Epoch,
  float TrainLoss,
  float TrainAccuracy,
  float TestLoss,
  float TestAccuracy,
  TimeSpan Duration);
