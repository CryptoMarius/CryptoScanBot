using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// Draws the "RSI Multi Length [LuxAlgo]" overbought/oversold areas into the shared oscillator
/// sub-panel (Y-axis key "stoch", range 0-100), matching the original Pine script:
///   - Overbought = GREEN area, rising from the baseline (0) up to the overbought %.
///   - Oversold   = RED area, hanging from the top (100); Pine plots (100 - oversold %).
/// (In LuxAlgo overbought is green and oversold is red — it is a momentum/strength reading,
///  not a reversal colouring.)
///
/// The indicator is evaluated on the chart interval. The two percentages are the share of the
/// multi-length RSI bucket (lengths 10..20, N = 11) that is above 70 / below 30 — identical to
/// the Pine "overbuy/N*100" and "oversell/N*100".
/// </summary>
public class Lux
{
    // Pine: ob_area = color.new(#0cb51a, 70) (green), os_area = color.new(#ff1100, 70) (red).
    // Pine transparency 70 → alpha ≈ 255*(100-70)/100 ≈ 76; bumped slightly for visibility.
    private static readonly OxyColor OverBoughtFill = OxyColor.FromArgb(110, 12, 181, 26);  // #0cb51a green
    private static readonly OxyColor OverSoldFill = OxyColor.FromArgb(110, 255, 17, 0);      // #ff1100 red

    internal static void Draw(
        PlotModel chart,
        CryptoSymbol symbol,
        CryptoInterval interval,
        CandleTime minDate,
        CandleTime maxDate,
        string tag, string AxisKey = "stoch")
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        uint duration = symbolInterval.Interval.Duration;

        // Last candle open time that is still inside the visible range and present in the list.
        CandleTime endOpenTime = IntervalTools.StartOfIntervalCandle(maxDate, duration);
        int guard = 0;
        while (!symbolInterval.CandleList.ContainsKey(endOpenTime) && guard++ < 1000)
            endOpenTime -= duration;
        if (!symbolInterval.CandleList.ContainsKey(endOpenTime))
            return;

        // First candle open time at/after minDate.
        CandleTime startOpenTime = IntervalTools.StartOfIntervalCandle(minDate, duration);
        if (startOpenTime > endOpenTime)
            return;

        // Number of candles in [startOpenTime .. endOpenTime] (inclusive).
        int count = (int)((endOpenTime - startOpenTime) / duration) + 1;
        if (count < 1)
            return;

        LuxIndicator.CalculateRange(symbol, interval.IntervalPeriod, endOpenTime, count,
            out int[] overSoldHistory, out int[] overBoughtHistory);

        // Overbought — green, filled from the baseline (0) up to the overbought %.
        var overBoughtArea = new AreaSeries
        {
            Title = "Lux overbought",
            Color = OxyColors.Transparent,
            Fill = OverBoughtFill,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // Oversold — red, filled from the top (100) down to (100 - oversold %), like Pine's per_under.
        var overSoldArea = new AreaSeries
        {
            Title = "Lux oversold",
            Color = OxyColors.Transparent,
            Fill = OverSoldFill,
            StrokeThickness = 0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        // CalculateRange writes output[i] for the candle at startOpenTime + i * duration,
        // where output[count - 1] corresponds to endOpenTime.
        for (int i = 0; i < count; i++)
        {
            CandleTime openTime = startOpenTime + (uint)i * duration;
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;

            // Overbought area: baseline 0 -> overbought %. A 0 reading just sits flat on the baseline.
            overBoughtArea.Points.Add(new DataPoint(x, overBoughtHistory[i]));
            overBoughtArea.Points2.Add(new DataPoint(x, 0));

            // Oversold area: top 100 -> (100 - oversold %). A 0 reading just sits flat on the top.
            overSoldArea.Points.Add(new DataPoint(x, 100 - overSoldHistory[i]));
            overSoldArea.Points2.Add(new DataPoint(x, 100));
        }

        chart.Series.Add(overBoughtArea);
        chart.Series.Add(overSoldArea);

        // Reference baselines: 0 (overbought grows from here), 100 (oversold hangs from here) and
        // the Pine mid-line at 50. These also give the "0 line" the fills connect to.
        AddHLine(chart, 0, tag, AxisKey);
        AddHLine(chart, 50, tag, AxisKey);
        AddHLine(chart, 100, tag, AxisKey);
    }

    private static void AddHLine(PlotModel chart, double y, string tag, string axisKey)
    {
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = y,
            Color = OxyColor.FromAColor(60, OxyColors.Gray),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            YAxisKey = axisKey,
            Tag = tag,
        });
    }
}
