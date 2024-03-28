using Bybit.Net.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BybitFutures;

public class PriceTickerItem
{
    public int TickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> Symbols = [];

    public async Task StartAsync()
    {
        //bool first = true;
        //GlobalData.AddTextToLogTab($"{Api.ExchangeName} Starting price ticker stream");
        socketClient = new();
        CallResult<UpdateSubscription> subscriptionResult = await socketClient.V5LinearApi.SubscribeToTickerUpdatesAsync(Symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                    {
                        TickerCount++;
                        // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                        //symbol.OpenPrice = tick.OpenPrice;
                        //symbol.HighPrice = tick.HighPrice;
                        //symbol.LowPrice = tick.LowPrice;
                        if (tick.LastPrice.HasValue)
                            symbol.LastPrice = (decimal)tick.LastPrice;
                        //if (tick.BestBidPrice.HasValue)
                        //    symbol.BidPrice = tick.BestBidPrice;
                        //if (tick.BestAskPrice.HasValue)
                        //    symbol.AskPrice = tick.BestAskPrice;
                        //symbol.Volume = tick.BaseVolume; //?
                        if (tick.Turnover24h.HasValue)
                            symbol.Volume = (decimal)tick.Turnover24h; //= Quoted = het volume * de prijs
                        
                        //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                

                        if (tick.FundingRate.HasValue)
                            symbol.FundingRate = (decimal)tick.FundingRate;

                        //"nextFundingTime": "1673280000000",

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

                        //Bewaren voor debug werkzaamheden
                        //if (first && tick.Symbol == "BTCUSDT")
                        //{
                        //    first = false;
                        //    string filename = GlobalData.GetBaseDir();
                        //    filename += @"\Bybit\";
                        //    Directory.CreateDirectory(filename);
                        //    filename += "PriceTicker.json";

                        //    string text = JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                        //    File.WriteAllText(filename, text);
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
            _subscription = subscriptionResult.Data;

            // Events
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
            //GlobalData.AddTextToLogTab($"{Api.ExchangeName} started price ticker stream for {Symbols.Count} symbols");
        }
        else
        {
            GlobalData.AddTextToLogTab($"ERROR {Api.ExchangeName} starting price ticker stream " + subscriptionResult.Error.Message);
            GlobalData.AddTextToLogTab($"ERROR {Api.ExchangeName} starting price ticker stream " + String.Join(',', Symbols));

        }
    }

    public async Task StopAsync()
    {
        string GroupName = "";
        ScannerLog.Logger.Trace($"kline ticker stopping for group {GroupName}");
        if (_subscription != null)
        {
            _subscription.Exception -= Exception;
            _subscription.ConnectionLost -= ConnectionLost;
            _subscription.ConnectionRestored -= ConnectionRestored;

            await socketClient?.UnsubscribeAsync(_subscription);
            _subscription = null;

            socketClient?.Dispose();
            socketClient = null;
        }
        ScannerLog.Logger.Trace($"kline ticker stopped for group {GroupName}");
    }

    private void ConnectionLost()
    {
        //ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker stream connection lost.");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker stream connection restored.");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker stream connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}