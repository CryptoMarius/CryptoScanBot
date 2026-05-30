using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// SMC (Smart Money Concepts) supply/demand detector — base + expansion model. Intended to be
/// visualised in the chart window and finetuned later. NOT yet wired into the periodic
/// zone-calculation pipeline; the chart window calls <see cref="Detect"/> directly when the
/// user toggles the "SMC zones" option.
///
/// Anatomy modelled (classic supply/demand: RBR / DBR / RBD / DBD):
///   1. BASE      — a short cluster of small "consolidation" candles (range below average).
///                  This IS the zone: the area price is expected to react to on a later return.
///   2. EXPANSION — a strong impulsive candle leaving the base (range well above average).
///                  Its size grades the zone's strength; its direction sets the side:
///                    impulse UP   after a base → DEMAND zone (Long,  bounce up expected)
///                    impulse DOWN after a base → SUPPLY zone (Short, rejection down expected)
///
/// A zone is only created at a base→expansion transition (the impulse candle's predecessor must
/// itself be a base candle). That naturally dedupes long trend runs and only marks the genuine
/// departure points, which is what keeps the chart readable.
///
/// The zone price band is the full base range [min low, max high of the base candles]; OpenTime
/// is the first base candle. (A tighter proximal/distal body-based band is a later refinement.)
///
/// What's INTENTIONALLY still missing (to add later):
///   - Mitigation tracking (CE 50% touch) and TouchCount — we only do a hard break invalidation
///   - Liquidity sweep filter (only zones that swept BSL/SSL first)
///   - Premium/Discount tagging using a Fib midpoint of the dominant leg
///   - BOS/CHoCH structure-event linkage
///   - DB persistence — these zones live in <see cref="CryptoSymbolInterval.SmcZones"/> only
///   - Per-zone realtime invalidation through <see cref="ZoneInvalidation"/>
/// </summary>
public static class ZoneSmc
{
    // All tuning knobs live in Settings.Signal.ZonesSmc (appsettings.json) so they can be
    // finetuned without a rebuild — see SettingsSignalStrategySmc.

    /// <summary>
    /// Recompute the SMC supply/demand zones for one (symbol, interval) from scratch and store
    /// them in <see cref="CryptoSymbolInterval.SmcZones"/>. Replaces whatever was there.
    /// Cheap enough to call from the chart toggle or the zone worker directly.
    /// </summary>
    public static void Detect(CryptoSymbol symbol, CryptoInterval interval)
    {
        var settings = GlobalData.Settings.Signal.ZonesSmc;
        int averageWindow = Math.Max(2, settings.AverageWindow);
        decimal baseMaxRangeFactor = settings.BaseMaxRangeFactor;
        decimal expansionMinRangeFactor = settings.ExpansionMinRangeFactor;
        decimal expansionBodyFraction = settings.ExpansionBodyFraction;
        decimal strongExpansionFactor = settings.StrongExpansionFactor;
        int baseMaxCandles = Math.Max(1, settings.BaseMaxCandles);
        int maxBlocksPerInterval = Math.Max(1, settings.MaxBlocksPerInterval);

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

        // Need at least a full average window plus one impulse candle to do anything useful.
        if (candles.Count < averageWindow + 2)
        {
            symbolInterval.SmcZones = zones;
            return;
        }

        // Prefix sums of candle range (High-Low) for O(1) trailing averages.
        // prefix[k] = sum of ranges of candles[0..k-1].
        decimal[] prefix = new decimal[candles.Count + 1];
        for (int k = 0; k < candles.Count; k++)
            prefix[k + 1] = prefix[k] + (candles[k].High - candles[k].Low);

        // Scan for base→expansion transitions. Start at averageWindow so every candle has a
        // full trailing window to measure against.
        for (int i = averageWindow; i < candles.Count; i++)
        {
            decimal avgRange = AverageRange(prefix, i, averageWindow);
            if (avgRange <= 0)
                continue;

            CryptoCandle impulse = candles[i];
            decimal range = impulse.High - impulse.Low;
            decimal body = Math.Abs(impulse.Close - impulse.Open);

            // 1) Is candle i an expansion (impulsive leg-out)?
            bool isExpansion = range >= expansionMinRangeFactor * avgRange
                && body >= expansionBodyFraction * range;
            if (!isExpansion)
                continue;

            // 2) Does it depart from a base? The immediately preceding candle must be small.
            int b = i - 1;
            if (b < 0 || (candles[b].High - candles[b].Low) > baseMaxRangeFactor * avgRange)
                continue;

            // 3) Walk back over the consecutive small candles to capture the whole base.
            int baseEnd = b;          // last (newest) base candle, adjacent to the impulse
            int baseStart = b;        // will move backwards
            int collected = 1;
            while (baseStart - 1 >= 0
                && collected < baseMaxCandles
                && (candles[baseStart - 1].High - candles[baseStart - 1].Low) <= baseMaxRangeFactor * avgRange)
            {
                baseStart--;
                collected++;
            }

            // 4) Base price band = full range across the base candles.
            decimal top = decimal.MinValue;
            decimal bottom = decimal.MaxValue;
            for (int j = baseStart; j <= baseEnd; j++)
            {
                if (candles[j].High > top)
                    top = candles[j].High;
                if (candles[j].Low < bottom)
                    bottom = candles[j].Low;
            }

            // 5) Direction + strength from the expansion.
            bool up = impulse.Close >= impulse.Open;
            CryptoTradeSide side = up ? CryptoTradeSide.Long : CryptoTradeSide.Short;
            CryptoZoneStrength strength = range >= strongExpansionFactor * avgRange
                ? CryptoZoneStrength.Strong
                : CryptoZoneStrength.Weak;

            zones.Add(BuildZone(symbol, interval, candles[baseStart].OpenTime, top, bottom,
                side, strength, interval.Name));
        }

        // Mitigation + touch-counting + break invalidation in one pass per zone.
        ApplyMitigationAndInvalidation(zones, candles);

        // Trim to the newest N so the chart doesn't get overwhelmed on long histories.
        if (zones.Count > maxBlocksPerInterval)
        {
            zones.Sort((a, b) => a.OpenTime.Minutes.CompareTo(b.OpenTime.Minutes));
            zones.RemoveRange(0, zones.Count - maxBlocksPerInterval);
        }

        symbolInterval.SmcZones = zones;
    }

