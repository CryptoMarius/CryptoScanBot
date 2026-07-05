using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// Single source of truth for FVG/DLZ zone invalidation. Implements the loosened rule:
/// a zone is only "broken" when a candle closes through the far side (body break).
/// A mere wick into the zone counts as a TEST and increments <see cref="CryptoZone.TouchCount"/>.
/// Once TouchCount reaches MaxTouches the zone is considered exhausted and closed as well.
///
/// Theory background:
///   - Supply/demand school (Seiden et al.): each touch consumes part of the unfilled
///     institutional order book sitting at the zone. After 2–3 touches the remaining
///     liquidity is depleted and the zone is no longer a high-probability reversal area.
///   - ICT: consequent encroachment (CE, 50% midpoint) marks the FVG as mitigated.
///
/// Touch counting and mitigation flags are persisted to the DB on <see cref="CryptoZone"/>
/// so they survive scanner restarts. On restart, only candles after LastZoneCheckTime
/// are replayed to catch up on missed touches and breaks.
/// </summary>
public static class ZoneInvalidation
{
    /// <summary>
    /// Apply the invalidation rule for a single candle to a single zone.
    /// Returns true when the candle caused the zone to close (CloseTime set in this call
    /// or already set on entry).
    /// </summary>
    /// <param name="zone">Zone to evaluate. Its <see cref="CryptoZone.TouchCount"/>,
    /// <see cref="CryptoZone.IsMitigated"/> and <see cref="CryptoZone.CloseTime"/> may be mutated.</param>
    /// <param name="candle">Candle to test against the zone.</param>
    /// <param name="interval">Interval the zone lives on (for CloseTime stamping).</param>
    /// <param name="maxTouches">Maximum number of wick-touches before the zone is closed
    /// as "exhausted". Pass 0 to disable touch-based closure.</param>
    public static bool ApplyToCandle(CryptoZone zone, CryptoCandle candle, CryptoInterval interval, int maxTouches)
    {
        if (zone.CloseTime != null)
            return true;
        if (candle.OpenTime < zone.OpenTime)
            return false;

        decimal midpoint = (zone.Top + zone.Bottom) / 2m;

        if (zone.Side == CryptoTradeSide.Long)
        {
            // Body close through the floor — zone is genuinely broken.
            if (candle.Close < zone.Bottom)
            {
                zone.CloseTime = candle.OpenTime + interval.Duration;
                return true;
            }

            // Wick into the zone (low pierced the top, but the body did not break through).
            // Count this as a test of the zone.
            if (candle.Low <= zone.Top)
            {
                zone.TouchCount++;
                if (candle.Low <= midpoint)
                    zone.IsMitigated = true;

                if (maxTouches > 0 && zone.TouchCount >= maxTouches)
                {
                    zone.CloseTime = candle.OpenTime + interval.Duration;
                    return true;
                }
            }
        }
        else // Short
        {
            // Body close through the ceiling — zone is genuinely broken.
            if (candle.Close > zone.Top)
            {
                zone.CloseTime = candle.OpenTime + interval.Duration;
                return true;
            }

            // Wick into the zone (high pierced the bottom, but the body did not break through).
            if (candle.High >= zone.Bottom)
            {
                zone.TouchCount++;
                if (candle.High >= midpoint)
                    zone.IsMitigated = true;

                if (maxTouches > 0 && zone.TouchCount >= maxTouches)
                {
                    zone.CloseTime = candle.OpenTime + interval.Duration;
                    return true;
                }
            }
        }

        return false;
    }
}
