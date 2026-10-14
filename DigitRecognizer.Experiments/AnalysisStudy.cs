using System.Globalization;
using System.Text;
using Fuzzy;

namespace DigitRecognizer.Experiments;

/// <summary>
/// Spracuje vysledky ostatnych studii: vypocita priemery, smerodajne odchylky a statisticke testy a vytvori
/// tabulky (Markdown) a grafy (SVG) pre pracu.
/// </summary>
internal static class AnalysisStudy
{
  private const string Blue = "#4682B4";
  private const string Orange = "#E4572E";
  private const string Green = "#2E8B57";
  private const string Purple = "#7B4EA3";
  private const string Gold = "#C9A227";
  private const string Gray = "#777777";
  private const string Teal = "#1B998B";
  private const string Brown = "#8C5A3C";

  private static readonly CultureInfo Culture = new("sk-SK");

  // porovnavane metody odhadu spolahlivosti v poradi zobrazenia a ich slovenske nazvy
  private static readonly (string Key, string Name, string Color)[] Methods =
  [
    ("fuzzy_dual", "Fuzzy (CNN + MLP)", Green),
    ("fuzzy_single", "Fuzzy (len CNN)", Teal),
    ("max_softmax", "Max. pravdepodobnosť", Blue),
    ("margin", "Rozdiel top-2", Purple),
    ("neg_entropy", "Záporná entropia", Gold),
    ("agreement_only", "Zhoda MLP", Gray),
    ("ensemble_mean", "Priemer CNN a MLP", Orange)
  ];

  /// <summary>
  /// Nacita vysledky z priecinka s CSV subormi a zapise analyzu (docs/data/analysis.md) a grafy (docs/img/*.svg).
  /// Studie, ktorych vysledky chybaju, sa preskocia.
  /// </summary>
  /// <param name="options">Nastavenie experimentov (priecinok s vysledkami).</param>
  public static void Run(ExperimentOptions options)
  {
    var data = options.OutputDirectory;
    var images = Path.GetFullPath(Path.Combine(data, "..", "img"));
    Directory.CreateDirectory(images);
    var markdown = new StringBuilder();

    Section(markdown, data, "seed_study_final.csv", () => SeedSection(data, images, markdown));
    Section(markdown, data, "shift_study.csv", () => ShiftSection(data, images, markdown));
    Section(markdown, data, "reliability_metrics.csv", () => ReliabilitySection(data, images, markdown));
    Section(markdown, data, "optimizer_study.csv", () => OptimizerSection(data, images, markdown));
    MembershipFigure(images);

    File.WriteAllText(Path.Combine(data, "analysis.md"), markdown.ToString());
    Console.WriteLine(markdown);
    Console.WriteLine($"Analyza zapisana do {Path.Combine(data, "analysis.md")}, grafy do {images}");
  }

  // spusti cast analyzy, ak existuje vstupny subor
  private static void Section(StringBuilder markdown, string data, string requiredFile, Action action)
  {
    if (File.Exists(Path.Combine(data, requiredFile)))
    {
      action();
    }
    else
    {
      markdown.AppendLine($"_Vysledky '{requiredFile}' chybaju, cast analyzy sa preskocila._\n");
    }
  }

