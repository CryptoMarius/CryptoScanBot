using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Draws the MACD(12, 26, 9) indicator into the dedicated MACD sub-panel.
/// The sub-panel Y-axis (key "macd", auto-ranged including negative values) is managed by
/// AdjustPanels in ChartWindowViewModel. Three series are produced:
///   - MACD line (blue)
///   - Signal line (orange)
///   - Histogram bars split into a positive/negative RectangleBarSeries so each bar can
///     show whether the MACD is above or below its signal without per-item fill gymnastics.
/// All series share the same group tag so the toggle path removes them in one go.
/// </summary>
public class Macd
{
    internal static void Draw(
        PlotModel chart,
        CryptoSymbol symbol,
        CryptoInterval interval,
        CandleTime minDate,
        CandleTime maxDate,
        string tag, string AxisKey = "macd")
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        // Defensive: skip drawing when the macd sub-panel axis is not (yet) attached to
        // the model. Adding series with a YAxisKey that does not resolve at render time
        // triggers a NullReferenceException in OxyPlot's GetClippingRect.
        if (!chart.Axes.Any(a => a.Key == AxisKey))
            return;

        var macdList = symbolInterval.CandleList.Values.GetMacd();

        // Histogram bar takes up 90 % of one candle interval so adjacent bars don't touch.
        double halfWidth = 0.45 * interval.Duration;

        // MACD line (12, 26 EMA difference)
        var macdLine = new LineSeries
        {
            Title = "MACD",
            Color = OxyColors.CornflowerBlue,
            StrokeThickness = 1.0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // Signal line (9-period EMA of MACD)
        var signalLine = new LineSeries
        {
            Title = "Signal",
            Color = OxyColors.Orange,
            StrokeThickness = 1.0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // Positive histogram (MACD above signal): green
        var histUp = new RectangleBarSeries
        {
            Title = "Hist+",
            FillColor = OxyColor.FromAColor(180, OxyColors.LimeGreen),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // Negative histogram (MACD below signal): red
        var histDown = new RectangleBarSeries
        {
            Title = "Hist-",
            FillColor = OxyColor.FromAColor(180, OxyColors.Tomato),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        foreach (var item in macdList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(item.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;

            if (item.Macd.HasValue)
                macdLine.Points.Add(new DataPoint(x, item.Macd.Value));
            if (item.Signal.HasValue)
                signalLine.Points.Add(new DataPoint(x, item.Signal.Value));
            if (item.Histogram.HasValue)
            {
                double h = item.Histogram.Value;
                var bar = new RectangleBarItem(x - halfWidth, 0.0, x + halfWidth, h);
                if (h >= 0)
                    histUp.Items.Add(bar);
                else
                    histDown.Items.Add(bar);
            }
        }

        // Histograms first so the MACD/Signal lines render on top of the bars.
        if (histUp.Items.Count > 0)
            chart.Series.Add(histUp);
        if (histDown.Items.Count > 0)
            chart.Series.Add(histDown);
        chart.Series.Add(macdLine);
        chart.Series.Add(signalLine);
    }

    /// <summary>
    /// Zero reference line. Drawn under its own group tag so it can be toggled together
    /// with the MACD series.
    /// </summary>
    internal static void DrawLines(PlotModel chart, string tag, string AxisKey = "macd")
    {
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 0,
            Color = OxyColor.FromAColor(80, OxyColors.White),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            YAxisKey = AxisKey,
            Tag = tag,
        });
    }
}
