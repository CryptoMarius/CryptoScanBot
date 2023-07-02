using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Bybit;

public class BybitStreamPriceTicker
{
    public int tickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;

    public async Task ExecuteAsync(List<string> symbols)
    {
        GlobalData.AddTextToLogTab("Bybit Starting price ticker stream");

        // Wordt gebruikt voor het zetten van de 24 uur volume
        //socketClient = new BybitSocketClient();

        socketClient = new();
        //CallResult<UpdateSubscription> subscriptionResult2 = await socketClient.V5SpotStreams.SubscribeToTickerUpdatesAsync(symbols, data =>
        //{
        //    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        //    {
        //        var tick = data.Data;
        //        //foreach (var tick in data.Data)
        //        {
        //            tickerCount++;

        //            if (exchange.SymbolListName.TryGetValue(data.Topic, out CryptoSymbol symbol))
        //            {
        //                // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
        //                //symbol.OpenPrice = tick.OpenPrice;
        //                //symbol.HighPrice = tick.HighPrice24h;
        //                //symbol.LowPrice = tick.LowPrice24h;
        //                symbol.LastPrice = tick.LastPrice;
        //                //symbol.BidPrice = tick.BestBidPrice;
        //                //symbol.AskPrice = tick.BestAskPrice;
        //                //symbol.Volume = tick.BaseVolume; //?
        //                symbol.Volume = tick.Volume24h; //= Quoted = het volume * de prijs                                


        //                // Hiermee kunnen we een "toekomstige" candle opbouwen.
        //                // (maar de berekeningen verwachten dat niet en dan gaan er zaken fout)
        //                // Kortom: Beslissingen op basis van niet voltooide candles moet je vermijden.
        //                //try
        //                //{
        //                //Monitor.Enter(symbol.CandleList);
        //                //try
        //                //{
        //                //    //symbol.HandleExchangeMiniTick(GlobalData.Settings, symbol, tick);
        //                //}
        //                //catch (Exception error)
        //                //{
        //                //    GlobalData.AddTextToLogTab(error.ToString());
        //                //}
        //                //}
        //                //finally
        //                //{
        //                //    Monitor.Exit(symbol.CandleList);
        //                //}
        //            }
        //        }

        //        if (tickerCount > 999999999)
        //            tickerCount = 0;
        //    }
        //});


        CallResult<UpdateSubscription> subscriptionResult = await socketClient.V5LinearStreams.SubscribeToTickerUpdatesAsync(symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                BybitLinearTickerUpdate tick = data.Data;
                {
                    tickerCount++;

                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                    {
                        // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                        //symbol.OpenPrice = tick.OpenPrice;
                        //symbol.HighPrice = tick.HighPrice;
                        //symbol.LowPrice = tick.LowPrice;
                        if (tick.LastPrice.HasValue)
                            symbol.LastPrice = tick.LastPrice;
                        if (tick.BestBidPrice.HasValue)
                            symbol.BidPrice = tick.BestBidPrice;
                        if (tick.BestAskPrice.HasValue)
                            symbol.AskPrice = tick.BestAskPrice;
                        //symbol.Volume = tick.BaseVolume; //?
                        if (tick.Volume24h.HasValue)
                            symbol.Volume = (decimal)tick.Volume24h; //= Quoted = het volume * de prijs                                
                        //else
                          //  symbol.Volume = 0; // Wat moet je anders?


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
            GlobalData.AddTextToLogTab("ERROR Bybit starting price ticker stream " + subscriptionResult.Error.Message);
            GlobalData.AddTextToLogTab("ERROR Bybit starting price ticker stream " + String.Join(',', symbols));

        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return;

        GlobalData.AddTextToLogTab("Bybit stopping price ticker stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.UnsubscribeAsync(_subscription);

        return;
    }

    private void ConnectionLost()
    {
        GlobalData.AddTextToLogTab("Bybit price ticker stream connection lost.");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab("Bybit price ticker stream connection restored.");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"Bybit price ticker stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
