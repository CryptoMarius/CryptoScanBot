using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalFairValueGapLong : SignalCreateBase
{
    public SignalFairValueGapLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.FairValueGap;
    }


    public override bool IsSignal()
    {
        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} fvg zones {symbolData.FvgListLong.Count}");

        int index = 0;
        ExtraText = "";
        bool result = false;
        while (index < symbolData.FvgListLong.Count) // sorted on Zone.Top descending
        {
            decimal? alarmPrice = null;
            var zone = symbolData.FvgListLong[index];
            if (CandleLast.OpenTime >= zone.OpenTime) // emulator..
            {
                // Close old invalid zone without notifications..
                if (CandleLast.High < zone.Bottom)
                {
                    zone.CloseTime = CandleLast.OpenTime;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old zone {zone.Id} {zone.Side} {zone.Description}");
                }
                else
                {
                    // If it is within a certain percentage signal it..
                    alarmPrice = zone.Top;
                    if (CandleLast.Low <= alarmPrice)
                    {
                        if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                        {
                            result = true;
                            zone.AlarmDate = CandleLast.Date;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            decimal dist = 100m * (CandleLast.Low - zone.Top) / CandleLast.Close;
                            ExtraText = $"{zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        }
                    }


                    // Close if the candle touched the zone..
                    if (CandleLast.Low < zone.Top)
                    {
                        ExtraText += "....";
                        zone.CloseTime = CandleLast.OpenTime;
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Closed zone {zone.Id} {zone.Side} {zone.Description}");
                    }
                }
            }

            if (zone.CloseTime != null)
            {
                symbolData.FvgListLong.RemoveAt(index);
                GlobalData.AddTextToLogTab($"{Symbol.Name} Removed fvg zone {zone.Id} {zone.Side} {zone.Description}");
            }
            else index++;


            // The list is sorted on its top value and if there are no more reachable zones break
            // (this saves a lot of looping time)
            if (alarmPrice != null && alarmPrice > zone.Top)
                break;
        }
        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}