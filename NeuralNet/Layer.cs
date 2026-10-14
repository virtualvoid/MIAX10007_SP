namespace NeuralNet;

/// <summary>
/// Zaklad pre vsetky vrstvy neuronovej siete.
/// Davka (batch) je ulozena ako jedno dlhe pole dlzky batch * size: prvy obrazok, potom druhy atd.
/// </summary>
public abstract class Layer
{
  /// <summary>
  /// Pocet hodnot vstupu pre jeden obrazok z davky.
  /// </summary>
  public abstract int InSize { get; }

  /// <summary>
  /// Pocet hodnot vystupu pre jeden obrazok z davky.
  /// </summary>
  public abstract int OutSize { get; }

  /// <summary>
  /// Trenovatelne parametre vrstvy; vrstvy bez parametrov (ReLU, Dropout) vratia prazdny zoznam.
  /// </summary>
  public virtual IEnumerable<Param> Params => [];

  /// <summary>
  /// Dopredny prechod vrstvou.
  /// </summary>
  /// <param name="x">Vstupna davka dlzky batch * InSize.</param>
  /// <param name="batch">Pocet obrazkov v davke.</param>
  /// <param name="training">Ci ide o trenovanie (dolezite hlavne pre dropout).</param>
  /// <returns>Vystupna davka dlzky batch * OutSize.</returns>
  public abstract float[] Forward(float[] x, int batch, bool training);

  /// <summary>
  /// Spatny prechod vrstvou; akumuluje gradienty parametrov do <see cref="Param.G"/>.
  /// </summary>
  /// <param name="grad">Gradient z nasledujucej vrstvy, dlzka batch * OutSize.</param>
  /// <param name="batch">Pocet obrazkov v davke.</param>
  /// <returns>Gradient pre predchadzajucu vrstvu, dlzka batch * InSize.</returns>
  public abstract float[] Backward(float[] grad, int batch);
}