  // porovnanie MLP a CNN cez opakovania: presnost, Welchov test, McNemarov test, krivky ucenia
  private static void SeedSection(string data, string images, StringBuilder md)
  {
    var final = CsvTable.Read(Path.Combine(data, "seed_study_final.csv"));
    var epochs = CsvTable.Read(Path.Combine(data, "seed_study_epochs.csv"));
    var mcnemar = CsvTable.Read(Path.Combine(data, "mcnemar_counts.csv"));
    var kinds = new[] { ("Mlp", "MLP"), ("Cnn", "CNN") };

    double[] Values(string kind, string column, double scale = 1)
      => [.. final.Rows.Where(r => r.Text("model") == kind).Select(r => r.Number(column) * scale)];

    md.AppendLine("### Porovnanie MLP a CNN (opakované trénovanie)\n");
    md.AppendLine(Table(
      ["Model", "Opakovaní", "Presnosť na teste [%] (priemer ± SD)", "Minimum – maximum [%]", "Chýb z 10 000 (priemer)", "Čas trénovania [s]"],
      kinds.Select(k =>
      {
        var accuracy = Values(k.Item1, "test_accuracy", 100);
        return new[]
        {
          k.Item2, accuracy.Length.ToString(Culture), PlusMinus(accuracy, 2), $"{Num(accuracy.Min(), 2)} – {Num(accuracy.Max(), 2)}",
          Num(Values(k.Item1, "errors").Average(), 1), Num(Values(k.Item1, "train_seconds").Average(), 0)
        };
      })));

    var welch = Statistics.WelchTTest(Values("Cnn", "test_accuracy", 100), Values("Mlp", "test_accuracy", 100));
    md.AppendLine($"Rozdiel CNN − MLP: {Signed(welch.Difference, 2)} p. b., 95 % IS [{Num(welch.CiLow, 2)}; {Num(welch.CiHigh, 2)}], " +
                  $"Welchov t-test: t = {Num(welch.T, 2)}, df = {Num(welch.Df, 1)}, p {PValue(welch.P)}.\n");

    md.AppendLine("McNemarov presný test na rovnakých testovacích obrázkoch (dvojica MLP a CNN s rovnakým seedom):\n");
    md.AppendLine(Table(
      ["Seed", "Len MLP správny", "Len CNN správny", "Obaja zlí", "p (McNemar)"],
      mcnemar.Rows.Select(r => new[]
      {
        r.Text("seed"), r.Text("mlp_only"), r.Text("cnn_only"), r.Text("both_wrong"),
        PValue(Statistics.McNemarExactP(r.Integer("mlp_only"), r.Integer("cnn_only")))
      })));

    var shown = new[] { 1, 2, 3, 5, 10 }.Where(e => epochs.Rows.Any(r => r.Integer("epoch") == e)).ToArray();
    md.AppendLine("Presnosť na testovacej množine po epochách [%] (priemer ± SD):\n");
    md.AppendLine(Table(
      ["Model", .. shown.Select(e => $"Epocha {e}")],
      kinds.Select(k => new[] { k.Item2 }.Concat(shown.Select(e =>
        PlusMinus([.. epochs.Rows.Where(r => r.Text("model") == k.Item1 && r.Integer("epoch") == e).Select(r => r.Number("test_accuracy") * 100)], 2))).ToArray())));

    foreach (var (file, title, column, scale, yLabel, yFormat) in new[]
             {
               ("learning_accuracy.svg", "Presnosť na testovacej množine", "test_accuracy", 100.0, "Presnosť [%]", "0.#"),
               ("learning_loss.svg", "Strata na testovacej množine", "test_loss", 1.0, "Cross-entropy", "0.00")
             })
    {
      var plot = new SvgPlot(title, "Epocha", yLabel) { YFormat = yFormat, XFormat = "0" };
      foreach (var (kind, name, color) in new[] { ("Mlp", "MLP", Orange), ("Cnn", "CNN", Blue) })
      {
        var points = new List<(double, double)>();
        var errors = new List<double>();
        foreach (var epoch in epochs.Rows.Select(r => r.Integer("epoch")).Distinct().Order())
        {
          var values = epochs.Rows.Where(r => r.Text("model") == kind && r.Integer("epoch") == epoch).Select(r => r.Number(column) * scale).ToArray();
          points.Add((epoch, values.Average()));
          errors.Add(Statistics.StdDev(values));
        }
        plot.Series.Add(new PlotSeries(name, color, points, errors, ErrorStyle.Band));
      }
      File.WriteAllText(Path.Combine(images, file), plot.ToSvg());
    }
  }

