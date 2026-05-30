using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// SKELETON SMC (Smart Money Concepts) detector — first iteration, intended to be visualised
/// in the chart window and finetuned later. NOT yet wired into the periodic zone-calculation
/// pipeline; the chart window calls <see cref="Detect"/> directly when the user toggles the
/// "SMC zones" option.
///
/// What it currently produces:
///   - Bullish Order Blocks (CryptoTradeSide.Long): the last bearish candle (Close &lt; Open)
///     directly before a confirmed swing low — i.e. the candle that "absorbed" the supply
///     just before price reversed up.
///   - Bearish Order Blocks (CryptoTradeSide.Short): the last bullish candle (Close ≥ Open)
///     directly before a confirmed swing high — the candle that "absorbed" the demand just
///     before price reversed down.
///
/// Swing detection is the simplest possible (fractal): candle is a swing high/low if it is
/// the strict high/low of the [i-N, i+N] window. N is hard-coded for now; expose to Settings
/// once we've tuned it visually.
///
/// What's INTENTIONALLY missing for the first cut (to add later):
///   - Mitigation tracking (CE 50% touch) and TouchCount
///   - Liquidity sweep filter (only blocks that swept BSL/SSL first)
///   - Premium/Discount tagging using a Fib midpoint of the dominant leg
///   - BOS/CHoCH structure-event linkage
///   - DB persistence — these zones live in <see cref="CryptoSymbolInterval.SmcZones"/> only
///   - Per-zone realtime invalidation through <see cref="ZoneInvalidation"/>
/// </summary>
public static class ZoneSmc
{
    // Swing fractal window: candle i is a swing high/low if it's the strict high/low across
    // [i - SwingLookback, i + SwingLookback]. 5 is a reasonable starting point for most TFs.
    private const int SwingLookback = 5;

    // Maximum walk-back distance from a swing point when looking for the "last opposite
    // candle" that becomes the order block. Larger values catch deeper OBs but blur the
    // concept — keep this snug.
    private const int MaxWalkBack = 10;

    // Cap to keep the chart from being overloaded. Newest blocks are kept, oldest dropped
    // when the count would exceed this — change once we add real strength filtering.
    private const int MaxBlocksPerInterval = 50;

