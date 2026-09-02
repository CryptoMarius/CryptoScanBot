using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// "Only fire when this happens in a zone", shared by the strategies that ask for it.
/// <para>
/// It started as a private method of the candle-pattern strategy and moved here the moment
/// FailedBreakout wanted the same requirement. Shared as CODE and not as a settings block: both
/// strategies keep their own RequireZone and ZoneTolerancePercentage properties, because folding
/// them into one object would rename the paths the settings file and the emulator queue already
/// address them by.
/// </para>
/// </summary>
public static class ZoneRequirementHelper
{
    /// <summary>
    /// Whether the candle being evaluated touches an open zone of the same side, for every zone
    /// kind in <paramref name="requireZone"/>. Off (an empty list) means every candle passes.
    /// <para>
    /// "Touching" is the same test the three zone strategies use - the candle's range overlaps the
    /// zone band - so a candle that only pokes into the zone with its wick counts, which is exactly
    /// the case worth catching. The zone must already exist at this candle: without that check the
    /// emulator would read a zone that was only detected later, which is look-ahead.
    /// </para>
    /// </summary>
    public static bool InsideARequiredZone(this SignalCreateBase myBase, List<string> requireZone,
        decimal tolerancePercentage, out string reason)
    {
        reason = "";
        if (requireZone.Count == 0)
            return true;

        CryptoSymbolData symbolData = myBase.Symbol.Data;

        foreach (string kind in requireZone)
        {
            // Held as names rather than as the enum itself, for the same reason the pattern list is
            // - so read case-insensitively, because the settings file and the emulator queue spell
            // them in lower case by hand. An unknown name is a hard error: it would otherwise reject
            // every signal and read exactly like a strategy that produces nothing.
            if (!Enum.TryParse(kind, ignoreCase: true, out CryptoZoneSource source))
                throw new NotSupportedException($"{nameof(requireZone)} does not know "
                    + $"\"{kind}\" - use {string.Join(", ", Enum.GetNames<CryptoZoneSource>())}");
            string name = source.ToString().ToLowerInvariant();

            // IntervalList is declared per zone-settings class rather than on their shared base,
            // so the list and the zones are fetched per kind instead of through one interface.
            List<string> intervals = source switch
            {
                CryptoZoneSource.Dlz => GlobalData.Settings.Signal.ZonesDlz.IntervalList,
                CryptoZoneSource.Fvg => GlobalData.Settings.Signal.ZonesFvg.IntervalList,
                _ => GlobalData.Settings.Signal.ZonesSmc.IntervalList,
            };

            foreach (string intervalName in intervals)
            {
                if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                    continue;
                CryptoSymbolInterval symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // DLZ and FVG keep their zones split on side and state; SMC keeps one flat list.
                IEnumerable<CryptoZone> zones = source switch
                {
                    CryptoZoneSource.Dlz => myBase.SignalSide == CryptoTradeSide.Long
                        ? symbolIntervalData.Dlz.Zones.LongOpen : symbolIntervalData.Dlz.Zones.ShortOpen,
                    CryptoZoneSource.Fvg => myBase.SignalSide == CryptoTradeSide.Long
                        ? symbolIntervalData.Fvg.Zones.LongOpen : symbolIntervalData.Fvg.Zones.ShortOpen,
                    _ => symbolIntervalData.Smc.Zones,
                };

                foreach (CryptoZone zone in zones)
                {
                    if (!ZoneTools.Touches(zone, myBase.CandleLast!.Candle, myBase.SignalSide,
                            tolerancePercentage))
                        continue;
                    reason = $"in {name} {interval.Name} {zone.Bottom} .. {zone.Top}";
                    return true;
                }
            }
        }

        reason = $"not in a {string.Join("/", requireZone)} zone";
        return false;
    }
}
