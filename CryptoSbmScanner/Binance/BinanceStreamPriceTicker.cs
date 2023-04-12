using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using System;
using System.Threading.Tasks;

namespace CryptoSbmScanner
{
    public class BinanceStreamPriceTicker
    {
        public int tickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
        private BinanceSocketClient socketClient;
        private UpdateSubscription _subscription;

        public async Task ExecuteAsync()
        {
            GlobalData.AddTextToLogTab("Starting ticker stream");

            // Wordt gebruikt voor het zetten van de 24 uur volume
            socketClient = new BinanceSocketClient();

            //
            CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotStreams.SubscribeToAllTickerUpdatesAsync((data) =>
            {
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out CryptoExchange exchange))
                {
                    //GET /api/v3/ticker/24hr
                    //client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);
                    //Die heeft dan een ask en een bid price

                    foreach (BinanceStreamTick tick in data.Data)
                    {
                        tickerCount++;

                        if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                        {
                            //Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                            symbol.OpenPrice = tick.OpenPrice;
                            symbol.HighPrice = tick.HighPrice;
                            symbol.LowPrice = tick.LowPrice;
                            symbol.LastPrice = tick.LastPrice;
                            symbol.BidPrice = tick.BestBidPrice;
                            symbol.AskPrice = tick.BestAskPrice;
                            //symbol.Volume = tick.BaseVolume; //=Quoted = het volume wat verhandeld is
                            symbol.Volume = tick.QuoteVolume; //=Quoted = het volume * de prijs                                
                        }
                    }

                    if (tickerCount > 999999999)
                        tickerCount = 0;
                }
            }).ConfigureAwait(false);

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
                GlobalData.AddTextToLogTab("ERROR starting price ticker stream " + subscriptionResult.Error.Message);

            }
        }

        public async Task StopAsync()
        {
            if (_subscription == null)
                return; // Task.CompletedTask;

            GlobalData.AddTextToLogTab("Stopping price ticker stream");

            _subscription.Exception -= Exception;
            _subscription.ConnectionLost -= ConnectionLost;
            _subscription.ConnectionRestored -= ConnectionRestored;

            await socketClient.UnsubscribeAsync(_subscription);

            return; // Task.CompletedTask;
        }

        private void ConnectionLost()
        {
            GlobalData.AddTextToLogTab("Binance price ticker stream connection lost.");
        }

        private void ConnectionRestored(TimeSpan timeSpan)
        {
            GlobalData.AddTextToLogTab("Binance price ticker stream connection restored.");
        }

        private void Exception(Exception ex)
        {
            GlobalData.AddTextToLogTab($"Binance price ticker stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
        }

    }
}