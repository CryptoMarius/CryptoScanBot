using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class Sma
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles, int length,
        OxyColor color, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesSma = new LineSeries
        {
            Title = $"sma{length}",
            MarkerSize = 1,
            MarkerFill = color,
            Color = color,
            YAxisKey = "price",
            Tag = group,
        };

        if (candles.Count == 0)
            return;

        IReadOnlyList<SmaResult> smaList = candles.AsQuotes().ToSma(length).ToList();


        foreach (var bb in smaList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(bb.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? value = bb.Sma;
                if (value.HasValue)
                    seriesSma.Points.Add(new DataPoint(openTime.Minutes, value.Value));
            }
        }

        chart.Series.Add(seriesSma);
    }
}