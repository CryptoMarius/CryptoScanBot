using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Spot;
using Kucoin.Net.Objects.Models.Spot.Socket;

using NPOI.Util;

namespace CryptoSbmScanner.Exchange.Kucoin;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerStream
{
    //KucoinKline klinePrev;
    //long klinePrevOpenTime = 0;
#if KUCOINDEBUG
    private static int tickerIndex = 0;
#endif
    public string quote;
    public int TickerCount = 0;
    private KucoinSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> symbols = new();
    

    public KLineTickerStream(CryptoQuoteData quoteData)
    {
        quote = quoteData.Name;
    }

    private void ProcessCandle(string topic, KucoinKline kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        // De interval wordt geprefixed in de topic (rare jongens bij Bybit, extra veld?)
        //string x[] = topic.Split('-');
        //string symbolName = topic; // string.Join(); // [2..];

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            string symbolName = topic.Replace("-", "");
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                TickerCount++;
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));

                CryptoCandle candle = null;
                Monitor.Enter(symbol.CandleList);
                try
                {
                    // Dit is de laatste bekende prijs (de priceticker vult aan)
                    symbol.LastPrice = kline.ClosePrice;
                    symbol.AskPrice = kline.ClosePrice;
                    symbol.BidPrice = kline.ClosePrice;

                    // Process the single 1m candle
                    candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume);
#if SQLDATABASE
                    GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif

                    // Calculate the higher timeframes
                    foreach (CryptoInterval interval in GlobalData.IntervalList)
                    {
                        // Deze doen een call naar de TaskSaveCandles en doet de UpdateCandleFetched (wellicht overlappend?)
                        if (interval.ConstructFrom != null)
                            CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle.OpenTime);
                        CandleTools.UpdateCandleFetched(symbol, interval);
                    }
                }
                finally
                {
                    Monitor.Exit(symbol.CandleList);
                }


                // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
                if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running && candle != null)
                {
                    // Aanbieden voor analyse
                    GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
                }
            }
        }

    }


    public async Task StartAsync(KucoinSocketClient socketClient)
    {
        if (symbols.Count > 0)
        {
            KucoinKline klinePrev;
            long klinePrevOpenTime = 0; // Unix time, OpenTime module 60

            string symbolNames = string.Join(',', symbols);
            //socketClient = new KucoinSocketClient(); wel of niet (geeft problemen bij de Close()?)
            var subscriptionResult = await socketClient.SpotApi.SubscribeToKlineUpdatesAsync(symbolNames,
                KlineInterval.OneMinute, data =>
            {
                KucoinKline kline = data.Data.Candles;
                long klineOpenTime = CandleTools.GetUnixTime(kline.OpenTime, 60);

                if (klinePrevOpenTime == 0)
                {
                    klinePrev = kline;
                    klinePrevOpenTime = CandleTools.GetUnixTime(kline.OpenTime, 60);
                }


                // Het is een definitieve candle (niet eentje in opbouw)
                // We gaan ervan uit dat de laatste aangeboden info klopt..
                bool final = false;
                if (klineOpenTime - klinePrevOpenTime >= 60)
                {
                    final = true;
                    klinePrev = kline;
                    klinePrevOpenTime = klineOpenTime;
                    Task.Run(() => { ProcessCandle(data.Topic, kline); });
                }

#if KUCOINDEBUG
                //Debug
                tickerIndex++;
                long unix = CandleTools.GetUnixTime(kline.OpenTime, 60);
                string filename = GlobalData.GetBaseDir() + $@"\Kucoin\Kline-{data.Topic}-1m-{unix}-#{tickerIndex}-{final}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(kline, new System.Text.Json.JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });
                File.WriteAllText(filename, text);
#endif

            });
            // .ConfigureAwait(false);

            // Subscribe to network-related stuff
            if (subscriptionResult.Success)
            {
                _subscription = subscriptionResult.Data;

                // Events
                _subscription.Exception += Exception;
                _subscription.ConnectionLost += ConnectionLost;
                _subscription.ConnectionRestored += ConnectionRestored;


                //    // TODO: Put a CancellationToken in order to stop it gracefully
                //    BinanceClient client = new();
                //    var keepAliveTask = Task.Run(async () =>
                //    {
                //        while (true)
                //        {
                //            await client.V5LinearApi.Account.KeepAliveUserStreamAsync(subscriptionResult.Data.); //???
                //            await Task.Delay(TimeSpan.FromMinutes(30));
                //        }
                //    });
                //GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m started candle stream {1} symbols", quote, symbols.Count));
            }
            else
            {
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m ERROR starting candle stream {subscriptionResult.Error.Message}");
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m ERROR starting candle stream {symbolNames}");
                
            }
        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return; // Task.CompletedTask;
        if (socketClient == null)
            return; // Task.CompletedTask;

        //GlobalData.AddTextToLogTab("Bybit {quote} 1m stopping candle stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;
        try
        {
            //socketClient.CurrentSubscriptions?
            // Null pointers? TODO: Nazoeken!
            await socketClient?.UnsubscribeAsync(_subscription);
        }
        catch
        {
            // dont care
        }

        return; // Task.CompletedTask;
    }

    private void ConnectionLost()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m candle stream connection lost.");
        ScannerSession.ConnectionWasLost("");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m candle stream connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} 1m candle stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
