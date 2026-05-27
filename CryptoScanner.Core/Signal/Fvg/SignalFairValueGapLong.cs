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

                    if (CandleLast.Candle.OpenTime >= zone.OpenTime)
                    {
                        // Zone is invalid when the 1m candle is entirely below the zone bottom
                        // (high < bottom means price has moved completely away from the zone).
                        // A mere touch or wick into the zone keeps it open so the combined
                        // Stobb+FVG / StoRsi+FVG signals can still find it at their interval close.
                        if (CandleLast.Candle.High < zone.Bottom)
                        {
                            zone.CloseTime = CandleLast.Candle.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old fvg zone {zone.Id} {zone.Side} {zone.Description}");
                        }
                        else if (CandleLast.Candle.Low <= zone.Top)
                        {
                            if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                            {
                                result = true;
                                zone.AlarmDate = CandleLast.Candle.OpenTime;
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                decimal dist = 100m * (CandleLast.Candle.Low - zone.Top) / CandleLast.Candle.Close;
                                ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                            }
                            // Zone stays open — removed only when candle is entirely below zone.Bottom.
                        }
                    }

                    // Remove closed zones
                    if (zone.CloseTime != null)
                    {
                        longOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{zone.ZoneText("Removed fvg zone")}");
                    }
                    else index++;


                    // The list is sorted on zone.top and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.Low > zone.Top)
                        break;
                }
            }
        }
        return result;
    }

}