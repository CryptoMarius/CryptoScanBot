namespace CryptoScanner.UI.Services;

/// <summary>
/// Lets a host show the chart in a window of its own instead of on the Chart tab, the way the
/// Avalonia scanner has always done it. The Photino host sets <see cref="OpenWindow"/>; the web
/// host cannot open windows and leaves it empty, so it keeps the Chart tab.
/// <para>
/// The window follows the selected symbol by itself: the chart listens to
/// SymbolService.SelectedSymbolChanged, and SymbolService is shared by every window of the host.
/// </para>
/// </summary>
public class ChartWindowService
{
    /// <summary>Set by the host when it can show the chart in a window of its own.</summary>
    public Action? OpenWindow { get; set; }

    /// <summary>True when the chart opens in a window instead of on the Chart tab.</summary>
    public bool HasWindow => OpenWindow != null;

    /// <summary>Show the chart window, or bring it to the front when it is already open.</summary>
    public void Open() => OpenWindow?.Invoke();
}
