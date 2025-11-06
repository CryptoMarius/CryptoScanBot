using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ZoneVisualisation.Chart;

public class Signals
{
    internal static void Draw(PlotModel chart, List<CryptoSignal> signalList, long minDate, long maxDate)
    {
        var seriesShort = new ScatterSeries { Title = "s high", MarkerSize = 2, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Diamond, };
        var seriesLong = new ScatterSeries { Title = "s low", MarkerSize = 2, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Diamond, };
        foreach (var signal in signalList)
        {
            if (signal.EventTime >= minDate && signal.EventTime <= maxDate)
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

                series?.Points.Add(new ScatterPoint(signal.EventTime, (double)y));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }


}
