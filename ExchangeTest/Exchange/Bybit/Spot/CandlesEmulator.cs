using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Exchange.BybitApi.Spot;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;


namespace ExchangeTest.Exchange.Bybit.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class CandlesEmulatorKanWeg
{
    private static async Task<long> GetCandlesForInterval(BybitRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval);
        if (exchangeInterval == null)
            return 0;

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        // The maximum is 1000 candles
        // (suprise) the sort order can be from new to old..
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        var result = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol.Name, (KlineInterval)exchangeInterval, startTime: dateStart, limit: 1000);
        if (!result.Success)
        {
            // Might do something better than this
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return 0;
        }


        // Might have problems with no internet etc.
        if (result == null || result.Data == null || !result.Data.List.Any())
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
            return 0;
        }

        // Remember
        long? startFetchDate = symbolInterval.LastCandleSynchronized;

        Monitor.Enter(symbol.CandleList);
        try
        {
            long last = long.MinValue;
            // Combine candles, calculating other interval's
            foreach (var kline in result.Data.List)
            {
                // Combine candles, calculating other interval's
                CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.StartTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume, false);

                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);

                // Onthoud de laatste candle, t/m die datum is alles binnen gehaald.
                // NB: De candle volgorde is niet gegarandeerd (op bybit zelfs omgedraaid)
                if (candle.OpenTime > last)
                    last = candle.OpenTime + interval.Duration; // new (saves 1 candle)
            }

            // For the next session
            if (last > long.MinValue)
            {
                symbolInterval.LastCandleSynchronized = last;
                // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                //CandleTools.UpdateCandleFetched(symbol, interval);
            }

            //SaveInformation(symbol, result.Data.List);
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }


        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " UTC tot " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime() + " UTC";
        GlobalData.AddTextToLogTab(s + " received: " + result.Data.List.Count() + " totaal: " + candles.Count.ToString());
        return result.Data.List.Count();
    }


    private static async Task FetchCandlesInternal(BybitRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            bool intervalSupported = Interval.GetExchangeInterval(interval) != null;


            if (intervalSupported)
            {
                // Fetch the candles
                while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
                {
                    long lastDate = (long)symbolInterval.LastCandleSynchronized;
                    //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                    //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());

                    if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                        break;

                    // Nothing more? (we have coins stopping, beaware for endless loops)
                    long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval);
                    if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                        break;
                }
            }


            Monitor.Enter(symbol.CandleList);
            try
            {
                // Fill missing candles (at only place we know it can be done safely)
                if (symbolInterval.CandleList.Count != 0)
                {
                    CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                    //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                    long unixTime = stickOld.OpenTime;
                    while (unixTime < symbolInterval.LastCandleSynchronized)
                    {
                        if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle? candle))
                        {
                            candle = new()
                            {
                                OpenTime = unixTime,
                                Open = stickOld.Close,
                                High = stickOld.Close,
                                Low = stickOld.Close,
                                Close = stickOld.Close,
                                Volume = 0
                            };
                            symbolInterval.CandleList.Add(candle.OpenTime, candle);
                            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Added missing candle " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                        }
                        stickOld = candle;
                        unixTime += interval.Duration;
                    }
                }

                // Calculate higher interval candles
                for (int j = i + 1; j < GlobalData.IntervalList.Count; j++)
                {
                    CryptoInterval intervalHigherTimeFrame = GlobalData.IntervalList[j];
                    CryptoInterval intervalLowerTimeFrame = intervalHigherTimeFrame.ConstructFrom!;

                    CryptoSymbolInterval periodLowerTimeFrame = symbol.GetSymbolInterval(intervalLowerTimeFrame.IntervalPeriod);
                    SortedList<long, CryptoCandle> candlesLowerTimeFrame = periodLowerTimeFrame.CandleList;

                    if (candlesLowerTimeFrame.Values.Any())
                    {
                        long candleHigherTimeFrameStart = candlesLowerTimeFrame.Values.First().OpenTime;
                        candleHigherTimeFrameStart -= candleHigherTimeFrameStart % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameStartDate = CandleTools.GetUnixDate(candleHigherTimeFrameStart);

                        long candleHigherTimeFrameEinde = candlesLowerTimeFrame.Values.Last().OpenTime;
                        candleHigherTimeFrameEinde -= candleHigherTimeFrameEinde % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameEindeDate = CandleTools.GetUnixDate(candleHigherTimeFrameEinde);

                        // Bulk calculation
                        while (candleHigherTimeFrameStart <= candleHigherTimeFrameEinde)
                        {
                            // Die laatste parameter is de closetime van een candle
                            candleHigherTimeFrameStart += intervalHigherTimeFrame.Duration;
                            CandleTools.CalculateCandleForInterval(intervalHigherTimeFrame, intervalLowerTimeFrame, symbol, candleHigherTimeFrameStart);
                        }

                        CandleTools.UpdateCandleFetched(symbol, intervalHigherTimeFrame);
                    }
                }

            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }
        }
    }


    public static async Task ExecuteAsync(CryptoSymbol symbol, DateTime endDate)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        //GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name}");

        // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
        // De datum en tijd tot na het activeren van beide streams (overlap)
        long fetchEndUnix = CandleTools.GetUnixTime(endDate, 60);

        using BybitRestClient client = new();
        await FetchCandlesInternal(client, symbol, fetchEndUnix);

        //GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
    }


}
