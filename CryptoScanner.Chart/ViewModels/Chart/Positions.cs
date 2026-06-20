using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Positions
{
    // Color convention: Buy orders = green, Sell orders = red.
    // This naturally handles all cases: long entry/DCA (buy=green), long TP (sell=red),
    // short entry/DCA (sell=red), short TP (buy=green).
    //private static OxyColor StepColor(CryptoPositionStep step) =>
    //    step.Side == CryptoOrderSide.Buy ? OxyColors.Green : OxyColors.Red;

    // Draws a vertical line at the open time of a position.
    // Long: grows up from y=0. Short: hangs down from y=yAxisTop.
    private static void DrawVerticalLine(PlotModel chart, DateTime time,
        decimal atPrice, decimal yAxisTop, OxyColor color, string group)
    {
        var series = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.Dot,
            StrokeThickness = 1.0,
            Font = Const.OxyFontName,
            YAxisKey = "price",
            Tag = group
        };
        double x = CandleTime.FromDateTime(time).Minutes;
        series.Points.Add(new DataPoint(x, (double)yAxisTop));
        series.Points.Add(new DataPoint(x, (double)atPrice));
        chart.Series.Add(series);
    }

    // Draws a labeled horizontal line between xStart and xEnd at the given price.
    // The caption is placed just right of xStart in white left-aligned text.
    private static void DrawHorizontalLine(PlotModel chart, double xStart, double xEnd,
        decimal atPrice, OxyColor color, string caption, double xLabelOffset, string group)
    {
        var series = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.DashDashDot,
            StrokeThickness = 1.0,
            Font = Const.OxyFontName,
            YAxisKey = "price",
            Tag = group
        };
        series.Points.Add(new DataPoint(xStart, (double)atPrice));
        series.Points.Add(new DataPoint(xEnd, (double)atPrice));
        chart.Series.Add(series);

        if (caption != "")
        {
            chart.Annotations.Add(new TextAnnotation
            {
                Text = caption,
                TextPosition = new DataPoint(xStart + xLabelOffset, (double)atPrice),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextColor = OxyColors.White,
                Background = OxyColors.Transparent,
                FontSize = 9,
                Tag = group,
            });
        }
    }


    internal static void Draw(PlotModel chart, CryptoSymbol symbol, List<CryptoPosition> positionList, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        // Label offset: 1.5 candle-widths to the right of the line start
        double xLabelOffset = interval.Duration * 1.5;

        var seriesBuy = new ScatterSeries
        {
            Title = "buy",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesSell = new ScatterSeries
        {
            Title = "sell",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group
        };

        foreach (CryptoPosition position in positionList)
        {
            double xStart = CandleTime.FromDateTime(position.CreateTime).Minutes;
            double xEnd = position.CloseTime == null ? maxDate.Minutes + 2 : CandleTime.FromDateTime(position.CloseTime!.Value).Minutes;
            double firstEntry = xStart;

            decimal yTop = position.EntryPrice!.Value;
            decimal yBottom = position.EntryPrice.Value;

            // Steps: first entry-side step = "entry", subsequent = "dca#1", "dca#2", ...
            foreach (CryptoPositionPart positionPart in position.PartList.Values)
            {
                foreach (var step in positionPart.StepList.Values)
                {
                    //if (step.Status > CryptoOrderStatus.Filled)
                    //    continue;

                    CandleTime stepTime = step.CloseTime.HasValue
                        ? CandleTime.FromDateTime(step.CloseTime.Value)
                        : CandleTime.FromDateTime(step.CreateTime);

                    if (stepTime < minDate) // || stepTime > maxDate
                        continue;

                    //bool isStopTriggered = isFilled && step.StopPrice.HasValue && step.AveragePrice == step.StopPrice;
                    //OxyColor stepColor = isStopTriggered ? OxyColors.Orange : StepColor(step);
                    //OxyColor StepColor(CryptoPositionStep step) =>
                    OxyColor stepColor = step.Side == CryptoOrderSide.Buy ? OxyColors.DarkGreen : OxyColors.DarkRed;

                    switch (positionPart.Purpose)
                    {
                        case CryptoPartPurpose.Entry:
                            DrawHorizontalLine(chart, xStart, xEnd, step.Price, stepColor, "entry", xLabelOffset, group);
                            if (firstEntry == xStart && step.CloseTime.HasValue)
                                firstEntry = CandleTime.FromDateTime(step.CloseTime.Value).Minutes;
                            break;
                        case CryptoPartPurpose.Dca:
                            double x2 = CandleTime.FromDateTime(step.CreateTime).Minutes;
                            double xEndDca = step.CloseTime == null ? maxDate.Minutes + 2 : CandleTime.FromDateTime(step.CloseTime!.Value).Minutes;
                            DrawHorizontalLine(chart, x2, xEndDca, step.Price, stepColor, $"dca-{positionPart.PartNumber}", xLabelOffset, group);
                            break;
                        case CryptoPartPurpose.TakeProfit:
                            double x1 = CandleTime.FromDateTime(step.CreateTime).Minutes;
                            double xEndTp = step.CloseTime == null ? maxDate.Minutes + 2 : CandleTime.FromDateTime(step.CloseTime!.Value).Minutes;
                            DrawHorizontalLine(chart, x1, xEndTp, step.Price, stepColor, "take profit", xLabelOffset, group);

                            //if (step.CloseTime.HasValue && step.StopPrice.HasValue && step.AveragePrice == step.StopPrice)
                            //    stepColor = OxyColors.Yellow; // just to see for now (orange ain't much different then red)
                            if (step.StopPrice.HasValue)
                                DrawHorizontalLine(chart, x1, xEndTp, step.StopPrice.Value, stepColor, "stop price", xLabelOffset, group);

                            if (step.StopLimitPrice.HasValue)
                                DrawHorizontalLine(chart, x1, xEndTp, step.StopLimitPrice.Value, stepColor, "stop limit", xLabelOffset, group);
                            break;
                    }

                    if (step.CloseTime.HasValue)
                    {
                        ScatterSeries scatter = step.Side == CryptoOrderSide.Buy ? seriesBuy : seriesSell;
                        double x = CandleTime.FromDateTime(step.CloseTime.Value).Minutes;
                        scatter?.Points.Add(new ScatterPoint(x, (double)step.AveragePrice));
                    }

                    // Extend the vertical line if needed
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        if (step.Price > yTop)
                            yTop = step.Price;
                        if (step.Price < yBottom)
                            yBottom = step.Price;
                    }
                    else
                    {
                        if (step.Price < yTop)
                            yTop = step.Price;
                        if (step.Price < yBottom)
                            yBottom = step.Price;
                    }
                }
            }

            // Vertical marker at position open time.
            // Long grows up from y=0; short hangs down from 2× entry price.
            // TODO: Draw line to the TP above the entry
            OxyColor positionColor = position.Side == CryptoTradeSide.Long ? OxyColors.DarkGreen : OxyColors.DarkRed;



            // Allow a clear gap around the wicks so it does not cover any part of the wicks
            CandleTime openCandleTime = CandleTime.AlignFromDateTime(position.CreateTime, interval.Duration);
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            decimal entry = position.EntryPrice!.Value;
            decimal candleAbove = entry;
            decimal candleBelow = entry;
            if (symbolInterval.CandleList.TryGetValue(openCandleTime, out CryptoCandle openCandle))
            {
                candleAbove = openCandle.High + 0.01m * openCandle.High;
                candleBelow = openCandle.Low - 0.01m * openCandle.Low;
            }
            // start the vertical line 10% below/above price..
            decimal boxAbove = entry * 1.1m;
            if (boxAbove < yTop)
                boxAbove = yTop;

            decimal boxBelow = entry * 0.9m;
            if (boxBelow > yBottom)
                boxBelow = yBottom;

            DrawVerticalLine(chart, position.CreateTime, boxAbove, candleAbove, positionColor, group);
            DrawVerticalLine(chart, position.CreateTime, boxBelow, candleBelow, positionColor, group);

            // Break-even and take-profit levels, only while the position is open
            if (position.CloseTime == null)
            {
                if (position.BreakEvenPrice > 0)
                    DrawHorizontalLine(chart, firstEntry, xEnd, position.BreakEvenPrice, OxyColors.Gray, "breakeven", xLabelOffset, group);
            }

        }

        // Scatter markers added last so they render on top of all position lines
        chart.Series.Add(seriesBuy);
        chart.Series.Add(seriesSell);
    }

}