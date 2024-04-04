using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BybitSpot;

public class PriceTickerItem(string apiName, CryptoQuoteData quoteData) : PriceTickerItemBase(apiName, quoteData)
{
    public override async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} already started");
            return;
        }

        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"price ticker for group {GroupName} starting");

        socketClient = new BybitSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BybitSocketClient)socketClient).V5SpotApi.SubscribeToTickerUpdatesAsync(Symbols, data =>
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
                        //if (tick.LastPrice.HasValue)
                        symbol.LastPrice = (decimal)tick.LastPrice;
                        //if (tick.BestBidPrice.HasValue)
                        //symbol.BidPrice = tick.BestBidPrice;
                        //if (tick.BestAskPrice.HasValue)
                        //    symbol.AskPrice = tick.BestAskPrice;
                        //symbol.Volume = tick.BaseVolume; //?
                        //if (tick.Turnover24h.HasValue)
                        symbol.Volume = (decimal)tick.Turnover24h; //= Quoted = het volume * de prijs

                        //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                


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


        if (subscriptionResult.Success)
        {
            _subscription = subscriptionResult.Data;
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} started");
        }
        else
        {
            _subscription.Exception -= Exception;
            _subscription.ConnectionLost -= ConnectionLost;
            _subscription.ConnectionRestored -= ConnectionRestored;
            _subscription = null;

            socketClient.Dispose();
            socketClient = null;

            ConnectionLostCount++;
            ErrorDuringStartup = true;
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
            GlobalData.AddTextToLogTab($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
        }
    }

}
