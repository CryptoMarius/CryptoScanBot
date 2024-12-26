using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;
using Mexc.Net.Objects.Models.Spot;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    //KucoinKline klinePrev;
    //long klinePrevOpenTime = 0;
    //#if KUCOINDEBUG
    //private static int tickerIndex = 0;
    //#endif

    //CryptoSymbol Symbol = null!;

    static double GetInterval()
    {
        // bewust 6 seconden zodat we zeker weten dat de kline er is
        // (anders zou deze 60 seconden later alsnog verwerkt worden, maar dat is te laat)
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SemaphoreSlim symbolListSemaphore = new(1, 1);
        SortedList<string, CryptoCandleList> klineListTemp = [];

        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        //string symbolName = "";
        foreach (var symbol in SymbolList)
        {
            //Symbol = symbol;
            //if (symbolName == "")
            //    symbolName = symbol.Name;
            //else
            //    symbolName += "," + symbol.Name;
            symbols.Add(symbol.Name);
            klineListTemp.Add(symbol.Name, []);
        }


        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
            throw new Exception("Geen intervallen?");


        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        TickerGroup!.SocketClient ??= new MexcSocketClient();
        //TickerGroup!.SocketClient.ClientOptions.OutputOriginalData = true;
        var subscriptionResult = await ((MexcSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToKlineUpdatesAsync(symbols, KlineInterval.OneMinute, data =>
        {
            Task taskKline = Task.Run(async () =>
            {
                MexcStreamKline kline = data.Data;
                //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
                //GlobalData.AddTextToLogTab($"kline ticker {data.Symbol} {json}");

                if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                {
                    var tick = data.Data;
                    string symbolName = data.Symbol!;
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        await symbolListSemaphore.WaitAsync();
                        try
                        {
                            // Add or update the local cache
                            long candleOpenUnix = CandleTools.GetUnixTime(kline.StartTime, 60);
                            CryptoCandleList tempList = klineListTemp[symbolName];
                            if (!tempList.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
                            {
                                candle = new();
                                tempList.TryAdd(candleOpenUnix, candle);
                            }
                            candle.OpenTime = candleOpenUnix;
                            candle.Open = kline.OpenPrice;
                            candle.High = kline.HighPrice;
                            candle.Low = kline.LowPrice;
                            candle.Close = kline.ClosePrice;
#if SUPPORTBASEVOLUME
                            candle.BaseVolume = kline.Volume;
#endif
                            candle.Volume = kline.QuoteVolume;
                            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, true)}");

                            // Last known price(s) (this is what the priceticker should do)
                            //symbol.LastPrice = kline.ClosePrice;
                            //symbol.AskPrice = kline.ClosePrice;
                            //symbol.BidPrice = kline.ClosePrice;
                        }
                        finally
                        {
                            symbolListSemaphore.Release();
                        }
                    }
                }
            });
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);


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
                    CryptoCandleList tempList = klineListTemp[symbol.Name];
                    CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;

                    //await symbol.Lock("kucoin kline ticker2");
                    await symbolListSemaphore.WaitAsync();
                    try
                    {
                        // If needed add dummy candle(s) with the same price as the last candle
                        if (symbolPeriod.CandleList.Count > 0 && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                        {
                            CryptoCandle lastCandle = symbolPeriod.CandleList.Values.Last();
                            while (lastCandle.OpenTime < expectedCandlesUpto)
                            {
                                // Als deze al aanwezig dmv een ticker update niet dupliceren
                                long nextCandleUnix = lastCandle.OpenTime + interval.Duration;
                                if (tempList.TryGetValue(nextCandleUnix, out CryptoCandle? nextCandle))
                                    break;

                                // Dplicate the last candle if it is not present ("flat" candle)
                                if (!symbolPeriod.CandleList.TryGetValue(nextCandleUnix, out nextCandle))
                                {
                                    nextCandle = new();
                                    tempList.Add(nextCandleUnix, nextCandle);
                                    nextCandle.OpenTime = nextCandleUnix;
                                    nextCandle.Open = lastCandle.Close;
                                    nextCandle.High = lastCandle.Close;
                                    nextCandle.Low = lastCandle.Close;
                                    nextCandle.Close = lastCandle.Close;
#if SUPPORTBASEVOLUME
                                    nextCandle.BaseVolume = 0; // no volume (flat candle)
#endif
                                    nextCandle.Volume = 0; // no volume (flat candle)
                                    lastCandle = nextCandle;
                                }
                                else break;
                            }
                        }


                        // Finally do something with the cached data
                        foreach (CryptoCandle candle in tempList.Values.ToList())
                        {
                            if (candle.OpenTime <= expectedCandlesUpto)
                            {
                                tempList.Remove(candle.OpenTime);
                                Interlocked.Increment(ref TickerCount);
                                if (TickerCount > 999999999)
                                    Interlocked.Exchange(ref TickerCount, 0);


                                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                                await CandleTools.Process1mCandleAsync(symbol, candle.Date, candle.Open, candle.High, candle.Low, candle.Close,
#if SUPPORTBASEVOLUME
                                    candle.BaseVolume, 
#else
                                    0,
#endif
                                    (decimal)candle.Volume);

                                //if (symbol.Name == "GAMEUSDT") // debug very low volume symbol
                                //    GlobalData.AddTextToLogTab($"candle added {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");

                                // Debug...
                                //GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));

                                // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
                                if (candle.OpenTime == expectedCandlesUpto)
                                {
                                    //GlobalData.AddTextToLogTab("Aanbieden analyze " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
                                    GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candle);
                                }
                            }
                            else break;
                        }

                    }
                    finally
                    {
                        symbolListSemaphore.Release();
                    }
                }

                if (sender is System.Timers.Timer t)
                {
                    t.Interval = GetInterval();
                    t.Start();
                }
            });
            timerKline.Interval = GetInterval();
            timerKline.Start();
        }

        return subscriptionResult;
    }

}