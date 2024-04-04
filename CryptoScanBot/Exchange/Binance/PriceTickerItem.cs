using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Binance;

public class PriceTickerItem
{
    public int TickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
    private BinanceSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> Symbols = [];
    public string GroupName = "";

    public async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} already started");
            return;
        }

        ScannerLog.Logger.Trace($"price ticker for group {GroupName} starting");

        socketClient = new();
        CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                foreach (BinanceStreamTick tick in data.Data.Cast<BinanceStreamTick>())
                {
                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                    {
                        TickerCount++;

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

                if (TickerCount > 999999999)
                    TickerCount = 0;
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);

        // Subscribe to network-related stuff
        if (subscriptionResult.Success)
        {
            //ErrorDuringStartup = false;
            _subscription = subscriptionResult.Data;

            // Events
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
            //GlobalData.AddTextToLogTab($"{Api.ExchangeName} started price ticker stream for all symbols");
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} started");
        }
        else
        {
            _subscription = null;
            socketClient.Dispose();
            socketClient = null;
            //ConnectionLostCount++;
            //ErrorDuringStartup = true;
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
            GlobalData.AddTextToLogTab($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"price ticker for group {GroupName} stopping");
        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;
        socketClient?.Dispose();
        socketClient = null;
        ScannerLog.Logger.Trace($"price ticker for group {GroupName} stopped");
    }


    private void ConnectionLost()
    {
        //ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection lost for group {GroupName}.");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection restored for group {GroupName}.");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
