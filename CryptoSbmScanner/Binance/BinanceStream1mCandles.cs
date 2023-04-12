using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Objects;

namespace CryptoSbmScanner
{
    /// <summary>
    /// Monitoren van 1m candles (die gepushed worden door Binance)
    /// </summary>
    public class BinanceStream1mCandles
    {

        public string quote;
        private BinanceSocketClient socketClient;
        private UpdateSubscription _subscription;
        public List<string> symbols = new List<string>();
        public int CandlesKLinesCount = 0;

        public BinanceStream1mCandles(CryptoQuoteData quoteData)
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

            CandlesKLinesCount++;

            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                CryptoSymbol symbol;
                if (exchange.SymbolListName.TryGetValue(temp.Symbol, out symbol))
                {
                    //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));

                    CryptoCandle candle = null;
                    Monitor.Enter(symbol.CandleList);
                    try
                    {
                        // Verwerk de 1m candle in onze data
                        candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], temp.Data.OpenTime,
                            temp.Data.OpenPrice, temp.Data.HighPrice, temp.Data.LowPrice, temp.Data.ClosePrice, temp.Data.QuoteVolume);
                        if (!symbol.LastPrice.HasValue)
                            symbol.LastPrice = temp.Data.ClosePrice;

                        // Herbereken de candles in andere intervallen
                        foreach (CryptoInterval interval in GlobalData.IntervalList)
                        {
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
                    if ((GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning) && (candle != null))
                    {
                        // Aanbieden voor analyse
                        GlobalData.ThreadCreateSignal.AddToQueue(candle);

                        // Het signal monitoring aanroepen (In ieder geval aanroepen)?
                        //if ((symbol.SignalList.Count + symbol.PositionList.Count) > 0)
                        //    GlobalData.TaskMonitorSignal.AddToQueue(symbol);

                        //if (GlobalData.Settings.BalanceBot.Active && (symbol.IsBalancing))
                        //    GlobalData.ThreadBalanceSymbols.AddToQueue(symbol);
                    }
                }
            }

        }


        public async Task StartAsync()
        {
            if (symbols.Count > 0)
            {
                GlobalData.AddTextToLogTab(String.Format("Binance {0} 1m started candle stream {1} symbols", quote, symbols.Count));

                socketClient = new BinanceSocketClient();
                CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotStreams.SubscribeToKlineUpdatesAsync(symbols, KlineInterval.OneMinute, (data) =>
                {
                    if (data.Data.Data.Final)
                    {
                        //Er zit tot ongeveer 8 a 10 seconden vertraging is van Binance tot hier, dat moet ansich genoeg zijn
                        //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.Symbol));

                        //GlobalData.ThreadProcessCandle.AddToQueue(data as BinanceStreamKlineData);
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
                }
                else
                {
                    GlobalData.AddTextToLogTab(string.Format("Binance {0} 1m ERROR starting candle stream {1}", quote, subscriptionResult.Error.Message));
                }
            }
        }

        public async Task StopAsync()
        {
            if (_subscription == null)
                return; // Task.CompletedTask;

            GlobalData.AddTextToLogTab(string.Format("Binance {0} 1m stopping candle stream", quote));

            _subscription.Exception -= Exception;
            _subscription.ConnectionLost -= ConnectionLost;
            _subscription.ConnectionRestored -= ConnectionRestored;

            await socketClient?.UnsubscribeAsync(_subscription);

            return; // Task.CompletedTask;
        }

        private void ConnectionLost()
        {
            GlobalData.AddTextToLogTab(String.Format("Binance {0} 1m candle stream connection lost.", quote));
            GlobalData.ConnectionWasLost("");
        }

        private void ConnectionRestored(TimeSpan timeSpan)
        {
            GlobalData.AddTextToLogTab(String.Format("Binance {0} 1m candle stream connection restored.", quote));
            GlobalData.ConnectionWasRestored("");
        }

        private void Exception(Exception ex)
        {
            GlobalData.AddTextToLogTab($"Binance 1m candle stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
        }

    }
}