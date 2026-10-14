using System.Buffers.Binary;

namespace NeuralNet;

/// <summary>
/// Nacitanie suborov datasetu MNIST vo formate IDX (cisla v hlavicke su big-endian).
/// </summary>
public static class Mnist
{
  /// <summary>
  /// Magicke cislo suboru s obrazkami.
  /// </summary>
  private const int MagicImages = 0x803;
  /// <summary>
  /// Magicke cislo suboru s popiskami.
  /// </summary>
  private const int MagicLabels = 0x801;

  /// <summary>
  /// Nazov suboru s trenovacimi obrazkami.
  /// </summary>
  private const string TrainImages = "train-images.idx3-ubyte";
  /// <summary>
  /// Nazov suboru s trenovacimi popiskami.
  /// </summary>
  private const string TrainLabels = "train-labels.idx1-ubyte";
  /// <summary>
  /// Nazov suboru s testovacimi obrazkami.
  /// </summary>
  private const string TestImages = "t10k-images.idx3-ubyte";
  /// <summary>
  /// Nazov suboru s testovacimi popiskami.
  /// </summary>
  private const string TestLabels = "t10k-labels.idx1-ubyte";

  /// <summary>
  /// Ci su v priecinku vsetky styri subory datasetu.
  /// </summary>
  /// <returns>True, ak su v priecinku vsetky styri subory datasetu.</returns>
  public static bool IsAvailable(string? directory) =>
    !string.IsNullOrWhiteSpace(directory) &&
    new[] { TrainImages, TrainLabels, TestImages, TestLabels }.All(f => File.Exists(Path.Combine(directory, f)));

  /// <summary>
  /// Nacita trenovaciu mnozinu z priecinka s datasetom.
  /// </summary>
  /// <param name="directory">Priecinok so subormi MNIST.</param>
  /// <returns>Trenovacia mnozina (60 000 obrazkov s popiskami).</returns>
  public static Dataset LoadTrain(string directory) =>
    new(LoadImages(Path.Combine(directory, TrainImages)), LoadLabels(Path.Combine(directory, TrainLabels)));

  /// <summary>
  /// Nacita testovaciu mnozinu z priecinka s datasetom.
  /// </summary>
  /// <param name="directory">Priecinok so subormi MNIST.</param>
  /// <returns>Testovacia mnozina (10 000 obrazkov s popiskami).</returns>
  public static Dataset LoadTest(string directory) =>
    new(LoadImages(Path.Combine(directory, TestImages)), LoadLabels(Path.Combine(directory, TestLabels)));

  /// <summary>
  /// Nacita obrazky zo suboru IDX (hlavicka 16 bajtov: magic, pocet, riadky, stlpce, potom pixely 0..255).
  /// </summary>
  /// <param name="path">Cesta k suboru s obrazkami.</param>
  /// <returns>Jedno dlhe pole dlzky n * 784 s hodnotami 0..1.</returns>
  /// <exception cref="InvalidDataException">Subor nema ocakavane magicke cislo.</exception>
  public static float[] LoadImages(string path)
  {
    var d = File.ReadAllBytes(path);
    if (BinaryPrimitives.ReadInt32BigEndian(d) != MagicImages)
    {
      throw new InvalidDataException(path);
    }
    var n = BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(4));
    var size = BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(8)) * BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(12));
    var img = new float[n * size];
    for (var i = 0; i < img.Length; i++)
    {
      img[i] = d[16 + i] / 255f;
    }
    return img;
  }

  /// <summary>
  /// Nacita popisky zo suboru IDX (hlavicka 8 bajtov, potom jeden bajt s cifrou na kazdy obrazok).
  /// </summary>
  /// <param name="path">Cesta k suboru s popiskami.</param>
  /// <returns>Popisky (cislice 0 az 9), jeden bajt na obrazok.</returns>
  /// <exception cref="InvalidDataException">Subor nema ocakavane magicke cislo.</exception>
  public static byte[] LoadLabels(string path)
  {
    var d = File.ReadAllBytes(path);
    if (BinaryPrimitives.ReadInt32BigEndian(d) != MagicLabels)
    {
      throw new InvalidDataException(path);
    }
    var n = BinaryPrimitives.ReadInt32BigEndian(d.AsSpan(4));
    return [.. d.AsSpan(8, n)];
  }
}
