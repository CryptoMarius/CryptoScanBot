using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects.Models.Spot.Socket;

namespace CryptoScanBot.Core.Exchange.KucoinSpot;
#if TRADEBOT
public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KucoinSocketClient();
        var subscriptionResult = await ((KucoinSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        return subscriptionResult;
    }

    private void OnOrderUpdate(DataEvent<KucoinStreamOrderNewUpdate> @event)
    {
        throw new NotImplementedException();
    }
}

#endif
