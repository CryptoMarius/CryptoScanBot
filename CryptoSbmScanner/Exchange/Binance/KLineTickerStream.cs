using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Binance;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerStream
{

    public string quote;
    private BinanceSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> symbols = new();
    public int TickerCount = 0;

    public KLineTickerStream(CryptoQuoteData quoteData)
    {
        quote = quoteData.Name;
    }

    private void ProcessCandle(BinanceStreamKlineData temp)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(temp.Symbol, out CryptoSymbol symbol))
            {
                TickerCount++;
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));

                CryptoCandle candle = null;
                Monitor.Enter(symbol.CandleList);
                try
                {
                    // Dit is de laatste bekende prijs (de priceticker vult aan)
                    symbol.LastPrice = temp.Data.ClosePrice;

                    // Process the single 1m candle
                    candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], temp.Data.OpenTime,
                        temp.Data.OpenPrice, temp.Data.HighPrice, temp.Data.LowPrice, temp.Data.ClosePrice, temp.Data.QuoteVolume);
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


    public async Task StartAsync()
    {
        if (symbols.Count > 0)
        {
            socketClient = new BinanceSocketClient();
            CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbols, KlineInterval.OneMinute, (data) =>
            {
                if (data.Data.Data.Final)
                {
                    Task.Run(() => { ProcessCandle(data.Data as BinanceStreamKlineData); });
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
                //GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m started candle stream {symbols.Count} symbols");
            }
            else
            {
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m ERROR starting candle stream {subscriptionResult.Error.Message}");
            }
        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return; // Task.CompletedTask;

        //GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m stopping candle stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);

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
