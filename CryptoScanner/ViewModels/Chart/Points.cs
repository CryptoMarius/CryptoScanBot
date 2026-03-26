using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

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
            Tag = group
        };
        var seriesLow = new ScatterSeries
        {
            Title = "p low",
            MarkerSize = 2,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Square,
            Tag = group
        };

        foreach (var zigzag in pivotList)
        {
            if (zigzag.Candle!.OpenTime >= minDate && zigzag.Candle!.OpenTime <= maxDate)
            {
                decimal value;
                ScatterSeries? series;
                if (zigzag.PointType == 'L')
                {
                    series = seriesLow;
                    value = zigzag.Value * 0.995m;
                }
                else
                {
                    value = zigzag.Value * 1.005m;
                    series = seriesHigh;
                }
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime.Minutes, (double)value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
    }

}
