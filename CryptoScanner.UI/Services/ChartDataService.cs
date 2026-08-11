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

    /// <summary>Vertical marker at one moment, running between two prices.</summary>
    private static ChartSegment Vertical(long time, decimal priceFrom, decimal priceTo, string color) => new()
    {
        time1 = time,
        time2 = time,
        price1 = (double)priceFrom,
        price2 = (double)priceTo,
        color = color,
        dash = 1, // Dot, as in Positions.DrawVerticalLine
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
                AddZones(result, dataInterval.DlzZones.LongOpen, from, to);
                AddZones(result, dataInterval.DlzZones.ShortOpen, from, to);
                AddZones(result, dataInterval.DlzZones.LongClosed, from, to);
                AddZones(result, dataInterval.DlzZones.ShortClosed, from, to);
            }
        }

        if (showFvg)
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
            {
                if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    continue;

                var dataInterval = symbolData.Get(interval.IntervalPeriod);
                AddZones(result, dataInterval.FvgZones.LongOpen, from, to);
                AddZones(result, dataInterval.FvgZones.ShortOpen, from, to);
                AddZones(result, dataInterval.FvgZones.LongClosed, from, to);
                AddZones(result, dataInterval.FvgZones.ShortClosed, from, to);
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
                AddZones(result, dataInterval.SmcZones.ToList(), from, to);
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

                // A closed position keeps every level it never reached. Those lines describe an
                // intention, not what happened, and on a finished trade they only make the picture
                // harder to read. While the position runs they are exactly what you want to see.
                bool positionClosed = position.CloseTime != null;

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
                                segments.Add(Horizontal(xStart, xEnd, step.Price, color, "entry"));
                                if (firstEntry == xStart && step.CloseTime.HasValue)
                                    firstEntry = stepEnd;
                                break;

                            case CryptoPartPurpose.Dca:
                                if (positionClosed && !stepFilled)
                                {
                                    levelDrawn = false;
                                    break;
                                }
                                segments.Add(Horizontal(stepStart, stepEnd, step.Price, color, $"dca-{part.PartNumber}"));
                                break;

                            case CryptoPartPurpose.TakeProfit:
                                segments.Add(Horizontal(stepStart, stepEnd, step.Price, color, $"take profit-{part.PartNumber}"));

                                // Same rule for the stop legs: on a finished trade only the one
                                // that actually triggered says anything
                                if (!positionClosed || stepFilled)
                                {
                                    if (step.StopPrice.HasValue)
                                        segments.Add(Horizontal(stepStart, stepEnd, step.StopPrice.Value, color, "stop price"));
                                    if (step.StopLimitPrice.HasValue)
                                        segments.Add(Horizontal(stepStart, stepEnd, step.StopLimitPrice.Value, color, "stop limit"));
                                }
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
                                time = CandleTime.AlignFromDateTime(step.CloseTime.Value, interval.Duration).ToUnixSeconds(),
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

                    segments.Add(Vertical(xStart, boxAbove, candleAbove, positionColor));
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
