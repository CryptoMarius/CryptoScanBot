using CryptoScanner.Core.Model;

using CryptoScanner.Analyzers.Chart;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class KeltnerChannel
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesHigh = new LineSeries
        {
            Title = "kc.upper",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            //Color = OxyColors.Gray,
            // Colour, width and dash from Settings / Chart styles, the same as the Photino chart
            Color = ChartStyleOxy.ColorFor("keltnerUpper", OxyColors.Gray),
            StrokeThickness = ChartStyleOxy.ThicknessFor("keltnerUpper", 2),
            LineStyle = ChartStyleOxy.LineStyleFor("keltnerUpper", LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };
        var seriesMiddle = new LineSeries
        {
            Title = "kc.middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            //Color = OxyColors.Gray,
            Color = ChartStyleOxy.ColorFor("keltnerMiddle", OxyColors.Gray),
            StrokeThickness = ChartStyleOxy.ThicknessFor("keltnerMiddle", 2),
            LineStyle = ChartStyleOxy.LineStyleFor("keltnerMiddle", LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "kc.lower",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            //Color = OxyColors.Gray,
            Color = ChartStyleOxy.ColorFor("keltnerLower", OxyColors.Gray),
            StrokeThickness = ChartStyleOxy.ThicknessFor("keltnerLower", 2),
            LineStyle = ChartStyleOxy.LineStyleFor("keltnerLower", LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };

        if (candles.Count == 0)
            return;

        IReadOnlyList<KeltnerResult> keltnerList = candles.AsQuotes().ToKeltner().ToList();


        foreach (var kc in keltnerList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(kc.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? upperBand = kc.UpperBand;
                double? middleBand = kc.Centerline;
                double? lowerBand = kc.LowerBand;

                if (lowerBand.HasValue)
                    seriesLow.Points.Add(new DataPoint(openTime.Minutes, lowerBand.Value));
                if (upperBand.HasValue)
                    seriesHigh.Points.Add(new DataPoint(openTime.Minutes, upperBand.Value));
                if (middleBand.HasValue)
                    seriesMiddle.Points.Add(new DataPoint(openTime.Minutes, middleBand.Value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesMiddle);
    }
}