  // odolnost voci posunu: presnost podla velkosti posunu, priemer cez smery a opakovania
  private static void ShiftSection(string data, string images, StringBuilder md)
  {
    var rows = CsvTable.Read(Path.Combine(data, "shift_study.csv")).Rows;
    var perSeed = rows
      .GroupBy(r => (Model: r.Text("model"), Seed: r.Integer("seed"), Shift: r.Integer("shift")))
      .Select(g => (g.Key.Model, g.Key.Seed, g.Key.Shift, Accuracy: g.Average(r => r.Number("test_accuracy")) * 100))
      .ToList();
    var shifts = perSeed.Select(x => x.Shift).Distinct().Order().ToArray();

    double[] Values(string model, int shift) => [.. perSeed.Where(x => x.Model == model && x.Shift == shift).Select(x => x.Accuracy)];

    md.AppendLine("### Odolnosť voči posunu číslice\n");
    md.AppendLine("Presnosť [%] na testovacích obrázkoch posunutých o daný počet pixelov (priemer cez štyri smery a opakovania, ± SD):\n");
    md.AppendLine(Table(
      ["Posun [px]", "MLP", "CNN", "Rozdiel CNN − MLP [p. b.]", "p (Welch)"],
      shifts.Select(s =>
      {
        var test = Statistics.WelchTTest(Values("Cnn", s), Values("Mlp", s));
        return new[] { s.ToString(Culture), PlusMinus(Values("Mlp", s), 2), PlusMinus(Values("Cnn", s), 2), Signed(test.Difference, 2), PValue(test.P) };
      })));

    var plot = new SvgPlot("Odolnosť voči posunu číslice", "Posun obrázka [px]", "Presnosť [%]") { YFormat = "0", XFormat = "0" };
    foreach (var (model, name, color) in new[] { ("Mlp", "MLP", Orange), ("Cnn", "CNN", Blue) })
    {
      plot.Series.Add(new PlotSeries(
        name, color,
        [.. shifts.Select(s => ((double)s, Values(model, s).Average()))],
        [.. shifts.Select(s => Statistics.StdDev(Values(model, s)))],
        ErrorStyle.Bars, Markers: true));
    }
    File.WriteAllText(Path.Combine(images, "shift_robustness.svg"), plot.ToSvg());
  }

