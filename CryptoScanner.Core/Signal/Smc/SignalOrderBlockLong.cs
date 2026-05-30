using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Smc;

/// <summary>
/// SMC demand order block — long, TOUCH variant ("smc"). Fires when price actually enters a
/// fresh/strong demand base zone (a candle wicks into [Bottom, Top]). Mirror of
/// SignalDominantLevelLong, reading <see cref="CryptoSymbolInterval.SmcZones"/>.
///
/// The companion <see cref="SignalOrderBlockNearLong"/> ("smc.near") fires earlier, while
/// price is still approaching the proximal edge.
/// </summary>
public class SignalOrderBlockLong : SignalCreateBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;

        var settings = GlobalData.Settings.Signal.ZonesSmc;
        CryptoSymbolData symbolData = Symbol.Data;

        foreach (var intervalName in settings.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

            // Capture reference so a concurrent SmcZones swap mid-loop is safe.
            var zones = symbolIntervalData.SmcZones;
            for (int index = 0; index < zones.Count; index++)
            {
                var zone = zones[index];

                // Demand zones only, still active (not broken), past their open time.
                if (zone.Side != CryptoTradeSide.Long || zone.CloseTime != null)
                    continue;
                if (CandleLast.Candle.OpenTime < zone.OpenTime) // emulator..
                    continue;

                // Freshness / strength filters.
                if (settings.OnlyStrong && zone.Strength != CryptoZoneStrength.Strong)
                    continue;
                if (zone.TouchCount > settings.MaxTouches)
                    continue;

                // Touch: price has entered the zone band (wick inside [Bottom, Top]).
                if (CandleLast.Candle.Low <= zone.Top && CandleLast.Candle.High >= zone.Bottom)
                {
                    if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                    {
                        result = true;
                        zone.AlarmDate = CandleLast.Candle.OpenTime;
                        decimal dist = 100m * (CandleLast.Candle.Low - zone.Top) / CandleLast.Candle.Close;
                        ExtraText = $"{interval.Name} demand OB {zone.Bottom} .. {zone.Top} ({dist:N2}%) touches={zone.TouchCount}";
                    }
                }
            }
        }

        return result;
    }
}
