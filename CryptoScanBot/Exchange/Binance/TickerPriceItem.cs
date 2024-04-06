using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Binance;

public class TickerPriceItem() : TickerPriceItemBase(Api.ExchangeOptions)
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

        socketClient = new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)socketClient).SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
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
