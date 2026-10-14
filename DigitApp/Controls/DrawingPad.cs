using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitApp.Controls;

/// <summary>
/// Platno na kreslenie cislic mysou (biele ciary na cierno). Ciary pridava rucne, nie cez editovanie InkCanvas,
/// lebo ten by ciaru zaregistroval az po jej dokonceni a predikcia ma bezat uz pocas kreslenia.
/// </summary>
public sealed class DrawingPad : InkCanvas
{
  /// <summary>
  /// Strana stvorcoveho platna v pixeloch.
  /// </summary>
  public const int Size = 280;

  /// <summary>
  /// Minimalny odstup medzi oznameniami o zmene pocas kreslenia, predikcia cca 25x za sekundu, nie na kazdy pixel pohybu.
  /// </summary>
  private const int ThrottleMilliseconds = 40;

  /// <summary>
  /// Vlastnosti pera, ktorym sa kresli (biela ciara s elipsovym hrotom).
  /// </summary>
  private readonly DrawingAttributes pen = new()
  {
    Color = Colors.White,
    Width = 18,
    Height = 18,
    FitToCurve = false,
    StylusTip = StylusTip.Ellipse
  };

  /// <summary>
  /// Casovac na obmedzenie frekvencie oznameni pocas kreslenia.
  /// </summary>
  private readonly Stopwatch throttle = Stopwatch.StartNew();

  /// <summary>
  /// Prave kreslena ciara, alebo null ak sa nekresli.
  /// </summary>
  private Stroke? current;

  /// <summary>
  /// Vytvori prazdne cierne platno s vypnutym standardnym editovanim.
  /// </summary>
  public DrawingPad()
  {
    Width = Size;
    Height = Size;
    Background = Brushes.Black;
    EditingMode = InkCanvasEditingMode.None;
  }

  /// <summary>
  /// Nastane pocas kreslenia (obmedzene casovo) a po dokonceni kazdej ciary.
  /// </summary>
  public event EventHandler? StrokesChanged;

  /// <summary>
  /// Zmaze vsetky nakreslene ciary.
  /// </summary>
  public void Clear()
  {
    Strokes.Clear();
    current = null;
    ReleaseMouseCapture();
  }

  /// <summary>
  /// Nakreslene ciary ako sivotonovy obrazok Size x Size, hodnoty 0..1.
  /// </summary>
  /// <returns>Intenzity pixelov po riadkoch, dlzka Size x Size.</returns>
  public float[] GetPixels()
  {
    // renderujem priamo zoznam ciar, nie samotny InkCanvas, ten sa pri rozkreslenom tahu nemusi stihnut prekreslit
    var visual = new DrawingVisual();
    using (var context = visual.RenderOpen())
    {
      context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, Size, Size));
      Strokes.Draw(context);
    }
    var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);

    var pixels = new byte[Size * Size * 4];
    bitmap.CopyPixels(pixels, Size * 4, 0);
    var gray = new float[Size * Size];
    for (var i = 0; i < gray.Length; i++)
    {
      gray[i] = pixels[i * 4 + 1] / 255f; // zeleny kanal staci, kresba je biela
    }
    return gray;
  }

  /// <inheritdoc />
  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
  {
    base.OnMouseLeftButtonDown(e);
    CaptureMouse(); // aby nepadli udalosti, ked sa s myskou vyjde von z platna
    current = new Stroke([ToPoint(e)], pen.Clone());
    Strokes.Add(current);
    StrokesChanged?.Invoke(this, EventArgs.Empty);
  }

  /// <inheritdoc />
  protected override void OnMouseMove(MouseEventArgs e)
  {
    base.OnMouseMove(e);
    if (current is null || e.LeftButton != MouseButtonState.Pressed)
    {
      return;
    }

    current.StylusPoints.Add(ToPoint(e));
    if (throttle.ElapsedMilliseconds < ThrottleMilliseconds)
    {
      return;
    }
    throttle.Restart();
    StrokesChanged?.Invoke(this, EventArgs.Empty);
  }

  /// <inheritdoc />
  protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
  {
    base.OnMouseLeftButtonUp(e);
    if (current is null)
    {
      return;
    }

    current.StylusPoints.Add(ToPoint(e));
    current = null;
    ReleaseMouseCapture();
    StrokesChanged?.Invoke(this, EventArgs.Empty); // este raz s uplne dokoncenou ciarou
  }

  /// <summary>
  /// Prevedie poziciu mysky na bod ciary ohraniceny na platno.
  /// </summary>
  /// <param name="e">Udalost mysky.</param>
  /// <returns>Bod ciary, ktory lezi v ramci platna.</returns>
  private StylusPoint ToPoint(MouseEventArgs e)
  {
    var p = e.GetPosition(this);
    return new StylusPoint(Math.Clamp(p.X, 0, Size - 1), Math.Clamp(p.Y, 0, Size - 1));
  }
}
