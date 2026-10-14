using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DigitApp;
using DigitApp.Controls;
using DigitApp.ViewModels;

namespace DigitRecognizer.Screenshots;

/// <summary>
/// Vytvori obrazky aplikacie pre pracu bez zobrazenia na obrazovke: otvori hlavne okno, programovo nakresli cislice,
/// spusti vyhodnotenie aj trenovanie a okno vykresli do PNG. Pouzitie: <c>--out docs/img</c>.
/// </summary>
internal static class Program
{
  private const int Width = 1130;
  private const int Height = 860;

  /// <summary>
  /// Vstupny bod programu.
  /// </summary>
  /// <param name="args">Argumenty prikazoveho riadku (<c>--out priecinok</c>).</param>
  /// <returns>Navratovy kod procesu (0 = uspech).</returns>
  [STAThread]
  private static int Main(string[] args)
  {
    var output = args is ["--out", { } path] ? path : Path.Combine(FindRepositoryRoot(), "docs", "img");
    Directory.CreateDirectory(output);

    var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    var exitCode = 0;
    application.Startup += async (_, _) =>
    {
      try
      {
        await new Scenario(output).RunAsync();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
        exitCode = 1;
      }
      finally
      {
        application.Shutdown();
      }
    };
    application.Run();
    return exitCode;
  }

  // koren repozitara je priecinok so subormi .sln
  private static string FindRepositoryRoot()
  {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      if (dir.GetFiles("*.sln").Length > 0)
      {
        return dir.FullName;
      }
    }
    return Directory.GetCurrentDirectory();
  }

  /// <summary>
  /// Postupnost krokov, pri ktorych sa robia obrazky.
  /// </summary>
  private sealed class Scenario(string output)
  {
    private readonly MainWindow window = new() { Width = Width, Height = Height, ShowActivated = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0 };
    private readonly MainViewModel viewModel = new();

    /// <summary>
    /// Vytvori vsetky obrazky. Trenovanie sa robi ako posledne, lebo nahradi model, ktory pouziva zvysok aplikacie.
    /// </summary>
    public async Task RunAsync()
    {
      window.DataContext = viewModel;
      window.Show();
      await SettleAsync();
      var pad = Find<DrawingPad>(window);

      // 1. spolahlivo rozpoznana cislica
      Draw(pad, DigitShapes.Digit(3));
      await SettleAsync();
      Save("app_recognition.png");

      // 2. nejednoznacna kresba, pri ktorej fuzzy hodnotenie hlasi nizsiu spolahlivost
      var best = (Name: "", Score: 101.0, Strokes: (IReadOnlyList<System.Windows.Ink.Stroke>?)null);
      foreach (var (name, strokes) in DigitShapes.Ambiguous)
      {
        Draw(pad, strokes);
        await SettleAsync();
        var score = viewModel.Recognition.Reliability.Score;
        Console.WriteLine($"[kandidat] {name}: spolahlivost {score:F1} ({viewModel.Recognition.Reliability.Label})");
        if (score < best.Score)
        {
          best = (name, score, strokes);
        }
      }
      Draw(pad, best.Strokes!);
      await SettleAsync();
      Console.WriteLine($"[vybrane] {best.Name}");
      Save("app_recognition_uncertain.png");

      // 3. hra
      await PlayGameAsync(pad);
      Save("app_game.png");

      // 4. vyhodnotenie (pred trenovanim, kym sa pouzivaju dodane modely)
      Select(1);
      Select(2);
      viewModel.Evaluation.RunCommand.Execute(null);
      await WaitAsync(() => !viewModel.Evaluation.IsRunning && viewModel.Evaluation.ModelRows.Count > 0);
      await SettleAsync();
      Save("app_evaluation.png");

      // 5. trenovanie
      Select(1);
      var training = viewModel.Training;
      training.SelectedModel = NeuralNet.ModelKind.Cnn;
      training.Epochs = 6;
      Save("app_training_idle.png");
      training.StartCommand.Execute(null);
      await WaitAsync(() => training.IsRunning);
      await WaitAsync(() => !training.IsRunning, TimeSpan.FromMinutes(10));
      await SettleAsync();
      Save("app_training.png");
    }

    // vymaze platno a vlozi ciary, potom rozpozna kresbu, rovnako ako po nakreslenia mysou
    private void Draw(DrawingPad pad, IReadOnlyList<System.Windows.Ink.Stroke> strokes)
    {
      pad.Clear();
      foreach (var stroke in strokes)
      {
        pad.Strokes.Add(stroke.Clone());
      }
      viewModel.Recognition.Recognize(pad.GetPixels());
    }

    // zahra par kol, aby bolo na obrazku vidiet skore a spravu o poslednom kole
    private async Task PlayGameAsync(DrawingPad pad)
    {
      var game = viewModel.Recognition.Game;
      for (var attempt = 0; attempt < 100; attempt++)
      {
        game.StartCommand.Execute(null);
        if (DigitShapes.Digit(int.Parse(game.Target)).Count > 0)
        {
          break;
        }
        game.EndCommand.Execute(null);
      }

      for (var round = 0; round < 3; round++)
      {
        Draw(pad, DigitShapes.Digit(int.Parse(game.Target)));
        await SettleAsync();
        if (round < 2)
        {
          game.SubmitCommand.Execute(null);
          await SettleAsync();
        }
      }
    }

    private void Select(int tabIndex) => Find<TabControl>(window).SelectedIndex = tabIndex;

    // pocka na dokoncenie vazieb a rozlozenia
    private static async Task SettleAsync()
    {
      await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
      await Task.Delay(250);
      await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static async Task WaitAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
      var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(2));
      while (!condition())
      {
        if (DateTime.UtcNow > deadline)
        {
          throw new TimeoutException("Podmienka sa nesplnila v limite.");
        }
        await Task.Delay(200);
      }
    }

    // vykresli aktualny stav okna do PNG
    private void Save(string fileName)
    {
      window.UpdateLayout();
      var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(window);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      using var stream = File.Create(Path.Combine(output, fileName));
      encoder.Save(stream);
      Console.WriteLine($"[obrazok] {fileName}");
    }

    // prvy prvok daneho typu vo vizualnom strome
    private static T Find<T>(DependencyObject root) where T : DependencyObject
    {
      if (root is T match)
      {
        return match;
      }
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var child = VisualTreeHelper.GetChild(root, i);
        if (child is T direct)
        {
          return direct;
        }
        try
        {
          return Find<T>(child);
        }
        catch (InvalidOperationException)
        {
          // v tejto vetve prvok nie je, pokracuje sa dalsou
        }
      }
      throw new InvalidOperationException($"Prvok typu {typeof(T).Name} sa nenasiel.");
    }
  }
}
