using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Fvg;

public class SignalFairValueGapLong : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        CryptoSymbolData symbolData = Symbol.Data;
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} fvg zones {symbolData.FvgListLong.Count}");

        // ALARM-only: this method now exclusively handles alarm bookkeeping (zone.AlarmDate).
        // Zone CLOSURE used to live here too — using the signal's 1m candle to invalidate
        // higher-TF zones (1h/4h/1d/1w). That was wrong: a 1d zone should only close when
        // the 1d candle itself breaks through it. Invalidation is now centralised in
        // ZoneFvg.ScanForNew → InvalidateRealtime (per zone-interval candle close) and the
        // full ZoneFvg.CalculateZonesAsync recalc, both using ZoneInvalidation.ApplyToCandle.
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // Capture reference so a concurrent FvgZones swap mid-loop does not cause IndexOutOfRangeException
                var longOpen = symbolIntervalData.FvgZones.LongOpen;
                int index = 0;
                while (index < longOpen.Count) // sorted on Zone.Top descending
                {
                    var zone = longOpen[index];

                    if (CandleLast.Candle.OpenTime >= zone.OpenTime
                        && CandleLast.Candle.Low <= zone.Top
                        && CandleLast.Candle.High >= zone.Bottom)
                    {
                        // 1m candle is touching the zone (wick inside the range) — fire an
                        // alarm at most once per hour per zone.
                        if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                        {
                            result = true;
                            Interval = interval; // Report different interval back
                            zone.AlarmDate = CandleLast.Candle.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            decimal dist = 100m * (CandleLast.Candle.Low - zone.Top) / CandleLast.Candle.Close;
                            ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        }
                    }

                    index++;

                    // The list is sorted on zone.top and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.Low > zone.Top)
                        break;
                }
            }
        }
        return result;
    }

}