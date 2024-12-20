using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Clients;

namespace ExchangeTest.Exchange.Kucoin.Futures;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    //KucoinKline klinePrev;
    //long klinePrevOpenTime = 0;
    //#if KUCOINDEBUG
    //private static int tickerIndex = 0;
    //#endif

    CryptoSymbol Symbol = null!;

    //static double GetInterval()
    //{
    //    // bewust 5 seconden en een beetje layer zodat we zeker weten dat de kline er is
    //    // (anders zou deze 60 seconden later alsnog verwerkt worden, maar dat is te laat)
    //    DateTime now = DateTime.Now;
    //    return 5050 + ((60 - now.Second) * 1000 - now.Millisecond);
    //}


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        string symbolName = "";
        foreach (var symbol in SymbolList)
        {
            Symbol = symbol;
            if (symbolName == "")
                symbolName = symbol.Base + "-" + symbol.Quote;
            else
                symbolName += "," + symbol.Base + "-" + symbol.Quote;
        }

        //CryptoCandleList klineListTemp = [];

        //if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
        //    throw new Exception("Geen intervallen?");


        //TODO!!!!
        // Problem, can't get this to work, this is blocking..

        //await ((KucoinSocketClient)TickerGroup.SocketClient).???????????? missing in action? Overlooking something???

        // Implementatie kline ticker (via cache, wordt door de timer verwerkt)
        TickerGroup!.SocketClient ??= new KucoinSocketClient();
        //var subscriptionResult = await ((KucoinSocketClient)TickerGroup.SocketClient).FuturesApi.SubscribeToKlineUpdatesAsync(symbolName, KlineInterval.OneMinute, data =>
        //{
        //    Task taskKline = Task.Run(() =>
        //    {
        //        KucoinKline kline = data.Data.Candles;
        //        //string json = JsonSerializer.Serialize(kline, ExchangeHelper.JsonSerializerNotIndented);
        //        //ScannerLog.Logger.Trace($"kline ticker {data.Topic} {json}");

        //        //TickerCount++;
        //        //ScannerLog.Logger.Trace(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));

        //        Monitor.Enter(Symbol.CandleList);
        //        try
        //        {
        //            // Toevoegen aan de lokale cache en/of aanvullen
        //            // (via de cache omdat de candle in opbouw is)
        //            // (bij veel updates is dit stukje cpu-intensief?)
        //            long candleOpenUnix = CandleTools.GetUnixTime(kline.OpenTime, 60);
        //            if (!klineListTemp.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
        //            {
        //                //TickerCount++;
        //                candle = new();
        //                klineListTemp.TryAdd(candleOpenUnix, candle);
        //            }
        //            candle.IsDuplicated = false;
        //            candle.OpenTime = candleOpenUnix;
        //            candle.Open = kline.OpenPrice;
        //            candle.High = kline.HighPrice;
        //            candle.Low = kline.LowPrice;
        //            candle.Close = kline.ClosePrice;
        //            candle.Volume = kline.QuoteVolume;
        //            //ScannerLog.Logger.Trace($"candle update {candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, true)}");

        //            // Dit is de laatste bekende prijs (de priceticker vult eventueel aan)
        //            Symbol.LastPrice = kline.ClosePrice;
        //            Symbol.AskPrice = kline.ClosePrice;
        //            Symbol.BidPrice = kline.ClosePrice;
        //        }
        //        finally
        //        {
        //            Monitor.Exit(Symbol.CandleList);
        //        }
        //    });
        //}, ExchangeBase.CancellationToken).ConfigureAwait(false);


        // Implementatie kline timer (fix)
        // Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
        // kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
        // De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

        //        if (subscriptionResult.Success)
        //        {
        //            System.Timers.Timer timerKline = new()
        //            {
        //                AutoReset = false,
        //            };
        //            timerKline.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) =>
        //            {
        //                CryptoSymbolInterval symbolPeriod = Symbol.GetSymbolInterval(interval.IntervalPeriod);
        //                long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;

        //                // locking.. nog eens nagaan of dat echt noodzakelijk is hier.
        //                // in principe kun je hier geen "collision" hebben met threads?
        //                Monitor.Enter(Symbol.CandleList);
        //                try
        //                {
        //                    // De niet aanwezige candles dupliceren (pas als de candles zijn opgehaald)
        //                    if (symbolPeriod.CandleList.Count > 0 && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
        //                    {
        //                        CryptoCandle lastCandle = symbolPeriod.CandleList.Values.Last();
        //                        while (lastCandle.OpenTime < expectedCandlesUpto)
        //                        {
        //                            // Als deze al aanwezig dmv een ticker update niet dupliceren
        //                            long nextCandleUnix = lastCandle.OpenTime + interval.Duration;
        //                            if (klineListTemp.TryGetValue(nextCandleUnix, out CryptoCandle? nextCandle))
        //                                break;

        //                            // Dupliceer de laatste candle als deze niet voorkomt (zogenaamde "flat" candle)
        //                            // En zet deze in de kline list cache (anders teveel duplicatie van de logica)
        //                            if (!symbolPeriod.CandleList.TryGetValue(nextCandleUnix, out nextCandle))
        //                            {
        //                                nextCandle = new();
        //                                klineListTemp.Add(nextCandleUnix, nextCandle);
        //                                nextCandle.IsDuplicated = true;
        //                                nextCandle.OpenTime = nextCandleUnix;
        //                                nextCandle.Open = lastCandle.Close;
        //                                nextCandle.High = lastCandle.Close;
        //                                nextCandle.Low = lastCandle.Close;
        //                                nextCandle.Close = lastCandle.Close;
        //                                nextCandle.Volume = 0; // geen volume
        //                                lastCandle = nextCandle;
        //                            }
        //                            else break;
        //                        }
        //                    }


        //                    // De data van de ticker updates en duplicatie verwerken
        //                    foreach (CryptoCandle candle in klineListTemp.Values.ToList())
        //                    {
        //                        if (candle.OpenTime <= expectedCandlesUpto)
        //                        {
        //                            klineListTemp.Remove(candle.OpenTime);

        //                            //Process1mCandle(Symbol, candle.Date, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
        //                            CandleTools.CreateCandle(Symbol, interval, candle.Date, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, candle.IsDuplicated);
        //                            //SaveCandleAndUpdateHigherTimeFrames(Symbol, candle);
        //                            // Calculate higher timeframes
        //                            long candle1mCloseTime = candle.OpenTime + interval.Duration;
        //                            foreach (CryptoInterval interval in GlobalData.IntervalList)
        //                            {
        //                                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
        //                                {
        //                                    CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, Symbol, candle1mCloseTime);
        //                                    CandleTools.UpdateCandleFetched(Symbol, interval);
        //                                }
        //                            }

        //                            // Dit is de laatste bekende prijs (de priceticker vult eventueel aan)
        //                            Symbol.LastPrice = candle.Close;
        //                            Symbol.AskPrice = candle.Close;
        //                            Symbol.BidPrice = candle.Close;

        //                            //if (candle.IsDuplicated)
        //                            //    ScannerLog.Logger.Trace("Dup candle " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, true));
        //                            //else
        //                            //    ScannerLog.Logger.Trace("New candle " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, true));

        //                            // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
        //                            if (candle.OpenTime == expectedCandlesUpto)
        //                            {
        //                                //ScannerLog.Logger.Trace("Aanbieden analyze " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, true));
        //                                TickerCount++;
        //                                if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
        //                                    GlobalData.ThreadMonitorCandle?.AddToQueue(Symbol, candle);
        //                            }
        //                        }
        //                        else break;
        //                    }

        //                }
        //                finally
        //                {
        //                    Monitor.Exit(Symbol.CandleList);
        //                }

        //                if (sender is System.Timers.Timer t)
        //                {
        //                    t.Interval = GetInterval();
        //                    t.Start();
        //                }
        //            });
        //            timerKline.Interval = GetInterval();
        //            timerKline.Start();
        //        }

        return null; // subscriptionResult;
    }

}