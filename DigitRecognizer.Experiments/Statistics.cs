namespace DigitRecognizer.Experiments;

/// <summary>
/// Zakladna statistika pouzita pri vyhodnoteni experimentov: priemer, smerodajna odchylka, t-testy, McNemarov test
/// a Wilsonov interval. Specialne funkcie (gama, neuplna beta) su implementovane priamo, bez externych kniznic.
/// </summary>
internal static class Statistics
{
  /// <summary>
  /// Kvantil standardneho normalneho rozdelenia pre obojstranny 95 % interval spolahlivosti.
  /// </summary>
  private const double Z95 = 1.959963984540054;

  // koeficienty Lanczosovej aproximacie (g = 7, n = 9)
  private static readonly double[] Lanczos =
  [
    0.99999999999980993, 676.5203681218851, -1259.1392167224028, 771.32342877765313, -176.61502916214059,
    12.507343278686905, -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7
  ];

  /// <summary>
  /// Aritmeticky priemer.
  /// </summary>
  /// <param name="values">Hodnoty (aspon jedna).</param>
  /// <returns>Priemer hodnot.</returns>
  public static double Mean(IReadOnlyList<double> values) => values.Average();

  /// <summary>
  /// Vyberova smerodajna odchylka (delenie n - 1).
  /// </summary>
  /// <param name="values">Hodnoty.</param>
  /// <returns>Smerodajna odchylka, alebo 0 pre jedinu hodnotu.</returns>
  public static double StdDev(IReadOnlyList<double> values)
  {
    if (values.Count < 2)
    {
      return 0;
    }
    var mean = Mean(values);
    return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1));
  }

  /// <summary>
  /// Welchov t-test pre dve nezavisle skupiny s moznou rozdielnou varianciou.
  /// </summary>
  /// <param name="first">Prva skupina.</param>
  /// <param name="second">Druha skupina.</param>
  /// <param name="confidence">Uroven intervalu spolahlivosti rozdielu.</param>
  /// <returns>Rozdiel priemerov (prva minus druha), statistika, stupne volnosti, p-hodnota a interval spolahlivosti.</returns>
  public static TTestResult WelchTTest(IReadOnlyList<double> first, IReadOnlyList<double> second, double confidence = 0.95)
  {
    var difference = Mean(first) - Mean(second);
    var varianceFirst = Math.Pow(StdDev(first), 2) / first.Count;
    var varianceSecond = Math.Pow(StdDev(second), 2) / second.Count;
    var standardError = Math.Sqrt(varianceFirst + varianceSecond);

    // Welchova-Satterthwaiteova aproximacia stupnov volnosti
    var df = Math.Pow(varianceFirst + varianceSecond, 2)
             / (varianceFirst * varianceFirst / (first.Count - 1) + varianceSecond * varianceSecond / (second.Count - 1));
    return Build(difference, standardError, df, confidence);
  }

  /// <summary>
  /// Parovy t-test pre dve skupiny meranych na tych istych jednotkach (napriklad rovnaky seed).
  /// </summary>
  /// <param name="first">Prva skupina.</param>
  /// <param name="second">Druha skupina, rovnako dlha a v rovnakom poradi.</param>
  /// <param name="confidence">Uroven intervalu spolahlivosti rozdielu.</param>
  /// <returns>Priemerny rozdiel (prva minus druha), statistika, stupne volnosti, p-hodnota a interval spolahlivosti.</returns>
  public static TTestResult PairedTTest(IReadOnlyList<double> first, IReadOnlyList<double> second, double confidence = 0.95)
  {
    var differences = first.Zip(second, (a, b) => a - b).ToArray();
    var standardError = StdDev(differences) / Math.Sqrt(differences.Length);
    return Build(Mean(differences), standardError, differences.Length - 1, confidence);
  }

  /// <summary>
  /// Presny obojstranny McNemarov test: ci sa dva klasifikatory lisia v pocte chyb. Pocita sa z poctu obrazkov,
  /// kde sa pomylil iba prvy (b), a tych, kde sa pomylil iba druhy (c), proti hypoteze b = c (binomicke rozdelenie 0,5).
  /// </summary>
  /// <param name="b">Pocet obrazkov, kde bol spravny iba prvy klasifikator.</param>
  /// <param name="c">Pocet obrazkov, kde bol spravny iba druhy klasifikator.</param>
  /// <returns>Obojstranna p-hodnota (1 ak sa klasifikatory nelisia v ziadnom obrazku).</returns>
  public static double McNemarExactP(int b, int c)
  {
    var n = b + c;
    if (n == 0)
    {
      return 1;
    }
    var k = Math.Min(b, c);
    double tail = 0;
    for (var i = 0; i <= k; i++)
    {
      var logTerm = LogGamma(n + 1) - LogGamma(i + 1) - LogGamma(n - i + 1) - n * Math.Log(2);
      tail += Math.Exp(logTerm);
    }
    return Math.Min(1, 2 * tail);
  }

  /// <summary>
  /// Wilsonov 95 % interval spolahlivosti pre podiel (vhodnejsi nez normalna aproximacia pri podieloch blizko 1).
  /// </summary>
  /// <param name="successes">Pocet uspechov (napriklad spravnych predikcii).</param>
  /// <param name="trials">Pocet pokusov.</param>
  /// <returns>Dolna a horna hranica intervalu v intervale 0 az 1.</returns>
  public static (double Low, double High) WilsonInterval(int successes, int trials)
  {
    var p = (double)successes / trials;
    var denominator = 1 + Z95 * Z95 / trials;
    var center = (p + Z95 * Z95 / (2.0 * trials)) / denominator;
    var half = Z95 * Math.Sqrt(p * (1 - p) / trials + Z95 * Z95 / (4.0 * trials * trials)) / denominator;
    return (center - half, center + half);
  }

  /// <summary>
  /// Obojstranna p-hodnota Studentovho t-rozdelenia.
  /// </summary>
  /// <param name="t">Hodnota statistiky.</param>
  /// <param name="df">Pocet stupnov volnosti.</param>
  /// <returns>Pravdepodobnost, ze absolutna hodnota statistiky je aspon taka velka ako t.</returns>
  public static double StudentTwoSidedP(double t, double df)
    => RegularizedIncompleteBeta(df / 2, 0.5, df / (df + t * t));

  /// <summary>
  /// Kriticka hodnota Studentovho t-rozdelenia pre obojstranny interval spolahlivosti, najdena polenim intervalu.
  /// </summary>
  /// <param name="confidence">Uroven spolahlivosti, napriklad 0,95.</param>
  /// <param name="df">Pocet stupnov volnosti.</param>
  /// <returns>Hodnota t, pre ktoru je obojstranna p-hodnota rovna 1 - confidence.</returns>
  public static double CriticalValue(double confidence, double df)
  {
    double low = 0, high = 1000;
    for (var i = 0; i < 200; i++)
    {
      var mid = (low + high) / 2;
      if (StudentTwoSidedP(mid, df) > 1 - confidence)
      {
        low = mid;
      }
      else
      {
        high = mid;
      }
    }
    return (low + high) / 2;
  }

  /// <summary>
  /// Logaritmus gama funkcie (Lanczosova aproximacia).
  /// </summary>
  /// <param name="x">Argument, kladne cislo.</param>
  /// <returns>Hodnota ln(Gamma(x)).</returns>
  public static double LogGamma(double x)
  {
    if (x < 0.5)
    {
      // reflexny vzorec pre male argumenty
      return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1 - x);
    }
    x -= 1;
    var a = Lanczos[0];
    var t = x + 7.5;
    for (var i = 1; i < Lanczos.Length; i++)
    {
      a += Lanczos[i] / (x + i);
    }
    return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(a);
  }

  /// <summary>
  /// Regularizovana neuplna beta funkcia I_x(a, b), ktora je distribucnou funkciou beta rozdelenia.
  /// </summary>
  /// <param name="a">Prvy parameter, kladny.</param>
  /// <param name="b">Druhy parameter, kladny.</param>
  /// <param name="x">Hodnota v intervale 0 az 1.</param>
  /// <returns>Hodnota v intervale 0 az 1.</returns>
  public static double RegularizedIncompleteBeta(double a, double b, double x)
  {
    if (x <= 0)
    {
      return 0;
    }
    if (x >= 1)
    {
      return 1;
    }
    var front = Math.Exp(LogGamma(a + b) - LogGamma(a) - LogGamma(b) + a * Math.Log(x) + b * Math.Log(1 - x));

    // retazovy zlomok konverguje rychlo len pre x pod prahom, inak sa pouzije symetria funkcie
    return x < (a + 1) / (a + b + 2)
      ? front * BetaContinuedFraction(a, b, x) / a
      : 1 - front * BetaContinuedFraction(b, a, 1 - x) / b;
  }

  // vyhodnoti t-test z rozdielu, standardnej chyby a stupnov volnosti
  private static TTestResult Build(double difference, double standardError, double df, double confidence)
  {
    var t = difference / standardError;
    var critical = CriticalValue(confidence, df);
    return new TTestResult(
      difference,
      t,
      df,
      StudentTwoSidedP(t, df),
      difference - critical * standardError,
      difference + critical * standardError);
  }

  // retazovy zlomok pre neuplnu beta funkciu (modifikovany Lentzov algoritmus)
  private static double BetaContinuedFraction(double a, double b, double x)
  {
    const int maxIterations = 300;
    const double epsilon = 3e-16;
    const double tiny = 1e-300;

    var qab = a + b;
    var qap = a + 1;
    var qam = a - 1;
    var c = 1.0;
    var d = 1 - qab * x / qap;
    d = 1 / (Math.Abs(d) < tiny ? tiny : d);
    var h = d;
    for (var m = 1; m <= maxIterations; m++)
    {
      var m2 = 2 * m;
      var even = m * (b - m) * x / ((qam + m2) * (a + m2));
      d = 1 + even * d;
      d = 1 / (Math.Abs(d) < tiny ? tiny : d);
      c = 1 + even / c;
      c = Math.Abs(c) < tiny ? tiny : c;
      h *= d * c;

      var odd = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
      d = 1 + odd * d;
      d = 1 / (Math.Abs(d) < tiny ? tiny : d);
      c = 1 + odd / c;
      c = Math.Abs(c) < tiny ? tiny : c;
      var delta = d * c;
      h *= delta;
      if (Math.Abs(delta - 1) < epsilon)
      {
        break;
      }
    }
    return h;
  }
}
