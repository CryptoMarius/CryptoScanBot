using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class ZigZag
{
    internal static void Draw(PlotModel chart, List<ZigZagResult> zigZagList, string caption,
        OxyColor color, CandleTime minDate, CandleTime maxDate, string tag)
    {
        var seriesZigZag = new LineSeries { Title = caption, Color = color, Tag = tag };
        var seriesHigh = new ScatterSeries { Title = "Markers high", MarkerSize = 3, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Circle, Tag = tag };
        var seriesLow = new ScatterSeries { Title = "Markers low", MarkerSize = 3, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle, Tag = tag };
        var seriesDummyHigh = new ScatterSeries { Title = "Markers dummy", MarkerSize = 4, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Square, Tag = tag };
        var seriesDummyLow = new ScatterSeries { Title = "Markers dummy", MarkerSize = 4, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, Tag = tag };
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
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime.Minutes, (double)zigzag.Value));
                seriesZigZag.Points.Add(new DataPoint(zigzag.Candle.OpenTime.Minutes, (double)zigzag.Value));
            }
        }

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
