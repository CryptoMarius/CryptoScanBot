using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Signal.Other;

public class DominantLevelLong : SignalCreateBase
{
    public DominantLevelLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.DominantLevel;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;

        //if (!CandleLast.IsStochOversold(0)) // GlobalData.Settings.Signal.Zones.AddStochAmount
        //{
        //    ExtraText = "stoch not oversold";
        //    return false;
        //}

        //if (!CandleLast.IsRsiOversold(0)) // GlobalData.Settings.Signal.Zones.AddRsiAmount
        //{
        //    ExtraText = "rsi not oversold";
        //    return false;
        //}


        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
        symbolIntervalData.BestLongZone = 100m;
        if (symbolData.ZoneListLong.Count == 0)
            return false;

        //// Every zone that is overlapping with this zone must be signalled
        //decimal boundaryHigh = CandleLast.High;
        //decimal boundaryLow = CandleLast.Low * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;

        //// The ZoneList is sorted from low to high

        //// Low index
        //int indexLow = symbolData.ZoneListLong.Keys.BinarySearchIndexOf(boundaryLow) - 1;
        //if (indexLow < 0)
        //    indexLow = 0;
        //if (indexLow >= symbolData.ZoneListLong.Count)
        //    indexLow = symbolData.ZoneListLong.Count - 1;

        //// High index
        //int indexHigh = symbolData.ZoneListLong.Keys.BinarySearchIndexOf(boundaryHigh) + 1;
        //if (indexHigh < 0)
        //    indexHigh = 0;
        //if (indexHigh >= symbolData.ZoneListLong.Count)
        //    indexHigh = symbolData.ZoneListLong.Count - 1;


        decimal? distance = null;
        //for (int index = indexLow; index < indexHigh; index++)
        foreach (var zone in symbolData.ZoneListLong.Values)
        {
            //var zone = symbolData.ZoneListLong.Values[index];
            if (zone.OpenTime != null && CandleLast.OpenTime >= zone.OpenTime && zone.CloseTime == null)
            {
                bool changed = false;
                decimal alarmPrice = zone.Top * (decimal)(100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                if (CandleLast.Low <= alarmPrice)
                {
                    if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddMinutes(5))
                    {
                        result = true;
                        changed = true;
                        zone.AlarmDate = CandleLast.Date;
                        ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id}  {CandleLast.Low})";
                    }
                }

                // todo, delete the zone somewhere else?
                if (CandleLast.Low < zone.Top) // || CandleLast.Close <= zone.Top || CandleLast.Close <= zone.Top
                {
                    changed = true;
                    ExtraText += "....";
                    zone.CloseTime = CandleLast.OpenTime;
                    GlobalData.AddTextToLogTab($"{Symbol.Name} Closed zone {zone.Id} {zone.Side} {zone.Description}");
                }
                else
                {
                    decimal dist = 100m * (CandleLast.Low - zone.Top) / CandleLast.Close; 
                    if (distance == null || dist < distance)
                        distance = dist;
                }

                if (changed)
                {
                    try
                    {
                        using var database = new CryptoDatabase();
                        database.Connection.Update(zone);
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab(error.ToString());
                    }
                }

                // How about multiple zones overlapping?
                if (result)
                    break;
            }
        }
        symbolIntervalData.BestLongZone = distance;

        //// close higher long zones (they should not be there)
        ////for (int index = indexLow; index < symbolData.ZoneListLong.Count; index++)
        //foreach (var zone in symbolData.ZoneListShort.Values)
        //{
        //    //var zone = symbolData.ZoneListLong.Values[index];
        //    if (zone.CloseTime == null && CandleLast.Low < zone.Top)
        //    {
        //        zone.CloseTime = CandleLast.OpenTime;
        //        try
        //        {
        //            using var database = new CryptoDatabase();
        //            database.Connection.Update(zone);
        //            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed zone {zone.Id} {zone.Side} {zone.Description} (bulk)");
        //        }
        //        catch (Exception error)
        //        {
        //            ScannerLog.Logger.Error(error, "");
        //            GlobalData.AddTextToLogTab(error.ToString());
        //        }
        //    }
        //}


        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}