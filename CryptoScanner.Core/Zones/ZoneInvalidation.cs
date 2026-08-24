using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// What a zone survives, and what uses it up. One implementation for all three kinds - DLZ, FVG and
/// the SMC order blocks - because they answer the same four questions and only disagree about how
/// far price has to come in. That difference is <see cref="ZoneTouchRules.TouchLevel"/>, a setting.
/// <para>
/// Per candle, in this order:
/// </para>
/// <list type="number">
///   <item><b>Broken</b> - the body closes through the FAR side of the zone. The zone is gone; price
///         went straight through instead of turning.</item>
///   <item><b>Entered</b> - the candle reaches the touch level while price was outside. That is one
///         visit: <see cref="CryptoZone.TouchCount"/> goes up by one and the zone is marked as
///         entered. Reaching the middle also sets <see cref="CryptoZone.ReachedMidpoint"/>.</item>
///   <item><b>Left</b> - price was inside and this candle no longer reaches the near edge. The zone
///         is open for a next visit.</item>
///   <item><b>Used up</b> - the visits reached MaxTouches. The zone is gone: every visit consumes
///         part of what made the level hold, and after a few there is nothing left to turn price.</item>
///   <item><b>Halfway in</b> - optional, off by default: price has been at or past the middle, so
///         the zone is gone whatever the visit count says.</item>
/// </list>
/// <para>
/// Counting per VISIT and not per candle is the whole point of rules 2 and 3. A visit that lasts
/// three candles is one test of the level, not three - and with MaxTouches at 2 the per-candle
/// version killed a zone halfway through its first visit. That is how the order-block side counted
/// it from the start (30-06-2026); this shared implementation only counted candles until 24-08-2026,
/// which is exactly the kind of split this class was named "single source of truth" to prevent.
/// </para>
/// <para>
/// TouchCount and ReachedMidpoint are persisted so they survive a scanner restart; the
/// <see cref="CryptoZone.LastInsideCandle"/> bookkeeping is not - after a restart the first candle inside a
/// zone counts as a fresh visit, which over-counts by at most one per zone.
/// </para>
/// </summary>
public static class ZoneInvalidation
{
    /// <summary>
    /// The two knobs, per zone kind. Passed in rather than read from the settings inside
    /// <see cref="ApplyToCandle"/> so the rule stays a pure function of (zone, candle, rules) and a
    /// test can state exactly what it is testing.
    /// </summary>
    /// <param name="MaxTouches">How many visits the zone survives. 0 means it is never used up and
    /// only a break can close it.</param>
    /// <param name="TouchLevel">How far price has to come in before a visit counts.</param>
    /// <param name="CloseAtMidpoint">Close the zone as soon as price has been at or past its middle,
    /// whatever the visit count says.</param>
    public readonly record struct ZoneTouchRules(int MaxTouches, CryptoZoneTouchLevel TouchLevel,
        bool CloseAtMidpoint);


    /// <summary>
    /// The rules for one kind of zone, from its own settings. This is where "one implementation,
    /// three settings" is actually wired: every caller asks for the rules of the zone it holds.
    /// </summary>
    public static ZoneTouchRules RulesFor(CryptoZoneKind kind)
    {
        return kind switch
        {
            CryptoZoneKind.FairValueGap => new(GlobalData.Settings.Signal.ZonesFvg.MaxTouches,
                                               GlobalData.Settings.Signal.ZonesFvg.TouchLevel,
                                               GlobalData.Settings.Signal.ZonesFvg.CloseZonesPastMidpoint),
            CryptoZoneKind.OrderBlock => new(GlobalData.Settings.Signal.ZonesSmc.MaxTouches,
                                             GlobalData.Settings.Signal.ZonesSmc.TouchLevel,
                                             GlobalData.Settings.Signal.ZonesSmc.CloseZonesPastMidpoint),
            _ => new(GlobalData.Settings.Signal.ZonesDlz.MaxTouches,
                     GlobalData.Settings.Signal.ZonesDlz.TouchLevel,
                     GlobalData.Settings.Signal.ZonesDlz.CloseZonesPastMidpoint),
        };
    }


