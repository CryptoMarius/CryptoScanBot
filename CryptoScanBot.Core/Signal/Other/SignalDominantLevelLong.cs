using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
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

        //for (int index = indexLow; index < indexHigh; index++)
        foreach (var zone in symbolData.ZoneListShort.Values)
        {
            //var zone = symbolData.ZoneListLong.Values[index];

            if (zone.CloseTime == null)
            {

                bool changed = false;
                decimal alarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                if (CandleLast.Low <= alarmPrice)
                {
                    if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddMinutes(5))
                    {
                        result = true;
                        changed = true;
                        zone.AlarmDate = CandleLast.Date;
                        ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id})";
                    }
                }

                // todo, delete the zone somewhere else?
                if (CandleLast.Low < zone.Top) // || CandleLast.Close <= zone.Top || CandleLast.Close <= zone.Top
                {
                    changed = true;
                    zone.CloseTime = CandleLast.OpenTime;
                    GlobalData.AddTextToLogTab($"Closed zone {zone.Id} {zone.Side} {zone.Description}");
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
                if (result)
                    break;
            }
        }


        // close higher long zones (they should not be there)
        try
        {
            //for (int index = indexLow; index < symbolData.ZoneListLong.Count; index++)
            foreach (var zone in symbolData.ZoneListShort.Values)
            {
                //var zone = symbolData.ZoneListLong.Values[index];
                if (zone.CloseTime == null && CandleLast.Low < zone.Top)
                {
                    zone.CloseTime = CandleLast.OpenTime;

                    try
                    {
                        using var database = new CryptoDatabase();
                        database.Connection.Update(zone);
                        GlobalData.AddTextToLogTab($"Closed zone {zone.Id} {zone.Side} {zone.Description} (bulk)");
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab(error.ToString());
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
        }

        return result;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}
