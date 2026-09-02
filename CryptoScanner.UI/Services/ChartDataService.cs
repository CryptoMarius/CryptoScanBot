using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.UI.Models;

using Dapper;

namespace CryptoScanner.UI.Services;

/// <summary>
/// Builds the zone/signal/position payload for the lightweight-charts widget.
/// Mirrors what the Avalonia chart draws through OxyPlot annotations, but emits
/// plain data the JavaScript side turns into rectangles, markers and price lines.
/// </summary>
public static class ChartDataService
{
    // Same colour convention as the Avalonia chart (Const.ColorList)
    private static readonly Dictionary<(CryptoZoneKind, CryptoTradeSide), string> ZoneColors = new()
    {
        [(CryptoZoneKind.DominantLevel, CryptoTradeSide.Long)] = "0,100,0",
        [(CryptoZoneKind.DominantLevel, CryptoTradeSide.Short)] = "139,0,0",
        [(CryptoZoneKind.FairValueGap, CryptoTradeSide.Long)] = "169,169,169",
        [(CryptoZoneKind.FairValueGap, CryptoTradeSide.Short)] = "169,169,169",
        [(CryptoZoneKind.OrderBlock, CryptoTradeSide.Long)] = "70,130,180",
        [(CryptoZoneKind.OrderBlock, CryptoTradeSide.Short)] = "147,112,219",
    };

    public sealed class ChartRect
    {
        public long time1 { get; set; }
        public long? time2 { get; set; }
        public double price1 { get; set; }
        public double price2 { get; set; }
        public string fill { get; set; } = "";
        public string border { get; set; } = "";
        public string text { get; set; } = "";
        public string textColor { get; set; } = "#ffffff";
    }

    public sealed class ChartMarker
    {
        public long time { get; set; }
        public string position { get; set; } = "aboveBar";
        public string color { get; set; } = "#ffffff";
        public string shape { get; set; } = "circle";
        public string text { get; set; } = "";
        public int size { get; set; } = 1;
    }

    public sealed class ChartPriceLine
    {
        public double price { get; set; }
        public string color { get; set; } = "#888888";
        public int lineStyle { get; set; } = 2;
        public string title { get; set; } = "";
    }

    /// <summary>
    /// A bounded line piece, the equivalent of the OxyPlot LineSeries the Avalonia chart uses for
    /// position levels. Unlike a price line it starts and stops at a given time, so an entry, its
    /// DCA levels and its take profit each cover only their own stretch of the chart.
    /// </summary>
    public sealed class ChartSegment
    {
        public long time1 { get; set; }
        public long time2 { get; set; }
        public double price1 { get; set; }
        public double price2 { get; set; }
        public string color { get; set; } = "#888888";
        public int width { get; set; } = 2;

        /// <summary>0 = solid, 1 = dotted, 2 = dash-dash-dot.</summary>
        public int dash { get; set; } = 2;

        public string text { get; set; } = "";

        /// <summary>
        /// Price the caption is drawn at, when that should not be the start of the line itself.
        /// Null means "at price1", which is what every horizontal level uses.
        /// </summary>
        public double? textPrice { get; set; }
    }

    /// <summary>
    /// A small filled circle at one moment and price. Used to mark where an order actually filled,
    /// the equivalent of the 4 pixel scatter diamond in the Avalonia chart.
    /// </summary>
    public sealed class ChartDot
    {
        public long time { get; set; }
        public double price { get; set; }
        public string color { get; set; } = "#ffffff";
        public int radius { get; set; } = 3;
    }

    /// <summary>
    /// One moment, drawn top to bottom in the sub-panels (volume, RSI/stochastic, MACD) so a
    /// position's open can be followed across all four panes at once. The main chart draws the same
    /// moment as a pair of bounded <see cref="ChartSegment"/> verticals instead, which is what keeps
    /// them off the candle it belongs to.
    /// </summary>
    public sealed class ChartVertical
    {
        public long time { get; set; }
        public string color { get; set; } = "#888888";
    }

