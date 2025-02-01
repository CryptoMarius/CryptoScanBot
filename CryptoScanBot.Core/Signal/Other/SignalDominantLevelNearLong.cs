using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalDominantLevelNearLong : SignalCreateBase
{
    public SignalDominantLevelNearLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.DominantLevelNear;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        decimal closestZone = 100;
        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);


        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.GetAccountSymbolInterval(interval.IntervalPeriod);

                int index = 0;
                decimal distance = 100m;
                while (index < symbolIntervalData.DlzZones.LongOpen.Count) // sorted on Zone.Top descending
                {
                    decimal? alarmPrice = null;
                    var zone = symbolIntervalData.DlzZones.LongOpen[index];
                    if (CandleLast.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // Close old invalid zone without notifications..
                        if (CandleLast.High <= zone.Bottom)
                        {
                            zone.CloseTime = CandleLast.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old zone #{zone.Id} {zone.Side} {zone.Description}");
                        }
                        else
                        {
                            // If it is within a certain percentage signal it..
                            alarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.ZonesDlz.WarnPercentage) / 100;
                            if (CandleLast.Low <= alarmPrice)
                            {
                                if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddHours(1))
                                {
                                    result = true;
                                    zone.AlarmDate = CandleLast.Date;
                                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                    decimal dist = 100m * (CandleLast.Low - zone.Top) / CandleLast.Close;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                                }
                            }


                            // Close if the candle touched the zone..
                            if (CandleLast.Low <= zone.Top)
                            {
                                ExtraText += "....";
                                zone.CloseTime = CandleLast.OpenTime;
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                GlobalData.AddTextToLogTab($"{Symbol.Name} Closed zone #{zone.Id} {zone.Side} {zone.Description}");
                            }


                            // Show the distance to the next available zone (for the symbol grid)
                            if (zone.CloseTime == null)
                            {
                                decimal dist = 100m * (CandleLast.Low - zone.Top) / CandleLast.Close;
                                if (dist < distance)
                                    distance = dist;
                            }
                        }
                    }

                    if (zone.CloseTime != null)
                    {
                        symbolIntervalData.DlzZones.LongOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Removed zone #{zone.Id} {zone.Side} {zone.Description}");
                    }
                    else index++;


                    // The list is sorted on zone.top and break if there are no more reachable zones (save some looping time)
                    if (alarmPrice != null && alarmPrice > zone.Top)
                        break;
                }

                symbolIntervalData.Zones.BestLongZone = distance;
                if (distance < closestZone)
                    closestZone = distance;
            }
        }

        symbolData.BestLongZone = closestZone; 
        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}