using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.ViewModels.Chart;

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
    // Bump this up if you want more context, lower it for an even cleaner chart.
    private const int NearbyZonesPerSide = 3;

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
            Text = zone.Description,
            ToolTip = zone.Interval.Name,
            Tag = group,
        };
        chart.Annotations.Add(rectangle);
    }

    public static void Draw(PlotModel chart, CryptoSymbol symbol, CandleTime minDate, CandleTime maxDate, string group)
    {
        var symbolData = symbol.Data;
        decimal? currentPrice = GetCurrentPrice(symbol);

        // For the first iteration we reuse the DLZ interval list — same set of timeframes the
        // user already considers "important". We can split this into its own SettingsSmc list
        // once we have an SMC-specific UI section.
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
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
        foreach (var si in symbol.Data.SymbolIntervalList)
        {
            var last = si.LastCandle;
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
