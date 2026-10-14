namespace NeuralNet;

/// <summary>
/// Nastavenia trenovania siete.
/// </summary>
/// <param name="Epochs">Pocet epoch.</param>
/// <param name="BatchSize">Velkost davky.</param>
/// <param name="LearningRate">Rychlost ucenia.</param>
/// <param name="Optimizer">Druh optimalizatora.</param>
/// <param name="Seed">Seed generatora nahodnych cisel (miesanie obrazkov).</param>
public sealed record TrainingOptions(
  int Epochs = 10,
  int BatchSize = 64,
  float LearningRate = 0.001f,
  OptimizerKind Optimizer = OptimizerKind.Adam,
  int Seed = 42);