    /// <summary>
    /// The candle that CONTAINS this moment. A fill that lands exactly on a candle boundary belongs
    /// to the candle that just closed, not to the one starting at that instant — and paper trading
    /// stamps every fill with the close time of the base candle it happened in, so plain alignment
    /// put each marker one candle to the right of the candle that actually filled the order.
    /// </summary>
    private static CandleTime CandleContaining(DateTime moment, uint duration)
    {
        CandleTime aligned = CandleTime.AlignFromDateTime(moment, duration);
        if (aligned.ToDateTime() == moment.ToUniversalTime())
            aligned -= duration;
        return aligned;
    }

    /// <summary>Horizontal level between two moments, captioned just right of its start.</summary>
    private static ChartSegment Horizontal(long time1, long time2, decimal price, ChartLineStyle style, string caption) => new()
    {
        time1 = time1,
        time2 = time2,
        price1 = (double)price,
        price2 = (double)price,
        color = style.ToCssColor(),
        width = style.LineWidth,
        // Dotted with a wide gap by default; the Avalonia dash-dash-dot was too loud here
        dash = style.LineStyle,
        text = caption,
    };

    /// <summary>
    /// The vertical piece that joins two heights of the SAME level, so a stop that trails along
    /// reads as one staircase instead of a row of loose lines. Drawn in the same colour, width and
    /// pattern as the level itself: solid, it sat on the chart as a heavy bar next to the thin
    /// dotted levels it was only meant to tie together.
    /// </summary>
    private static ChartSegment Connector(long time, decimal priceFrom, decimal priceTo, ChartLineStyle style) => new()
    {
        time1 = time,
        time2 = time,
        price1 = (double)priceFrom,
        price2 = (double)priceTo,
        color = style.ToCssColor(),
        width = style.LineWidth,
        dash = style.LineStyle,
    };

    /// <summary>
    /// Vertical marker at one moment, running between two prices. The caption is drawn at
    /// <paramref name="priceFrom"/>, so pass the far end of the line to keep the text clear of the
    /// candles.
    /// </summary>
    private static ChartSegment Vertical(long time, decimal priceFrom, decimal priceTo, ChartLineStyle style,
        string caption = "", decimal? captionPrice = null) => new()
        {
            time1 = time,
            time2 = time,
            price1 = (double)priceFrom,
            price2 = (double)priceTo,
            color = style.ToCssColor(),
            width = style.LineWidth,
            dash = style.LineStyle, // Dot by default, as in Positions.DrawVerticalLine
            text = caption,
            textPrice = captionPrice == null ? null : (double)captionPrice.Value,
        };

    /// <summary>
    /// One level of a position followed through time: the entry, a DCA step, a take profit or one
    /// of its stop legs. Every time the order behind it is cancelled and placed again the level
    /// arrives as another piece, and the pieces together are drawn as a single staircase.
    /// </summary>
    private sealed class LevelChain
    {
        public string Caption = "";
        public ChartLineStyle Style = new();
        public List<(long Start, long End, decimal Price)> Pieces = [];
    }

