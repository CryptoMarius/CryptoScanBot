using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;
using CryptoScanner.Analyzers.Chart;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

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
            //if (zigzag.Item1.PointType == 'L')
            //    color = OxyColors.Green;
            //else
            //    color = OxyColors.Orange;
            // The colours of the DTB gear since 07-11-2026
            string styleKey = zigzag.Item1.PointType == 'L' ? "dtbBottom" : "dtbTop";
            color = ChartStyleOxy.ColorFor(styleKey, zigzag.Item1.PointType == 'L' ? OxyColors.Green : OxyColors.Orange);
            double thickness = ChartStyleOxy.ThicknessFor(styleKey, 1);
            LineStyle lineStyle = ChartStyleOxy.LineStyleFor(styleKey, LineStyle.Solid);
            var rectangle = new PolygonAnnotation
            {
                StrokeThickness = 1, // Border thickness
                Fill = ChartStyleOxy.WithAlpha(color, 75),
                Stroke = ChartStyleOxy.WithAlpha(color, 75),
                Tag = group
            };

            rectangle.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime.Minutes, (double)zigzag.Item1.Value));
            rectangle.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime.Minutes, (double)zigzag.Item2.Value));
            rectangle.Points.Add(new DataPoint(zigzag.Item3.Candle.OpenTime.Minutes, (double)zigzag.Item3.Value));
            chart.Annotations.Add(rectangle);

            //var series = new LineSeries { Title = "1", Color = boxColor, Tag = group };
            //series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime, (double)zigzag.Item1.Value));
            //series.Points.Add(new DataPoint(zigzag.Item3.Candle.OpenTime, (double)zigzag.Item1.Value));
            //plotModel.Series.Add(series);

            var series = new LineSeries { Title = "1", Color = color, StrokeThickness = thickness, LineStyle = lineStyle, YAxisKey = "price", Tag = group };
            series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime.Minutes, (double)zigzag.Item1.Value));
            series.Points.Add(new DataPoint(zigzag.Item1.Candle.OpenTime.Minutes + interval.Duration * 5, (double)zigzag.Item1.Value));
            chart.Series.Add(series);

            series = new LineSeries { Title = "2", Color = color, StrokeThickness = thickness, LineStyle = lineStyle, YAxisKey = "price", Tag = group };
            series.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime.Minutes, (double)zigzag.Item2.Value));
            series.Points.Add(new DataPoint(zigzag.Item2.Candle.OpenTime.Minutes + interval.Duration * 5, (double)zigzag.Item2.Value));
            chart.Series.Add(series);

            // "DB" or "DT" at the top left of the pattern, the caption the Photino chart writes
            double top = Math.Max((double)zigzag.Item1.Value, Math.Max((double)zigzag.Item2.Value, (double)zigzag.Item3.Value));
            chart.Annotations.Add(new TextAnnotation
            {
                Text = zigzag.Item1.PointType == 'L' ? "DB" : "DT",
                TextPosition = new DataPoint(zigzag.Item1.Candle.OpenTime.Minutes, top),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Bottom,
                TextColor = color,
                Background = OxyColors.Transparent,
                StrokeThickness = 0,
                FontSize = 9,
                YAxisKey = "price",
                Tag = group,
            });
        }
    }


}
