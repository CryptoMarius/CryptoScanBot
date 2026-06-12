using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class KeltnerChannel
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesHigh = new LineSeries
        {
            Title = "kc.upper",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            Color = OxyColors.Gray,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesMiddle = new LineSeries
        {
            Title = "kc.middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            Color = OxyColors.Gray,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "kc.lower",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            Color = OxyColors.Gray,
            YAxisKey = "price",
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<KeltnerResult> keltnerList = (List<KeltnerResult>)symbolInterval.CandleList.Values.GetKeltner();


        foreach (var kc in keltnerList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(kc.Date, interval.Duration);
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
