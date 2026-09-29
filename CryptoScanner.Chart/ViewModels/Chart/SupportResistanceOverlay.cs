using CryptoScanner.Analyzers.SrFlip;
using CryptoScanner.Analyzers.SrFlip.Signal;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

// The older SupportResistance/SupportResistanceLevel in this namespace are a different, unused
// experiment; these aliases point at the Core building block.
using CoreScan = CryptoScanner.Core.Trend.SupportResistance;
using CoreLevel = CryptoScanner.Core.Trend.SupportResistanceLevel;

namespace CryptoScanner.Chart.ViewModels.Chart;

/// <summary>
/// Support and resistance in the chart (open point 46), from the same Core scan the Photino chart
/// and the trader filter use: the horizontal levels as they stand now (red above the price, green
/// under it, with the number of pivots), the sloped lines from their first pivot to the last candle,
/// and a triangle at every flip - a broken level that was retested from the other side and held.
/// Each level is also drawn as a block, from its lowest to its highest pivot (at least one ATR high),
/// starting at its oldest pivot and running to the right edge; overlapping blocks merge.
/// </summary>
public static class SupportResistanceOverlay
{
    private static readonly OxyColor Above = OxyColor.FromRgb(0xef, 0x53, 0x50);
    private static readonly OxyColor Below = OxyColor.FromRgb(0x26, 0xa6, 0x9a);
    private static readonly OxyColor ResistanceLine = OxyColor.FromRgb(0xff, 0x70, 0x43);
    private static readonly OxyColor SupportLine = OxyColor.FromRgb(0x66, 0xbb, 0x6a);

    /// <summary>
    /// From how many touches a zone is worth drawing. Three, the same bar the scan itself uses
    /// before it acts on a level - drawing what we would not trade on only fills the chart.
    /// </summary>
    private const int DrawFrom = 3;

    internal static void Draw(PlotModel chart, List<CryptoCandle> candles, string tag)
    {
        if (candles.Count < 30)
            return;

        SupportResistanceResult result = CoreScan.Scan(candles);
        double last = (double)candles[^1].Close;

        // The MERGED zones, not the loose levels. Both used to be drawn: a block per zone and on
        // top of that a dashed line WITH its own caption per level, so there were more captions on
        // the chart than blocks under them. On a weekly bitcoin chart that came to thirteen of
        // them stacked down the right-hand edge.
        List<SupportResistanceZone> merged = CoreScan.Zones(result.Levels);

        // Two touches is the weakest thing the clustering can produce, and the scan itself does
        // not act on a level below MinEventTouches either. The nearest level above and below
        // survive whatever their strength: those are the two the eye is looking for.
        SupportResistanceZone? nearestAbove = null;
        SupportResistanceZone? nearestBelow = null;
        foreach (SupportResistanceZone zone in merged)
        {
            if (zone.Price >= last)
            {
                if (nearestAbove == null || zone.Price < nearestAbove.Value.Price)
                    nearestAbove = zone;
            }
            else if (nearestBelow == null || zone.Price > nearestBelow.Value.Price)
            {
                nearestBelow = zone;
            }
        }

        foreach (SupportResistanceZone zone in merged)
        {
            bool nearest = (nearestAbove != null && zone.Price == nearestAbove.Value.Price)
                || (nearestBelow != null && zone.Price == nearestBelow.Value.Price);
            if (zone.Touches < DrawFrom && !nearest)
                continue;

            OxyColor color = zone.Price >= last ? Above : Below;

            // Strength as WEIGHT instead of as a number: a band that has held six times is darker
            // and has a firmer edge than one that held three.
            byte fill = zone.Touches >= 6 ? (byte)56 : zone.Touches >= 4 ? (byte)36 : (byte)20;

            chart.Annotations.Add(new RectangleAnnotation
            {
                Layer = AnnotationLayer.BelowSeries,
                MinimumX = candles[zone.FirstIndex].OpenTime.Minutes,
                // MaximumX left at its default: the block runs to the right edge
                MinimumY = zone.Low,
                MaximumY = zone.High,
                Fill = OxyColor.FromAColor(fill, color),
                Stroke = OxyColor.FromAColor(zone.Touches >= 6 ? (byte)160 : (byte)110, color),
                StrokeThickness = zone.Touches >= 6 ? 2 : 1,
                Text = $"{zone.Touches}x",
                TextColor = color,
                YAxisKey = "price",
                Tag = tag,
            });

            // Only the two nearest levels keep a line. For the rest the block says it all, and a
            // line on the weighted middle of a band an ATR high claims a precision the method does
            // not have.
            if (!nearest)
                continue;

            chart.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = zone.Price,
                Color = color,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
                Text = string.Empty,
                YAxisKey = "price",
                Tag = tag,
            });
        }

        int lastIndex = candles.Count - 1;
        foreach (SupportResistanceLine line in result.Lines)
        {
            // Only a line with its third touch counts, and a broken one ends at its breakout
            if (!line.IsConfirmed)
                continue;
            int endIndex = line.BrokenAt ?? lastIndex;
            var series = new LineSeries
            {
                Title = tag + (line.IsResistance ? " resistance" : " support"),
                Color = line.IsResistance ? ResistanceLine : SupportLine,
                StrokeThickness = 1,
                YAxisKey = "price",
                Tag = tag,
            };
            series.Points.Add(new DataPoint(candles[line.Index1].OpenTime.Minutes, line.Price1));
            series.Points.Add(new DataPoint(candles[endIndex].OpenTime.Minutes, line.ValueAt(endIndex)));
            chart.Series.Add(series);
        }

        var flipLong = new ScatterSeries { Title = tag + " flip long", MarkerSize = 5, MarkerFill = Below, MarkerType = MarkerType.Triangle, YAxisKey = "price", Tag = tag };
        var flipShort = new ScatterSeries { Title = tag + " flip short", MarkerSize = 5, MarkerFill = Above, MarkerType = MarkerType.Triangle, YAxisKey = "price", Tag = tag };
        foreach (SupportResistanceEvent e in result.Events)
        {
            if (e.Type != SupportResistanceEventType.Flip)
                continue;
            // On the candle the srflip strategy enters on (the confirmation candle), not on the retest;
            // a flip that never got an entry gets no marker
            int? entry = SrFlipBase.EntryIndex(candles, e, SrFlipPlugin.Settings);
            if (entry == null)
                continue;
            CryptoCandle candle = candles[entry.Value];
            if (e.Side == CryptoTradeSide.Long)
                flipLong.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.Low * 0.998));
            else
                flipShort.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.High * 1.002));
        }
        chart.Series.Add(flipLong);
        chart.Series.Add(flipShort);
    }
}
