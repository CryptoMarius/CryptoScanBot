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
/// </summary>
public static class SupportResistanceOverlay
{
    private static readonly OxyColor Above = OxyColor.FromRgb(0xef, 0x53, 0x50);
    private static readonly OxyColor Below = OxyColor.FromRgb(0x26, 0xa6, 0x9a);
    private static readonly OxyColor ResistanceLine = OxyColor.FromRgb(0xff, 0x70, 0x43);
    private static readonly OxyColor SupportLine = OxyColor.FromRgb(0x66, 0xbb, 0x6a);

    internal static void Draw(PlotModel chart, List<CryptoCandle> candles, string tag)
    {
        if (candles.Count < 30)
            return;

        SupportResistanceResult result = CoreScan.Scan(candles);
        double last = (double)candles[^1].Close;

        foreach (CoreLevel level in result.Levels)
        {
            chart.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = level.Price,
                Color = level.Price >= last ? Above : Below,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
                Text = $"S/R {level.Touches}x",
                TextColor = level.Price >= last ? Above : Below,
                YAxisKey = "price",
                Tag = tag,
            });
        }

        int lastIndex = candles.Count - 1;
        foreach (SupportResistanceLine line in result.Lines)
        {
            var series = new LineSeries
            {
                Title = tag + (line.IsResistance ? " resistance" : " support"),
                Color = line.IsResistance ? ResistanceLine : SupportLine,
                StrokeThickness = 1,
                YAxisKey = "price",
                Tag = tag,
            };
            series.Points.Add(new DataPoint(candles[line.Index1].OpenTime.Minutes, line.Price1));
            series.Points.Add(new DataPoint(candles[lastIndex].OpenTime.Minutes, line.ValueAt(lastIndex)));
            chart.Series.Add(series);
        }

        var flipLong = new ScatterSeries { Title = tag + " flip long", MarkerSize = 5, MarkerFill = Below, MarkerType = MarkerType.Triangle, YAxisKey = "price", Tag = tag };
        var flipShort = new ScatterSeries { Title = tag + " flip short", MarkerSize = 5, MarkerFill = Above, MarkerType = MarkerType.Triangle, YAxisKey = "price", Tag = tag };
        foreach (SupportResistanceEvent e in result.Events)
        {
            if (e.Type != SupportResistanceEventType.Flip)
                continue;
            CryptoCandle candle = candles[e.Index];
            if (e.Side == CryptoTradeSide.Long)
                flipLong.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.Low * 0.998));
            else
                flipShort.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.High * 1.002));
        }
        chart.Series.Add(flipLong);
        chart.Series.Add(flipShort);
    }
}
