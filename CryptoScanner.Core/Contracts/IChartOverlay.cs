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

    /// <summary>Price the caption is anchored to, normally the candle's high or low.</summary>
    public double Price { get; set; }

    public string Text { get; set; } = "";
    public string Color { get; set; } = "#ffffff";

    /// <summary>
    /// Which <see cref="ChartOverlayStyleDefinition"/> this label takes its colour from, empty when
    /// it has none. The host looks up what the user configured for that key and overrides
    /// <see cref="Color"/> with it, so a marker follows the colour screen like a line does.
    /// </summary>
    public string StyleKey { get; set; } = "";
}

/// <summary>
/// One stylable element of an overlay, as the overlay itself declares it: which key the chart draws
/// it under, what to call it on screen, and how it looks when nobody has changed anything.
/// <para>
/// This is what lets the colour screen follow the loaded plugins instead of a hand-kept list. A
/// strategy that is not registered in this build has no overlay, so it contributes no entries and
/// disappears from the screen by itself.
/// </para>
/// </summary>
public sealed class ChartOverlayStyleDefinition
{
    /// <summary>Series key, the same one <see cref="IChartOverlay.GetSeries"/> or a band uses.</summary>
    public string Key { get; set; } = "";

    /// <summary>What the colour screen calls it, e.g. "Fast EMA".</summary>
    public string Label { get; set; } = "";

    /// <summary>Default colour as "#AARRGGBB" - the alpha is part of it, so a fill can be faint.</summary>
    public string Color { get; set; } = "#FF888888";

    /// <summary>Line thickness in pixels (1..4). Ignored for a fill.</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>0 = solid, 1 = dotted, 2 = dashed. Ignored for a fill.</summary>
    public int LineStyle { get; set; }

    /// <summary>True for a filled area rather than a line, so the screen can say so.</summary>
    public bool IsFill { get; set; }
}


/// <summary>
/// A filled band between a high and a low value per candle - a cloud between two lines. Kept apart
/// from <see cref="ChartOverlaySeries"/> because a line and an area are drawn by different means in
/// both renderers, and because a band that changes colour is several bands, one per stretch.
/// </summary>
public sealed class ChartOverlayBand
{
    public string Key { get; set; } = "";

    /// <summary>
    /// The two <see cref="ChartOverlayStyleDefinition"/> keys this band paints with: one for the
    /// stretches where <see cref="ChartOverlayBandPoint.Up"/> is true and one for the rest. A band
    /// covers the WHOLE chart and changes colour along the way, rather than being cut into a series
    /// per stretch - a chart full of little series behaved differently at the edge of the window
    /// and threw wedges across the screen on zoom and pan.
    /// </summary>
    public string StyleKeyUp { get; set; } = "";
    public string StyleKeyDown { get; set; } = "";

    /// <summary>Fallback fill colours as CSS, used when the host has nothing configured.</summary>
    public string FillColorUp { get; set; } = "rgba(128,128,128,0.15)";
    public string FillColorDown { get; set; } = "rgba(128,128,128,0.15)";

    /// <summary>
    /// The fraction of the configured colour's opacity this band is painted with, 1 being the full
    /// value. It lets one colour carry several depths: a cloud of stacked bands reads as one shape
    /// when the outer band is the firm one and the inner bands step back, and that stays true when
    /// the user picks another colour, because only the one colour is configured.
    /// </summary>
    public double AlphaFactor { get; set; } = 1.0;

    public List<ChartOverlayBandPoint> Points { get; set; } = [];
}

public sealed class ChartOverlayBandPoint
{
    /// <summary>Unix timestamp in seconds.</summary>
    public long Time { get; set; }

    /// <summary>Upper edge, or null where this band has a gap.</summary>
    public double? High { get; set; }

    /// <summary>Lower edge, or null where this band has a gap.</summary>
    public double? Low { get; set; }

    /// <summary>Which of the band's two colours this point is filled with.</summary>
    public bool Up { get; set; }
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

    /// <summary>
    /// Filled areas the overlay wants between two of its lines. Empty for overlays that only draw
    /// lines. The Avalonia chart fills them in <see cref="Draw"/>; the web chart draws them from
    /// here, so an overlay with a cloud has to fill both in or the two charts disagree.
    /// </summary>
    IReadOnlyList<ChartOverlayBand> GetBands(CryptoSymbol symbol, CryptoInterval interval,
              List<CryptoCandle> candles) => [];

    /// <summary>
    /// The stylable elements of this overlay with their default look, so the colour screen can be
    /// built from the plugins that are actually loaded. An overlay that returns nothing keeps the
    /// hand-kept entries it already has in the host.
    /// </summary>
    IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions => [];

    /// <summary>
    /// An empty series that starts out with the look this overlay declared for that key, so
    /// <see cref="GetSeries"/> does not repeat the colours that are already in
    /// <see cref="StyleDefinitions"/>. A key without a definition falls back to a plain series, which
    /// the chart then draws in its own default.
    /// </summary>
    ChartOverlaySeries SeriesFor(string key)
    {
        foreach (var definition in StyleDefinitions)
        {
            if (definition.Key != key)
                continue;

            return new ChartOverlaySeries
            {
                Key = definition.Key,
                Label = definition.Label,
                Color = definition.Color,
                LineWidth = definition.LineWidth,
                LineStyle = definition.LineStyle,
            };
        }
        return new ChartOverlaySeries { Key = key, Label = key };
    }
}
