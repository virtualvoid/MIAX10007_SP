using NeuralNet;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Nastavenie experimentov nacitane z argumentov prikazoveho riadka.
/// </summary>
/// <param name="DataDirectory">Priecinok so subormi datasetu MNIST.</param>
/// <param name="OutputDirectory">Priecinok, do ktoreho sa zapisu vysledky vo formate CSV.</param>
/// <param name="Seeds">Pocet opakovani trenovania (seedov) pre kazdy model.</param>
/// <param name="Epochs">Pocet epoch pri porovnani MLP a CNN.</param>
/// <param name="OptimizerSeeds">Pocet opakovani pri porovnani optimalizatorov.</param>
/// <param name="OptimizerEpochs">Pocet epoch pri porovnani optimalizatorov.</param>
/// <param name="Studies">Nazvy studii, ktore sa maju spustit.</param>
internal sealed record ExperimentOptions(
  string DataDirectory,
  string OutputDirectory,
  int Seeds,
  int Epochs,
  int OptimizerSeeds,
  int OptimizerEpochs,
  IReadOnlySet<string> Studies)
{
  /// <summary>
  /// Nazov studie porovnania MLP a CNN cez viac seedov.
  /// </summary>
  public const string SeedsStudy = "seeds";

  /// <summary>
  /// Nazov studie odolnosti voci posunu cislice.
  /// </summary>
  public const string ShiftStudy = "shift";

  /// <summary>
  /// Nazov studie fuzzy hodnotenia spolahlivosti.
  /// </summary>
  public const string ReliabilityStudy = "reliability";

  /// <summary>
  /// Nazov studie optimalizatorov a learning rate.
  /// </summary>
  public const string OptimizersStudy = "optimizers";

  /// <summary>
  /// Nazov kroku, ktory spracuje vysledky ostatnych studii na tabulky a grafy.
  /// </summary>
  public const string AnalyzeStudy = "analyze";

  /// <summary>
  /// Ci su na beh potrebne natrenovane modely zo studie seedov.
  /// </summary>
  public bool NeedsModels => Studies.Contains(SeedsStudy) || Studies.Contains(ShiftStudy) || Studies.Contains(ReliabilityStudy);

  /// <summary>
  /// Nacita nastavenie z argumentov v tvare <c>--nazov hodnota</c>. Nezadane hodnoty sa nahradia predvolenymi.
  /// </summary>
  /// <param name="args">Argumenty prikazoveho riadku.</param>
  /// <returns>Nacitane nastavenie.</returns>
  /// <exception cref="ArgumentException">Neznamy argument alebo chybajuca hodnota.</exception>
  public static ExperimentOptions Parse(string[] args)
  {
    var values = new Dictionary<string, string>();
    for (var i = 0; i < args.Length; i += 2)
    {
      if (!args[i].StartsWith("--") || i + 1 >= args.Length)
      {
        throw new ArgumentException($"Neplatny argument '{args[i]}'. Pouzitie: --nazov hodnota.");
      }
      values[args[i][2..]] = args[i + 1];
    }

    var known = new[] { "data", "out", "seeds", "epochs", "opt-seeds", "opt-epochs", "studies" };
    if (values.Keys.FirstOrDefault(k => !known.Contains(k)) is { } unknown)
    {
      throw new ArgumentException($"Neznamy argument '--{unknown}'. Mozne: {string.Join(", ", known.Select(k => "--" + k))}.");
    }

    var studies = values.TryGetValue("studies", out var list)
      ? list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet()
      : [SeedsStudy, ShiftStudy, ReliabilityStudy, OptimizersStudy, AnalyzeStudy];

    return new ExperimentOptions(
      values.GetValueOrDefault("data") ?? FindDataset(),
      values.GetValueOrDefault("out") ?? Path.Combine(FindRepositoryRoot(), "docs", "data"),
      int.Parse(values.GetValueOrDefault("seeds") ?? "5"),
      int.Parse(values.GetValueOrDefault("epochs") ?? "10"),
      int.Parse(values.GetValueOrDefault("opt-seeds") ?? "3"),
      int.Parse(values.GetValueOrDefault("opt-epochs") ?? "5"),
      studies);
  }

  // dataset sa hlada smerom hore od spusteneho programu, rovnako ako v aplikacii
  private static string FindDataset()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      var candidate = Path.Combine(dir.FullName, "mnist_dataset");
      if (Mnist.IsAvailable(candidate))
      {
        return candidate;
      }
    }
    throw new DirectoryNotFoundException("Dataset MNIST sa nenasiel. Zadaj ho argumentom --data.");
  }

  // koren repozitara je priecinok so subormi .sln
  private static string FindRepositoryRoot()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      if (dir.GetFiles("*.sln").Length > 0)
      {
        return dir.FullName;
      }
    }
    return Directory.GetCurrentDirectory();
  }
}
