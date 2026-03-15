using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class PSar
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string tag)
    {
        var series = new ScatterSeries
        {
            Title = "psar",
            MarkerSize = 2,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            Tag = tag
        };


        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<ParabolicSarResult> psarList = (List<ParabolicSarResult>)symbolInterval.CandleList.Values.GetParabolicSar();

        foreach (var psar in psarList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(psar.Date, interval.Duration);
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