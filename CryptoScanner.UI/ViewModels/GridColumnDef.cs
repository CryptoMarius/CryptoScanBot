namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// Defines a single column in a Blazor data-table grid.
/// Immutable blueprint — runtime state (visibility, width, order) lives in GridColumnState.
/// </summary>
public class GridColumnDef<TEnum> where TEnum : struct, Enum
{
    public TEnum Column { get; init; }
    public string Header { get; init; } = "";
    public string CssClass { get; init; } = "";
    public double DefaultWidth { get; init; }
    public bool DefaultVisible { get; init; } = true;
    public int DefaultDisplayIndex { get; init; }
}
