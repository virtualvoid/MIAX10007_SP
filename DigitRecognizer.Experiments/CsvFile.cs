using System.Globalization;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Zapis tabuliek vo formate CSV (oddelovac ciarka, desatinna bodka), aby sa dali spracovat nezavisle od jazykoveho nastavenia.
/// </summary>
internal static class CsvFile
{
  /// <summary>
  /// Zapise tabulku do suboru. Priecinok sa vytvori, ak neexistuje.
  /// </summary>
  /// <param name="path">Cesta k suboru.</param>
  /// <param name="header">Nazvy stlpcov.</param>
  /// <param name="rows">Riadky tabulky, kazdy s rovnakym poctom hodnot ako hlavicka.</param>
  public static void Write(string path, IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var writer = new StreamWriter(path);
    writer.WriteLine(string.Join(',', header));
    foreach (var row in rows)
    {
      if (row.Count != header.Count)
      {
        throw new ArgumentException($"Riadok ma {row.Count} hodnot, hlavicka {header.Count}.");
      }
      writer.WriteLine(string.Join(',', row));
    }
  }

  /// <summary>
  /// Prevedie cislo na text s invariantnou kulturou.
  /// </summary>
  /// <param name="value">Cislo.</param>
  /// <param name="decimals">Pocet desatinnych miest.</param>
  /// <returns>Text s desatinnou bodkou.</returns>
  public static string Number(double value, int decimals = 6)
    => value.ToString("F" + decimals, CultureInfo.InvariantCulture);

  /// <summary>
  /// Prevedie cele cislo na text s invariantnou kulturou.
  /// </summary>
  /// <param name="value">Cislo.</param>
  /// <returns>Textova podoba cisla.</returns>
  public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
