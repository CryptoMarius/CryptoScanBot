using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

/// <summary>
/// Overlay that marks candles where <see cref="SlideDetector"/> flags a "glijbaan" (a clean, sustained
/// downtrend). Additive/experimental: nothing else uses the detector, so you can toggle this on and
/// judge it next to the price/positions without affecting any existing logic. A marker sits just below
/// the candle Low; the tracker shows the R² and slope so you can tune the thresholds.
/// </summary>
public class Slide
{
    internal static void Draw(PlotModel chart, IReadOnlyList<CryptoCandle> candles,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var series = new ScatterSeries
        {
            Title = "slide",
            MarkerSize = 4,
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColor.FromArgb(200, 255, 140, 0), // orange
            MarkerStroke = OxyColor.FromArgb(255, 200, 90, 0),
            MarkerStrokeThickness = 3,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        var results = SlideDetector.Detect(candles);
        for (int i = 0; i < candles.Count; i++)
        {
            if (!results[i].Ready || !results[i].IsSliding)
                continue;

            CryptoCandle c = candles[i];
            if (c.OpenTime < minDate || c.OpenTime > maxDate)
                continue;

            string tag = $"slide eff={results[i].Efficiency:0.00} drop={results[i].DropPercent:0.0}%";
            series.Points.Add(new ScatterPoint(c.OpenTime.Minutes, (double)(c.Low * 0.998m), double.NaN, double.NaN, tag: tag));
        }

        chart.Series.Add(series);
    }
}
