using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using CryptoScanner.Analyzers.Chart;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Chart.ViewModels.Chart;

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
            //Color = OxyColors.White,
            //StrokeThickness = 1.0,
            // Colour, width and dash from Settings / Chart styles, the same as the Photino chart
            Color = ChartStyleOxy.ColorFor("rsi", OxyColors.White),
            StrokeThickness = ChartStyleOxy.ThicknessFor("rsi", 1.0),
            LineStyle = ChartStyleOxy.LineStyleFor("rsi", LineStyle.Solid),
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
            //Color = OxyColor.FromAColor(140, OxyColors.Tomato),
            //StrokeThickness = 1,
            //LineStyle = LineStyle.Solid,
            Color = ChartStyleOxy.ColorFor("rsiOverbought", OxyColor.FromAColor(140, OxyColors.Tomato)),
            StrokeThickness = ChartStyleOxy.ThicknessFor("rsiOverbought", 1),
            LineStyle = ChartStyleOxy.LineStyleFor("rsiOverbought", LineStyle.Solid),
            YAxisKey = AxisKey,
            Tag = tag,
        });

        // Mid-line at 50
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 50,
            //Color = OxyColor.FromAColor(60, OxyColors.White),
            //StrokeThickness = 1,
            //LineStyle = LineStyle.Dash,
            Color = ChartStyleOxy.ColorFor("rsiMiddle", OxyColor.FromAColor(60, OxyColors.White)),
            StrokeThickness = ChartStyleOxy.ThicknessFor("rsiMiddle", 1),
            LineStyle = ChartStyleOxy.LineStyleFor("rsiMiddle", LineStyle.Dash),
            YAxisKey = AxisKey,
            Tag = tag,
        });

        // Oversold at 20
        chart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = GlobalData.Settings.General.SettingsRsi.Oversold,
            //Color = OxyColor.FromAColor(140, OxyColors.LimeGreen),
            //StrokeThickness = 1,
            //LineStyle = LineStyle.Solid,
            Color = ChartStyleOxy.ColorFor("rsiOversold", OxyColor.FromAColor(140, OxyColors.LimeGreen)),
            StrokeThickness = ChartStyleOxy.ThicknessFor("rsiOversold", 1),
            LineStyle = ChartStyleOxy.LineStyleFor("rsiOversold", LineStyle.Solid),
            YAxisKey = AxisKey,
            Tag = tag,
        });
    }
}
