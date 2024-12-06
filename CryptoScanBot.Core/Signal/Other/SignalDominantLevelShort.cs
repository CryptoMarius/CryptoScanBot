﻿using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Signal.Other;

public class DominantLevelShort : SignalCreateBase
{
    public DominantLevelShort(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.DominantLevel;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;

        //if (!CandleLast.IsStochOverbought(0)) // GlobalData.Settings.Signal.StoRsi.AddStochAmount
        //{
        //    ExtraText = "stoch not overbought";
        //    return false;
        //}

        //if (!CandleLast.IsRsiOverbought()) // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
        //{
        //    ExtraText = "rsi not overbought";
        //    return false;
        //}


        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        if (symbolData.ZoneListShort.Count == 0)
            return false;

        // Every zone that is overlapping with this zone must be signalled
        decimal boundaryHigh = CandleLast.High * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
        decimal boundaryLow = CandleLast.Low;

        // The ZoneList is sorted from low to high

        // High index
        int indexHigh = symbolData.ZoneListShort.Keys.BinarySearchIndexOf(boundaryHigh);
        if (indexHigh < 0)
            indexHigh = 0;
        if (indexHigh >= symbolData.ZoneListShort.Count)
            indexHigh = symbolData.ZoneListShort.Count - 1;

        // Low index
        int indexLow = symbolData.ZoneListShort.Keys.BinarySearchIndexOf(boundaryLow);
        if (indexLow < 0)
            indexLow = 0;
        if (indexLow >= symbolData.ZoneListShort.Count)
            indexLow = symbolData.ZoneListShort.Count - 1;


        for (int index = indexLow; index < indexHigh; index++)
        {
            var zone = symbolData.ZoneListShort.Values[index];
            if (zone.OpenTime != null && CandleLast.OpenTime >= zone.OpenTime && zone.CloseTime == null)
            {
                if (zone.CloseTime == null)
                {
                    bool changed = false;
                    decimal alarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    if (CandleLast.High >= alarmPrice)
                    {
                        if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddMinutes(5))
                        {
                            ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id})";
                            zone.AlarmPrice = CandleLast.High;
                            zone.AlarmDate = CandleLast.Date;
                            changed = true;
                            result = true;
                        }
                    }

                    // todo, delete the zone somewhere else?
                    if (CandleLast.High > zone.Bottom || CandleLast.Close >= zone.Bottom || CandleLast.Open >= zone.Bottom)
                    {
                        zone.CloseTime = CandleLast.OpenTime;
                        zone.ClosePrice = CandleLast.Low;
                        changed = true;
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
        }

        //// close higher zones
        //for (int index = 0; index < indexLow; index++)
        //{
        //    var zone = symbolData.ZoneListShort.Values[index];

        //    if (zone.CloseTime == null && zone.Bottom < CandleLast.Low)
        //    {
        //        zone.CloseTime = CandleLast.OpenTime;
        //        zone.ClosePrice = CandleLast.Low;
        //        try
        //        {
        //            using var database = new CryptoDatabase();
        //            database.Connection.Update(zone);
        //            GlobalData.AddTextToLogTab($"Closed zone {zone.Id} {zone.Side} {zone.Description} (bulk)");
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
