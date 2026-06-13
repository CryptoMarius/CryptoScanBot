using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Draws the MACD(12, 26, 9) indicator into the dedicated MACD sub-panel.
/// The sub-panel Y-axis (key "macd", auto-ranged including negative values) is managed by
/// AdjustPanels in ChartWindowViewModel. Series produced:
///   - MACD line (blue)
///   - Signal line (orange)
///   - Histogram bars split into four RectangleBarSeries matching the official TradingView
///     histo coloring: dark green (above 0, growing), light green (above 0, fading),
///     dark red (below 0, falling further), light red (below 0, recovering).
/// All series share the same group tag so the toggle path removes them in one go.
/// </summary>
public class Macd
{
    internal static void Draw(
        PlotModel chart,
        CryptoSymbol symbol,
        CryptoInterval interval,
        List<CryptoCandle> candles,
        CandleTime minDate,
        CandleTime maxDate,
        string tag, string AxisKey = "macd")
    {
        if (candles.Count == 0)
            return;

        // Defensive: skip drawing when the macd sub-panel axis is not (yet) attached to
        // the model. Adding series with a YAxisKey that does not resolve at render time
        // triggers a NullReferenceException in OxyPlot's GetClippingRect.
        if (!chart.Axes.Any(a => a.Key == AxisKey))
            return;

        var macdList = candles.GetMacd();

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

        // Histogram, official TradingView 4-color scheme. Each bar is binned by its sign and
        // by whether |histogram| is growing versus the previous bar:
        //   above 0, growing  → dark green   (momentum building up)
        //   above 0, fading   → light green  (momentum fading)
        //   below 0, falling  → dark red     (momentum building down)
        //   below 0, recovering → light red  (momentum fading)
        var histUpStrong = new RectangleBarSeries
        {
            Title = "Hist+ strong",
            FillColor = OxyColor.FromAColor(200, OxyColors.LimeGreen),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };
        var histUpWeak = new RectangleBarSeries
        {
            Title = "Hist+ weak",
            FillColor = OxyColor.FromAColor(200, OxyColors.PaleGreen),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };
        var histDownStrong = new RectangleBarSeries
        {
            Title = "Hist- strong",
            FillColor = OxyColor.FromAColor(200, OxyColors.Red),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };
        var histDownWeak = new RectangleBarSeries
        {
            Title = "Hist- weak",
            FillColor = OxyColor.FromAColor(200, OxyColors.LightPink),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        double? prevH = null;
        foreach (var item in macdList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(item.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
            {
                // Still track prevH outside the visible window so the first visible bar
                // gets the correct strong/weak classification relative to its real predecessor.
                if (item.Histogram.HasValue)
                    prevH = item.Histogram.Value;
                continue;
            }

            double x = openTime.Minutes;

            if (item.Macd.HasValue)
                macdLine.Points.Add(new DataPoint(x, item.Macd.Value));
            if (item.Signal.HasValue)
                signalLine.Points.Add(new DataPoint(x, item.Signal.Value));
            if (item.Histogram.HasValue)
            {
                double h = item.Histogram.Value;
                var bar = new RectangleBarItem(x - halfWidth, 0.0, x + halfWidth, h);
                // First bar has no predecessor → treat as "strong" (matches TradingView).
                bool growing = !prevH.HasValue || Math.Abs(h) >= Math.Abs(prevH.Value);
                if (h >= 0)
                {
                    if (growing)
                        histUpStrong.Items.Add(bar);
                    else
                        histUpWeak.Items.Add(bar);
                }
                else
                {
                    if (growing)
                        histDownStrong.Items.Add(bar);
                    else
                        histDownWeak.Items.Add(bar);
                }
                prevH = h;
            }
        }

        // Histograms first so the MACD/Signal lines render on top of the bars.
        if (histUpStrong.Items.Count > 0)
            chart.Series.Add(histUpStrong);
        if (histUpWeak.Items.Count > 0)
            chart.Series.Add(histUpWeak);
        if (histDownStrong.Items.Count > 0)
            chart.Series.Add(histDownStrong);
        if (histDownWeak.Items.Count > 0)
            chart.Series.Add(histDownWeak);
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
