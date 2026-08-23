using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Smc.Signal;

/// <summary>
/// SMC supply order block — short, REJECTION variant ("smc.rejection"). This is the
/// entry-grade signal: it fires only after price has actually rejected off the zone, i.e.
///   1) a candle within RejectionLookback tested the zone (wicked into [Bottom, Top]), and
///   2) the CURRENT candle closed back BELOW the proximal edge (Bottom) — the confirmed drop.
///
/// Compared with the other supply variant:
///   • smc       fires on the TOUCH (no proof the zone holds)
///   • smc.rejection (this) waits for the close-back-outside, so you enter on the rejection.
///
/// Zones are produced by <see cref="Zones.ZoneSmc"/> and live in
/// <see cref="CryptoSymbolIntervalSmc.Zones"/>. This class only reads them and sets AlarmDate.
/// </summary>
public class SignalOrderBlockRejectionShort : SignalCreateBase
{
    // Captured from the zone that fired this signal (the proximal edge — Bottom for a supply
    // zone). Exposed via OverrideSignalPrice so the trader can place a limit order on the
    // zone band itself instead of at the rejection close (which sits below the zone by
    // definition). Combinable with EntryOrderPrice = SignalPriceWithPullback to drift the
    // entry further into the zone (toward CE) via the pullback percentage.
    private decimal? _zoneProximalEdge;
    public override decimal? OverrideSignalPrice => _zoneProximalEdge;

    public override bool IsSignal()
    {
        ExtraText = "";
        _zoneProximalEdge = null;
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

                // Confirmation: the current candle must close back BELOW the proximal edge.
                // Without this we'd be reacting to the touch, not the rejection.
                if (CandleLast.Candle.Close >= zone.Bottom)
                    continue;

                // Look back over the last RejectionLookback candles (including the current one)
                // for a candle that actually tested the zone (wicked into [Bottom, Top]).
                bool tested = false;
                MyData? c = CandleLast;
                int lookback = Math.Max(1, settings.RejectionLookback);
                while (lookback-- > 0 && c != null)
                {
                    if (c.Candle.High >= zone.Bottom && c.Candle.Low <= zone.Top)
                    {
                        tested = true;
                        break;
                    }
                    if (!GetPrevCandle(c, out c))
                        break;
                }
                if (!tested)
                    continue;

                if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                {
                    result = true;
                    Interval = interval; // Report different interval back
                    _zoneProximalEdge = zone.Bottom; // supply zone: proximal = Bottom
                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                    decimal dist = 100m * (zone.Bottom - CandleLast.Candle.Close) / CandleLast.Candle.Close;
                    ExtraText = $"{interval.Name} supply OB rejection {zone.Bottom} .. {zone.Top} (+{dist:N2}%) touches={zone.TouchCount}";
                }
            }
        }

        return result;
    }
}