    /// <summary>
    /// Apply one candle to one zone. Returns true when the zone is closed afterwards - either
    /// because this candle closed it, or because it was already closed on entry.
    /// </summary>
    /// <param name="zone">The zone. TouchCount, ReachedMidpoint, the visit bookkeeping and CloseTime may be
    /// changed.</param>
    /// <param name="candle">The candle to apply.</param>
    /// <param name="interval">The interval the zone lives on, for stamping CloseTime.</param>
    /// <param name="rules">See <see cref="ZoneTouchRules"/>.</param>
    public static bool ApplyToCandle(CryptoZone zone, CryptoCandle candle, CryptoInterval interval,
        ZoneTouchRules rules)
    {
        if (zone.CloseTime != null)
            return true;
        if (candle.OpenTime < zone.OpenTime)
            return false;
        // A zone can be told to start counting only after a given candle - the order blocks use this
        // to keep the base candles and the impulse's own wick out of their own count.
        if (zone.TouchCountingFrom != null && candle.OpenTime < zone.TouchCountingFrom.Value)
            return false;

        decimal midpoint = (zone.Top + zone.Bottom) / 2m;
        bool isLong = zone.Side == CryptoTradeSide.Long;

        // 1. Broken - the body closes through the far side.
        if (isLong ? candle.Close < zone.Bottom : candle.Close > zone.Top)
        {
            zone.CloseTime = candle.OpenTime + interval.Duration;
            return true;
        }

        // How deep this candle came in, expressed as two questions the rest of the method asks.
        bool reachedEdge = isLong ? candle.Low <= zone.Top : candle.High >= zone.Bottom;
        bool reachedMidpoint = isLong ? candle.Low <= midpoint : candle.High >= midpoint;
        bool reachedTouchLevel = rules.TouchLevel == CryptoZoneTouchLevel.Midpoint
            ? reachedMidpoint : reachedEdge;

        // The midpoint flag is about how far price came, not about counting, so it is set whenever it
        // happens - also on a candle that does not open a new visit.
        if (reachedMidpoint)
            zone.ReachedMidpoint = true;

        // 3. Left - and worked out from WHEN price was last seen inside, not from an exit candle.
        // The callers do not feed every candle: the broken-check loops break out as soon as a candle
        // cannot reach any zone, so the candle on which price left is often never applied here. A
        // visit is therefore over as soon as the last candle seen inside is more than one candle ago.
        // Measured against the EDGE and not against the touch level: with the midpoint as touch
        // level, price that pulls back to just inside the edge has not left, and counting its next
        // dip to the middle as a second visit would turn one long test into two.
        bool sameVisit = zone.LastInsideCandle != null
            && candle.OpenTime <= zone.LastInsideCandle.Value + interval.Duration;
        if (!sameVisit)
            zone.VisitCounted = false;
        if (reachedEdge)
            zone.LastInsideCandle = candle.OpenTime;

        // 2. Entered - one visit, counted once however many candles it lasts.
        if (reachedTouchLevel && !zone.VisitCounted)
        {
            zone.TouchCount++;
            zone.VisitCounted = true;

            // 4. Used up.
            if (rules.MaxTouches > 0 && zone.TouchCount >= rules.MaxTouches)
            {
                zone.CloseTime = candle.OpenTime + interval.Duration;
                return true;
            }
        }

        // 5. Halfway in - off by default. Price took half of what made the level hold, so what is
        // left is not worth waiting for. Independent of the visit count: this can close a zone on
        // its very first visit, and it also fires on a candle that only continues a visit.
        if (rules.CloseAtMidpoint && zone.ReachedMidpoint)
        {
            zone.CloseTime = candle.OpenTime + interval.Duration;
            return true;
        }

        return false;
    }
}
