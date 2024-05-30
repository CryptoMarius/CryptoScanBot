using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects.Models.Futures.Socket;

namespace CryptoScanBot.Core.Exchange.Kucoin.Futures;

#if TRADEBOT
public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KucoinSocketClient();
        //var subscriptionResult = await ((KucoinSocketClient)TickerGroup.SocketClient).FuturesApi.SubscribeToTradeUpdatesAsync( OrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        return null; // subscriptionResult;
    }

    private void OnOrderUpdate(DataEvent<KucoinStreamFuturesOrderUpdate> @event)
    {
        throw new NotImplementedException();
    }
}

#endif
