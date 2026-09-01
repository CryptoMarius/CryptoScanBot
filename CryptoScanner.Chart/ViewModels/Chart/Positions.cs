using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

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
            StrokeThickness = 2.0,
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
            StrokeThickness = 2.0,
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


    // Draws the vertical piece that joins two heights of the SAME level, so a stop that trails
    // along reads as one staircase instead of a row of loose lines. Solid, because it is the
    // moment the level moved and not a level of its own.
    private static void DrawConnectorLine(PlotModel chart, double x,
        decimal priceFrom, decimal priceTo, OxyColor color, string group)
    {
        var series = new LineSeries
        {
            Color = color,
            LineStyle = LineStyle.Solid,
            StrokeThickness = 2.0,
            Font = Const.OxyFontName,
            YAxisKey = "price",
            Tag = group
        };
        series.Points.Add(new DataPoint(x, (double)priceFrom));
        series.Points.Add(new DataPoint(x, (double)priceTo));
        chart.Series.Add(series);
    }

    // One level of a position followed through time: the entry, a DCA step, a take profit or one
    // of its stop legs. Every time the order behind it is cancelled and placed again the level
    // arrives as another piece, and the pieces together are drawn as a single staircase.
    private sealed class LevelChain
    {
        public string Caption = "";
        public OxyColor Color;
        public List<(double Start, double End, decimal Price)> Pieces = [];
    }

    // Draws one level as a staircase: a horizontal piece for every stretch it stood still and a
    // vertical connector wherever it moved. Only the first piece carries the caption, so a stop
    // that trails along no longer writes "stop price" over the chart at every step it takes.
    private static void DrawChain(PlotModel chart, LevelChain chain, double xLabelOffset, string group)
    {
        // Merge what describes the same price back to back. Every take profit level carries the
        // same stop, so with a multi level take profit the same stop piece arrives once per level.
        List<(double Start, double End, decimal Price)> pieces = [];
        foreach (var piece in chain.Pieces.OrderBy(p => p.Start).ThenBy(p => p.End))
        {
            if (pieces.Count > 0)
            {
                var last = pieces[^1];
                if (last.Price == piece.Price && piece.Start <= last.End)
                {
                    if (piece.End > last.End)
                        pieces[^1] = (last.Start, piece.End, last.Price);
                    continue;
                }
            }
            pieces.Add(piece);
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            double end = piece.End;

            // Runs on to where the next piece starts: the order is cancelled and placed again a
            // moment later, and the hole that leaves reads as a level that was not there.
            if (i + 1 < pieces.Count && pieces[i + 1].Start > end)
                end = pieces[i + 1].Start;

            DrawHorizontalLine(chart, piece.Start, end, piece.Price, chain.Color,
                i == 0 ? chain.Caption : "", xLabelOffset, group);

            if (i + 1 < pieces.Count && pieces[i + 1].Price != piece.Price)
                DrawConnectorLine(chart, pieces[i + 1].Start, piece.Price, pieces[i + 1].Price, chain.Color, group);
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

            // Caption once per level. An order that is cancelled and placed again produces a new
            // line every time, and with the take profit being repositioned on every break-even
            // change that put the same "stop price" and "stop limit" text across the chart four or
            // five times over. A trailing stop made that worse still: every step it takes is a
            // level of its own, so it was labelled again on each one.
            //
            // So the pieces of one level are collected here and drawn as a single staircase
            // (DrawChain): horizontal where the level stood still, a vertical connector where it
            // moved, and the caption only on the very first piece.
            Dictionary<string, LevelChain> chains = [];
            void AddPiece(string key, string caption, OxyColor color, double start, double end, decimal atPrice)
            {
                if (!chains.TryGetValue(key, out LevelChain? chain))
                {
                    chain = new LevelChain { Caption = caption, Color = color };
                    chains[key] = chain;
                }
                chain.Pieces.Add((start, end, atPrice));
            }

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
                            AddPiece("entry", "entry", stepColor, xStart, xEnd, step.Price);
                            if (firstEntry == xStart && step.CloseTime.HasValue)
                                firstEntry = CandleTime.FromDateTime(step.CloseTime.Value).Minutes;
                            break;
                        case CryptoPartPurpose.Dca:
                            double x2 = CandleTime.FromDateTime(step.CreateTime).Minutes;
                            double xEndDca = step.CloseTime == null ? maxDate.Minutes + 2 : CandleTime.FromDateTime(step.CloseTime!.Value).Minutes;
                            AddPiece($"dca-{positionPart.PartNumber}", $"dca-{positionPart.PartNumber}", stepColor, x2, xEndDca, step.Price);
                            break;
                        case CryptoPartPurpose.TakeProfit:
                            double x1 = CandleTime.FromDateTime(step.CreateTime).Minutes;
                            double xEndTp = step.CloseTime == null ? maxDate.Minutes + 2 : CandleTime.FromDateTime(step.CloseTime!.Value).Minutes;
                            AddPiece($"tp-{positionPart.PartNumber}", $"take profit-{positionPart.PartNumber}", stepColor, x1, xEndTp, step.Price);

                            //if (step.CloseTime.HasValue && step.StopPrice.HasValue && step.AveragePrice == step.StopPrice)
                            //    stepColor = OxyColors.Yellow; // just to see for now (orange ain't much different then red)

                            // Both stop legs are shared by every take profit level, so they are
                            // chained per position and not per part - a two level take profit would
                            // otherwise draw the very same staircase twice.
                            if (step.StopPrice.HasValue)
                                AddPiece("stop price", "stop price", stepColor, x1, xEndTp, step.StopPrice.Value);

                            if (step.StopLimitPrice.HasValue)
                                AddPiece("stop limit", "stop limit", stepColor, x1, xEndTp, step.StopLimitPrice.Value);
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

            // Every level as one staircase, in the order the levels were first seen
            foreach (LevelChain chain in chains.Values)
                DrawChain(chart, chain, xLabelOffset, group);

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