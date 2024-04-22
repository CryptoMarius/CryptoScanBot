using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Kucoin.Net.Clients;

namespace CryptoScanBot.Core.Exchange.KucoinSpot;
#if TRADEBOT
public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override Task<CallResult<UpdateSubscription>> Subscribe()
    {
        TickerGroup.SocketClient ??= new KucoinSocketClient();
        //var subscriptionResult = await ((KucoinSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        //var subscriptionResult = new CallResult<UpdateSubscription>(null);
        return null; // subscriptionResult;
    }

}

#endif
