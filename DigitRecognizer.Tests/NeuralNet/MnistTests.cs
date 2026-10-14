using System.Buffers.Binary;
using NeuralNet;

namespace DigitRecognizer.Tests.NeuralNet;

/// <summary>
/// Testy nacitania datasetu MNIST (format IDX) v triede <see cref="Mnist"/> na malych vygenerovanych suboroch.
/// </summary>
public sealed class MnistTests : IDisposable
{
  /// <summary>
  /// Docasny priecinok so subormi vytvorenymi pre jeden test.
  /// </summary>
  private readonly string directory = Path.Combine(Path.GetTempPath(), $"mnist_test_{Guid.NewGuid():N}");

  /// <summary>
  /// Vytvori prazdny docasny priecinok.
  /// </summary>
  public MnistTests() => Directory.CreateDirectory(directory);

  /// <summary>
  /// Zmaze docasny priecinok aj so subormi.
  /// </summary>
  public void Dispose() => Directory.Delete(directory, true);

  /// <summary>
  /// Vytvori subor s obrazkami vo formate IDX (hlavicka: magic, pocet, riadky, stlpce, potom pixely).
  /// </summary>
  /// <param name="name">Nazov suboru v docasnom priecinku.</param>
  /// <param name="count">Pocet obrazkov.</param>
  /// <param name="pixels">Hodnoty pixelov vsetkych obrazkov za sebou.</param>
  /// <param name="magic">Magicke cislo v hlavicke.</param>
  /// <returns>Cesta k suboru.</returns>
  private string WriteImages(string name, int count, byte[] pixels, int magic = 0x803)
  {
    var header = new byte[16];
    BinaryPrimitives.WriteInt32BigEndian(header, magic);
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), count);
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8), 2); // riadky
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(12), 3); // stlpce
    var path = Path.Combine(directory, name);
    File.WriteAllBytes(path, [.. header, .. pixels]);
    return path;
  }

  /// <summary>
  /// Vytvori subor s popiskami vo formate IDX (hlavicka: magic, pocet, potom jeden bajt na popisok).
  /// </summary>
  /// <param name="name">Nazov suboru v docasnom priecinku.</param>
  /// <param name="labels">Popisky.</param>
  /// <param name="magic">Magicke cislo v hlavicke.</param>
  /// <returns>Cesta k suboru.</returns>
  private string WriteLabels(string name, byte[] labels, int magic = 0x801)
  {
    var header = new byte[8];
    BinaryPrimitives.WriteInt32BigEndian(header, magic);
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), labels.Length);
    var path = Path.Combine(directory, name);
    File.WriteAllBytes(path, [.. header, .. labels]);
    return path;
  }

  /// <summary>
  /// Over, ze obrazky sa nacitaju po riadkoch a pixely sa normalizuju z rozsahu 0 az 255 na 0 az 1.
  /// </summary>
  [Fact]
  public void LoadImagesNormalizesPixels()
  {
    var path = WriteImages("images", 2, [0, 51, 102, 153, 204, 255, 255, 0, 255, 0, 255, 0]);

    var images = Mnist.LoadImages(path);

    Assert.Equal(12, images.Length);
    Assert.Equal(0f, images[0]);
    Assert.Equal(0.2f, images[1], 1e-6f);
    Assert.Equal(1f, images[5]);
    Assert.Equal(1f, images[6]);
  }

  /// <summary>
  /// Over, ze popisky sa nacitaju ako bajty v poradi v subore.
  /// </summary>
  [Fact]
  public void LoadLabelsReadsBytes()
  {
    var path = WriteLabels("labels", [7, 3, 0]);

    Assert.Equal([7, 3, 0], Mnist.LoadLabels(path));
  }

  /// <summary>
  /// Over, ze subor s nespravnym magickym cislom (napriklad popisky namiesto obrazkov) skonci vynimkou InvalidDataException.
  /// </summary>
  [Fact]
  public void WrongMagicNumberIsRejected()
  {
    var images = WriteImages("images_bad", 1, [0, 0, 0, 0, 0, 0], magic: 0x801);
    var labels = WriteLabels("labels_bad", [1], magic: 0x803);

    Assert.Throws<InvalidDataException>(() => Mnist.LoadImages(images));
    Assert.Throws<InvalidDataException>(() => Mnist.LoadLabels(labels));
  }

  /// <summary>
  /// Over, ze trenovacia a testovacia mnozina sa nacitaju z priecinka so standardnymi nazvami suborov
  /// a obsahuju obrazky aj popisky rovnakeho poctu.
  /// </summary>
  [Fact]
  public void LoadTrainAndTestReadStandardFileNames()
  {
    WriteImages("train-images.idx3-ubyte", 2, new byte[12]);
    WriteLabels("train-labels.idx1-ubyte", [1, 2]);
    WriteImages("t10k-images.idx3-ubyte", 1, new byte[6]);
    WriteLabels("t10k-labels.idx1-ubyte", [9]);

    var train = Mnist.LoadTrain(directory);
    var test = Mnist.LoadTest(directory);

    Assert.Equal(2, train.Count);
    Assert.Equal(12, train.Images.Length);
    Assert.Equal(1, test.Count);
    Assert.Equal((byte)9, test.Labels[0]);
  }

  /// <summary>
  /// Over, ze dostupnost datasetu sa urci podla pritomnosti vsetkych styroch suborov a prazdna cesta je nedostupna.
  /// </summary>
  [Fact]
  public void IsAvailableRequiresAllFourFiles()
  {
    Assert.False(Mnist.IsAvailable(null));
    Assert.False(Mnist.IsAvailable(" "));
    Assert.False(Mnist.IsAvailable(directory));

    WriteImages("train-images.idx3-ubyte", 1, new byte[6]);
    WriteLabels("train-labels.idx1-ubyte", [1]);
    WriteImages("t10k-images.idx3-ubyte", 1, new byte[6]);
    Assert.False(Mnist.IsAvailable(directory));

    WriteLabels("t10k-labels.idx1-ubyte", [1]);
    Assert.True(Mnist.IsAvailable(directory));
  }
}
