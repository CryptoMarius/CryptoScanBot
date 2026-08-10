using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// A single line of an overlay, expressed as plain data so a host that does not
/// use OxyPlot (the web chart) can render it too.
/// </summary>
public sealed class ChartOverlaySeries
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "#888888";
    public int LineWidth { get; set; } = 1;

    /// <summary>0 = solid, 1 = dotted, 2 = dashed (matches lightweight-charts).</summary>
    public int LineStyle { get; set; }

    public List<ChartOverlayPoint> Points { get; set; } = [];
}

public sealed class ChartOverlayPoint
{
    /// <summary>Unix timestamp in seconds.</summary>
    public long Time { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// A short text an overlay wants on a specific candle, such as the stop-loss and take-profit
/// distance at a band break. Rendered as a marker so it does not need its own drawing surface.
/// </summary>
public sealed class ChartOverlayLabel
{
    /// <summary>Unix timestamp in seconds of the candle it belongs to.</summary>
    public long Time { get; set; }

    /// <summary>Above the candle (a high/short side break) or below it.</summary>
    public bool Above { get; set; }

    public string Text { get; set; } = "";
    public string Color { get; set; } = "#ffffff";
}

/// <summary>
/// Contract for a strategy plugin that wants to draw on the chart.
/// The host iterates all loaded overlays in the draw loop and calls
/// <see cref="Draw"/> when the user has toggled this overlay on.
/// PlotModel is passed as object to avoid an OxyPlot dependency in Core.
/// </summary>
public interface IChartOverlay
{
    string Label { get; }
    string GroupKey { get; }

    // Raised when the overlay has new data and wants the chart to redraw.
    event Action? RequestRedraw;

    void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
              List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group);

    /// <summary>
    /// Renderer-agnostic variant of <see cref="Draw"/>, used by the web chart.
    /// Returns the overlay's lines as plain points; overlays that have no
    /// meaningful line representation can leave the default empty result.
    /// </summary>
    IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval,
              List<CryptoCandle> candles) => [];

    /// <summary>
    /// Texts the overlay wants on individual candles (stop-loss / take-profit distances at a band
    /// break, and the like). Empty for overlays that only draw lines.
    /// </summary>
    IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval,
              List<CryptoCandle> candles) => [];
}
