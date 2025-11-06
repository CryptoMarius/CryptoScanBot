using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Other;

public class SignalFairValueGapLong : SignalCreateBase
{
    public SignalFairValueGapLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


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

                int index = 0;
                while (index < symbolIntervalData.FvgZones.LongOpen.Count) // sorted on Zone.Top descending
                {
                    var zone = symbolIntervalData.FvgZones.LongOpen[index];

                    if (CandleLast.OpenTime >= zone.OpenTime)
                    {
                        if (CandleLast.High < zone.Bottom) // Close without notifications..
                        {
                            zone.CloseTime = CandleLast.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old fvg zone {zone.Id} {zone.Side} {zone.Description}");
                        }
                        else if (CandleLast.Low <= zone.Top)
                        {
                            if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                            {
                                result = true;
                                zone.AlarmDate = CandleLast.Date;
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                decimal dist = 100m * (CandleLast.Low - zone.Top) / CandleLast.Close;
                                ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                            }

                            // Close if the candle touched the zone..
                            zone.CloseTime = CandleLast.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                        }
                    }

                    // Remove closed zones
                    if (zone.CloseTime != null)
                    {
                        symbolIntervalData.FvgZones.LongOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                    }
                    else index++;


                    // The list is sorted on zone.top and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Low > zone.Top)
                        break;
                }
            }
        }
        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}