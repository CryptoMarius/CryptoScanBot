using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalDominantLevelShort : SignalCreateBase
{
    public SignalDominantLevelShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.DominantLevel;
    }


    public override bool IsSignal()
    {
        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
        int index = 0;
        ExtraText = "";
        bool result = false;
        decimal distance = 100m;
        while (index < symbolData.ZoneListShort.Count) // sorted on Zone.Bottom asscending
        {
            decimal? alarmPrice = null;
            var zone = symbolData.ZoneListShort[index];
            if (CandleLast.OpenTime >= zone.OpenTime) // emulator..
            {
                // Close old invalid zone without notifications..
                if (CandleLast.Low > zone.Top)
                {
                    zone.CloseTime = CandleLast.OpenTime;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old zone {zone.Id} {zone.Side} {zone.Description}");
                }
                else
                {
                    // If it is within a certain percentage signal it..
                    alarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    if (CandleLast.High >= alarmPrice)
                    {
                        if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                        {
                            result = true;
                            zone.AlarmDate = CandleLast.Date;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            decimal dist = 100m * (zone.Bottom - CandleLast.High) / CandleLast.Close;
                            ExtraText = $"{zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                        }
                    }


                    // Close if the candle touched the zone..
                    if (CandleLast.High > zone.Bottom)
                    {
                        ExtraText += "....";
                        zone.CloseTime = CandleLast.OpenTime;
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Closed zone {zone.Id} {zone.Side} {zone.Description}");
                    }


                    // Show the distance to the next available zone (for the symbol grid)
                    if (zone.CloseTime == null)
                    {
                        decimal dist = 100m * (zone.Bottom - CandleLast.High) / CandleLast.Close;
                        if (dist < distance)
                            distance = dist;
                    }
                }
            }

            // Remove closed zones
            if (zone.CloseTime != null)
            {
                symbolData.ZoneListShort.RemoveAt(index);
                GlobalData.AddTextToLogTab($"{Symbol.Name} Removed zone {zone.Id} {zone.Side} {zone.Description}");
            }
            else index++;


            // The list is sorted on its top value and if there are no more reachable zones break
            // (this saves a lot of looping time)
            if (alarmPrice != null && alarmPrice < zone.Bottom)
                break;
        }
        symbolIntervalData.BestShortZone = distance;
        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}