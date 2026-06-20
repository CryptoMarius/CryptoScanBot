using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Draws the Stochastic (14,3,3) lines into the shared indicator sub-panel.
/// The sub-panel Y-axis (key "stoch", range 0-100) is managed by AdjustPanels in ChartWindowViewModel.
/// Each indicator is drawn separately so it can be toggled on/off independently.
/// Threshold lines (80 / 50 / 20) are drawn via DrawLines and share their own group tag.
/// </summary>
public class Stoch
{
    // -------------------------------------------------------------------------
    // Stochastic (14, 3, 3)
    // -------------------------------------------------------------------------

    internal static void Draw(
        PlotModel chart,
        CryptoSymbol symbol,
        CryptoInterval interval,
        List<CryptoCandle> candles,
        CandleTime minDate,
        CandleTime maxDate,
        string tag, string AxisKey = "stoch")
    {
        if (candles.Count == 0)
            return;

        var stochList = candles.AsQuotes().GetStoch(14, 3, 3);

        // %K — fast stochastic (blue)
        var seriesK = new LineSeries
        {
            Title = "Stoch %K",
            Color = OxyColors.CornflowerBlue,
            StrokeThickness = 1,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // %D — signal / smooth line (orange, dashed)
        var seriesD = new LineSeries
        {
            Title = "Stoch %D",
            Color = OxyColors.Orange,
            StrokeThickness = 1,
            LineStyle = LineStyle.Solid,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        foreach (var item in stochList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(item.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (item.K.HasValue)
                    seriesK.Points.Add(new DataPoint(openTime.Minutes, item.K.Value));
                if (item.D.HasValue)
                    seriesD.Points.Add(new DataPoint(openTime.Minutes, item.D.Value));
            }
        }

        chart.Series.Add(seriesK);
        chart.Series.Add(seriesD);
    }

    // -------------------------------------------------------------------------
    // Threshold lines: overbought (80), mid (50), oversold (20)
    // Drawn once under a shared group tag so they are removed together.
    // -------------------------------------------------------------------------

    internal static void DrawLines(PlotModel chart, string tag, string AxisKey = "stoch")
    {
        // Overbought at 80
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = GlobalData.Settings.General.SettingsStoch.Overbought,
            Color = OxyColor.FromAColor(140, OxyColors.Tomato),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            YAxisKey = AxisKey,
            Tag = tag,
        });

        // Mid-line at 50
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 50,
            Color = OxyColor.FromAColor(60, OxyColors.White),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            YAxisKey = AxisKey,
            Tag = tag,
        });

        // Oversold at 20
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = GlobalData.Settings.General.SettingsStoch.Oversold,
            Color = OxyColor.FromAColor(140, OxyColors.LimeGreen),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            YAxisKey = AxisKey,
            Tag = tag,
        });
    }
}