  // kvalita fuzzy hodnotenia v porovnani so zakladnymi metodami
  private static void ReliabilitySection(string data, string images, StringBuilder md)
  {
    var metrics = CsvTable.Read(Path.Combine(data, "reliability_metrics.csv")).Rows;
    var levels = CsvTable.Read(Path.Combine(data, "reliability_levels.csv")).Rows;
    var coverageColumns = new[] { "risk_at_0.90", "risk_at_0.95", "risk_at_0.98", "risk_at_0.99" };

    double[] Values(string method, string column, double scale = 1)
      => [.. metrics.Where(r => r.Text("method") == method).OrderBy(r => r.Integer("seed")).Select(r => r.Number(column) * scale)];

    md.AppendLine("### Fuzzy hodnotenie spoľahlivosti a základné metódy\n");
    md.AppendLine($"Odhaľovanie chybných predikcií CNN (priemerný počet chýb na teste: {Num(Values("fuzzy_dual", "errors").Average(), 1)}). " +
                  "AUROC: pravdepodobnosť, že správna predikcia má vyššie skóre než chybná (0,5 = náhoda). " +
                  "AURC a riziko pri pokrytí: miera chyby medzi prijatými predikciami [%], menej je lepšie. Priemer ± SD cez opakovania.\n");
    md.AppendLine(Table(
      ["Metóda", "AUROC", "AURC [%]", .. coverageColumns.Select(c => $"Riziko pri pokrytí {c[^2..]} % [%]")],
      Methods.Select(m => (string[])
      [
        m.Name, PlusMinus(Values(m.Key, "auroc"), 4), PlusMinus(Values(m.Key, "aurc", 100), 3),
        .. coverageColumns.Select(c => PlusMinus(Values(m.Key, c, 100), 3))
      ])));

    md.AppendLine("Rozlíšenie skóre: koľko rôznych hodnôt skóre metóda priradí a aký podiel obrázkov je v najväčšej skupine s rovnakým skóre (priemer ± SD):\n");
    md.AppendLine(Table(
      ["Metóda", "Rôznych hodnôt skóre", "Najväčšia skupina rovnakého skóre [%]"],
      Methods.Select(m => new[]
      {
        m.Name, PlusMinus(Values(m.Key, "distinct_scores"), 0), PlusMinus(Values(m.Key, "largest_tie_share", 100), 2)
      })));

    md.AppendLine("Párový t-test (rovnaké opakovania) fuzzy hodnotenia (CNN + MLP) proti ostatným metódam. " +
                  "Kladný rozdiel AUROC a záporný rozdiel AURC alebo rizika znamená, že fuzzy je lepšie:\n");
    md.AppendLine(Table(
      ["Porovnaná metóda", "Δ AUROC", "p", "Δ AURC [p. b.]", "p", "Δ riziko pri pokrytí 99 % [p. b.]", "p"],
      Methods.Skip(1).Select(m =>
      {
        var auroc = Statistics.PairedTTest(Values("fuzzy_dual", "auroc"), Values(m.Key, "auroc"));
        var aurc = Statistics.PairedTTest(Values("fuzzy_dual", "aurc", 100), Values(m.Key, "aurc", 100));
        var risk = Statistics.PairedTTest(Values("fuzzy_dual", "risk_at_0.99", 100), Values(m.Key, "risk_at_0.99", 100));
        return new[] { m.Name, Signed(auroc.Difference, 4), PValue(auroc.P), Signed(aurc.Difference, 3), PValue(aurc.P), Signed(risk.Difference, 3), PValue(risk.P) };
      })));

    var seedCount = levels.Select(r => r.Integer("seed")).Distinct().Count();
    md.AppendLine("Rozdelenie testovacej množiny podľa úrovne fuzzy spoľahlivosti (súčet cez všetky opakovania):\n");
    md.AppendLine(Table(
      ["Spoľahlivosť", "Obrázkov na opakovanie", "Podiel", "Presnosť CNN [%]", "Chýb na opakovanie"],
      new[] { "High", "Medium", "Low" }.Select(level =>
      {
        var group = levels.Where(r => r.Text("level") == level).ToList();
        double count = group.Sum(r => r.Number("count")), correct = group.Sum(r => r.Number("correct"));
        var total = levels.Sum(r => r.Number("count"));
        return new[]
        {
          level switch { "High" => "Vysoká", "Medium" => "Stredná", _ => "Nízka" }, Num(count / seedCount, 1), Num(100 * count / total, 2) + " %",
          count > 0 ? Num(100 * correct / count, 2) : "–", Num((count - correct) / seedCount, 1)
        };
      })));

    var bars = new SvgBarChart("Odhaľovanie chýb: AUROC (priemer ± SD)", "AUROC") { Min = 0.9, Max = 1.0, Format = "0.000" };
    foreach (var m in Methods)
    {
      bars.Add(m.Name, Values(m.Key, "auroc").Average(), Statistics.StdDev(Values(m.Key, "auroc")), m.Color);
    }
    File.WriteAllText(Path.Combine(images, "auroc_methods.svg"), bars.ToSvg());

    var curves = CsvTable.Read(Path.Combine(data, "risk_coverage_seed1.csv")).Rows;
    var curve = new SvgPlot("Krivka riziko–pokrytie (opakovanie 1)", "Pokrytie (podiel prijatých predikcií)", "Miera chyby medzi prijatými [%]")
    {
      XMin = 0.9, XMax = 1.0, YMin = 0, XFormat = "0.00", YFormat = "0.0#"
    };
    foreach (var m in Methods.Where(m => m.Key is "fuzzy_dual" or "fuzzy_single" or "max_softmax" or "margin" or "ensemble_mean"))
    {
      curve.Series.Add(new PlotSeries(
        m.Name, m.Color,
        [.. curves.Where(r => r.Text("method") == m.Key && r.Number("coverage") >= 0.9).Select(r => (r.Number("coverage"), r.Number("risk") * 100))],
        Dashed: m.Key != "fuzzy_dual"));
    }
    File.WriteAllText(Path.Combine(images, "risk_coverage.svg"), curve.ToSvg());
  }

