using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Enums;
using HyperLiquid.Net.Objects.Models;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Futures;

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
        TickerGroup!.SocketClient ??= new HyperLiquidSocketClient();
        var client = (HyperLiquidSocketClient)TickerGroup.SocketClient;
        var api = client.FuturesApi;

        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        SortedList<string, CryptoCandleList> symbolCandleCache = [];
        foreach (var symbol in SymbolList)
        {
            symbols.Add(symbol.ExchangeName);
            symbolCandleCache.Add(symbol.ExchangeName, []);
        }
        string symbolNames = string.Join(",", symbols);


        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
            throw new Exception("Geen intervallen?");


        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbolNames, KlineInterval.OneMinute, data =>
        {
            Task taskKline = Task.Run(async () =>
            {
                HyperLiquidKline kline = data.Data;
                //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
                //GlobalData.AddTextToLogTab($"kline ticker {data.ScannerSymbol} {json}");

                // Prossible change in flow:
                // Create some variables or temp candle
                // Update that candle until OpenTime is different
                // The last 1m candle added can be cached (avoiding the Last())
                // Then: Add the in between candles and the tempcandle
                // Finally add the tempcandle to the Analysis Queue / Monitoring Queue

                if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                {
                    var tick = data.Data;
                    if (exchange.SymbolListExchangeName.TryGetValue(data.Symbol!, out CryptoSymbol? symbol))
                    {
                        await cacheListSemaphore.WaitAsync();
                        try
                        {
                            // Add or update the local cache
                            long candleOpenUnix = CandleTools.GetUnixTime(kline.OpenTime, 60);
                            CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                            if (!candleCache.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
                            {
                                candle = new();
                                candle.OpenTime = candleOpenUnix;
                                candleCache.TryAdd(candleOpenUnix, candle);

                                //CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);

                                //// do something with the cached data, this assumes we always get a candle (even if it is flat or empty)
                                //CryptoCandle? candleLast = null;
                                //foreach (CryptoCandle c in candleCache.Values.ToList())
                                //{
                                //    // Only the ready candles (might change the flow?)
                                //    if (c.OpenTime < candleOpenUnix)
                                //    {
                                //        candleCache.Remove(c.OpenTime);
                                //        Interlocked.Increment(ref TickerCount);
                                //        if (TickerCount > 999999999)
                                //            Interlocked.Exchange(ref TickerCount, 0);

                                //        //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                                //        //GlobalData.AddTextToLogTab($"{symbol.Name} Candle start processing {c.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");
                                //        await CandleTools.Process1mCandleAsync(symbol, c.Date,
                                //            c.Open, c.High, c.Low, c.Close,
                                //            0, c.Volume);
                                //        candleLast = c;
                                //    }
                                //    else break;
                                //}

                                //// Add the last candle in the analysis queue
                                //long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;
                                //if (candleLast != null && candleLast.OpenTime == expectedCandlesUpto)
                                //{
                                //    // Last known price(s)
                                //    if (!GlobalData.BackTest)
                                //    {
                                //        symbol.LastPrice = candleLast.Close;
                                //    }

                                //    //GlobalData.AddTextToLogTab("Aanbieden analyze " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
                                //    GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candleLast);
                                //}
                            }
                            candle.Open = kline.OpenPrice;
                            candle.High = kline.HighPrice;
                            candle.Low = kline.LowPrice;
                            candle.Close = kline.ClosePrice;
                            candle.Volume = kline.Volume;
                            //kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice
                            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(ScannerSymbol, interval, ScannerSymbol.PriceDisplayFormat, true, true)}");
                        }
                        finally
                        {
                            cacheListSemaphore.Release();
                        }
                    }
                }
            });
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Debug...
        //GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
        //            //var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, 
        //            //    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
        //            //    kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
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
                            long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;

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
                                    //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                                    await CandleTools.Process1mCandleAsync(symbol, candle.Date,
                                        candle.Open, candle.High, candle.Low, candle.Close,
                                        0, candle.Volume);
                                    candleLast = candle;
                                }
                                else break;
                            }
                            // Add the last candle in the analysis queue
                            if (candleLast != null && candleLast.OpenTime == expectedCandlesUpto)
                            {
                                // Last known price(s)
                                if (!GlobalData.BackTest)
                                {
                                    symbol.LastPrice = candleLast.Close;
                                }

                                //GlobalData.AddTextToLogTab("Aanbieden analyze " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
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