    /// <summary>
    /// Draws one level as a staircase: a horizontal piece for every stretch it stood still and a
    /// vertical connector wherever it moved. Only the first piece carries the caption, so a stop
    /// that trails along no longer writes "stop price" over the chart at every step it takes.
    /// </summary>
    private static void EmitChain(List<ChartSegment> segments, LevelChain chain)
    {
        // Merge what describes the same price back to back. Every take profit level carries the
        // same stop, so with a multi level take profit the same stop piece arrives once per level.
        List<(long Start, long End, decimal Price)> pieces = [];
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
            long end = piece.End;

            // Runs on to where the next piece starts: the order is cancelled and placed again a
            // moment later, and the hole that leaves reads as a level that was not there.
            if (i + 1 < pieces.Count && pieces[i + 1].Start > end)
                end = pieces[i + 1].Start;

            segments.Add(Horizontal(piece.Start, end, piece.Price, chain.Style, i == 0 ? chain.Caption : ""));

            if (i + 1 < pieces.Count && pieces[i + 1].Price != piece.Price)
                segments.Add(Connector(pieces[i + 1].Start, piece.Price, pieces[i + 1].Price, chain.Style));
        }
    }

    public static List<ChartRect> BuildZones(CryptoSymbol symbol, bool showDlz, bool showFvg, bool showSmc,
        CandleTime from, CandleTime to)
    {
        var result = new List<ChartRect>();
        if (!showDlz && !showFvg && !showSmc)
            return result;

        var symbolData = symbol.Data;

        if (showDlz)
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
            {
                if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    continue;

                var dataInterval = symbolData.Get(interval.IntervalPeriod);
                AddZones(result, dataInterval.Dlz.Zones.LongOpen, from, to);
                AddZones(result, dataInterval.Dlz.Zones.ShortOpen, from, to);
                AddZones(result, dataInterval.Dlz.Zones.LongClosed, from, to);
                AddZones(result, dataInterval.Dlz.Zones.ShortClosed, from, to);
            }
        }

        if (showFvg)
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
            {
                if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    continue;

                var dataInterval = symbolData.Get(interval.IntervalPeriod);
                AddZones(result, dataInterval.Fvg.Zones.LongOpen, from, to);
                AddZones(result, dataInterval.Fvg.Zones.ShortOpen, from, to);
                AddZones(result, dataInterval.Fvg.Zones.LongClosed, from, to);
                AddZones(result, dataInterval.Fvg.Zones.ShortClosed, from, to);
            }
        }

        if (showSmc)
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
            {
                if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    continue;

                // SMC order blocks live in a plain list, ZoneSmc mutates it in place
                var dataInterval = symbolData.Get(interval.IntervalPeriod);
                AddZones(result, dataInterval.Smc.Zones.ToList(), from, to);
            }
        }

        return result;
    }

    private static void AddZones(List<ChartRect> target, IEnumerable<CryptoZone> zones,
        CandleTime from, CandleTime to)
    {
        foreach (var zone in zones)
        {
            // Only zones whose lifetime overlaps the loaded candles. Without this every zone of
            // every configured interval was sent over, thousands of translucent boxes stacking
            // into one solid wash that hid the candles completely.
            if (zone.OpenTime > to)
                continue;
            if (zone.CloseTime.HasValue && zone.CloseTime.Value < from)
                continue;

            if (!ZoneColors.TryGetValue((zone.Kind, zone.Side), out string? rgb))
                rgb = "128,128,128";

            // Closed and weak zones are drawn fainter, same as the Avalonia chart
            double alpha = zone.CloseTime.HasValue ? 0.20 : 0.32;
            if (zone.Strength != CryptoZoneStrength.Strong)
                alpha *= 0.6;

            target.Add(new ChartRect
            {
                time1 = zone.OpenTime.ToUnixSeconds(),
                time2 = zone.CloseTime?.ToUnixSeconds(),
                price1 = (double)zone.Bottom,
                price2 = (double)zone.Top,
                fill = $"rgba({rgb},{alpha.ToString(System.Globalization.CultureInfo.InvariantCulture)})",
                border = $"rgba({rgb},0.75)",
                text = zone.Description ?? "",
            });
        }
    }

    /// <summary>
    /// Signals as small dots, the way the Avalonia chart draws them (Chart/Signals.cs: a
    /// ScatterSeries of MarkerSize 2, yellow for long and red for short, placed just off the
    /// candle at 0.99x / 1.01x the signal price).
    /// <para>
    /// Dots and not series markers, for two reasons. The captions: an arrow per signal carried its
    /// StrategyText, and on a busy symbol those piled into an unreadable wall over the candles.
    /// And the size: lightweight-charts' own marker shapes start out bigger than the candles, which
    /// is the same reason the order fills are drawn this way — see the remarks in chart-widget.js.
    /// </para>
    /// </summary>
    public static List<ChartDot> BuildSignalDots(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime to)
    {
        var dots = new List<ChartDot>();

        // Personal colours from Settings / Chart styles, group "Signals"
        var styles = ChartStyleSettings.Current;
        ChartLineStyle longStyle = styles.Get("signalLong");
        ChartLineStyle shortStyle = styles.Get("signalShort");

        string sql = "select * from signal where SymbolId = @SymbolId " +
            "and CloseDate > @From and CloseDate <= @To and EmulatorRunId is null";

        using var database = new CryptoDatabase();
        try
        {
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql,
                new { SymbolId = symbol.Id, From = from.ToDateTime(), To = to.ToDateTime() }))
            {
                bool isLong = signal.Side == CryptoTradeSide.Long;
                dots.Add(new ChartDot
                {
                    // Aligned onto the candle grid of the interval SHOWN. A signal's close time is
                    // the end of its own (often smaller) candle, so on an hourly chart nothing
                    // lined up with a bar at all.
                    time = CandleTime.AlignFromDateTime(signal.CloseDate, interval.Duration).ToUnixSeconds(),

                    // Off the candle rather than on it, the same 1% either way as Avalonia: a dot
                    // exactly on the signal price disappears into the body it belongs to.
                    price = (double)(isLong ? 0.99m * signal.SignalPrice : 1.01m * signal.SignalPrice),
                    color = (isLong ? longStyle : shortStyle).ToCssColor(),
                    radius = 2,
                });
            }
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Chart signal load error: {ex.Message}");
        }
        finally
        {
            database.Close();
        }

        return dots;
    }

    /// <param name="verticals">Filled with the open moment of every position drawn, for the
    /// sub-panels to carry down. Same times as the vertical markers on the main chart.</param>
    public static void BuildPositionOverlays(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime to, List<ChartSegment> segments, List<ChartDot> dots,
        List<ChartVertical> verticals)
    {
        string sql = "select * from position where SymbolId = @SymbolId " +
            "and CreateTime <= @To and (CloseTime is null or CloseTime >= @From) " +
            "and EmulatorRunId is null order by CreateTime";

        // Personal colours, widths and line styles from Settings / Chart styles, group "Positions".
        // Read once per redraw; the whole chart is rebuilt whenever one of them is changed.
        var styles = ChartStyleSettings.Current;
        ChartLineStyle buyStyle = styles.Get("positionBuy");
        ChartLineStyle sellStyle = styles.Get("positionSell");
        ChartLineStyle stopPriceStyle = styles.Get("positionStopPrice");
        ChartLineStyle stopLimitStyle = styles.Get("positionStopLimit");
        ChartLineStyle breakEvenStyle = styles.Get("positionBreakEven");
        ChartLineStyle openLongStyle = styles.Get("positionOpenLong");
        ChartLineStyle openShortStyle = styles.Get("positionOpenShort");
        ChartLineStyle fillBuyStyle = styles.Get("positionFillBuy");
        ChartLineStyle fillSellStyle = styles.Get("positionFillSell");

        using var database = new CryptoDatabase();
        try
        {
            var positions = new List<CryptoPosition>();
            foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql,
                new { SymbolId = symbol.Id, From = from.ToDateTime(), To = to.ToDateTime() }))
            {
                if (!GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Core.Model.CryptoExchange? exchange))
                    continue;
                if (!exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbolX))
                    continue;

                position.Exchange = exchange;
                position.Symbol = symbolX;
                if (position.IntervalId != null && GlobalData.IntervalListId.TryGetValue((int)position.IntervalId, out CryptoInterval? positionInterval))
                    position.Interval = positionInterval;

                positions.Add(position);
                PositionTools.LoadPosition(database, position);
            }

            // Line drawing mirrors CryptoScanner.Chart/ViewModels/Chart/Positions.cs exactly:
            // every level is a BOUNDED segment from where the order was placed to where it closed
            // (or the right edge while it is still open), captioned just right of its start.
            long rightEdge = (to + 2 * interval.Duration).ToUnixSeconds();

            foreach (CryptoPosition position in positions)
            {
                // CandleContaining, not plain alignment. A position is created from a signal on a
                // candle that has just CLOSED, so its CreateTime lands exactly on a candle
                // boundary — and aligning that forward puts the whole position one candle to the
                // right of the candle that actually triggered it. Same reasoning, and the same
                // helper, as the fill dots below.
                long xStart = CandleContaining(position.CreateTime, interval.Duration).ToUnixSeconds();
                long xEnd = position.CloseTime == null
                    ? rightEdge
                    : CandleContaining(position.CloseTime.Value, interval.Duration).ToUnixSeconds();
                long firstEntry = xStart;

                decimal entry = position.EntryPrice ?? 0m;
                decimal yTop = entry;
                decimal yBottom = entry;

                // Once a position is finished, every order that never filled - DCA, take profit,
                // stop price, stop limit - describes an intention rather than what happened, and
                // only makes the picture harder to read. They were dropped for that reason, and
                // including the stop legs of a take profit that never triggered.
                //
                // That is undone again. What was left of a finished position was three or four
                // stubs a couple of candles wide, while the trail its stop and its take profit
                // walked is exactly what you open a closed position for. The staircase is what
                // makes that readable now: one line with one caption, instead of the wall of
                // separate levels the rule was written against.

                // Caption once per level. An order that is cancelled and placed again produces a
                // new segment every time, and with the take profit being repositioned on every
                // break-even change that put the same "stop price" and "stop limit" text across
                // the chart four or five times over. A trailing stop made that worse still: every
                // step it takes is a level of its own, so it was labelled again on each one.
                //
                // So the pieces of one level are collected here and drawn as a single staircase
                // (EmitChain): horizontal where the level stood still, a vertical connector where
                // it moved, and the caption only on the very first piece.
                var chains = new Dictionary<string, LevelChain>();
                void AddPiece(string key, string caption, ChartLineStyle style, long start, long end, decimal price)
                {
                    if (!chains.TryGetValue(key, out LevelChain? chain))
                    {
                        chain = new LevelChain { Caption = caption, Style = style };
                        chains[key] = chain;
                    }
                    chain.Pieces.Add((start, end, price));
                }

                foreach (CryptoPositionPart part in position.PartList.Values)
                {
                    foreach (var step in part.StepList.Values)
                    {
                        // Buy orders green, sell orders red — covers long and short in one rule
                        // ...and the entry, the DCA levels and the take profit still follow it.
                        // What changed is only where the two colours come from: Settings / Chart
                        // styles instead of the two constants that used to sit here. The stop legs
                        // below have a colour of their own, so they can be told apart from the
                        // orders they hang under.
                        ChartLineStyle sideStyle = step.Side == CryptoOrderSide.Buy ? buyStyle : sellStyle;

                        // CloseTime alone is not enough: it is also set when an order is cancelled
                        // or replaced. Only the status says whether anything was actually bought
                        // or sold.
                        bool stepFilled = step.Status.IsFilled();

                        // Same boundary rule as the position itself: an order placed or closed
                        // exactly on a candle boundary belongs to the candle that just ended.
                        long stepStart = CandleContaining(step.CreateTime, interval.Duration).ToUnixSeconds();
                        long stepEnd = step.CloseTime == null
                            ? rightEdge
                            : CandleContaining(step.CloseTime.Value, interval.Duration).ToUnixSeconds();

                        switch (part.Purpose)
                        {
                            case CryptoPartPurpose.Entry:
                                AddPiece("entry", "entry", sideStyle, xStart, xEnd, step.Price);
                                if (firstEntry == xStart && step.CloseTime.HasValue)
                                    firstEntry = stepEnd;
                                break;

                            case CryptoPartPurpose.Dca:
                                AddPiece($"dca-{part.PartNumber}", $"dca-{part.PartNumber}", sideStyle,
                                    stepStart, stepEnd, step.Price);
                                break;

                            case CryptoPartPurpose.TakeProfit:
                                AddPiece($"tp-{part.PartNumber}", $"take profit-{part.PartNumber}", sideStyle,
                                    stepStart, stepEnd, step.Price);

                                // Both stop legs are shared by every take profit level, so they are
                                // chained per position and not per part - a two level take profit
                                // would otherwise draw the very same staircase twice.
                                if (step.StopPrice.HasValue)
                                    AddPiece("stop price", "stop price", stopPriceStyle,
                                        stepStart, stepEnd, step.StopPrice.Value);
                                if (step.StopLimitPrice.HasValue)
                                    AddPiece("stop limit", "stop limit", stopLimitStyle,
                                        stepStart, stepEnd, step.StopLimitPrice.Value);
                                break;
                        }

                        // Where the order actually filled, at the price it filled at. The Avalonia
                        // chart puts a 4 pixel diamond here; this is the same idea drawn by the
                        // segment primitive, because the marker shapes lightweight-charts offers
                        // start far bigger than the candles.
                        if (stepFilled && step.CloseTime.HasValue)
                        {
                            decimal filledPrice = step.AveragePrice > 0 ? step.AveragePrice : step.Price;
                            dots.Add(new ChartDot
                            {
                                time = CandleContaining(step.CloseTime.Value, interval.Duration).ToUnixSeconds(),
                                price = (double)filledPrice,
                                color = (step.Side == CryptoOrderSide.Buy ? fillBuyStyle : fillSellStyle).ToCssColor(),
                            });
                        }

                        // Keeps the vertical marker long enough to span every level of the position.
                        if (step.Price > yTop)
                            yTop = step.Price;
                        if (step.Price < yBottom)
                            yBottom = step.Price;
                    }
                }

                // Every level as one staircase, in the order the levels were first seen
                foreach (LevelChain chain in chains.Values)
                    EmitChain(segments, chain);

                // Two vertical dotted markers at the open time, one reaching up and one down, with
                // a gap around the candle so its wicks stay readable
                if (entry > 0)
                {
                    ChartLineStyle markerStyle = position.Side == CryptoTradeSide.Long ? openLongStyle : openShortStyle;
                    string positionColor = markerStyle.ToCssColor();

                    decimal candleAbove = entry;
                    decimal candleBelow = entry;
                    // The candle the marker is drawn ON, so the gap it leaves matches that candle's
                    // own wicks — the same one xStart resolved to.
                    var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    CandleTime openCandleTime = CandleContaining(position.CreateTime, interval.Duration);
                    if (symbolInterval.CandleList.TryGetValue(openCandleTime, out CryptoCandle openCandle))
                    {
                        candleAbove = openCandle.High + 0.01m * openCandle.High;
                        candleBelow = openCandle.Low - 0.01m * openCandle.Low;
                    }

                    decimal boxAbove = entry * 1.1m;
                    if (boxAbove < yTop)
                        boxAbove = yTop;
                    decimal boxBelow = entry * 0.9m;
                    if (boxBelow > yBottom)
                        boxBelow = yBottom;

                    // Strategy and the interval the position started on, 3% out from the entry on
                    // the PROFIT side: above for a long, below for a short. The DCA levels and the
                    // stop sit on the losing side, so their captions stay clear of this one.
                    string caption = $"{position.StrategyText} {position.Interval?.Name}".Trim();
                    decimal captionPrice = position.Side == CryptoTradeSide.Long
                        ? entry * 1.03m
                        : entry * 0.97m;

                    segments.Add(Vertical(xStart, boxAbove, candleAbove, markerStyle, caption, captionPrice));
                    segments.Add(Vertical(xStart, boxBelow, candleBelow, markerStyle));

                    // The same moment for the sub-panels, where it runs the full height: volume,
                    // RSI/stochastic and MACD at the open are exactly what you compare afterwards.
                    verticals.Add(new ChartVertical { time = xStart, color = positionColor });
                }

                // Break-even only while the position is open. Blue and without a caption: it sits
                // very close to the entry line, and two labels on top of each other read as noise.
                if (position.CloseTime == null && position.BreakEvenPrice > 0)
                    segments.Add(Horizontal(firstEntry, xEnd, position.BreakEvenPrice, breakEvenStyle, ""));
            }
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Chart position load error: {ex.Message}");
        }
        finally
        {
            database.Close();
        }
    }
}
