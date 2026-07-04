using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.ViewModels.Chart;

// How to Find Institutional Supply & Demand Zones (with ZERO experience)
// https://www.youtube.com/watch?v=0YNWLzBEX2E

/// <summary>
/// Renders the SMC Order Blocks (from <see cref="CryptoSymbolInterval.SmcZones"/>) as
/// rectangle annotations on the chart, using the same visual idiom as DLZ / FVG zones.
/// Re-uses Const.ColorList for the colour scheme so SMC blocks pop visually distinct from
/// DLZ / FVG.
///
/// Declutter: only the zones SURROUNDING the current price are drawn. Per interval we keep
/// the nearest <see cref="NearbyZonesPerSide"/> zones above price, the nearest below, and
/// any zone the price is currently inside. This avoids the "wirwar" of dozens of overlapping
/// historical blocks (especially the big ones that span the whole chart).
/// </summary>
public class SmcZones
{
    // How many zones to show on each side (above / below) of the current price, PER interval.
    // Bump this up if you want more context, lower it for an even cleaner chart. Broken
    // (invalidated) zones are intentionally included so you can look back at past structure.
    private const int NearbyZonesPerSide = 5;

    private static void DrawZone(PlotModel chart, CryptoZone zone, CandleTime minDate, CandleTime maxDate, string group)
    {
        if (zone.OpenTime > maxDate)
            return;

        var colors = Const.ColorList[(zone.Kind, zone.Side, zone.CloseTime.HasValue)];
        OxyColor boxColor = colors.boxColor;
        OxyColor textColor = colors.textColor;

        // Left edge: clamp to the visible window so off-screen blocks still anchor at minDate.
        CandleTime dateOpen = zone.OpenTime;
        if (dateOpen < minDate)
            dateOpen = minDate;

        // Right edge: until CloseTime (invalidated) or a little past maxDate so the block
        // visibly "lives" up to the right edge of the chart, matching DLZ/FVG behaviour.
        CandleTime dateLast = zone.CloseTime ?? (maxDate + 25);

        // Slightly more opaque when still active, dimmer when closed/invalidated — same
        // visual cue scheme as DLZ.
        byte alpha = zone.CloseTime.HasValue ? (byte)64 : (byte)128;
        OxyColor fill = OxyColor.FromArgb(alpha, boxColor.R, boxColor.G, boxColor.B);
        OxyColor stroke = OxyColor.FromArgb(220, boxColor.R, boxColor.G, boxColor.B);

        var rectangle = new RectangleAnnotation
        {
            Layer = AnnotationLayer.BelowSeries,
            MinimumX = dateOpen.Minutes,
            MinimumY = (double)zone.Bottom,
            MaximumX = dateLast.Minutes,
            MaximumY = (double)zone.Top,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 0,
            TextColor = textColor,
            // Pin the annotation to the price Y-axis explicitly. The chart toggles extra
            // Y-axes (stoch / macd / volume) on top of "price"; without YAxisKey OxyPlot may
            // resolve to the wrong axis, and during the very first layout pass (before
            // PlotModel.Update runs) XAxis/YAxis stay null and GetClippingRect throws NRE
            // during render.
            YAxisKey = "price",
            // Interval + freshness: append the touch count (CE touches) so a quick glance tells
            // fresh (no number) from tested ("2x"). Colour already encodes demand vs supply.
            Text = zone.TouchCount > 0 ? $"{zone.Interval.Name} {zone.TouchCount}x" : zone.Interval.Name,
            ToolTip = zone.Interval.Name,
            Tag = group,
        };
        chart.Annotations.Add(rectangle);
    }

    public static void Draw(PlotModel chart, CryptoSymbol symbol, CandleTime minDate, CandleTime maxDate, string group)
    {
        var symbolData = symbol.Data;
        decimal? currentPrice = GetCurrentPrice(symbol);

        // SMC has its own interval list in Settings.Signal.ZonesSmc (appsettings.json).
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                continue;

            var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);

            foreach (var zone in SelectSurroundingZones(symbolDataInterval.SmcZones, currentPrice))
                DrawZone(chart, zone, minDate, maxDate, group);
        }
    }

    /// <summary>
    /// Best-effort "current price" for proximity filtering: the symbol's live LastPrice, or
    /// the most recent candle close across the symbol's intervals as a fallback (e.g. when
    /// LastPrice is not populated in a backtest/emulator run).
    /// </summary>
    private static decimal? GetCurrentPrice(CryptoSymbol symbol)
    {
        if (symbol.LastPrice.HasValue && symbol.LastPrice.Value > 0)
            return symbol.LastPrice.Value;

        decimal? best = null;
        CandleTime newest = CandleTime.MinValue;
        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            var last = symbolInterval.CandleList.LastCandle;
            if (last.OpenTime != 0 && last.OpenTime >= newest)
            {
                newest = last.OpenTime;
                best = last.Close;
            }
        }
        return best;
    }

    /// <summary>
    /// Keep only the zones surrounding the current price: every zone the price sits inside,
    /// plus the nearest <see cref="NearbyZonesPerSide"/> above and below. When no price is
    /// available (shouldn't normally happen) all zones are returned unfiltered.
    /// </summary>
    private static IEnumerable<CryptoZone> SelectSurroundingZones(List<CryptoZone> zones, decimal? currentPrice)
    {
        if (currentPrice == null || zones.Count == 0)
            return zones;

        decimal price = currentPrice.Value;

        List<CryptoZone> inside = [];
        List<CryptoZone> above = []; // entirely above price (Bottom > price) → resistance
        List<CryptoZone> below = []; // entirely below price (Top < price)    → support

        foreach (var zone in zones)
        {
            if (price >= zone.Bottom && price <= zone.Top)
                inside.Add(zone);
            else if (zone.Bottom > price)
                above.Add(zone);
            else
                below.Add(zone);
        }

        // Nearest first: above by lowest Bottom, below by highest Top.
        above.Sort((a, b) => a.Bottom.CompareTo(b.Bottom));
        below.Sort((a, b) => b.Top.CompareTo(a.Top));

        List<CryptoZone> result = [.. inside];
        result.AddRange(above.Take(NearbyZonesPerSide));
        result.AddRange(below.Take(NearbyZonesPerSide));
        return result;
    }
}
