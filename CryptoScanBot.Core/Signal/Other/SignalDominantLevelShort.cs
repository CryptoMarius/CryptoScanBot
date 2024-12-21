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
        ExtraText = "";
        bool result = false;


        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
        symbolIntervalData.BestShortZone = 100m;
        if (symbolData.ZoneListShort.Count == 0)
            return false;


        decimal? distance = null;
        foreach (var zone in symbolData.ZoneListShort) // sorted on Zone.Top asscending
        {
            if (CandleLast.OpenTime >= zone.OpenTime) // emulator..
            {
                // Old invalid zone? Close it without notifications..
                if (CandleLast.Low > zone.Top)
                {
                    ExtraText += "....";
                    zone.CloseTime = CandleLast.OpenTime;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old zone {zone.Id} {zone.Side} {zone.Description}");
                    continue;
                }


                // If it is within a certain percentage signal it..
                decimal alarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                if (CandleLast.High >= alarmPrice)
                {
                    if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                    {
                        result = true;
                        zone.AlarmDate = CandleLast.Date;
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                        ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id}  {CandleLast.High}";
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
                    if (distance == null || dist < distance)
                        distance = dist;
                }

                // The list is sorted on its top value and if there are no more reachable zones break
                // (this saves a lot of looping time)
                if (alarmPrice < zone.Bottom)
                    break;
            }
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