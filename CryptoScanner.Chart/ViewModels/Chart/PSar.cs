using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class PSar
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string tag)
    {
        var series = new ScatterSeries
        {
            Title = "psar",
            MarkerSize = 2,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = tag
        };


        if (candles.Count == 0)
            return;

        IReadOnlyList<ParabolicSarResult> psarList = candles.AsQuotes().ToParabolicSar().ToList();

        foreach (var psar in psarList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(psar.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? psarValue = psar.Sar;

                if (psarValue.HasValue)
                {
                    series?.Points.Add(new ScatterPoint(openTime.Minutes, psarValue.Value));
                }
            }
        }

        chart.Series.Add(series);
    }
}