using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Smc;

/// <summary>
/// SMC demand order block — long. Fires when price returns to the PROXIMAL edge (Top) of a
/// fresh/strong demand base zone. Entry is deliberately at the proximal edge, not the 50%
/// midpoint, so a shallow bounce into a large zone is not missed.
///
/// Zones are produced by <see cref="Zones.ZoneSmc"/> and live in
/// <see cref="CryptoSymbolInterval.SmcZones"/> (in-memory only). This class only reads them
/// and sets AlarmDate for proximity alarms — zone lifecycle (touch/mitigation/break) is
/// owned by ZoneSmc.
/// </summary>
public class SignalOrderBlockNearLong : SignalCreateBase
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

                // Demand zones only, still active (not broken), and past their open time.
                if (zone.Side != CryptoTradeSide.Long || zone.CloseTime != null)
                    continue;
                if (CandleLast.Candle.OpenTime < zone.OpenTime) // emulator..
                    continue;

                // Freshness / strength filters.
                if (settings.OnlyStrong && zone.Strength != CryptoZoneStrength.Strong)
                    continue;
                if (zone.TouchCount > settings.MaxTouches)
                    continue;

                // Proximal edge = Top for a demand zone. Allow price to be NearZonePercentage
                // above it and still trigger (approaching), but it must not have dropped below
                // the distal edge (Bottom) — that would be a break.
                decimal alarmPrice = zone.Top * (100 + settings.NearZonePercentage) / 100;
                if (CandleLast.Candle.Low <= alarmPrice && CandleLast.Candle.Low >= zone.Bottom)
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
