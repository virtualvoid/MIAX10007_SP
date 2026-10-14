namespace DigitApp.ViewModels;

/// <summary>
/// Jeden riadok tabulky s vysledkami vyhodnotenia modelu.
/// </summary>
/// <param name="Model">Nazov modelu.</param>
/// <param name="Accuracy">Presnost naformatovana na zobrazenie.</param>
/// <param name="Loss">Hodnota stratovej funkcie naformatovana na zobrazenie.</param>
/// <param name="Errors">Pocet chybne klasifikovanych vzoriek.</param>
/// <param name="Parameters">Pocet parametrov modelu.</param>
public sealed record ModelResultRow(string Model, string Accuracy, string Loss, string Errors, string Parameters);
