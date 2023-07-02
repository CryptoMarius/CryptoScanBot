using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Bybit;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class BybitStream1mCandles
{

    public string quote;
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> symbols = new();
    public int CandlesKLinesCount = 0;

    public BybitStream1mCandles(CryptoQuoteData quoteData)
    {
        quote = quoteData.Name;
    }

    private void ProcessCandle(string topic, BybitKlineUpdate kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        CandlesKLinesCount++;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(topic, out CryptoSymbol symbol))
            {
                CryptoCandle candle = null;
                Monitor.Enter(symbol.CandleList);
                try
                {
                    // Dit is de laatste bekende prijs (de priceticker vult aan)
                    symbol.LastPrice = kline.ClosePrice;

                    // Process the single 1m candle
                    candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], kline.StartTime,
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
                if (GlobalData.ApplicationStatus == CryptoApplicationStatus.AppStatusRunning && candle != null)
                {
                    // Aanbieden voor analyse
                    GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
                }
            }
        }

    }


    public async Task StartAsync()
    {
        if (symbols.Count > 0)
        {
            GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m started candle stream {1} symbols", quote, symbols.Count));

            socketClient = new BybitSocketClient();
            var subscriptionResult = await socketClient.V5SpotStreams.SubscribeToKlineUpdatesAsync(
                symbols, KlineInterval.OneMinute, data =>
            {
                //if (data.Data.Data.Confirm)
                {
                    //Er zit tot ongeveer 8 a 10 seconden vertraging is van Binance tot hier, dat moet ansich genoeg zijn
                    //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.Symbol));

                    foreach (BybitKlineUpdate kline in data.Data)
                    {
                        if (kline.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                            Task.Run(() => { ProcessCandle(data.Topic, kline); });
                    }

                }
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
                //            await client.SpotApi.Account.KeepAliveUserStreamAsync(subscriptionResult.Data.); //???
                //            await Task.Delay(TimeSpan.FromMinutes(30));
                //        }
                //    });
            }
            else
            {
                GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m ERROR starting candle stream {1}", quote, subscriptionResult.Error.Message));
                GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m ERROR starting candle stream {1}", quote, String.Join(',', symbols)));
                
            }
        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return; // Task.CompletedTask;

        GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m stopping candle stream", quote));

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);

        return; // Task.CompletedTask;
    }

    private void ConnectionLost()
    {
        GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m candle stream connection lost.", quote));
        GlobalData.ConnectionWasLost("");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab(string.Format("Bybit {0} 1m candle stream connection restored.", quote));
        GlobalData.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"Bybit 1m candle stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
