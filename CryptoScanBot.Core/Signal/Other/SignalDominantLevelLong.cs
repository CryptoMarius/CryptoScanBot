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
        foreach (var zone in symbolData.ZoneListLong.Values)
        {
            if (zone.OpenTime != null && CandleLast.OpenTime >= zone.OpenTime && zone.CloseTime == null)
            {
                bool changed = false;
                decimal alarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                if (CandleLast.Low <= alarmPrice)
                {
                    if (zone.AlarmDate == null || CandleLast.Date > zone.AlarmDate?.AddMinutes(5)) //!result && ?
                    {
                        ExtraText = $"{zone.Bottom} .. {zone.Top} (#{zone.Id})";
                        zone.AlarmPrice = CandleLast.Low;
                        zone.AlarmDate = CandleLast.Date;
                        changed = true;
                        result = true;
                    }
                }

                // todo, delete the zone somewhere else?
                if (zone.CloseTime == null && CandleLast.Low <= zone.Bottom)
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

                // How about multiple zones overlapping?
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
