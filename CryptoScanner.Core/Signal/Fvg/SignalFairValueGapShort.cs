using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Fvg;

public class SignalFairValueGapShort : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        CryptoSymbolData symbolData = Symbol.Data;
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} fvg zones {symbolData.FvgListLong.Count}");

        // ALARM-only: see SignalFairValueGapLong.IsSignal for the rationale. Zone closure
        // is the responsibility of ZoneFvg.ScanForNew / ZoneFvg.CalculateZonesAsync, not of
        // this per-1m signal class.
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // Capture reference so a concurrent FvgZones swap mid-loop does not cause IndexOutOfRangeException
                var shortOpen = symbolIntervalData.FvgZones.ShortOpen;
                int index = 0;
                while (index < shortOpen.Count) // sorted on Zone.Bottom ascending
                {
                    var zone = shortOpen[index];

                    if (CandleLast.Candle.OpenTime >= zone.OpenTime
                        && CandleLast.Candle.High >= zone.Bottom
                        && CandleLast.Candle.Low <= zone.Top)
                    {
                        // 1m candle is touching the zone (wick inside the range) — fire an
                        // alarm at most once per hour per zone.
                        if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                        {
                            result = true;
                            Interval = interval; // Report different interval back
                            zone.AlarmDate = CandleLast.Candle.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            decimal dist = 100m * (zone.Bottom - CandleLast.Candle.High) / CandleLast.Candle.Close;
                            ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        }
                    }

                    index++;

                    // The list is sorted on zone.bottom and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.High < zone.Bottom)
                        break;
                }
            }
        }
        return result;
    }

}