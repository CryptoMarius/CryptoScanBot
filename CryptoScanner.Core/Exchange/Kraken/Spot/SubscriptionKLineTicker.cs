using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models.Socket;

namespace CryptoScanner.Core.Exchange.Kraken.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    static double GetNextTimer()
    {
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SemaphoreSlim cacheListSemaphore = new(1, 1);
        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)TickerGroup!.SocketClient;
        var api = client.SpotApi;

        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            throw new Exception("No exchange?");

        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
            throw new Exception("No interval?");


        SortedList<string, CryptoCandleList> symbolCandleCache = [];

        List<string> symbols = [];
        //string symbolName = "";
        foreach (var symbol in SymbolList)
        {
            //string symbolName = api.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.Spot);
            //symbols.Add(symbolName);
            symbols.Add(symbol.ExchangeName);
            symbolCandleCache.Add(symbol.ExchangeName, []);
        }
        //string symbolNames = string.Join(",", symbols);




        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbols, KlineInterval.OneMinute, data =>
        {
            Task taskKline = Task.Run(async () =>
            {
                //var kline = data;
                //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
                //GlobalData.AddTextToLogTab($"kline ticker {data.ScannerSymbol} {json}");
                if (exchange.SymbolListExchangeName.TryGetValue(data.Symbol!, out CryptoSymbol? symbol))
                {
                    await cacheListSemaphore.WaitAsync();
                    try
                    {
                        foreach (KrakenKlineUpdate kline in data.Data)
                        {
                            // Add or update the local cache
                            bool addCandle = false;
                            CandleTime candleOpenUnix = CandleTime.AlignFromDateTime(kline.OpenTime, 1);
                            CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                            if (!candleCache.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
                            {
                                addCandle = true;
                                candle = new() { OpenTime = candleOpenUnix };
                            }
                            candle!.Open = kline.OpenPrice;
                            candle.High = kline.HighPrice;
                            candle.Low = kline.LowPrice;
                            candle.Close = kline.ClosePrice;
                            //candle.Volume = kline.Volume;
                            candle.Volume = kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice);
                            if (addCandle)
                                candleCache.TryAdd(candleOpenUnix, candle);
                            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)} count={candleCache.Count}");
                        }
                    }
                    finally
                    {
                        cacheListSemaphore.Release();
                    }
                }

            });
        }, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);


        // Implementatie kline timer (fix)
        // Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
        // kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
        // De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

        if (subscriptionResult.Success)
        {
            System.Timers.Timer timerKline = new()
            {
                AutoReset = false,
            };
            timerKline.Elapsed += new System.Timers.ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var symbol in SymbolList)
                {
                    try
                    {
                        await cacheListSemaphore.WaitAsync();
                        try
                        {
                            CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                            CandleTime expectedCandlesUpto = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - interval.Duration;

                            // Problem this = symbolPeriod.CandleList.Values.Last()
                            // TODO, this one gives me lots of problems, collection has been modified (fair, but how to solve this)
                            // Locking = high cpu and poor performance, need to rethink if we really need this?
                            // "Empty" repetition candles are already added in the CollectCandles() so its not really needed here
                            // Still, its something simple and technical that is bothering me, why is the enumerator modified, what is triggering this?
                            // For now just continue without adding dummy "repetition" candles

                            // ?Problem: Other algoritm's don't expect missing candles I think? Linke BBMA switching to higher or lower timeframes?
                            // Only the CollectCandles() works without problem..

                            //// If needed add dummy candle(s) with the same price as the last candle
                            //if (symbolPeriod.CandleList.Count > 0 && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            //{
                            //    //CryptoCandle lastCandle;
                            //    //await symbol.Data.CandleLock.WaitAsync();
                            //    //try
                            //    //{
                            //    // TODO, this one gives me lots of problems, collection has been modified (fair, but how to solve this)
                            //    // Locking = high cpu and poor performance, need to rethink if we really need this?
                            //    CryptoCandle lastCandle = symbolPeriod.CandleList.Values.Last();
                            //    //}
                            //    //finally
                            //    //{
                            //        //symbol.Data.CandleLock.Release();
                            //    //}

                            //    while (lastCandle.OpenTime < expectedCandlesUpto)
                            //    {
                            //        // Als deze al aanwezig dmv een ticker update niet dupliceren
                            //        long nextCandleUnix = lastCandle.OpenTime + interval.Duration;
                            //        if (candleCache.TryGetValue(nextCandleUnix, out CryptoCandle? nextCandle))
                            //            break;

                            //        // Dplicate the last candle if it is not present ("flat" candle)
                            //        if (!symbolPeriod.CandleList.TryGetValue(nextCandleUnix, out nextCandle))
                            //        {
                            //            nextCandle = new();
                            //            nextCandle.OpenTime = nextCandleUnix;
                            //            nextCandle.Open = lastCandle.Close;
                            //            nextCandle.High = lastCandle.Close;
                            //            nextCandle.Low = lastCandle.Close;
                            //            nextCandle.Close = lastCandle.Close;
                            //            nextCandle.Volume = 0; // no volume (flat candle)
                            //            candleCache.Add(nextCandleUnix, nextCandle);

                            //            lastCandle = nextCandle;
                            //         }
                            //         else break;
                            //      }
                            //   }


                            // Finally do something with the cached data
                            CryptoCandle? candleLast = null;
                            foreach (CryptoCandle candle in candleCache.Values.ToList())
                            {
                                // Only the ready candles (might change the flow?)
                                if (candle.OpenTime <= expectedCandlesUpto)
                                {
                                    candleCache.Remove(candle.OpenTime);
                                    Interlocked.Increment(ref TickerCount);
                                    if (TickerCount > 999999999)
                                        Interlocked.Exchange(ref TickerCount, 0);

                                    //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                                    //GlobalData.AddTextToLogTab($"{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                                    //GlobalData.AddTextToLogTab("Start processing " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
                                    await CandleTools.Process1mCandleAsync(symbol, candle.Date,
                                        candle.Open, candle.High, candle.Low, candle.Close,
                                        candle.Volume);
                                    candleLast = candle;
                                    // Debug...
                                    //GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
                                }
                                else break;
                            }
                            // Add the last candle in the analysis queue
                            if (candleLast != null && candleLast.OpenTime == expectedCandlesUpto)
                            {
                                // Last known price(s) (this is what the priceticker should do)
                                if (!GlobalData.BackTest)
                                {
                                    symbol.LastPrice = candleLast.Close;
                                }
                                //GlobalData.AddTextToLogTab("Aanbieden analyze " + candleLast.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
                                GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candleLast);
                            }
                        }
                        finally
                        {
                            cacheListSemaphore.Release();
                        }
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, symbol.Name);
#if DEBUG
                        GlobalData.AddTextToLogTab($"KLine Ticker {symbol.Name} ERROR {error.Message}");
#endif
                    }
                }

                if (sender is System.Timers.Timer t)
                {
                    t.Interval = GetNextTimer();
                    t.Start();
                }
            });
            timerKline.Interval = GetNextTimer();
            timerKline.Start();
        }

        return subscriptionResult;
    }

}