using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Smc;

/// <summary>
/// SMC supply order block — short. Fires when price returns to the PROXIMAL edge (Bottom) of
/// a fresh/strong supply base zone. Entry is at the proximal edge, not the 50% midpoint, so a
/// shallow rejection into a large zone is not missed.
///
/// Zones are produced by <see cref="Zones.ZoneSmc"/> and live in
/// <see cref="CryptoSymbolInterval.SmcZones"/> (in-memory only). This class only reads them
/// and sets AlarmDate for proximity alarms — zone lifecycle (touch/mitigation/break) is
/// owned by ZoneSmc.
/// </summary>
public class SignalOrderBlockNearShort : SignalCreateBase
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

                // Supply zones only, still active (not broken), and past their open time.
                if (zone.Side != CryptoTradeSide.Short || zone.CloseTime != null)
                    continue;
                if (CandleLast.Candle.OpenTime < zone.OpenTime) // emulator..
                    continue;

                // Freshness / strength filters.
                if (settings.OnlyStrong && zone.Strength != CryptoZoneStrength.Strong)
                    continue;
                if (zone.TouchCount > settings.MaxTouches)
                    continue;

                // Proximal edge = Bottom for a supply zone. Allow price to be NearZonePercentage
                // below it and still trigger (approaching), but it must not have risen above the
                // distal edge (Top) — that would be a break.
                decimal alarmPrice = zone.Bottom * (100 - settings.NearZonePercentage) / 100;
                if (CandleLast.Candle.High >= alarmPrice && CandleLast.Candle.High <= zone.Top)
                {
                    if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                    {
                        result = true;
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
