namespace NeuralNet;

/// <summary>
/// Cela neuronova siet: zoznam vrstiev za sebou spolu s trenovanim, vyhodnocovanim a ukladanim vah.
/// </summary>
public sealed class Network
{
  private readonly List<Layer> layers = [];
  private readonly List<Param> parameters = [];

  /// <summary>
  /// Vrstvy siete v poradi od vstupu k vystupu.
  /// </summary>
  public IReadOnlyList<Layer> Layers => layers;
  /// <summary>
  /// Vsetky trenovatelne parametre siete v poradi vrstiev.
  /// </summary>
  public IReadOnlyList<Param> Parameters => parameters;
  /// <summary>
  /// Celkovy pocet trenovatelnych hodnot (vah a biasov).
  /// </summary>
  public int ParameterCount => parameters.Sum(p => p.W.Length);
  /// <summary>
  /// Pocet vstupnych hodnot pre jeden obrazok.
  /// </summary>
  public int InSize => layers[0].InSize;
  /// <summary>
  /// Pocet vystupnych tried.
  /// </summary>
  public int Classes => layers[^1].OutSize;

  /// <summary>
  /// Prida vrstvu na koniec siete a overi, ze jej vstup sedi na vystup predchadzajucej vrstvy.
  /// </summary>
  /// <param name="l">Pridavana vrstva.</param>
  /// <returns>Tato siet, aby sa volania dali retazit.</returns>
  /// <exception cref="ArgumentException">Velkosti vrstiev na seba nenadvazuju.</exception>
  public Network Add(Layer l)
  {
    // hned skontrolujem ci na seba vrstvy sedia, nech to nepadne az pri trenovani
    if (layers.Count > 0 && layers[^1].OutSize != l.InSize)
    {
      throw new ArgumentException($"Nesedí veľkosť vrstiev: {layers[^1].OutSize} != {l.InSize}");
    }
    layers.Add(l);
    parameters.AddRange(l.Params);
    return this;
  }

  /// <summary>
  /// Dopredny prechod cez vsetky vrstvy; vystupom su logity (este bez softmaxu).
  /// </summary>
  /// <param name="x">Vstupna davka dlzky batch * InSize.</param>
  /// <param name="batch">Pocet obrazkov v davke.</param>
  /// <param name="training">Ci ide o trenovanie (zapina dropout).</param>
  /// <returns>Logity (vystup poslednej vrstvy) pre kazdy obrazok davky, bez softmaxu.</returns>
  public float[] Forward(float[] x, int batch, bool training)
    => layers.Aggregate(x, (current, l) => l.Forward(current, batch, training));

  /// <summary>
  /// Predikcia pre jeden obrazok.
  /// </summary>
  /// <param name="input">Pixely jedneho obrazku dlzky InSize.</param>
  /// <returns>Pravdepodobnosti pre kazdu cifru.</returns>
  public float[] Predict(float[] input) => Probabilities(Forward(input, 1, false));

  /// <summary>
  /// Predikcia pre celu mnozinu, spracovava sa po davkach, aby sa nezaplnila pamat.
  /// </summary>
  /// <param name="data">Dataset na predikciu.</param>
  /// <param name="batchSize">Velkost davky pri spracovani.</param>
  /// <returns>Pole pravdepodobnosti tried, jeden vektor na obrazok.</returns>
  public float[][] PredictAll(Dataset data, int batchSize = 500)
  {
    var result = new float[data.Count][];
    for (var start = 0; start < data.Count; start += batchSize)
    {
      var count = Math.Min(batchSize, data.Count - start);
      var x = new float[count * InSize];
      Array.Copy(data.Images, start * InSize, x, 0, x.Length);
      var logits = Forward(x, count, false);
      for (var i = 0; i < count; i++)
      {
        var p = new float[Classes];
        Softmax(logits, i * Classes, p);
        result[start + i] = p;
      }
    }
    return result;
  }

  /// <summary>
  /// Dopredny prechod pre jeden obrazok, ktory vrati vystup kazdej vrstvy (vhodne na vizualizaciu vnutra siete).
  /// </summary>
  /// <param name="input">Pixely jedneho obrazku dlzky InSize.</param>
  /// <returns>Vystupy vrstiev v poradi vrstiev siete.</returns>
  public float[][] ForwardAll(float[] input)
  {
    var outs = new float[layers.Count][];
    var x = input;
    for (var i = 0; i < layers.Count; i++)
    {
      outs[i] = x = layers[i].Forward(x, 1, false);
    }
    return outs;
  }

  /// <summary>
  /// Prevedie logity jedneho obrazku na pravdepodobnosti pomocou softmaxu.
  /// </summary>
  /// <param name="logits">Vystup poslednej vrstvy pre jeden obrazok.</param>
  /// <returns>Pravdepodobnosti jednotlivych tried, ktorych sucet je 1.</returns>
  public static float[] Probabilities(float[] logits)
  {
    var p = new float[logits.Length];
    Softmax(logits, 0, p);
    return p;
  }

  /// <summary>
  /// Softmax s odpocitanim maxima, aby exp() nepretiekol pri velkych cislach.
  /// </summary>
  /// <param name="logits">Pole logitov (moze obsahovat viac obrazkov).</param>
  /// <param name="offset">Index prveho logitu spracuvaneho obrazku.</param>
  /// <param name="p">Vystupne pole pravdepodobnosti, jeho dlzka urcuje pocet tried.</param>
  private static void Softmax(float[] logits, int offset, float[] p)
  {
    var max = float.MinValue;
    for (var i = 0; i < p.Length; i++)
    {
      max = Math.Max(max, logits[offset + i]);
    }
    float sum = 0;
    for (var i = 0; i < p.Length; i++)
    {
      sum += p[i] = MathF.Exp(logits[offset + i] - max);
    }
    for (var i = 0; i < p.Length; i++)
    {
      p[i] /= sum;
    }
  }

