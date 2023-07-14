using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;

namespace CryptoSbmScanner.Exchange.Kucoin;

public class PriceTickerStream
{
    public int TickerCount = 0; //Tellertje om te laten zien dat de stream doorloopt (anders geen candle uupdates)
    private KucoinSocketClient socketClient;
    private UpdateSubscription _subscription;
    public List<string> Symbols = new();

    public async Task StartAsync()
    {
        //GlobalData.AddTextToLogTab("Bybit Starting price ticker stream");
        socketClient = new();
        CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotApi.SubscribeToAllTickerUpdatesAsync(data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    string symbolName = tick.Symbol.Replace("-", "");
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
                    {
                        TickerCount++;
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
                        if (tick.LastQuantity.HasValue)
                            symbol.Volume = (decimal)tick.LastQuantity; //= Quoted = het volume * de prijs                                
                                                                        //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                

                        //bool first = true;
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
        }).ConfigureAwait(false);

        // Subscribe to network-related stuff
        if (subscriptionResult.Success)
        {
            _subscription = subscriptionResult.Data;

            // Events
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
            //GlobalData.AddTextToLogTab(string.Format("Bybit started price ticker stream for {0} symbols", Symbols.Count));
        }
        else
        {
            GlobalData.AddTextToLogTab($"ERROR {Api.ExchangeName}  starting price ticker stream " + subscriptionResult.Error.Message);
            GlobalData.AddTextToLogTab($"ERROR {Api.ExchangeName}  starting price ticker stream " + String.Join(',', Symbols));

        }
    }

    public async Task StopAsync()
    {
        if (_subscription == null)
            return;

        //GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping price ticker stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.UnsubscribeAsync(_subscription);

        return;
    }

    private void ConnectionLost()
    {
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
