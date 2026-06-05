using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class NweBb
{
    /// <summary>
    /// Draws markers for already-detected NWE × BB signals. The strategy runs walk-forward
    /// at signal time and stores its decision in <see cref="CryptoSignal"/>; we just place
    /// scatter points for the existing records. Avoids the expensive repainting recompute
    /// the chart would otherwise need to reproduce strategy state.
    /// </summary>
    internal static void Draw(PlotModel chart, List<CryptoSignal> signalList,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesLong = new ScatterSeries
        {
            Title = "nwe.bb ↑",
            MarkerSize = 7,
            MarkerFill = OxyColor.FromArgb(220, 0, 210, 100),
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        var seriesShort = new ScatterSeries
        {
            Title = "nwe.bb ↓",
            MarkerSize = 7,
            MarkerFill = OxyColor.FromArgb(220, 220, 60, 60),
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        foreach (var signal in signalList)
        {
            if (signal.Strategy != CryptoSignalStrategy.NweBb)
                continue;

            CandleTime closeDate = CandleTime.FromDateTime(signal.CloseDate);
            if (closeDate < minDate || closeDate > maxDate)
                continue;

            string tag = string.IsNullOrEmpty(signal.EventText)
                ? (signal.Side == CryptoTradeSide.Long ? "nwe.bb ↑" : "nwe.bb ↓")
                : signal.EventText!;

            if (signal.Side == CryptoTradeSide.Long)
            {
                seriesLong.Points.Add(new ScatterPoint(
                    closeDate.Minutes,
                    (double)(0.997m * signal.SignalPrice),
                    double.NaN,
                    double.NaN,
                    tag: tag));
            }
            else
            {
                seriesShort.Points.Add(new ScatterPoint(
                    closeDate.Minutes,
                    (double)(1.003m * signal.SignalPrice),
                    double.NaN,
                    double.NaN,
                    tag: tag));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }
}
