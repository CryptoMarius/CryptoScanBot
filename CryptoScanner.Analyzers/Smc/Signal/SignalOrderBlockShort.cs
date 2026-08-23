using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Smc.Signal;

/// <summary>
/// SMC supply order block — short, TOUCH variant ("smc"). Fires when price actually enters a
/// fresh/strong supply base zone (a candle wicks into [Bottom, Top]). Mirror of
/// SignalDominantLevelShort, reading <see cref="CryptoSymbolIntervalSmc.Zones"/>.
///
/// The companion <see cref="SignalOrderBlockRejectionShort"/> ("smc.rejection") waits for the
/// confirmed rejection (close back outside the proximal edge) — the entry-grade signal.
/// </summary>
public class SignalOrderBlockShort : SignalCreateBase
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
            var zones = symbolIntervalData.Smc.Zones;
            for (int index = 0; index < zones.Count; index++)
            {
                var zone = zones[index];

                // Supply zones only, still active (not broken), past their open time.
                if (zone.Side != CryptoTradeSide.Short || zone.CloseTime != null)
                    continue;
                if (CandleLast.Candle.OpenTime < zone.OpenTime) // emulator..
                    continue;

                // Freshness / strength filters.
                if (settings.OnlyStrong && zone.Strength != CryptoZoneStrength.Strong)
                    continue;
                if (zone.TouchCount > settings.MaxTouches)
                    continue;

                // Touch: price has entered the zone band (wick inside [Bottom, Top]).
                if (CandleLast.Candle.High >= zone.Bottom && CandleLast.Candle.Low <= zone.Top)
                {
                    if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                    {
                        result = true;
                        Interval = interval; // Report different interval back
                        zone.AlarmDate = CandleLast.Candle.OpenTime;
                        decimal dist = 100m * (zone.Bottom - CandleLast.Candle.High) / CandleLast.Candle.Close;
                        ExtraText = $"{interval.Name} supply OB {zone.Bottom} .. {zone.Top} ({dist:N2}%) touches={zone.TouchCount}";
                    }
                }
            }
        }

        return result;
    }
}
