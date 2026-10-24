using CryptoScanner.Analyzers.Chart;
using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

/// <summary>
/// The figure of the dashboard barometer graph as a line in a sub panel (see
/// <see cref="BarometerChartOverlay"/>). A figure of 0..100 goes into the oscillator panel beside the
/// RSI and the Stochastic, any other figure into a panel of its own under the candles. The panels
/// themselves are made by ChartWindowViewModel.AdjustPanels; this only draws the line into them.
/// </summary>
public static class BarometerOverlay
{
    /// <summary>Key of the axis of the barometer's own panel.</summary>
    public const string AxisKey = "barometer";

    /// <summary>Key of the axis of the oscillator panel (Stoch / RSI).</summary>
    public const string OscillatorAxisKey = "stoch";

    /// <summary>Whether the figure chosen on the dashboard is drawn in the oscillator panel.</summary>
    internal static bool InOscillatorPanel()
    {
        ApplicationStateService? state = GlobalData.GetService<ApplicationStateService>();
        return BarometerChartOverlay.BelongsInOscillatorPanel(state?.BarometerGraphValue);
    }

    internal static void Draw(PlotModel chart, List<CryptoCandle> candles, CryptoInterval interval, string quote,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        ApplicationStateService? state = GlobalData.GetService<ApplicationStateService>();
        List<CandleTime> opens = candles
            .Where(c => c.OpenTime >= minDate && c.OpenTime <= maxDate)
            .Select(c => c.OpenTime)
            .ToList();

        BarometerChartOverlay.Line? line = BarometerChartOverlay.Build(quote, state?.BarometerGraphValue,
            state?.BarometerInterval, opens, (int)interval.Duration);
        if (line == null || line.Points.Count == 0)
            return;

        // The panel is there already (AdjustPanels runs before the drawers); without it there is
        // nowhere to draw, and a series on a missing axis takes OxyPlot down
        string axisKey = BarometerChartOverlay.BelongsInOscillatorPanel(state?.BarometerGraphValue) ? OscillatorAxisKey : AxisKey;
        if (chart.Axes.FirstOrDefault(a => a.Key == axisKey) is not LinearAxis axis)
            return;

        // The own panel follows the figure: its name as the axis title, and a fixed range where the
        // dashboard uses one. The oscillator panel keeps its 0..100 and its "Stoch / RSI" title.
        if (axisKey == AxisKey)
        {
            axis.Title = line.Label;
            axis.Minimum = line.Low ?? double.NaN;
            axis.Maximum = line.High ?? double.NaN;
        }

        var series = new LineSeries
        {
            Title = line.Label,
            Color = ChartStyleOxy.ColorFor(BarometerChartOverlay.OverlayKey, OxyColor.FromRgb(0xc9, 0xa2, 0x27)),
            StrokeThickness = ChartStyleOxy.ThicknessFor(BarometerChartOverlay.OverlayKey, 2),
            LineStyle = ChartStyleOxy.LineStyleFor(BarometerChartOverlay.OverlayKey, LineStyle.Solid),
            YAxisKey = axisKey,
            Tag = group,
        };
        foreach ((CandleTime time, double value) in line.Points)
            series.Points.Add(new DataPoint(time.Minutes, value));
        chart.Series.Add(series);

        if (line.Reference.HasValue)
        {
            const string referenceKey = "barometerReference";
            chart.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = line.Reference.Value,
                Color = ChartStyleOxy.ColorFor(referenceKey, OxyColor.FromAColor(150, OxyColor.FromRgb(0xc9, 0xa2, 0x27))),
                StrokeThickness = ChartStyleOxy.ThicknessFor(referenceKey, 1),
                LineStyle = ChartStyleOxy.LineStyleFor(referenceKey, LineStyle.Dash),
                YAxisKey = axisKey,
                Tag = group,
            });
        }
    }
}
