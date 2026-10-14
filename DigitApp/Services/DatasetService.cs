using NeuralNet;

namespace DigitApp.Services;

/// <summary>
/// Najde a nacita dataset MNIST. Nacitane mnoziny drzi v pamati, aby sa nenacitavali pri kazdom pouziti.
/// </summary>
/// <param name="settings">Ulozisko nastaveni, z ktoreho sa cita zvoleny priecinok datasetu.</param>
public sealed class DatasetService(SettingsStore settings)
{
  /// <summary>
  /// Nazov priecinka, ktory sa hlada pri automatickom vyhladavani datasetu.
  /// </summary>
  private const string DefaultFolderName = "mnist_dataset";

  /// <summary>
  /// Priecinok, z ktoreho boli nacitane mnoziny drzane v pamati.
  /// </summary>
  private string? loadedFrom;

  /// <summary>
  /// Nacitana trenovacia mnozina, alebo null.
  /// </summary>
  private Dataset? train;

  /// <summary>
  /// Nacitana testovacia mnozina, alebo null.
  /// </summary>
  private Dataset? test;

  /// <summary>
  /// Zvoleny priecinok, inak automaticky najdeny (hlada sa smerom hore od aplikacie), inak prazdny retazec.
  /// </summary>
  public string DirectoryPath => settings.Current.DatasetDirectory ?? Discover() ?? string.Empty;

  /// <summary>
  /// Ulozi do nastaveni novy priecinok datasetu, ak sa lisi od aktualneho.
  /// </summary>
  /// <param name="path">Cesta k priecinku so subormi MNIST.</param>
  public void SetDirectory(string path)
  {
    if (path == DirectoryPath)
    {
      return;
    }
    settings.Update(settings.Current with { DatasetDirectory = path });
  }

  /// <summary>
  /// Asynchronne nacita trenovaciu mnozinu, pri opakovanom volani vrati ulozenu kopiu.
  /// </summary>
  /// <returns>Trenovacia mnozina.</returns>
  public Task<Dataset> LoadTrainAsync() => LoadAsync(() => train, d => train = d, Mnist.LoadTrain);

  /// <summary>
  /// Asynchronne nacita testovaciu mnozinu, pri opakovanom volani vrati ulozenu kopiu.
  /// </summary>
  /// <returns>Testovacia mnozina.</returns>
  public Task<Dataset> LoadTestAsync() => LoadAsync(() => test, d => test = d, Mnist.LoadTest);

  /// <summary>
  /// Vrati mnozinu z pamate, inak ju nacita z disku na pozadi a ulozi.
  /// </summary>
  /// <param name="cached">Ziska mnozinu ulozenu v pamati.</param>
  /// <param name="store">Ulozi nacitanu mnozinu do pamate.</param>
  /// <param name="load">Nacita mnozinu z daneho priecinka.</param>
  /// <returns>Mnozina nacitana zo suborov, alebo uz drzana v pamati.</returns>
  private async Task<Dataset> LoadAsync(Func<Dataset?> cached, Action<Dataset> store, Func<string, Dataset> load)
  {
    var directory = DirectoryPath;
    if (loadedFrom != directory)
    {
      // iny priecinok, cache uz neplati
      train = null;
      test = null;
      loadedFrom = directory;
    }
    if (cached() is { } existing)
    {
      return existing;
    }
    if (!Mnist.IsAvailable(directory))
    {
      throw new FileNotFoundException($"V priečinku '{directory}' chýbajú súbory datasetu MNIST.");
    }

    var dataset = await Task.Run(() => load(directory));
    store(dataset);
    return dataset;
  }

  /// <summary>
  /// Hlada priecinok s datasetom od priecinka aplikacie smerom hore, vrati null ak ho nenajde.
  /// </summary>
  /// <returns>Cesta k najdenemu priecinku datasetu, alebo null, ak sa nenasiel.</returns>
  private static string? Discover()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      var candidate = Path.Combine(dir.FullName, DefaultFolderName);
      if (Mnist.IsAvailable(candidate))
      {
        return candidate;
      }
    }
    return null;
  }
}
