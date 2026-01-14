using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Sma
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, int length, 
        OxyColor color, long minDate, long maxDate, string group)
    {
        var seriesSma = new LineSeries { Title = $"sma{length}", 
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
            long openTime = CandleTools.GetUnixTime(bb.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? value = bb.Sma;
                if (value.HasValue)
                    seriesSma.Points.Add(new DataPoint(openTime, value.Value));
            }
        }

        chart.Series.Add(seriesSma);
    }
}