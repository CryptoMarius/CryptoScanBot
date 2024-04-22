using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BinanceSpot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>> Subscribe()
    {
        TickerGroup.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
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

        return subscriptionResult;
    }

}
