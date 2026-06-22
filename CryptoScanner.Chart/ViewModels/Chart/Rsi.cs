using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Rsi
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string tag, string AxisKey = "stoch")
    {
        if (candles.Count == 0)
            return;

        var rsiList = candles.AsQuotes().ToRsi(14);

        // RSI(14) line — white, thinner than stoch lines so it remains readable when overlaid
        var series = new LineSeries
        {
            Title = "RSI",
            Color = OxyColors.White,
            StrokeThickness = 1.0,
            YAxisKey = AxisKey,
            Tag = tag,
        };

        foreach (var item in rsiList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(item.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (item.Rsi.HasValue)
                    series.Points.Add(new DataPoint(openTime.Minutes, item.Rsi.Value));
            }
        }

        chart.Series.Add(series);
    }


    internal static void DrawLines(PlotModel chart, string tag, string AxisKey = "stoch")
    {
        // Overbought at 80
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = GlobalData.Settings.General.SettingsRsi.Overbought,
            Color = OxyColor.FromAColor(140, OxyColors.Tomato),
            StrokeThickness = 1,
            LineStyle = LineStyle.Solid,
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
            Y = GlobalData.Settings.General.SettingsRsi.Oversold,
            Color = OxyColor.FromAColor(140, OxyColors.LimeGreen),
            StrokeThickness = 1,
            LineStyle = LineStyle.Solid,
            YAxisKey = AxisKey,
            Tag = tag,
        });
    }
}
