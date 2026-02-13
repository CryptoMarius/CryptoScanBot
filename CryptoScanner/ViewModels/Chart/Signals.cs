using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Signals
{
    internal static void Draw(PlotModel chart, List<CryptoSignal> signalList, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesShort = new ScatterSeries
        {
            Title = "s high",
            MarkerSize = 2,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Diamond,
            Tag = group
        };

        var seriesLong = new ScatterSeries
        {
            Title = "s low",
            MarkerSize = 2,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Diamond,
            Tag = group
        };

        foreach (var signal in signalList)
        {
            CandleTime closeDate = CandleTime.FromDateTime(signal.CloseDate);
            if (closeDate >= minDate && closeDate <= maxDate)
            {
                decimal y;
                ScatterSeries? series;
                if (signal.Side == CryptoTradeSide.Long)
                {
                    series = seriesLong;
                    y = 0.99m * signal.SignalPrice;
                }
                else
                {
                    series = seriesShort;
                    y = 1.01m * signal.SignalPrice;
                }

                series?.Points.Add(new ScatterPoint(closeDate.Minutes, (double)y));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }


}
