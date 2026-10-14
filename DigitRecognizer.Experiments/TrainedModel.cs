using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Natrenovana siet spolu s udajmi o jej trenovani.
/// </summary>
/// <param name="Kind">Druh modelu.</param>
/// <param name="Seed">Seed, s ktorym sa siet inicializovala a trenovala.</param>
/// <param name="Network">Natrenovana siet.</param>
/// <param name="TrainSeconds">Cas trenovania v sekundach.</param>
internal sealed record TrainedModel(ModelKind Kind, int Seed, Network Network, double TrainSeconds);
