using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalFairValueGapShort : SignalCreateBase
{
    public SignalFairValueGapShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
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
                while (index < symbolIntervalData.FvgZones.ShortOpen.Count) // sorted on Zone.Bottom asscending
                {
                    var zone = symbolIntervalData.FvgZones.ShortOpen[index];

                    if (CandleLast.OpenTime >= zone.OpenTime)
                    {
                        if (CandleLast.Low > zone.Top) // Close without notifications..
                        {
                            zone.CloseTime = CandleLast.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                        }
                        else if (CandleLast.High >= zone.Bottom)
                        {
                            if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                            {
                                result = true;
                                zone.AlarmDate = CandleLast.Date;
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                decimal dist = 100m * (zone.Bottom - CandleLast.High) / CandleLast.Close;
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
                        symbolIntervalData.FvgZones.ShortOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                    }
                    else index++;


                    // The list is sorted on zone.bottom and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.High < zone.Bottom)
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