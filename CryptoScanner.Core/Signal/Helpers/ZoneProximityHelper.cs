using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Read-only inside-or-near-zone checks against the precomputed DLZ and FVG zone lists.
///
/// A candle qualifies when its low (for long) or high (for short) is inside the zone OR
/// within <c>NearZonePercentage</c>% of the zone edge, AND the close has not broken through
/// the far side:
///   Long  : candle.Low  &lt;= zone.Top * (1 + NearZonePct/100) AND candle.Close &gt;= zone.Bottom
///   Short : candle.High &gt;= zone.Bottom * (1 - NearZonePct/100) AND candle.Close &lt;= zone.Top
///
/// NearZonePercentage is a dedicated setting separate from WarnPercentage (which is used by
/// SignalDominantLevelNearLong/Short for its "approaching zone" alarm timing).
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

        // Allow a marginal approach of NearZonePercentage% beyond the zone edge.
        // This is separate from WarnPercentage (used by SignalDominantLevelNearLong for its alarm).
        decimal warnPct = settings.NearZonePercentage;

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
                    // Qualifies when: low entered the zone OR is within WarnPct% above the top,
                    // AND close has not broken below the zone's floor (close >= bottom).
                    decimal toleranceTop = zone.Top * (100 + warnPct) / 100;
                    if (candle.Low > toleranceTop)
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
                    // Qualifies when: high entered the zone OR is within WarnPct% below the bottom,
                    // AND close has not broken above the zone's ceiling (close <= top).
                    decimal toleranceBottom = zone.Bottom * (100 - warnPct) / 100;
                    if (candle.High < toleranceBottom)
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

        // Allow a marginal approach of NearZonePercentage% beyond the zone edge.
        decimal warnPct = settings.NearZonePercentage;

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
                    // Qualifies when: low entered the zone OR is within WarnPct% above the top,
                    // AND close hasn't broken below the floor.
                    decimal toleranceTop = zone.Top * (100 + warnPct) / 100;
                    if (candle.Low > toleranceTop)
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
                    // Qualifies when: high entered the zone OR is within WarnPct% below the bottom,
                    // AND close hasn't broken above the ceiling.
                    decimal toleranceBottom = zone.Bottom * (100 - warnPct) / 100;
                    if (candle.High < toleranceBottom)
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


    /// <summary>
    /// Returns true when the most recent candle on the signal's interval shows a rejection
    /// off an open DLZ zone matching the signal direction. A rejection requires:
    ///   1) one of the last <c>RejectionLookback</c> candles wicked into the zone
    ///      (long: <c>Low &lt;= zone.Top + NearZonePct</c>; short: <c>High &gt;= zone.Bottom − NearZonePct</c>);
    ///   2) the CURRENT candle closes back outside the zone
    ///      (long: <c>Close &gt; zone.Top</c>; short: <c>Close &lt; zone.Bottom</c>);
    ///   3) the zone has not been exhausted (<c>TouchCount &lt; MaxTouches</c>) and
    ///      (optionally) is not mitigated past its 50% midpoint.
    /// </summary>
    public static bool WasRejectedAtDlzZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        var settings = GlobalData.Settings.Signal.ZonesDlz;
        return WasRejectedAtZone(myBase, settings.IntervalList,
            settings.NearZonePercentage, settings.MaxTouches, settings.RejectionLookback,
            settings.DisqualifyOnMitigation, weakZoneFilter: settings.ZoneStartApply,
            label: "dlz", isDlz: true, out zoneInfo);
    }


    /// <summary>
    /// Returns true when the most recent candle on the signal's interval shows a rejection
    /// off an open FVG zone matching the signal direction. See <see cref="WasRejectedAtDlzZone"/>
    /// for the rejection criteria.
    /// </summary>
    public static bool WasRejectedAtFvgZone(this SignalCreateBase myBase, out string zoneInfo)
    {
        var settings = GlobalData.Settings.Signal.ZonesFvg;
        return WasRejectedAtZone(myBase, settings.IntervalList,
            settings.NearZonePercentage, settings.MaxTouches, settings.RejectionLookback,
            settings.DisqualifyOnMitigation, weakZoneFilter: false,
            label: "fvg", isDlz: false, out zoneInfo);
    }


    // Shared rejection check used by both DLZ and FVG. Iterates each enabled interval and
    // each open zone on the signal side, applying the test+close-back-outside criteria.
    private static bool WasRejectedAtZone(SignalCreateBase myBase, List<string> intervalList,
        decimal nearZonePct, int maxTouches, int rejectionLookback, bool disqualifyOnMitigation,
        bool weakZoneFilter, string label, bool isDlz, out string zoneInfo)
    {
        zoneInfo = "";
        var candle = myBase.CandleLast.Candle;
        var symbolData = myBase.Symbol.Data;

        // Clamp lookback to at least 1 (only current candle).
        if (rejectionLookback < 1) rejectionLookback = 1;

        foreach (var intervalName in intervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
            var openZones = myBase.SignalSide == CryptoTradeSide.Long
                ? (isDlz ? symbolIntervalData.DlzZones.LongOpen : symbolIntervalData.FvgZones.LongOpen)
                : (isDlz ? symbolIntervalData.DlzZones.ShortOpen : symbolIntervalData.FvgZones.ShortOpen);

            int index = 0;
            while (index < openZones.Count)
            {
                var zone = openZones[index];

                if (candle.OpenTime < zone.OpenTime)
                {
                    index++;
                    continue;
                }

                // Honour the weak-zone filter used by the dlz.near algorithm (DLZ only).
                if (weakZoneFilter && zone.Strength == CryptoZoneStrength.Weak)
                {
                    index++;
                    continue;
                }

                // Zone is exhausted by repeated touches — supply has been consumed.
                if (maxTouches > 0 && zone.TouchCount >= maxTouches)
                {
                    index++;
                    continue;
                }

                // Optional ICT-style filter: mitigated past 50% midpoint.
                if (disqualifyOnMitigation && zone.IsMitigated)
                {
                    index++;
                    continue;
                }

                if (myBase.SignalSide == CryptoTradeSide.Long)
                {
                    // LongOpen sorted on Zone.Top DESCENDING — once the candle is well above
                    // the highest remaining zone we can stop looking.
                    decimal toleranceTop = zone.Top * (100 + nearZonePct) / 100;
                    if (candle.Low > toleranceTop)
                        break;

                    // Rejection condition for the CURRENT candle: close back above the zone top.
                    if (candle.Close <= zone.Top)
                    {
                        index++;
                        continue;
                    }

                    // Test condition: at least one of the last RejectionLookback candles
                    // (including this one) must have pierced the zone with its low.
                    if (HasWickIntoZoneLong(myBase, candle, zone, toleranceTop, rejectionLookback))
                    {
                        decimal dist = 100m * (candle.Close - zone.Top) / candle.Close;
                        zoneInfo = $"{label} {intervalName} rejection {zone.Description} " +
                            $"{zone.Bottom} .. {zone.Top} touches={zone.TouchCount} " +
                            $"mitig={zone.IsMitigated} ({dist:N2}% above)";
                        return true;
                    }
                    index++;
                }
                else
                {
                    // ShortOpen sorted on Zone.Bottom ASCENDING.
                    decimal toleranceBottom = zone.Bottom * (100 - nearZonePct) / 100;
                    if (candle.High < toleranceBottom)
                        break;

                    if (candle.Close >= zone.Bottom)
                    {
                        index++;
                        continue;
                    }

                    if (HasWickIntoZoneShort(myBase, candle, zone, toleranceBottom, rejectionLookback))
                    {
                        decimal dist = 100m * (zone.Bottom - candle.Close) / candle.Close;
                        zoneInfo = $"{label} {intervalName} rejection {zone.Description} " +
                            $"{zone.Bottom} .. {zone.Top} touches={zone.TouchCount} " +
                            $"mitig={zone.IsMitigated} ({dist:N2}% below)";
                        return true;
                    }
                    index++;
                }
            }
        }
        return false;
    }


    // Walk back up to lookback candles (including current) looking for a wick into the zone.
    private static bool HasWickIntoZoneLong(SignalCreateBase myBase, CryptoCandle current,
        CryptoZone zone, decimal toleranceTop, int lookback)
    {
        // Current candle first
        if (current.Low <= toleranceTop)
            return true;

        if (lookback <= 1)
            return false;

        var candleList = myBase.SymbolInterval.CandleList;
        CandleTime t = current.OpenTime - myBase.Interval.Duration;
        for (int i = 1; i < lookback; i++)
        {
            if (!candleList.TryGetValue(t, out CryptoCandle prev))
                return false;
            if (prev.OpenTime < zone.OpenTime)
                return false;
            if (prev.Low <= toleranceTop)
                return true;
            t -= myBase.Interval.Duration;
        }
        return false;
    }


    private static bool HasWickIntoZoneShort(SignalCreateBase myBase, CryptoCandle current,
        CryptoZone zone, decimal toleranceBottom, int lookback)
    {
        if (current.High >= toleranceBottom)
            return true;

        if (lookback <= 1)
            return false;

        var candleList = myBase.SymbolInterval.CandleList;
        CandleTime t = current.OpenTime - myBase.Interval.Duration;
        for (int i = 1; i < lookback; i++)
        {
            if (!candleList.TryGetValue(t, out CryptoCandle prev))
                return false;
            if (prev.OpenTime < zone.OpenTime)
                return false;
            if (prev.High >= toleranceBottom)
                return true;
            t -= myBase.Interval.Duration;
        }
        return false;
    }
}
