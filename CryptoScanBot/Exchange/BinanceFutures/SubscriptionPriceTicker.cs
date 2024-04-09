using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BinanceFutures;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>> Subscribe()
    {
        TickerGroup.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).UsdFuturesApi.SubscribeToAllTickerUpdatesAsync((data) =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                foreach (var tick in data.Data.Cast<BinanceStreamTick>())
                {
                    if (exchange.SymbolListName.TryGetValue(tick.Symbol, out CryptoSymbol symbol))
                    {
                        TickerCount++;

                        symbol.LastPrice = tick.LastPrice;
                        symbol.BidPrice = tick.BestBidPrice;
                        symbol.AskPrice = tick.BestAskPrice;
                        symbol.Volume = tick.QuoteVolume; //= Quoted = het volume * de prijs?
                    }
                }

                if (TickerCount > 999999999)
                    TickerCount = 0;
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
