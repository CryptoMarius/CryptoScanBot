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
/// The figure of the dashboard barometer graph as a line over the candles (see
/// <see cref="BarometerChartOverlay"/>). It gets an axis of its own on the left, over the same height
/// as the price axis, so the candles keep their own scale on the right.
/// </summary>
public static class BarometerOverlay
{
    /// <summary>Key of the left axis the line is drawn on.</summary>
    public const string AxisKey = "barometer";

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
        {
            RemoveAxis(chart);
            return;
        }

        UpdateAxis(chart, line);

        var series = new LineSeries
        {
            Title = line.Label,
            Color = ChartStyleOxy.ColorFor(BarometerChartOverlay.OverlayKey, OxyColor.FromRgb(0xc9, 0xa2, 0x27)),
            StrokeThickness = ChartStyleOxy.ThicknessFor(BarometerChartOverlay.OverlayKey, 2),
            LineStyle = ChartStyleOxy.LineStyleFor(BarometerChartOverlay.OverlayKey, LineStyle.Solid),
            YAxisKey = AxisKey,
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
                YAxisKey = AxisKey,
                Tag = group,
            });
        }
    }


    /// <summary>
    /// The left axis, made when it is missing and otherwise brought up to date. It spans the same
    /// height as the price axis; <see cref="AlignWithPrice"/> keeps it there when a sub panel is
    /// switched on or off.
    /// </summary>
    private static void UpdateAxis(PlotModel chart, BarometerChartOverlay.Line line)
    {
        if (chart.Axes.FirstOrDefault(a => a.Key == AxisKey) is not LinearAxis axis)
        {
            axis = new LinearAxis
            {
                Key = AxisKey,
                Position = AxisPosition.Left,
                Font = Const.OxyFontName,
                FontSize = Const.OxyFontSize,
                TextColor = Const.ChartTextColor,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                TicklineColor = OxyColors.Gray,
                TickStyle = TickStyle.Inside,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColors.Gray,
                AxislineThickness = 1,
            };
            chart.Axes.Add(axis);
        }

        axis.Title = line.Label;
        // A figure with a fixed range on the dashboard keeps it here, otherwise the axis follows the data
        axis.Minimum = line.Low ?? double.NaN;
        axis.Maximum = line.High ?? double.NaN;
        AlignWithPrice(chart);
    }


    /// <summary>Give the left axis the same height as the price axis, if both are there.</summary>
    internal static void AlignWithPrice(PlotModel chart)
    {
        Axis? price = chart.Axes.FirstOrDefault(a => a.Key == "price");
        Axis? axis = chart.Axes.FirstOrDefault(a => a.Key == AxisKey);
        if (price == null || axis == null)
            return;
        axis.StartPosition = price.StartPosition;
        axis.EndPosition = price.EndPosition;
    }


    /// <summary>Take the left axis away again; the series and the neutral line go by their group tag.</summary>
    internal static void RemoveAxis(PlotModel chart)
    {
        Axis? axis = chart.Axes.FirstOrDefault(a => a.Key == AxisKey);
        if (axis != null)
            chart.Axes.Remove(axis);
    }
}
