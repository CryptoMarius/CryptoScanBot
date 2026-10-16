using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class Points
{
    internal static void Draw(PlotModel chart, List<ZigZagResult> pivotList, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesHigh = new ScatterSeries
        {
            Title = "p high",
            MarkerSize = 2,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group
        };
        var seriesLow = new ScatterSeries
        {
            Title = "p low",
            MarkerSize = 2,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group
        };
        // The provisional point at the right-hand edge (a dummy of the zigzag): an open square, it
        // can still move with the next candles
        var seriesHighProvisional = new ScatterSeries
        {
            Title = "p high (provisional)",
            MarkerSize = 3,
            MarkerFill = OxyColors.Transparent,
            MarkerStroke = OxyColors.Red,
            MarkerStrokeThickness = 1,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group
        };
        var seriesLowProvisional = new ScatterSeries
        {
            Title = "p low (provisional)",
            MarkerSize = 3,
            MarkerFill = OxyColors.Transparent,
            MarkerStroke = OxyColors.Yellow,
            MarkerStrokeThickness = 1,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group
        };

        foreach (var zigzag in pivotList)
        {
            if (zigzag.Candle!.OpenTime >= minDate && zigzag.Candle!.OpenTime <= maxDate)
            {
                double value;
                ScatterSeries? series;
                if (zigzag.PointType == 'L')
                {
                    series = zigzag.Dummy ? seriesLowProvisional : seriesLow;
                    value = zigzag.Value * 0.995;
                }
                else
                {
                    value = zigzag.Value * 1.005;
                    series = zigzag.Dummy ? seriesHighProvisional : seriesHigh;
                }
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime.Minutes, value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesLowProvisional);
        chart.Series.Add(seriesHighProvisional);
    }

}
