using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Renders the SMC Order Blocks (from <see cref="CryptoSymbolInterval.SmcZones"/>) as
/// rectangle annotations on the chart, using the same visual idiom as DLZ / FVG zones.
/// Re-uses Const.ColorList for the colour scheme so SMC blocks pop visually distinct from
/// DLZ / FVG. First iteration: no premium/discount overlay, no liquidity-level lines.
/// </summary>
public class SmcZones
{
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
            ToolTip = zone.Description,
            Tag = group,
        };
        chart.Annotations.Add(rectangle);
    }

    public static void Draw(PlotModel chart, CryptoSymbol symbol, CandleTime minDate, CandleTime maxDate, string group)
    {
        var symbolData = symbol.Data;

        // For the first iteration we reuse the DLZ interval list — same set of timeframes the
        // user already considers "important". We can split this into its own SettingsSmc list
        // once we have an SMC-specific UI section.
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                continue;

            var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);
            foreach (var zone in symbolDataInterval.SmcZones)
                DrawZone(chart, zone, minDate, maxDate, group);
        }
    }
}
