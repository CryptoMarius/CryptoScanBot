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

    public static List<ChartMarker> BuildSignalMarkers(CryptoSymbol symbol, CandleTime from, CandleTime to)
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
                    time = CandleTime.FromDateTime(signal.CloseDate).ToUnixSeconds(),
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

    public static void BuildPositionOverlays(CryptoSymbol symbol, CandleTime from, CandleTime to,
        List<ChartMarker> markers, List<ChartPriceLine> priceLines)
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
                if (position.IntervalId != null && GlobalData.IntervalListId.TryGetValue((int)position.IntervalId, out CryptoInterval? interval))
                    position.Interval = interval;

                positions.Add(position);
                PositionTools.LoadPosition(database, position);
            }

            foreach (CryptoPosition position in positions)
            {
                foreach (CryptoPositionPart part in position.PartList.Values)
                {
                    foreach (var step in part.StepList.Values)
                    {
                        string color = step.Side == CryptoOrderSide.Buy ? "#2e7d32" : "#c62828";

                        switch (part.Purpose)
                        {
                            case CryptoPartPurpose.Entry:
                                priceLines.Add(new ChartPriceLine
                                {
                                    price = (double)step.Price,
                                    color = color,
                                    title = "entry",
                                });
                                break;

                            case CryptoPartPurpose.Dca:
                                priceLines.Add(new ChartPriceLine
                                {
                                    price = (double)step.Price,
                                    color = color,
                                    title = $"dca-{part.PartNumber}",
                                });
                                break;

                            case CryptoPartPurpose.TakeProfit:
                                priceLines.Add(new ChartPriceLine
                                {
                                    price = (double)step.Price,
                                    color = color,
                                    title = $"take profit-{part.PartNumber}",
                                });
                                if (step.StopPrice.HasValue)
                                {
                                    priceLines.Add(new ChartPriceLine
                                    {
                                        price = (double)step.StopPrice.Value,
                                        color = "#ff9800",
                                        title = "stop price",
                                    });
                                }
                                break;
                        }

                        if (step.CloseTime.HasValue)
                        {
                            markers.Add(new ChartMarker
                            {
                                time = CandleTime.FromDateTime(step.CloseTime.Value).ToUnixSeconds(),
                                position = step.Side == CryptoOrderSide.Buy ? "belowBar" : "aboveBar",
                                color = step.Side == CryptoOrderSide.Buy ? "#00e676" : "#ffffff",
                                shape = "circle",
                                text = part.Purpose == CryptoPartPurpose.Entry ? "E"
                                     : part.Purpose == CryptoPartPurpose.Dca ? "D" : "TP",
                            });
                        }
                    }
                }

                if (position.CloseTime == null && position.BreakEvenPrice > 0)
                {
                    priceLines.Add(new ChartPriceLine
                    {
                        price = (double)position.BreakEvenPrice,
                        color = "#9e9e9e",
                        title = "breakeven",
                    });
                }
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
