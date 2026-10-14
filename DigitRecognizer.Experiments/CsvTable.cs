using System.Globalization;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Nacitana tabulka vo formate CSV, ktoru zapisal <see cref="CsvFile"/> (prvy riadok je hlavicka).
/// </summary>
internal sealed class CsvTable
{
  private CsvTable(IReadOnlyList<CsvRow> rows) => Rows = rows;

  /// <summary>
  /// Riadky tabulky bez hlavicky.
  /// </summary>
  public IReadOnlyList<CsvRow> Rows { get; }

  /// <summary>
  /// Nacita tabulku zo suboru.
  /// </summary>
  /// <param name="path">Cesta k suboru CSV.</param>
  /// <returns>Nacitana tabulka.</returns>
  /// <exception cref="FileNotFoundException">Subor neexistuje.</exception>
  public static CsvTable Read(string path)
  {
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"Chyba subor s vysledkami '{path}'. Najprv spusti prislusnu studiu.", path);
    }
    var lines = File.ReadAllLines(path).Where(l => l.Length > 0).ToArray();
    var header = lines[0].Split(',');
    return new CsvTable([.. lines.Skip(1).Select(l => new CsvRow(header, l.Split(',')))]);
  }
}

/// <summary>
/// Jeden riadok tabulky CSV, hodnoty sa citaju podla nazvu stlpca.
/// </summary>
internal sealed class CsvRow
{
  private readonly Dictionary<string, string> values;

  /// <summary>
  /// Vytvori riadok z hlavicky a hodnot.
  /// </summary>
  /// <param name="header">Nazvy stlpcov.</param>
  /// <param name="cells">Hodnoty riadka v poradi stlpcov.</param>
  public CsvRow(string[] header, string[] cells)
    => values = header.Zip(cells).ToDictionary(pair => pair.First, pair => pair.Second);

  /// <summary>
  /// Textova hodnota stlpca.
  /// </summary>
  /// <param name="column">Nazov stlpca.</param>
  /// <returns>Text bunky.</returns>
  public string Text(string column) => values[column];

  /// <summary>
  /// Cislo zo stlpca (desatinna bodka).
  /// </summary>
  /// <param name="column">Nazov stlpca.</param>
  /// <returns>Hodnota bunky ako cislo.</returns>
  public double Number(string column) => double.Parse(values[column], CultureInfo.InvariantCulture);

  /// <summary>
  /// Cele cislo zo stlpca.
  /// </summary>
  /// <param name="column">Nazov stlpca.</param>
  /// <returns>Hodnota bunky ako cele cislo.</returns>
  public int Integer(string column) => int.Parse(values[column], CultureInfo.InvariantCulture);
}
