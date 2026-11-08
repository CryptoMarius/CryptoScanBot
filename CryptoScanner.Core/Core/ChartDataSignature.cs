using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Everything a chart draws from live data, condensed into a string. Both charts (the Avalonia chart
/// window and the Photino chart page) poll it every 15 seconds and only redraw when it changed: a
/// full rebuild every tick would be wasteful, and a subscription on the candle and position events
/// would have to cover far too many sources. Deliberately cheap: no snapshot copy of the candle list
/// and no database query, just the counters and the last candle the scanner keeps in memory anyway.
/// </summary>
public static class ChartDataSignature
{
    /// <param name="windowStart">Start of a position window, null for the live chart.</param>
    /// <param name="windowEnd">End of a position window; null while that position is still open.</param>
    /// <param name="includePositions">Whether the positions are on the chart.</param>
    public static string Build(CryptoSymbol? symbol, CryptoIntervalPeriod period,
        DateTime? windowStart, DateTime? windowEnd, bool includePositions)
    {
        if (symbol == null)
            return "";

        // A CLOSED position is finished history: no candle arriving now can change any of it, so
        // the signature is constant and the timer never reloads it. An open one (windowEnd null)
        // still grows, and falls through to the live signature below.
        if (windowStart != null && windowEnd != null)
            return $"{symbol.Name}|{period}|{windowStart:O}|{windowEnd:O}";

        var text = new System.Text.StringBuilder();
        text.Append(symbol.Name).Append('|').Append(period).Append('|');

        var candleList = symbol.GetSymbolInterval(period).CandleList;
        var last = candleList.LastCandle;
        // Count catches a new candle, close catches the running one moving, high/low catch a
        // wick that grows without the close changing.
        text.Append(candleList.Count).Append('|')
            .Append(last.OpenTime).Append('|')
            .Append(last.Close).Append('|')
            .Append(last.High).Append('|')
            .Append(last.Low).Append('|');

        // And the same four for the 1m candle, because the running candle at the right edge is built
        // from that list (CandleTools.BuildRunningCandles). Nothing above moves while a 30m interval
        // is halfway through - the closed 30m candle those counters read stays exactly as it was - so
        // without this the chart would only pick the running candle up once the interval closed,
        // which is the very lag it exists to remove.
        if (period != CryptoIntervalPeriod.interval1m && !GlobalData.IsEmulatorMode)
        {
            var last1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.LastCandle;
            text.Append(last1m.OpenTime).Append('|')
                .Append(last1m.Close).Append('|')
                .Append(last1m.High).Append('|')
                .Append(last1m.Low).Append('|');
        }

        // Positions are drawn from the database, but the open list in memory is what changes
        // first: a fill updates UpdateTime, and a close moves the position out of this list.
        if (includePositions && symbol.Exchange != null)
        {
            foreach (CryptoPosition position in symbol.Exchange.Data.PositionList.Values)
            {
                if (position.Symbol != symbol)
                    continue;
                text.Append(position.Id).Append(':')
                    .Append((int)position.Status).Append(':')
                    .Append(position.Quantity).Append(':')
                    .Append(position.UpdateTime?.Ticks ?? 0).Append(',');
            }
        }

        return text.ToString();
    }
}
