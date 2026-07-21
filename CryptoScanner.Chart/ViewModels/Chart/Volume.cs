using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

/// <summary>
/// Draws per-candle volume bars into the dedicated volume sub-panel.
/// The sub-panel Y-axis (key "volume", auto-ranged) is managed by AdjustPanels in
/// ChartWindowViewModel. Volume is split across two RectangleBarSeries (up/down)
/// so each bar can show whether the candle closed up or down without per-item fill
/// gymnastics. Bars share the same group tag with both series so the toggle path
/// removes them in one go.
/// </summary>
public class Volume
{
    internal static void Draw(
        PlotModel chart,
        CryptoSymbol symbol,
        CryptoInterval interval,
        List<CryptoCandle> candles,
        CandleTime minDate,
        CandleTime maxDate,
        string tag, string AxisKey = "volume")
    {
        if (candles.Count == 0)
            return;

        // Defensive: skip drawing when the volume sub-panel axis is not (yet) attached to
        // the model. Adding RectangleBarSeries with a YAxisKey that does not resolve at
        // render time triggers a NullReferenceException in OxyPlot's GetClippingRect.
        if (!chart.Axes.Any(a => a.Key == AxisKey))
            return;

        // Bar takes up 90 % of one candle interval so adjacent bars don't touch.
        // Same approach as DashboardPositionsViewModel.RectangleBarSeries — X-bounds
        // live in data-space (candle minutes) so the bars zoom with the rest of the chart.
        double halfWidth = 0.45 * interval.Duration;

        // Up bars (close >= open): green
        var seriesUp = new RectangleBarSeries
        {
            Title = "Volume up",
            FillColor = OxyColor.FromAColor(180, OxyColors.LimeGreen),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // Down bars (close < open): red
        var seriesDown = new RectangleBarSeries
        {
            Title = "Volume down",
            FillColor = OxyColor.FromAColor(180, OxyColors.Tomato),
            StrokeColor = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        foreach (var c in candles)
        {
            if (c.OpenTime < minDate || c.OpenTime > maxDate)
                continue;

            double centerX = c.OpenTime.Minutes;
            double x0 = centerX - halfWidth;
            double x1 = centerX + halfWidth;
            double volume = (double)c.Volume;

            var item = new RectangleBarItem(x0, 0.0, x1, volume);
            if (c.Close >= c.Open)
                seriesUp.Items.Add(item);
            else
                seriesDown.Items.Add(item);
        }

        // Only attach series that actually contain bars. An empty RectangleBarSeries still
        // takes part in clipping/range calculations and has been seen to misbehave on some
        // OxyPlot 2.x builds; skipping it is harmless either way.
        if (seriesUp.Items.Count > 0)
            chart.Series.Add(seriesUp);
        if (seriesDown.Items.Count > 0)
            chart.Series.Add(seriesDown);
    }
}
