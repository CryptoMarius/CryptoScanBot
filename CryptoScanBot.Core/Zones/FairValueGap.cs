using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Zones;

public class FairValueGap
{
    // FVG (just a quick approach)
    public static void ScanForNew(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side, long lastCandle1mCloseTime)
    {
        // Get the last 3 candles
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 1 * interval.Duration, out CryptoCandle? candle))
            return;
        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 2 * interval.Duration, out CryptoCandle? prev))
            return;
        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 3 * interval.Duration, out CryptoCandle? prev2))
            return;


        // scan voor long FVG
        if (side == CryptoTradeSide.Long && candle.Close > candle.Open && prev.Close > prev.Open && prev2.Close > prev2.Open)
        {
            // 3 green candles in a row..
            if (candle.Low > prev2.High && prev.Close > prev2.High)
            {
                // todo, add the zone
                double perc = 100 * (double)((candle.Low - prev2.High) / prev2.High);
                if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                {
                    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {side} FVG {prev2.High}..{candle.Low} {perc:N2}%");

                    CryptoZone zone = new()
                    {
                        Kind = CryptoZoneKind.FairValueGap,
                        CreateTime = candle.Date,
                        AccountId = GlobalData.ActiveAccount!.Id,
                        Account = GlobalData.ActiveAccount,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        OpenTime = candle.OpenTime,
                        Top = candle.Low,
                        Bottom = prev2.High,
                        Side = CryptoTradeSide.Long,
                        IsValid = false,
                        Description = $"{interval.Name} fvg {perc:N2}%",
                    };

                    AccountSymbolData symbolData = account!.Data.GetSymbolData(symbol.Name);
                    symbolData.FvgListLong.Add(zone);
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }

            // scan voor short FVG
            if (side == CryptoTradeSide.Short && candle.Open > candle.Close && prev.Open > prev.Close && prev2.Open > prev2.Close)
            {
                // 3 red candles in a row..
                if (candle.High < prev2.Low && prev.Close < prev2.Low)
                {
                    // todo, add the zone
                    double perc = 100 * (double)((prev2.Low - candle.High) / candle.High);
                    if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                    {
                        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {side} FVG {candle.Low}..{prev2.High} {perc:N2}%");

                        CryptoZone zone = new()
                        {
                            Kind = CryptoZoneKind.FairValueGap,
                            CreateTime = candle.Date,
                            AccountId = GlobalData.ActiveAccount!.Id,
                            Account = GlobalData.ActiveAccount,
                            ExchangeId = symbol.Exchange.Id,
                            Exchange = symbol.Exchange,
                            SymbolId = symbol.Id,
                            Symbol = symbol,
                            OpenTime = candle.OpenTime,
                            Top = prev2.Low,
                            Bottom = candle.High,
                            Side = CryptoTradeSide.Short,
                            IsValid = false,
                            Description = $"{interval.Name} fvg {perc:N2}%",
                        };

                        AccountSymbolData symbolData = account!.Data.GetSymbolData(symbol.Name);
                        symbolData.FvgListShort.Add(zone);
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);

                    }
                }
            }
        }
    }
}