  /// <summary>
  /// Dopredny aj spatny prechod na jednej davke; gradienty ostanu v parametroch a vahy sa nemenia.
  /// </summary>
  /// <param name="x">Vstupna davka dlzky batch * InSize.</param>
  /// <param name="labels">Skutocne triedy obrazkov v davke.</param>
  /// <param name="batch">Pocet obrazkov v davke.</param>
  /// <returns>Priemernu stratu (cross-entropy) a pocet spravne klasifikovanych obrazkov.</returns>
  public (float loss, int correct) ComputeGradients(float[] x, byte[] labels, int batch)
  {
    // najprv vynulovat gradienty z minuleho kroku
    foreach (var p in parameters)
    {
      Array.Clear(p.G);
    }

    var logits = Forward(x, batch, true);
    int c = Classes, correct = 0;
    float loss = 0;
    var grad = new float[batch * c];
    var prob = new float[c];

    for (var b = 0; b < batch; b++)
    {
      Softmax(logits, b * c, prob);
      int y = labels[b];

      // cross-entropy: -log(pravdepodobnost spravnej triedy), max() kvoli log(0)
      loss -= MathF.Log(Math.Max(prob[y], 1e-12f));

      for (var i = 0; i < c; i++)
      {
        // gradient softmax + cross-entropy je jednoducho (p - y), delim batchom aby to bol priemer
        grad[b * c + i] = (prob[i] - (i == y ? 1f : 0f)) / batch;
      }
      if (prob.ArgMax() == y)
      {
        correct++;
      }
    }

    // backprop: od poslednej vrstvy az po prvu
    for (var i = layers.Count - 1; i >= 0; i--)
    {
      grad = layers[i].Backward(grad, batch);
    }
    return (loss / batch, correct);
  }

  /// <summary>
  /// Jeden krok ucenia: vypocita gradienty a upravi vahy optimalizatorom.
  /// </summary>
  /// <param name="x">Vstupna davka dlzky batch * InSize.</param>
  /// <param name="labels">Skutocne triedy obrazkov v davke.</param>
  /// <param name="batch">Pocet obrazkov v davke.</param>
  /// <param name="optimizer">Optimalizator, ktory upravi vahy.</param>
  /// <returns>Priemernu stratu a pocet spravne klasifikovanych obrazkov.</returns>
  public (float loss, int correct) TrainBatch(float[] x, byte[] labels, int batch, IOptimizer optimizer)
  {
    var result = ComputeGradients(x, labels, batch);
    optimizer.Step(parameters);
    return result;
  }

  /// <summary>
  /// Vyhodnoti siet na celej mnozine, bez dropoutu a bez ucenia.
  /// </summary>
  /// <param name="data">Vyhodnocovany dataset.</param>
  /// <param name="batchSize">Velkost davky pri spracovani.</param>
  /// <returns>Presnost, strata a confusion matrix na cele mnozine.</returns>
  public EvaluationResult Evaluate(Dataset data, int batchSize = 500)
    => EvaluationResult.From(PredictAll(data, batchSize), data.Labels, Classes);

  /// <summary>
  /// Skopiruje vahy z inej siete rovnakej architektury, napr. na bezpecne vyhodnocovanie v inom vlakne,
  /// kedze vrstvy si pocas prechodu pamataju medzivysledky.
  /// </summary>
  /// <param name="source">Siet, z ktorej sa vahy kopiruju.</param>
  /// <exception cref="ArgumentException">Siete nemaju rovnaku architekturu.</exception>
  public void CopyWeightsFrom(Network source)
  {
    if (source.parameters.Count != parameters.Count)
    {
      throw new ArgumentException("Siete nemaju rovnaku architekturu.", nameof(source));
    }
    for (var i = 0; i < parameters.Count; i++)
    {
      if (source.parameters[i].W.Length != parameters[i].W.Length)
      {
        throw new ArgumentException("Siete nemaju rovnaku architekturu.", nameof(source));
      }
      Array.Copy(source.parameters[i].W, parameters[i].W, parameters[i].W.Length);
    }
  }

  /// <summary>
  /// Ulozi vahy do jednoducheho binarneho suboru (len cisla, pri nacitani sa musi zhodovat architektura).
  /// </summary>
  /// <param name="path">Cesta k vystupnemu suboru.</param>
  public void Save(string path)
  {
    using var w = new BinaryWriter(File.Create(path));
    foreach (var p in parameters)
    {
      w.Write(p.W.Length); // dlzku si ulozim ako poistku pri nacitani

      foreach (var v in p.W)
      {
        w.Write(v);
      }
    }
  }

  /// <summary>
  /// Nacita vahy zo suboru vytvoreneho metodou <see cref="Save"/>.
  /// </summary>
  /// <param name="path">Cesta k suboru s vahami.</param>
  /// <exception cref="InvalidDataException">Subor nezodpoveda architekture siete.</exception>
  public void Load(string path)
  {
    using var r = new BinaryReader(File.OpenRead(path));
    foreach (var p in parameters)
    {
      if (r.ReadInt32() != p.W.Length)
      {
        throw new InvalidDataException("Súbor váh nezodpovedá architektúre.");
      }
      for (var i = 0; i < p.W.Length; i++)
      {
        p.W[i] = r.ReadSingle();
      }
    }
  }
}
