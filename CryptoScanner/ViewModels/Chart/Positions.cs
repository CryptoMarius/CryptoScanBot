using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Positions
{
    // Draws a vertical line at the position's open time.
    // Long positions grow up from y=0; short positions hang down from y=yAxisTop.
    private static void DrawVerticalLine(PlotModel chart, DateTime? time,
        decimal? atPrice, double yAxisTop, OxyColor color, string title, string tag)
    {
        if (time.HasValue && atPrice.HasValue)
        {
            double x = CandleTime.FromDateTime(time.Value).Minutes;
            var series = new LineSeries { Title = title, Color = color, LineStyle = LineStyle.Dot, StrokeThickness = 0.5, Font = Const.OxyFontName, Tag = tag, };
            series.Points.Add(new DataPoint(x, yAxisTop));
            series.Points.Add(new DataPoint(x, (double)atPrice.Value));
            chart.Series.Add(series);
        }
    }

    // Low-level helper: draws a horizontal line with a white left-aligned text label.
    // xLabelOffset shifts the label slightly right of the line start for readability.
    private static void DrawLabeledLine(PlotModel chart, double xStart, double xEnd,
        decimal atPrice, OxyColor color, string title, string caption, double xLabelOffset, string tag)
    {
        var series = new LineSeries { Title = title, Color = color, LineStyle = LineStyle.Dot, StrokeThickness = 0.5, Font = Const.OxyFontName, Tag = tag };
        series.Points.Add(new DataPoint(xStart, (double)atPrice));
        series.Points.Add(new DataPoint(xEnd, (double)atPrice));
        chart.Series.Add(series);

        chart.Annotations.Add(new TextAnnotation
        {
            Text = caption,
            TextPosition = new DataPoint(xStart + xLabelOffset, (double)atPrice),
            TextHorizontalAlignment = HorizontalAlignment.Left,
            TextColor = OxyColors.White,
            Background = OxyColors.Transparent,
            FontSize = 9,
            Tag = tag,
        });
    }

    // Draws a horizontal line for a step, starting at the position's open time.
    // Extends to closeTime when filled, or to the end of the chart when still open.
    private static void DrawHorizontalLine(PlotModel chart, CryptoPosition position,
        CryptoPositionStep step, decimal atPrice, OxyColor color, CandleTime maxDate, string title, string caption, double xLabelOffset, string tag)
    {
        double xStart = CandleTime.FromDateTime(position.CreateTime).Minutes;
        double xEnd = step.CloseTime.HasValue
            ? CandleTime.FromDateTime(step.CloseTime.Value).Minutes
            : maxDate.Minutes;

        DrawLabeledLine(chart, xStart, xEnd, atPrice, color, title, caption, xLabelOffset, tag);
    }

    private static void DrawSomething(PlotModel chart, CryptoPosition position, CryptoPositionStep step,
        CandleTime time, ScatterSeries scatterSeries, OxyColor lineColor, string caption, double xLabelOffset, CandleTime maxDate, string tag, bool isFilled)
    {
        if (isFilled)
            scatterSeries?.Points.Add(new ScatterPoint(time.Minutes - 1, (double)step.AveragePrice));

        double xStart = CandleTime.FromDateTime(position.CreateTime).Minutes;

        if (isFilled)
        {
            // Draw from position open to the fill time
            DrawLabeledLine(chart, xStart, time.Minutes, step.AveragePrice, lineColor, "line", caption, xLabelOffset, tag);
        }
        else
        {
            // Draw from position open to the end of the chart (order still open)
            DrawLabeledLine(chart, xStart, maxDate.Minutes, step.Price, lineColor, "line", caption, xLabelOffset, tag);

            // Stop and stoplimit levels drawn in orange; only relevant while the order is open
            if (step.StopPrice.HasValue)
                DrawHorizontalLine(chart, position, step, step.StopPrice.Value, OxyColors.Orange, maxDate, "stop", "stoploss", xLabelOffset, tag);
            if (step.StopLimitPrice.HasValue)
                DrawHorizontalLine(chart, position, step, step.StopLimitPrice.Value, OxyColors.Orange, maxDate, "limit", "stoplimit", xLabelOffset, tag);
        }
    }


    internal static void Draw(PlotModel chart, List<CryptoPosition> positionList, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string tag)
    {
        var seriesSell = new ScatterSeries
        {
            Title = "buy",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Diamond,
            Tag = tag
        };

        var seriesBuy = new ScatterSeries
        {
            Title = "sell",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Diamond,
            Tag = tag
        };

        // Offset labels half an interval to the right so they sit just beside the vertical start line
        double xLabelOffset = interval.Duration / 2.0;

        foreach (CryptoPosition position in positionList)
        {
            OxyColor positionColor = position.Side == CryptoTradeSide.Long ? OxyColors.Green : OxyColors.Red;
            bool isOpen = !position.CloseTime.HasValue;

            // Long: vertical line grows up from baseline (y=0).
            // Short: vertical line hangs down from the top of the visible chart.
            if (position.Side == CryptoTradeSide.Long)
                DrawVerticalLine(chart, position.CreateTime, position.EntryPrice, (double)(position.EntryPrice!.Value * 0.5m), positionColor, "start", tag);
            else
                DrawVerticalLine(chart, position.CreateTime, position.EntryPrice, (double)(position.EntryPrice!.Value * 2.0m), positionColor, "start", tag);

            if (isOpen)
            {
                double xStart = CandleTime.FromDateTime(position.CreateTime).Minutes;
                double xEnd = maxDate.Minutes + 2;

                // Break-even level
                if (position.BreakEvenPrice > 0)
                    DrawLabeledLine(chart, xStart, xEnd, position.BreakEvenPrice, OxyColors.Gray, "be", "breakeven", xLabelOffset, tag);
            }

            // The first entry-side step = "entry", subsequent entry-side steps = "dca#1", "dca#2", ...
            // Opposite-side steps = "take profit" (executed TP orders).
            // Stop-triggered fills = "stoploss".
            CryptoOrderSide entrySide = position.Side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell;
            int entryCount = 0;

            foreach (CryptoPositionPart positionPart in position.PartList.Values)
            {
                foreach (var step in positionPart.StepList.Values)
                {
                    // For a number of reasons cancelled
                    if (step.Status > CryptoOrderStatus.Filled)
                        continue;

                    bool isFilled = step.Status != CryptoOrderStatus.New;

                    CandleTime time = step.CloseTime.HasValue
                        ? CandleTime.FromDateTime(step.CloseTime.Value)
                        : CandleTime.FromDateTime(step.CreateTime);

                    if (time < minDate || time > maxDate)
                        continue;

                    // Determine caption and color for this step
                    bool isStopTriggered = isFilled && step.StopPrice.HasValue && step.AveragePrice == step.StopPrice;
                    string caption;
                    OxyColor lineColor;

                    if (isStopTriggered)
                    {
                        caption = "stoploss";
                        lineColor = OxyColors.Orange;
                    }
                    else if (step.Side == entrySide)
                    {
                        caption = entryCount == 0 ? "entry" : $"dca#{entryCount}";
                        entryCount++;
                        lineColor = positionColor;
                    }
                    else
                    {
                        caption = "take profit";
                        lineColor = positionColor;
                    }

                    ScatterSeries scatterSeries = step.Side == CryptoOrderSide.Buy ? seriesBuy : seriesSell;
                    DrawSomething(chart, position, step, time, scatterSeries, lineColor, caption, xLabelOffset, maxDate, tag, isFilled);
                }
            }
        }

        // Scatter markers are added last so OxyPlot renders them on top of all position lines,
        // ensuring the diamond symbols are always visible.
        chart.Series.Add(seriesBuy);
        chart.Series.Add(seriesSell);
    }

}
