using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Read-only inside-zone checks against the precomputed DLZ and FVG zone lists.
///
/// A candle is considered "inside" a zone when its low (for long) or high (for short)
/// has entered the zone price range AND the close has not broken through the far side:
///   Long  : candle.Low  &lt;= zone.Top    AND candle.Close &gt;= zone.Bottom
///   Short : candle.High &gt;= zone.Bottom AND candle.Close &lt;= zone.Top
///
/// These helpers do NOT mutate zone state (no CloseTime, no AlarmDate, no removal from
/// the OrderedList). Zone lifecycle stays the responsibility of SignalDominantLevelNearLong/Short
/// and SignalFairValueGapLong/Short — the combined signals only piggy-back on the zone
/// state those algorithms keep current.
/// </summary>
public static class ZoneProximityHelper
{
    /// <summary>
    /// Returns true when the signal's last candle is inside an open DLZ zone on the side
    /// that matches the signal direction.
    /// "Inside" means the candle has entered the zone (wick or body) without closing through it.
    /// </summary>
    public static bool IsInsideDlzZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        zoneInfo = "";
        var settings = GlobalData.Settings.Signal.ZonesDlz;
        var candle = myBase.CandleLast.Candle;
        var symbolData = myBase.Symbol.Data;

        foreach (var intervalName in settings.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
            // Capture reference so a concurrent DlzZones swap mid-loop does not cause IndexOutOfRangeException
            var openZones = myBase.SignalSide == CryptoTradeSide.Long
                ? symbolIntervalData.DlzZones.LongOpen
                : symbolIntervalData.DlzZones.ShortOpen;

            int index = 0;
            while (index < openZones.Count)
            {
                var zone = openZones[index];

                // Zone created after the current candle — skip (emulator/backtest safety)
                if (candle.OpenTime < zone.OpenTime)
                {
                    index++;
                    continue;
                }

                // Honour the same weak-zone filter the dlz.near algorithm uses
                if (settings.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                {
                    index++;
                    continue;
                }

                if (myBase.SignalSide == CryptoTradeSide.Long)
                {
                    // Long zones provide support from below.
                    // LongOpen is sorted on Zone.Top DESCENDING (highest top first).
                    // Inside: candle entered the zone from above (low <= top) and close has
                    // not broken below the zone's floor (close >= bottom).
                    if (candle.Low > zone.Top)
                        break; // All subsequent zones have lower tops — stop early

                    if (candle.Close >= zone.Bottom)
                    {
                        decimal dist = 100m * (candle.Close - zone.Top) / candle.Close;
                        zoneInfo = $"dlz {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // Close broke below the zone floor — zone may have failed; check next
                    index++;
                }
                else
                {
                    // Short zones provide resistance from above.
                    // ShortOpen is sorted on Zone.Bottom ASCENDING (lowest bottom first).
                    // Inside: candle entered the zone from below (high >= bottom) and close has
                    // not broken above the zone's ceiling (close <= top).
                    if (candle.High < zone.Bottom)
                        break; // All subsequent zones have higher bottoms — stop early

                    if (candle.Close <= zone.Top)
                    {
                        decimal dist = 100m * (zone.Bottom - candle.Close) / candle.Close;
                        zoneInfo = $"dlz {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // Close broke above the zone ceiling — zone may have failed; check next
                    index++;
                }
            }
        }
        return false;
    }


    /// <summary>
    /// Returns true when the signal's last candle is inside an open FVG zone on the side
    /// that matches the signal direction. Uses the same inside-zone definition as
    /// <see cref="IsInsideDlzZone"/>: entered (wick) without closing through the far side.
    /// </summary>
    public static bool IsInsideFvgZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        zoneInfo = "";
        var settings = GlobalData.Settings.Signal.ZonesFvg;
        var candle = myBase.CandleLast.Candle;
        var symbolData = myBase.Symbol.Data;

        foreach (var intervalName in settings.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
            // Capture reference so a concurrent FvgZones swap mid-loop does not cause IndexOutOfRangeException
            var openZones = myBase.SignalSide == CryptoTradeSide.Long
                ? symbolIntervalData.FvgZones.LongOpen
                : symbolIntervalData.FvgZones.ShortOpen;

            int index = 0;
            while (index < openZones.Count)
            {
                var zone = openZones[index];

                if (candle.OpenTime < zone.OpenTime)
                {
                    index++;
                    continue;
                }

                if (myBase.SignalSide == CryptoTradeSide.Long)
                {
                    // FVG long: zone is below current price, sorted on Zone.Top DESCENDING.
                    // Inside: low entered the zone AND close hasn't broken below the floor.
                    if (candle.Low > zone.Top)
                        break; // All subsequent zones have lower tops — stop early

                    if (candle.Close >= zone.Bottom)
                    {
                        decimal dist = 100m * (candle.Close - zone.Top) / candle.Close;
                        zoneInfo = $"fvg {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // Close broke below the zone floor — skip this zone
                    index++;
                }
                else
                {
                    // FVG short: zone is above current price, sorted on Zone.Bottom ASCENDING.
                    // Inside: high entered the zone AND close hasn't broken above the ceiling.
                    if (candle.High < zone.Bottom)
                        break; // All subsequent zones have higher bottoms — stop early

                    if (candle.Close <= zone.Top)
                    {
                        decimal dist = 100m * (zone.Bottom - candle.Close) / candle.Close;
                        zoneInfo = $"fvg {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // Close broke above the zone ceiling — skip this zone
                    index++;
                }
            }
        }
        return false;
    }
}