  // vplyv optimalizatora a learning rate na ucenie MLP
  private static void OptimizerSection(string data, string images, StringBuilder md)
  {
    var rows = CsvTable.Read(Path.Combine(data, "optimizer_study.csv")).Rows;
    var configs = rows.Select(r => (Optimizer: r.Text("optimizer"), Lr: r.Number("learning_rate"))).Distinct().ToList();
    var colors = new[] { Blue, Teal, Green, Orange, Purple };
    var lastEpoch = rows.Max(r => r.Integer("epoch"));

    double[] Values(string optimizer, double lr, int epoch)
      => [.. rows.Where(r => r.Text("optimizer") == optimizer && Math.Abs(r.Number("learning_rate") - lr) < 1e-9 && r.Integer("epoch") == epoch).Select(r => r.Number("test_accuracy") * 100)];

    md.AppendLine("### Optimalizátor a learning rate (MLP)\n");
    md.AppendLine(Table(
      ["Optimalizátor", "Learning rate", "Presnosť po 1. epoche [%]", $"Presnosť po {lastEpoch}. epoche [%]"],
      configs.Select(c => new[]
      {
        c.Optimizer == "Sgd" ? "SGD" : "Adam", Num(c.Lr, 4), PlusMinus(Values(c.Optimizer, c.Lr, 1), 2), PlusMinus(Values(c.Optimizer, c.Lr, lastEpoch), 2)
      })));

    var plot = new SvgPlot("Presnosť MLP podľa optimalizátora", "Epocha", "Presnosť na teste [%]") { XFormat = "0", YFormat = "0.#" };
    for (var i = 0; i < configs.Count; i++)
    {
      var (optimizer, lr) = configs[i];
      plot.Series.Add(new PlotSeries(
        $"{(optimizer == "Sgd" ? "SGD" : "Adam")} {Num(lr, 4)}", colors[i % colors.Length],
        [.. Enumerable.Range(1, lastEpoch).Select(e => ((double)e, Values(optimizer, lr, e).Average()))], Markers: true));
    }
    File.WriteAllText(Path.Combine(images, "optimizers.svg"), plot.ToSvg());
  }

  // funkcie prislusnosti vsetkych premennych fuzzy systemu (obrazok 2 x 2)
  private static void MembershipFigure(string images)
  {
    var colors = new[] { Orange, Blue, Green };
    const int cellWidth = 520;
    const int cellHeight = 300;
    var content = new StringBuilder();
    var variables = new ReliabilityEstimator().Variables;
    for (var i = 0; i < variables.Count; i++)
    {
      var variable = variables[i];
      var plot = new SvgPlot(variable.Name, "Hodnota", "Stupeň príslušnosti") { XMin = variable.Min, XMax = variable.Max, YMin = 0, YMax = 1.05, YFormat = "0.0" };
      for (var s = 0; s < variable.Sets.Count; s++)
      {
        var set = variable.Sets[s];
        plot.Series.Add(new PlotSeries(
          set.Name, colors[s % colors.Length],
          [.. Enumerable.Range(0, 201).Select(k =>
          {
            var x = variable.Min + (variable.Max - variable.Min) * k / 200;
            return (x, set.Membership.Evaluate(x));
          })]));
      }
      content.Append(plot.ToGroup(i % 2 * cellWidth, i / 2 * cellHeight, cellWidth, cellHeight));
    }
    File.WriteAllText(Path.Combine(images, "fuzzy_membership.svg"), SvgPlot.Wrap(cellWidth * 2, cellHeight * 2, content.ToString()));
  }

  // tabulka v Markdowne, prvy stlpec vlavo, ostatne vpravo
  private static string Table(IReadOnlyList<string> headers, IEnumerable<string[]> rows)
  {
    var text = new StringBuilder();
    text.AppendLine("| " + string.Join(" | ", headers) + " |");
    text.AppendLine("|" + string.Join("|", headers.Select((_, i) => i == 0 ? "---" : "---:")) + "|");
    foreach (var row in rows)
    {
      text.AppendLine("| " + string.Join(" | ", row) + " |");
    }
    return text.ToString();
  }

  private static string Num(double value, int decimals) => value.ToString("F" + decimals, Culture);

  private static string Signed(double value, int decimals) => (value >= 0 ? "+" : "−") + Num(Math.Abs(value), decimals);

  private static string PlusMinus(IReadOnlyList<double> values, int decimals)
    => $"{Num(Statistics.Mean(values), decimals)} ± {Num(Statistics.StdDev(values), decimals)}";

  private static string PValue(double p) => p < 0.0001 ? "< 0,0001" : "= " + Num(p, 4);
}
