using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Spot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)SubscriptionBundle.SocketClient).SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                foreach (BinanceStreamTick tick in data.Data.Cast<BinanceStreamTick>())
                {
                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol? symbol))
                    {
                        IncrementTickerCount();

                        if (!GlobalData.IsEmulatorMode)
                        {
                            symbol.LastPrice = tick.LastPrice;
                            //symbol.BidPrice = tick.BestBidPrice;
                            //symbol.AskPrice = tick.BestAskPrice;
                            symbol.Volume = tick.QuoteVolume; //= Quoted = het volume * de prijs
                        }
                    }
                }

            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}