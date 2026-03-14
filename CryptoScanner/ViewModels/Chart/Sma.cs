using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Sma
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, int length,
        OxyColor color, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesSma = new LineSeries
        {
            Title = $"sma{length}",
            MarkerSize = 1,
            MarkerFill = color,
            Color = color,
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<SmaResult> smaList = (List<SmaResult>)symbolInterval.CandleList.Values.GetSma(length);


        foreach (var bb in smaList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(bb.Date, interval.Duration);
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