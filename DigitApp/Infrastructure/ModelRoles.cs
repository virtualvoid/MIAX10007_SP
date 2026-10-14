using NeuralNet;

namespace DigitApp.Infrastructure;

/// <summary>
/// Ktory model je hlavny a ktory druhy. CNN je presnejsia, preto je hlavna, ak existuje;
/// druhym modelom je potom MLP (podla neho sa pocita zhoda a v hre bonus).
/// </summary>
/// <param name="Primary">Hlavny model.</param>
/// <param name="Secondary">Druhy model, alebo null ak je dostupny len jeden.</param>
public readonly record struct ModelRoles(ModelKind Primary, ModelKind? Secondary)
{
  /// <summary>
  /// Urci roly modelov podla toho, ktore su dostupne.
  /// </summary>
  /// <param name="available">Dostupne druhy modelov.</param>
  /// <returns>Priradene roly, alebo null ak nie je dostupny ziadny model.</returns>
  public static ModelRoles? From(IReadOnlyCollection<ModelKind> available)
  {
    var hasCnn = available.Contains(ModelKind.Cnn);
    var hasMlp = available.Contains(ModelKind.Mlp);
    return hasCnn
      ? new ModelRoles(ModelKind.Cnn, hasMlp ? ModelKind.Mlp : null)
      : hasMlp ? new ModelRoles(ModelKind.Mlp, null) : null;
  }
}
