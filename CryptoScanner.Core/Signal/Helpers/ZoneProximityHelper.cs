using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Read-only proximity checks against the precomputed DLZ and FVG zone lists.
///
/// These helpers do NOT mutate zone state (no CloseTime, no AlarmDate, no removal from
/// the OrderedList). Zone lifecycle stays the responsibility of SignalDominantLevelNearLong/Short
/// and SignalFairValueGapLong/Short — the combined signals only piggy-back on the zone
/// state those algorithms keep current.
///
/// Both checks iterate the configured zone intervals (Settings.Signal.ZonesDlz.IntervalList /
/// ZonesFvg.IntervalList) and use the same proximity threshold (Settings.Signal.ZonesDlz.WarnPercentage)
/// — sharing the threshold avoids adding a new setting just for FVG; a future split is trivial.
/// </summary>
public static class ZoneProximityHelper
{
    /// <summary>
    /// Returns true when the signal's last candle is within WarnPercentage of an open DLZ zone
    /// (or already inside it) on the side that matches the signal direction.
    /// </summary>
    public static bool IsNearDlzZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        zoneInfo = "";
        var settings = GlobalData.Settings.Signal.ZonesDlz;
        var candle = myBase.CandleLast.Candle;
        var symbolData = myBase.Symbol.Data;
        decimal warnPercentage = settings.WarnPercentage;

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
                    decimal alarmPrice = zone.Top * (100m + warnPercentage) / 100m;
                    if (candle.Low <= alarmPrice)
                    {
                        decimal dist = 100m * (candle.Low - zone.Top) / candle.Close;
                        zoneInfo = $"dlz {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // Low above the warn-band — all subsequent zones have lower tops, so stop early
                    break;
                }
                else
                {
                    // Short zones provide resistance from above.
                    // ShortOpen is sorted on Zone.Bottom ASCENDING (lowest bottom first).
                    decimal alarmPrice = zone.Bottom * (100m - warnPercentage) / 100m;
                    if (candle.High >= alarmPrice)
                    {
                        decimal dist = 100m * (zone.Bottom - candle.High) / candle.Close;
                        zoneInfo = $"dlz {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    // High below the warn-band — all subsequent zones have higher bottoms, so stop early
                    break;
                }
            }
        }
        return false;
    }


    /// <summary>
    /// Returns true when the signal's last candle is within WarnPercentage of an open FVG zone
    /// (or already inside it) on the side that matches the signal direction.
    /// Shares the DLZ WarnPercentage setting — keeps things consistent without a new config field.
    /// </summary>
    public static bool IsNearFvgZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        zoneInfo = "";
        var settings = GlobalData.Settings.Signal.ZonesFvg;
        var candle = myBase.CandleLast.Candle;
        var symbolData = myBase.Symbol.Data;
        decimal warnPercentage = GlobalData.Settings.Signal.ZonesDlz.WarnPercentage;

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
                    decimal alarmPrice = zone.Top * (100m + warnPercentage) / 100m;
                    if (candle.Low <= alarmPrice)
                    {
                        // Guard: skip a stale zone that the candle has already fully passed through
                        if (candle.High < zone.Bottom)
                        {
                            index++;
                            continue;
                        }
                        decimal dist = 100m * (candle.Low - zone.Top) / candle.Close;
                        zoneInfo = $"fvg {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    break;
                }
                else
                {
                    // FVG short: zone is above current price, sorted on Zone.Bottom ASCENDING.
                    decimal alarmPrice = zone.Bottom * (100m - warnPercentage) / 100m;
                    if (candle.High >= alarmPrice)
                    {
                        if (candle.Low > zone.Top)
                        {
                            index++;
                            continue;
                        }
                        decimal dist = 100m * (zone.Bottom - candle.High) / candle.Close;
                        zoneInfo = $"fvg {intervalName} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        return true;
                    }
                    break;
                }
            }
        }
        return false;
    }
}
