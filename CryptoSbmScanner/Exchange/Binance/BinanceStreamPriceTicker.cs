using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Binance;

public class BinanceStreamPriceTicker
{
    public int tickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
    private BinanceSocketClient socketClient;
    private UpdateSubscription _subscription;

    public async Task ExecuteAsync()
    {
        GlobalData.AddTextToLogTab("Starting price ticker stream");

        // Wordt gebruikt voor het zetten van de 24 uur volume
        socketClient = new BinanceSocketClient();

        //
        CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotStreams.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                foreach (BinanceStreamTick tick in data.Data.Cast<BinanceStreamTick>())
                {
                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                    {
                        tickerCount++;

                        // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                        //symbol.OpenPrice = tick.OpenPrice;
                        //symbol.HighPrice = tick.HighPrice;
                        //symbol.LowPrice = tick.LowPrice;
                        symbol.LastPrice = tick.LastPrice;
                        symbol.BidPrice = tick.BestBidPrice;
                        symbol.AskPrice = tick.BestAskPrice;
                        //symbol.Volume = tick.BaseVolume; //?
                        symbol.Volume = tick.QuoteVolume; //= Quoted = het volume * de prijs                                


                        // Hiermee kunnen we een "toekomstige" candle opbouwen.
                        // (maar de berekeningen verwachten dat niet en dan gaan er zaken fout)
                        // Kortom: Beslissingen op basis van niet voltooide candles moet je vermijden.
                        //try
                        //{
                        //Monitor.Enter(symbol.CandleList);
                        //try
                        //{
                        //    //symbol.HandleExchangeMiniTick(GlobalData.Settings, symbol, tick);
                        //}
                        //catch (Exception error)
                        //{
                        //    GlobalData.AddTextToLogTab(error.ToString());
                        //}
                        //}
                        //finally
                        //{
                        //    Monitor.Exit(symbol.CandleList);
                        //}
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
            return;

        GlobalData.AddTextToLogTab("Stopping price ticker stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.UnsubscribeAsync(_subscription);

        return;
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
