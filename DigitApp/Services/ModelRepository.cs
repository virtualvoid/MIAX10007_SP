using NeuralNet;

namespace DigitApp.Services;

/// <summary>
/// Drzi nacitane modely. Nacitava sa z profilu pouzivatela (modely natrenovane v aplikacii),
/// ak tam nie su, tak z modelov dodanych s aplikaciou.
/// </summary>
public sealed class ModelRepository
{
  /// <summary>
  /// Nacitane siete podla druhu modelu.
  /// </summary>
  private readonly Dictionary<ModelKind, Network> networks = [];

  /// <summary>
  /// Vytvori repozitar a nacita dostupne modely.
  /// </summary>
  public ModelRepository() => Reload();

  /// <summary>
  /// Nastane po zmene sady modelov (natrenovanie, obnovenie povodnych).
  /// </summary>
  public event EventHandler? ModelsChanged;

  /// <summary>
  /// Druhy modelov, ktore su prave nacitane.
  /// </summary>
  public IReadOnlyCollection<ModelKind> Available => networks.Keys;

  /// <summary>
  /// Vrati nacitanu siet daneho druhu, alebo null ak nie je dostupna.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <returns>Siet daneho druhu, alebo null, ak nie je dostupna.</returns>
  public Network? Get(ModelKind kind) => networks.GetValueOrDefault(kind);

  /// <summary>
  /// Ulozi novo natrenovany model do profilu pouzivatela a zacne ho pouzivat.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <param name="network">Natrenovana siet.</param>
  public void Set(ModelKind kind, Network network)
  {
    Directory.CreateDirectory(AppPaths.UserModelsDirectory);
    network.Save(AppPaths.UserModelPath(kind));
    networks[kind] = network;
    ModelsChanged?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Zmaze modely natrenovane v aplikacii, takze sa vratia dodane.
  /// </summary>
  public void ResetToBundled()
  {
    foreach (var kind in Enum.GetValues<ModelKind>())
    {
      File.Delete(AppPaths.UserModelPath(kind));
    }
    Reload();
    ModelsChanged?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Nezavisla kopia modelu. Vrstvy si pocas prechodu pamataju medzivysledky, preto sa v inom vlakne
  /// (napr. pri vyhodnoteni testovacej mnoziny) nesmie pouzivat ten isty objekt ako na obrazovke.
  /// </summary>
  /// <param name="kind">Druh modelu, ktory musi byt nacitany.</param>
  /// <returns>Novy objekt siete s rovnakymi vahami, bezpecny na pouzitie v inom vlakne.</returns>
  public Network CreateIsolatedCopy(ModelKind kind)
  {
    var copy = Models.Create(kind, new Random(0));
    copy.CopyWeightsFrom(networks[kind]);
    return copy;
  }

  /// <summary>
  /// Znova nacita vsetky modely, pricom modely z profilu maju prednost pred dodanymi.
  /// </summary>
  private void Reload()
  {
    networks.Clear();
    foreach (var kind in Enum.GetValues<ModelKind>())
    {
      var network = TryLoad(kind, AppPaths.UserModelPath(kind)) ?? TryLoad(kind, AppPaths.BundledModelPath(kind));
      if (network is not null)
      {
        networks[kind] = network;
      }
    }
  }

  /// <summary>
  /// Pokusi sa nacitat model zo suboru, vrati null ak subor chyba alebo je neplatny.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <param name="path">Cesta k suboru s vahami.</param>
  /// <returns>Nacitana siet, alebo null, ak subor chyba alebo je neplatny.</returns>
  private static Network? TryLoad(ModelKind kind, string path)
  {
    if (!File.Exists(path))
    {
      return null;
    }
    try
    {
      var network = Models.Create(kind, new Random(0)); // seed je jedno, vahy sa hned prepisu
      network.Load(path);
      return network;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException)
    {
      // poskodeny alebo nekompatibilny subor, model sa povazuje za nedostupny
      return null;
    }
  }
}
