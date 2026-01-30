using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class PSar
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, long minDate, long maxDate, string group)
    {
        var seriesMiddle = new LineSeries
        {
            Title = "psar",
            MarkerSize = 1,
            MarkerFill = OxyColors.Yellow,
            Color = OxyColors.Yellow,
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<ParabolicSarResult> psarList = (List<ParabolicSarResult>)symbolInterval.CandleList.Values.GetParabolicSar();

        foreach (var psar in psarList)
        {
            long openTime = CandleTools.GetUnixTime(psar.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? psarValue = psar.Sar;

                if (psarValue.HasValue)
                    seriesMiddle.Points.Add(new DataPoint(openTime, psarValue.Value));
            }
        }

        chart.Series.Add(seriesMiddle);
    }
}