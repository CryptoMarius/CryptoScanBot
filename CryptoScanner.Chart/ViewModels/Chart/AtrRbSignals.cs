using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Draws markers for the STORED AtrRb signals of the run (the real triggers the strategy produced),
/// not a recompute of the bands. Use this to verify the chart against the strategy: every AtrRb
/// position must have a matching marker here on its trigger candle (the position itself opens one
/// candle later, on the entry band — see the delayed-entry rule). Placed at the signal's OpenDate (the
/// trigger candle), so it lines up with the AtrRb band-break label drawn at that same candle.
/// Only signals of the chart's OWN interval are drawn — the strategy can run on several intervals and
/// a (e.g.) 3m signal at 07:51 would land between the 10m candles and look "one candle too early".
/// </summary>
public class AtrRbSignals
{
    internal static void Draw(PlotModel chart, List<CryptoSignal> signalList, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesLong = new ScatterSeries
        {
            Title = "atrrb sig ↑",
            MarkerSize = 6,
            MarkerType = MarkerType.Triangle,
            MarkerFill = OxyColor.FromArgb(220, 0, 200, 90),
            MarkerStroke = OxyColor.FromArgb(255, 0, 120, 50),
            MarkerStrokeThickness = 1,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        var seriesShort = new ScatterSeries
        {
            Title = "atrrb sig ↓",
            MarkerSize = 6,
            MarkerType = MarkerType.Diamond,
            MarkerFill = OxyColor.FromArgb(220, 220, 50, 50),
            MarkerStroke = OxyColor.FromArgb(255, 130, 20, 20),
            MarkerStrokeThickness = 1,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        foreach (var signal in signalList)
        {
            if (signal.Strategy != CryptoSignalStrategy.AtrRb)
                continue;

            // Only this chart's interval — otherwise a 1m/3m/5m signal is drawn on a 10m chart and
            // lands off-grid (looks like it triggers a candle too early).
            if (signal.Interval.IntervalPeriod != interval.IntervalPeriod)
                continue;

            // OpenDate = the trigger (band-break) candle, so the marker coincides with the band-break label.
            CandleTime openDate = CandleTime.FromDateTime(signal.OpenDate);
            if (openDate < minDate || openDate > maxDate)
                continue;

            if (signal.Side == CryptoTradeSide.Long)
                seriesLong.Points.Add(new ScatterPoint(openDate.Minutes, (double)(0.996m * signal.SignalPrice), double.NaN, double.NaN, tag: "atrrb sig ↑"));
            else
                seriesShort.Points.Add(new ScatterPoint(openDate.Minutes, (double)(1.004m * signal.SignalPrice), double.NaN, double.NaN, tag: "atrrb sig ↓"));
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }
}