    /// <summary>
    /// Mean candle range (High-Low) over the trailing averageWindow candles ending just
    /// before index i, using the prefix-sum table for O(1) lookup.
    /// </summary>
    private static decimal AverageRange(decimal[] prefix, int i, int averageWindow)
    {
        int start = i - averageWindow;
        decimal sum = prefix[i] - prefix[start];
        return sum / averageWindow;
    }

    private static CryptoZone BuildZone(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime openTime, decimal top, decimal bottom, CryptoTradeSide side,
        CryptoZoneStrength strength, string description)
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
            Strength = strength,
            OpenTime = openTime,
            Top = top,
            Bottom = bottom,
            IsValid = true,
            Description = description,
        };
    }

    /// <summary>
    /// Single backward-looking pass per zone that fills three things from the candles AFTER
    /// the zone was formed:
    ///
    ///   • TouchCount  — number of separate excursions in which price reached the zone's 50%
    ///                   midpoint (Consequent Encroachment / CE). A "touch" only counts once
    ///                   per excursion: price must first LEAVE the zone again (back past the
    ///                   proximal edge) before a return can count as the next touch. This is
    ///                   the supply/demand "freshness" gauge: 0 = fresh, 1 = tested, 2+ = used.
    ///   • IsMitigated — true as soon as TouchCount >= 1 (price has reached CE at least once).
    ///   • CloseTime   — set when price BREAKS the zone: a close beyond the distal edge
    ///                   (below the bottom for a demand zone, above the top for a supply zone).
    ///                   Counting stops at the break — a broken zone is dead.
    ///
    /// Note on entry vs mitigation: CE (50%) is used for the freshness bookkeeping here, NOT
    /// as an entry trigger. Entry happens at the PROXIMAL edge (zone.Top for demand,
    /// zone.Bottom for supply) so a shallow bounce that only dips a few percent into a large
    /// zone is not missed. That entry logic will live in the (future) signal class; this
    /// method only records the analytics.
    /// </summary>
    private static void ApplyMitigationAndInvalidation(List<CryptoZone> zones, List<CryptoCandle> candles)
    {
        foreach (var zone in zones)
        {
            decimal ce = (zone.Top + zone.Bottom) / 2m;  // 50% midpoint (Consequent Encroachment)
            bool insideExcursion = false;                 // currently within a CE excursion?

            // candles is in OpenTime ascending order (CandleList is a SortedList).
            for (int k = 0; k < candles.Count; k++)
            {
                CryptoCandle c = candles[k];
                if (c.OpenTime.Minutes <= zone.OpenTime.Minutes)
                    continue;

                if (zone.Side == CryptoTradeSide.Short)
                {
                    // Supply zone: price approaches from BELOW (proximal = Bottom, distal = Top).
                    // Break first — a close above the top kills the zone.
                    if (c.Close > zone.Top)
                    {
                        zone.CloseTime = c.OpenTime;
                        break;
                    }

                    // CE touch: a wick reaching up to the 50% midpoint.
                    if (!insideExcursion && c.High >= ce)
                    {
                        zone.TouchCount++;
                        zone.IsMitigated = true;
                        insideExcursion = true;
                    }
                    // Excursion ends once price drops back below the proximal edge (left the zone).
                    else if (insideExcursion && c.High < zone.Bottom)
                    {
                        insideExcursion = false;
                    }
                }
                else
                {
                    // Demand zone: price approaches from ABOVE (proximal = Top, distal = Bottom).
                    // Break first — a close below the bottom kills the zone.
                    if (c.Close < zone.Bottom)
                    {
                        zone.CloseTime = c.OpenTime;
                        break;
                    }

                    // CE touch: a wick reaching down to the 50% midpoint.
                    if (!insideExcursion && c.Low <= ce)
                    {
                        zone.TouchCount++;
                        zone.IsMitigated = true;
                        insideExcursion = true;
                    }
                    // Excursion ends once price rises back above the proximal edge (left the zone).
                    else if (insideExcursion && c.Low > zone.Top)
                    {
                        insideExcursion = false;
                    }
                }
            }
        }
    }
}
