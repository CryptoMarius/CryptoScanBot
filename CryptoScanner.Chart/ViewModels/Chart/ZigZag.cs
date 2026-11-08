using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using CryptoScanner.Analyzers.Chart;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class ZigZag
{
    internal static void Draw(PlotModel chart, List<ZigZagResult> zigZagList, string caption,
        OxyColor color, CandleTime minDate, CandleTime maxDate, string tag, string? styleKey = null)
    {
        //var seriesZigZag = new LineSeries { Title = tag + caption, Color = color, YAxisKey = "price", Tag = tag };
        // With a style key ("zigzag" / "fibZigzag") the line takes colour, width and dash from
        // Settings / Chart styles, the same as the Photino chart
        var seriesZigZag = styleKey == null
            ? new LineSeries { Title = tag + caption, Color = color, YAxisKey = "price", Tag = tag }
            : new LineSeries
            {
                Title = tag + caption,
                Color = ChartStyleOxy.ColorFor(styleKey, color),
                StrokeThickness = ChartStyleOxy.ThicknessFor(styleKey, 2),
                LineStyle = ChartStyleOxy.LineStyleFor(styleKey, LineStyle.Solid),
                YAxisKey = "price",
                Tag = tag,
            };
        // The points take the colours of the ZigZag gear since 07-11-2026 (zigzagHigh / zigzagLow)
        OxyColor high = ChartStyleOxy.ColorFor("zigzagHigh", OxyColors.Red);
        OxyColor low = ChartStyleOxy.ColorFor("zigzagLow", OxyColors.Yellow);
        var seriesHigh = new ScatterSeries { Title = tag + "Markers high", MarkerSize = 3, MarkerFill = high, MarkerType = MarkerType.Circle, YAxisKey = "price", Tag = tag };
        var seriesLow = new ScatterSeries { Title = tag + "Markers low", MarkerSize = 3, MarkerFill = low, MarkerType = MarkerType.Circle, YAxisKey = "price", Tag = tag };
        var seriesDummyHigh = new ScatterSeries { Title = tag + "Markers dummy", MarkerSize = 4, MarkerFill = high, MarkerType = MarkerType.Square, YAxisKey = "price", Tag = tag };
        var seriesDummyLow = new ScatterSeries { Title = tag + "Markers dummy", MarkerSize = 4, MarkerFill = low, MarkerType = MarkerType.Square, YAxisKey = "price", Tag = tag };

        var linePoints = new List<(long minutes, double value)>();

        foreach (var zigzag in zigZagList)
        {
            if (zigzag.Candle!.OpenTime >= minDate && zigzag.Candle!.OpenTime <= maxDate)
            {
                ScatterSeries? series;
                if (zigzag.Dummy)
                {
                    if (zigzag.PointType == 'L')
                        series = seriesDummyLow;
                    else
                        series = seriesDummyHigh;
                }
                else
                {
                    if (zigzag.PointType == 'L')
                        series = seriesLow;
                    else
                        series = seriesHigh;
                }
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime.Minutes, zigzag.Value));
                linePoints.Add((zigzag.Candle.OpenTime.Minutes, zigzag.Value));
            }
        }

        // Guard: sort by time so the line never jumps backwards.
        for (int i = 1; i < linePoints.Count; i++)
        {
            if (linePoints[i].minutes < linePoints[i - 1].minutes)
            {
                ScannerLog.Logger.Info($"ZigZag.Draw: out-of-order point at index {i} " +
                    $"(prev={linePoints[i - 1].minutes}, cur={linePoints[i].minutes}), sorting");
                linePoints.Sort((a, b) => a.minutes.CompareTo(b.minutes));
                break;
            }
        }

        foreach (var (minutes, value) in linePoints)
            seriesZigZag.Points.Add(new DataPoint(minutes, value));

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesZigZag);
        chart.Series.Add(seriesDummyLow);
        chart.Series.Add(seriesDummyHigh);

        //string format = symbol.PriceFormat[1..];
        //string text = "Time: {yyyy-MM-dd HH:mm}\nPrice: {$:0.00}";
        //text = text.Replace("$", format);
        //seriesLong.TrackerFormatString = text;
        //seriesShort.TrackerFormatString = text;
        //seriesZigZag.TrackerFormatString = text;
    }

}
