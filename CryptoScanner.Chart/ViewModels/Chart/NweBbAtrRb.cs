using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Nwe;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class NweBbAtrRb
{
    /// <summary>
    /// Draws markers for the combined NWE×BB + AtrRb signal (both on the same side within 5 candles,
    /// drawn on the second). Recomputed on the fly from the visible candles via
    /// <see cref="NweBbAtrRbDetector"/> — the same algorithm the live strategy runs — so it also shows
    /// in the emulator where no signals were stored. The candle list is the bounded window list
    /// (incl. indicator warmup); markers outside [minDate, maxDate] are skipped.
    /// </summary>
    internal static void Draw(PlotModel chart, IReadOnlyList<CryptoCandle> candles,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesLong = new ScatterSeries
        {
            Title = "nwebb×atrrb ↑",
            MarkerSize = 9,
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColor.FromArgb(120, 0, 230, 118),
            MarkerStroke = OxyColor.FromArgb(255, 0, 230, 118),
            MarkerStrokeThickness = 1.5,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        var seriesShort = new ScatterSeries
        {
            Title = "nwebb×atrrb ↓",
            MarkerSize = 9,
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColor.FromArgb(120, 255, 64, 64),
            MarkerStroke = OxyColor.FromArgb(255, 255, 64, 64),
            MarkerStrokeThickness = 1.5,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        foreach (var marker in NweBbAtrRbDetector.Detect(candles))
        {
            if (marker.OpenTime < minDate || marker.OpenTime > maxDate)
                continue;

            if (marker.Side == CryptoTradeSide.Long)
            {
                seriesLong.Points.Add(new ScatterPoint(
                    marker.OpenTime.Minutes,
                    (double)(0.994m * marker.Price),
                    double.NaN,
                    double.NaN,
                    tag: "nwebb×atrrb ↑"));
            }
            else
            {
                seriesShort.Points.Add(new ScatterPoint(
                    marker.OpenTime.Minutes,
                    (double)(1.006m * marker.Price),
                    double.NaN,
                    double.NaN,
                    tag: "nwebb×atrrb ↓"));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }
}
