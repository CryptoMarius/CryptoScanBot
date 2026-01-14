using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Dtb
{
    internal static void Draw(PlotModel chart, CryptoInterval interval, ZigZagIndicator indicator, string group)
    {
        List<(ZigZagResult, ZigZagResult, ZigZagResult)> l = DoubleTopAndBottom.CalculateDoubleTopBottom(indicator);
        //var seriesHigh = new ScatterSeries { Title = "dtb high", MarkerSize = 15, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Circle, };
        //var seriesLow = new ScatterSeries { Title = "dtb low", MarkerSize = 15, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle, };
        foreach (var zigzag in l)
        {
            //var series = new LineSeries { Title = "1", Color = OxyColors.Red };
            //series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime, (double)zigzag.Item1.Value));
            //series.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime, (double)zigzag.Item2.Value));
            //series.Points.Add(new DataPoint(zigzag.Item3.Candle.OpenTime, (double)zigzag.Item3.Value));
            //plotModel.Series.Add(series);

            OxyColor color;
            if (zigzag.Item1.PointType == 'L')
                color = OxyColors.Green;
            else
                color = OxyColors.Orange;
            var rectangle = new PolygonAnnotation
            {
                StrokeThickness = 1, // Border thickness
                Fill = OxyColor.FromAColor(75, color),
                Stroke = OxyColor.FromAColor(75, color),
                Tag = group
            };
        
            rectangle.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime, (double)zigzag.Item1.Value));
            rectangle.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime, (double)zigzag.Item2.Value));
            rectangle.Points.Add(new DataPoint(zigzag.Item3.Candle.OpenTime, (double)zigzag.Item3.Value));
            chart.Annotations.Add(rectangle);

            //var series = new LineSeries { Title = "1", Color = boxColor, Tag = group };
            //series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime, (double)zigzag.Item1.Value));
            //series.Points.Add(new DataPoint(zigzag.Item3.Candle.OpenTime, (double)zigzag.Item1.Value));
            //plotModel.Series.Add(series);

            var series = new LineSeries { Title = "1", Color = color, Tag = group };
            series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime, (double)zigzag.Item1.Value));
            series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime + interval.Duration * 5, (double)zigzag.Item1.Value));
            chart.Series.Add(series);

            series = new LineSeries { Title = "2", Color = color, Tag = group };
            series.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime, (double)zigzag.Item2.Value));
            series.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime + interval.Duration * 5, (double)zigzag.Item2.Value));
            chart.Series.Add(series);
        }
    }


}
