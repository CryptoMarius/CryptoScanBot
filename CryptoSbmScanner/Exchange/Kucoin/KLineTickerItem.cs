using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Spot;
using Kucoin.Net.Objects.Models.Spot.Socket;

namespace CryptoSbmScanner.Exchange.Kucoin;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange, 
/// in Kucoin helaas ondersteund door een extra timer)
/// </summary>
public class KLineTickerItem
{
    //KucoinKline klinePrev;
    //long klinePrevOpenTime = 0;
#if KUCOINDEBUG
    private static int tickerIndex = 0;
#endif
    public CryptoQuoteData QuoteData;
    public int TickerCount = 0;
    private KucoinSocketClient _socketClient;
    private UpdateSubscription _subscription;
    public CryptoSymbol Symbol;


    public KLineTickerItem(CryptoQuoteData quoteData)
    {
        QuoteData = quoteData;
    }

    public async Task StartAsync(KucoinSocketClient socketClient)
    {
        _socketClient = socketClient;
        string symbolName = Symbol.Base + "-" + Symbol.Quote;
        SortedList<long, CryptoCandle> klineListTemp = new();

        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            throw new Exception("Geen intervallen?");


        // Implementatie kline ticker (via cache, wordt door de timer verwerkt)
        //var socketClient = new KucoinSocketClient();
        var subscriptionResult = await socketClient.SpotApi.SubscribeToKlineUpdatesAsync(symbolName, KlineInterval.OneMinute, data =>
            {
                Task taskKline = Task.Run(() =>
                {
                    KucoinKline kline = data.Data.Candles;
                    //string text = JsonSerializer.Serialize(kline, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //GlobalData.AddTextToLogTab(data.Topic + " " + text);

//TickerCount++;
//GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));

                    Monitor.Enter(Symbol.CandleList);
                    try
                    {
                        // Toevoegen aan de lokale cache en/of aanvullen
                        // (via de cache omdat de candle in opbouw is)
                        // (bij veel updates is dit stukje cpu-intensief?)
                        long candleOpenUnix = CandleTools.GetUnixTime(kline.OpenTime, 60);
                        if (!klineListTemp.TryGetValue(candleOpenUnix, out CryptoCandle candle))
                        {
                            //TickerCount++;
                            candle = new();
                            klineListTemp.TryAdd(candleOpenUnix, candle);
                        }
                        candle.IsDuplicated = false;
                        candle.OpenTime = candleOpenUnix;
                        candle.Open = kline.OpenPrice;
                        candle.High = kline.HighPrice;
                        candle.Low = kline.LowPrice;
                        candle.Close = kline.ClosePrice;
                        candle.Volume = kline.QuoteVolume;
                        //GlobalData.AddTextToLogTab("Ticker update " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat));

                        // Dit is de laatste bekende prijs (de priceticker vult eventueel aan)
                        Symbol.LastPrice = kline.ClosePrice;
                        Symbol.AskPrice = kline.ClosePrice;
                        Symbol.BidPrice = kline.ClosePrice;
                    }
                    finally
                    {
                        Monitor.Exit(Symbol.CandleList);
                    }
                });
            });

        // Subscribe to network-related stuff
        if (subscriptionResult.Success)
        {
            //GlobalData.AddTextToLogTab($"Subscription succes! {subscriptionResult.Data.Id}");
            _subscription = subscriptionResult.Data;

            // Events
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
        }
        else
        {
            GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m ERROR starting candle stream {Symbol.Name} {subscriptionResult.Error.Message}");
        }




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
            timerKline.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
            {
                CryptoSymbolInterval symbolPeriod = Symbol.GetSymbolInterval(interval.IntervalPeriod);
                long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;

                // locking.. nog eens nagaan of dat echt noodzakelijk is hier.
                // in principe kun je hier geen "collision" hebben met threads?
                Monitor.Enter(Symbol.CandleList);
                try
                {
                    // De niet aanwezige candles dupliceren (pas als de candles zijn opgehaald)
                    if (symbolPeriod.CandleList.Count > 0 && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                    {
                        CryptoCandle lastCandle = symbolPeriod.CandleList.Values.Last();
                        while (lastCandle.OpenTime < expectedCandlesUpto)
                        {
                            // Als deze al aanwezig dmv een ticker update niet dupliceren
                            long nextCandleUnix = lastCandle.OpenTime + interval.Duration;
                            if (klineListTemp.TryGetValue(nextCandleUnix, out CryptoCandle nextCandle))
                                break;

                            // Dupliceer de laatste candle als deze niet voorkomt (zogenaamde "flat" candle)
                            // En zet deze in de kline list cache (anders teveel duplicatie van de logica)
                            if (!symbolPeriod.CandleList.TryGetValue(nextCandleUnix, out nextCandle))
                            {
                                nextCandle = new();
                                klineListTemp.Add(nextCandleUnix, nextCandle);
                                nextCandle.IsDuplicated = true;
                                nextCandle.OpenTime = nextCandleUnix;
                                nextCandle.Open = lastCandle.Close;
                                nextCandle.High = lastCandle.Close;
                                nextCandle.Low = lastCandle.Close;
                                nextCandle.Close = lastCandle.Close;
                                nextCandle.Volume = 0; // geen volume
                                lastCandle = nextCandle;
                            }
                            else break;
                        }
                    }


                    // De data van de ticker updates en duplicatie verwerken
                    foreach (CryptoCandle candle in klineListTemp.Values.ToList())
                    {
                        if (candle.OpenTime <= expectedCandlesUpto)
                        {
                            klineListTemp.Remove(candle.OpenTime);

                            //Process1mCandle(Symbol, candle.Date, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
                            CandleTools.HandleFinalCandleData(Symbol, interval, candle.Date, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, candle.IsDuplicated);
                            //SaveCandleAndUpdateHigherTimeFrames(Symbol, candle);
#if SQLDATABASE
                            GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif
                            // Calculate higher timeframes
                            long candle1mCloseTime = candle.OpenTime + interval.Duration;
                            foreach (CryptoInterval interval in GlobalData.IntervalList)
                            {
                                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                                {
                                    CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, Symbol, candle1mCloseTime);
                                    CandleTools.UpdateCandleFetched(Symbol, interval);
                                }
                            }

                            // Dit is de laatste bekende prijs (de priceticker vult eventueel aan)
                            Symbol.LastPrice = candle.Close;
                            Symbol.AskPrice = candle.Close;
                            Symbol.BidPrice = candle.Close;

                            //if (candle.IsDuplicated)
                            //    GlobalData.AddTextToLogTab("Dup candle " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat));
                            //else
                            //    GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat));

                            // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
                            if (candle.OpenTime == expectedCandlesUpto)
                            {
                                //GlobalData.AddTextToLogTab("Aanbieden analyze " + candle.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat));
                                //GlobalData.AddTextToLogTab("");
                                TickerCount++;
                                if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                                    GlobalData.ThreadMonitorCandle.AddToQueue(Symbol, candle);
                            }
                        }
                        else break;
                    }

                }
                finally
                {
                    Monitor.Exit(Symbol.CandleList);
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
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return;
        if (_socketClient == null)
            return;

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await _socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;

        //_socketClient?.Dispose();
        //_socketClient = null;
    }

    static double GetInterval()
    {
        // bewust 5 seconden en een beetje layer zodat we zeker weten dat de kline er is
        // (anders zou deze 60 seconden later alsnog verwerkt worden, maar dat is te laat)
        DateTime now = DateTime.Now;
        return 5050 + ((60 - now.Second) * 1000 - now.Millisecond);
    }

    private void ConnectionLost()
    {
        //ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m kline ticker connection lost.");
        ScannerSession.ConnectionWasLost("");
    }
    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m kline ticker connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} 1m kline ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}