    /// <summary>
    /// Recompute the SMC Order Blocks for one (symbol, interval) from scratch and store
    /// them in <see cref="CryptoSymbolInterval.SmcZones"/>. Replaces whatever was there.
    /// Cheap enough to call from the chart toggle directly for the first iteration.
    /// </summary>
    public static void Detect(CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        // Snapshot to avoid enumerating a live collection while the scanner adds candles.
        symbolInterval.CandleList.Lock();
        List<CryptoCandle> candles;
        try
        {
            candles = [.. symbolInterval.CandleList.Values];
        }
        finally
        {
            symbolInterval.CandleList.Unlock();
        }

        List<CryptoZone> zones = [];

        // Need at least 2*lookback + 1 candles to find a single fractal swing.
        if (candles.Count < 2 * SwingLookback + 1)
        {
            symbolInterval.SmcZones = zones;
            return;
        }

        // Fractal swing scan — bounded so we always have full lookback windows on both sides.
        for (int i = SwingLookback; i < candles.Count - SwingLookback; i++)
        {
            CryptoCandle center = candles[i];

            bool isSwingHigh = true;
            bool isSwingLow = true;
            for (int j = i - SwingLookback; j <= i + SwingLookback; j++)
            {
                if (j == i)
                    continue;
                if (candles[j].High >= center.High)
                    isSwingHigh = false;
                if (candles[j].Low <= center.Low)
                    isSwingLow = false;
                if (!isSwingHigh && !isSwingLow)
                    break; // no point continuing this window
            }

            if (isSwingHigh)
            {
                // Bearish Order Block: walk back from i-1 looking for the last BULLISH
                // candle (Close >= Open) — that's the candle the market reversed off.
                int min = Math.Max(0, i - MaxWalkBack);
                for (int j = i - 1; j >= min; j--)
                {
                    CryptoCandle c = candles[j];
                    if (c.Close >= c.Open)
                    {
                        zones.Add(BuildOrderBlock(symbol, interval, c,
                            CryptoTradeSide.Short, $"SMC {interval.Name} bearish"));
                        break;
                    }
                }
            }
            else if (isSwingLow)
            {
                // Bullish Order Block: walk back from i-1 looking for the last BEARISH
                // candle (Close < Open).
                int min = Math.Max(0, i - MaxWalkBack);
                for (int j = i - 1; j >= min; j--)
                {
                    CryptoCandle c = candles[j];
                    if (c.Close < c.Open)
                    {
                        zones.Add(BuildOrderBlock(symbol, interval, c,
                            CryptoTradeSide.Long, $"SMC {interval.Name} bullish"));
                        break;
                    }
                }
            }
        }

        // First-cut invalidation: drop OBs that price has already body-closed clean through
        // on a LATER candle (above for bullish, below for bearish). Keeps the chart tidy
        // and matches the basic OB lifecycle. More nuanced touch/mitigation tracking can
        // replace this once we integrate ZoneInvalidation.
        ApplyBasicInvalidation(zones, candles);

        // Trim to the newest N so the chart doesn't get overwhelmed on long histories.
        if (zones.Count > MaxBlocksPerInterval)
        {
            // Keep the most recent blocks (highest OpenTime).
            zones.Sort((a, b) => a.OpenTime.Minutes.CompareTo(b.OpenTime.Minutes));
            zones.RemoveRange(0, zones.Count - MaxBlocksPerInterval);
        }

        symbolInterval.SmcZones = zones;
    }

    private static CryptoZone BuildOrderBlock(CryptoSymbol symbol, CryptoInterval interval,
        CryptoCandle source, CryptoTradeSide side, string description)
    {
        return new CryptoZone
        {
            ExchangeId = symbol.ExchangeId,
            Exchange = symbol.Exchange,
            SymbolId = symbol.Id,
            Symbol = symbol,
            IntervalId = interval.Id,
            Interval = interval,
            Kind = CryptoZoneKind.OrderBlock,
            Side = side,
            // No strength tiering yet — that's a finetuning step. Default to Strong so the
            // chart renders these at full opacity (DLZ filter would dim Weak blocks).
            Strength = CryptoZoneStrength.Strong,
            OpenTime = source.OpenTime,
            Top = source.High,
            Bottom = source.Low,
            IsValid = true,
            Description = description,
        };
    }

    /// <summary>
    /// First-pass invalidation: an OB is considered broken once a subsequent candle's CLOSE
    /// crosses through it the "wrong" way (close above the top for a bearish OB, close
    /// below the bottom for a bullish OB). Sets CloseTime so the chart renders it dimmer.
    /// Wick touches are ignored on purpose for now.
    /// </summary>
    private static void ApplyBasicInvalidation(List<CryptoZone> zones, List<CryptoCandle> candles)
    {
        foreach (var zone in zones)
        {
            // Find first candle after the OB's source candle.
            // candles is in OpenTime ascending order (CandleList is a SortedList).
            for (int k = 0; k < candles.Count; k++)
            {
                CryptoCandle c = candles[k];
                if (c.OpenTime.Minutes <= zone.OpenTime.Minutes)
                    continue;

                if (zone.Side == CryptoTradeSide.Short)
                {
                    // Bearish OB invalidated by a bullish close above the top of the block.
                    if (c.Close > zone.Top)
                    {
                        zone.CloseTime = c.OpenTime;
                        break;
                    }
                }
                else
                {
                    // Bullish OB invalidated by a bearish close below the bottom of the block.
                    if (c.Close < zone.Bottom)
                    {
                        zone.CloseTime = c.OpenTime;
                        break;
                    }
                }
            }
        }
    }
}
