using NeuralNet;

namespace DigitApp.Services;

/// <summary>
/// Cesty k datam aplikacie v profile pouzivatela a k modelom dodanym s aplikaciou.
/// </summary>
public static class AppPaths
{
  /// <summary>
  /// Korenovy priecinok dat aplikacie v lokalnom profile pouzivatela.
  /// </summary>
  public static string DataDirectory { get; } = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitRecognizer");

  /// <summary>
  /// Cesta k suboru s nastaveniami aplikacie.
  /// </summary>
  public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

  /// <summary>
  /// Modely natrenovane v aplikacii (maju prednost pred dodanymi).
  /// </summary>
  public static string UserModelsDirectory => Path.Combine(DataDirectory, "models");

  /// <summary>
  /// Modely dodane spolu s aplikaciou.
  /// </summary>
  private static string BundledModelsDirectory => Path.Combine(AppContext.BaseDirectory, "Weights");

  /// <summary>
  /// Cesta k suboru modelu natrenovaneho v aplikacii.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <returns>Cesta k suboru s vahami modelu v profile pouzivatela.</returns>
  public static string UserModelPath(ModelKind kind) => Path.Combine(UserModelsDirectory, FileName(kind));

  /// <summary>
  /// Cesta k suboru modelu dodaneho s aplikaciou.
  /// </summary>
  /// <param name="kind">Druh modelu.</param>
  /// <returns>Cesta k suboru s vahami modelu dodaneho s aplikaciou.</returns>
  public static string BundledModelPath(ModelKind kind) => Path.Combine(BundledModelsDirectory, FileName(kind));

  /// <summary>
  /// Nazov suboru s vahami pre dany druh modelu.
  /// </summary>
  /// <returns>Nazov suboru s vahami modelu.</returns>
  private static string FileName(ModelKind kind) => $"{kind.ToString().ToLowerInvariant()}.weights";
}
