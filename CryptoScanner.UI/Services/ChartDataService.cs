using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

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
    private static ChartSegment Horizontal(long time1, long time2, decimal price, string color, string caption) => new()
    {
        time1 = time1,
        time2 = time2,
        price1 = (double)price,
        price2 = (double)price,
        color = color,
        dash = 2, // Dotted with a wide gap; the Avalonia dash-dash-dot was too loud here
        text = caption,
    };

    /// <summary>
    /// Vertical marker at one moment, running between two prices. The caption is drawn at
    /// <paramref name="priceFrom"/>, so pass the far end of the line to keep the text clear of the
    /// candles.
    /// </summary>
    private static ChartSegment Vertical(long time, decimal priceFrom, decimal priceTo, string color,
        string caption = "", decimal? captionPrice = null) => new()
        {
            time1 = time,
            time2 = time,
            price1 = (double)priceFrom,
            price2 = (double)priceTo,
            color = color,
            dash = 1, // Dot, as in Positions.DrawVerticalLine
            text = caption,
            textPrice = captionPrice == null ? null : (double)captionPrice.Value,
        };

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

    public static List<ChartMarker> BuildSignalMarkers(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime to)
    {
        var markers = new List<ChartMarker>();

        string sql = "select * from signal where SymbolId = @SymbolId " +
            "and CloseDate > @From and CloseDate <= @To and EmulatorRunId is null";

        using var database = new CryptoDatabase();
        try
        {
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql,
                new { SymbolId = symbol.Id, From = from.ToDateTime(), To = to.ToDateTime() }))
            {
                bool isLong = signal.Side == CryptoTradeSide.Long;
                markers.Add(new ChartMarker
                {
                    // Aligned onto the candle grid of the interval SHOWN. A marker only renders
                    // when its time matches a bar, and a signal's close time is the end of its own
                    // (often smaller) candle - on an hourly chart nothing lined up at all.
                    time = CandleTime.AlignFromDateTime(signal.CloseDate, interval.Duration).ToUnixSeconds(),
                    position = isLong ? "belowBar" : "aboveBar",
                    color = isLong ? "#ffeb3b" : "#e53935",
                    shape = isLong ? "arrowUp" : "arrowDown",
                    text = signal.StrategyText ?? "",
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

        return markers;
    }

    public static void BuildPositionOverlays(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime to, List<ChartSegment> segments, List<ChartDot> dots)
    {
        string sql = "select * from position where SymbolId = @SymbolId " +
            "and CreateTime <= @To and (CloseTime is null or CloseTime >= @From) " +
            "and EmulatorRunId is null order by CreateTime";

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
                long xStart = CandleTime.AlignFromDateTime(position.CreateTime, interval.Duration).ToUnixSeconds();
                long xEnd = position.CloseTime == null
                    ? rightEdge
                    : CandleTime.AlignFromDateTime(position.CloseTime.Value, interval.Duration).ToUnixSeconds();
                long firstEntry = xStart;

                decimal entry = position.EntryPrice ?? 0m;
                decimal yTop = entry;
                decimal yBottom = entry;

                // Once a position is finished, every order that never filled - DCA, take profit,
                // stop price, stop limit - describes an intention rather than what happened, and
                // only makes the picture harder to read. They are dropped. While the position runs
                // they are exactly what you want to see, so nothing changes there. The entry is
                // always kept: it shows where you meant to get in even when it never filled.
                bool positionClosed = position.CloseTime != null;

                // Caption once per level. An order that is cancelled and placed again produces a
                // new segment every time, and with the take profit being repositioned on every
                // break-even change that put the same "stop price" and "stop limit" text across
                // the chart four or five times over. The line still gets drawn - only the repeated
                // caption is dropped, and a level that really moves is labelled again.
                var captioned = new HashSet<string>();
                string CaptionOnce(string caption, decimal price)
                    => captioned.Add($"{caption}|{price}") ? caption : "";

                foreach (CryptoPositionPart part in position.PartList.Values)
                {
                    foreach (var step in part.StepList.Values)
                    {
                        // Buy orders green, sell orders red — covers long and short in one rule
                        string color = step.Side == CryptoOrderSide.Buy ? "#006400" : "#8B0000";

                        // CloseTime alone is not enough: it is also set when an order is cancelled
                        // or replaced. Only the status says whether anything was actually bought
                        // or sold.
                        bool stepFilled = step.Status.IsFilled();

                        long stepStart = CandleTime.AlignFromDateTime(step.CreateTime, interval.Duration).ToUnixSeconds();
                        long stepEnd = step.CloseTime == null
                            ? rightEdge
                            : CandleTime.AlignFromDateTime(step.CloseTime.Value, interval.Duration).ToUnixSeconds();

                        bool levelDrawn = true;

                        switch (part.Purpose)
                        {
                            case CryptoPartPurpose.Entry:
                                segments.Add(Horizontal(xStart, xEnd, step.Price, color,
                                    CaptionOnce("entry", step.Price)));
                                if (firstEntry == xStart && step.CloseTime.HasValue)
                                    firstEntry = stepEnd;
                                break;

                            case CryptoPartPurpose.Dca:
                                if (positionClosed && !stepFilled)
                                {
                                    levelDrawn = false;
                                    break;
                                }
                                segments.Add(Horizontal(stepStart, stepEnd, step.Price, color,
                                    CaptionOnce($"dca-{part.PartNumber}", step.Price)));
                                break;

                            case CryptoPartPurpose.TakeProfit:
                                // Including its stop legs: a take profit that never triggered on a
                                // finished trade says nothing, and neither does the stop that sat
                                // underneath it.
                                if (positionClosed && !stepFilled)
                                {
                                    levelDrawn = false;
                                    break;
                                }

                                segments.Add(Horizontal(stepStart, stepEnd, step.Price, color,
                                    CaptionOnce($"take profit-{part.PartNumber}", step.Price)));
                                if (step.StopPrice.HasValue)
                                    segments.Add(Horizontal(stepStart, stepEnd, step.StopPrice.Value, color,
                                        CaptionOnce("stop price", step.StopPrice.Value)));
                                if (step.StopLimitPrice.HasValue)
                                    segments.Add(Horizontal(stepStart, stepEnd, step.StopLimitPrice.Value, color,
                                        CaptionOnce("stop limit", step.StopLimitPrice.Value)));
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
                                color = step.Side == CryptoOrderSide.Buy ? "#ffeb3b" : "#ffffff",
                            });
                        }

                        // Keeps the vertical marker long enough to span every level of the position.
                        // Only for levels that are actually drawn, otherwise it reaches towards a
                        // line that is no longer there.
                        if (!levelDrawn)
                            continue;

                        if (step.Price > yTop)
                            yTop = step.Price;
                        if (step.Price < yBottom)
                            yBottom = step.Price;
                    }
                }

                // Two vertical dotted markers at the open time, one reaching up and one down, with
                // a gap around the candle so its wicks stay readable
                if (entry > 0)
                {
                    string positionColor = position.Side == CryptoTradeSide.Long ? "#006400" : "#8B0000";

                    decimal candleAbove = entry;
                    decimal candleBelow = entry;
                    var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    CandleTime openCandleTime = CandleTime.AlignFromDateTime(position.CreateTime, interval.Duration);
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

                    segments.Add(Vertical(xStart, boxAbove, candleAbove, positionColor, caption, captionPrice));
                    segments.Add(Vertical(xStart, boxBelow, candleBelow, positionColor));
                }

                // Break-even only while the position is open. Blue and without a caption: it sits
                // very close to the entry line, and two labels on top of each other read as noise.
                if (position.CloseTime == null && position.BreakEvenPrice > 0)
                    segments.Add(Horizontal(firstEntry, xEnd, position.BreakEvenPrice, "#4da3ff", ""));
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
