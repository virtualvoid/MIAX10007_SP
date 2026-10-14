using System.Text.Json;

namespace DigitApp.Services;

/// <summary>
/// Nastavenia aplikacie ulozene ako JSON v profile pouzivatela.
/// </summary>
public sealed class SettingsStore
{
  /// <summary>
  /// Nastavenia serializacie JSON s odsadenim.
  /// </summary>
  private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

  /// <summary>
  /// Nacita nastavenia zo suboru, pri chybe pouzije predvolene.
  /// </summary>
  public SettingsStore()
  {
    try
    {
      if (File.Exists(AppPaths.SettingsFile))
      {
        Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.SettingsFile)) ?? new AppSettings();
      }
    }
    catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
    {
      // poskodene nastavenia nie su dovod nespustit aplikaciu, pouziju sa predvolene
      Current = new AppSettings();
    }
  }

  /// <summary>
  /// Aktualne nastavenia aplikacie.
  /// </summary>
  public AppSettings Current { get; private set; } = new();

  /// <summary>
  /// Nastavi nove nastavenia a pokusi sa ich ulozit do suboru.
  /// </summary>
  /// <param name="settings">Nove nastavenia.</param>
  public void Update(AppSettings settings)
  {
    Current = settings;
    try
    {
      Directory.CreateDirectory(AppPaths.DataDirectory);
      File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(settings, Options));
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // nastavenia su len pohodlie, ak sa nepodari ulozit, aplikacia funguje dalej
    }
  }
}
