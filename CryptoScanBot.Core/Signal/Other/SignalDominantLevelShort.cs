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

        if (!CandleLast.IsStochOverbought(0)) // GlobalData.Settings.Signal.StoRsi.AddStochAmount
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!CandleLast.IsRsiOverbought()) // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
        {
            ExtraText = "rsi not overbought";
            return false;
        }


        AccountSymbolData symbolData = Account.Data.GetSymbolData(Symbol.Name);
        foreach (var zone in symbolData.ZoneListShort)
        {
            if (zone.OpenTime != null && CandleLast.OpenTime >= zone.OpenTime && zone.CloseTime == null)
            {
                bool changed = false;
                decimal alarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                if (CandleLast.High >= alarmPrice)
                {
                    if (!result && zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddMinutes(5))
                    {
                        ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id})";
                        zone.AlarmDate = CandleLast.Date;
                        zone.AlarmPrice = CandleLast.Low;
                        changed = true;
                        result = true;
                    }
                }

                // todo, delete the zone somewhere else?
                if (zone.CloseTime == null && CandleLast.High >= zone.Top)
                {
                    zone.CloseTime = CandleLast.OpenTime;
                    zone.ClosePrice = CandleLast.Low;
                    changed = true;
                }

                if (changed)
                {
                    using var database = new CryptoDatabase();
                    database.Connection.Update(zone);
                }

                if (result)
                    break;
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