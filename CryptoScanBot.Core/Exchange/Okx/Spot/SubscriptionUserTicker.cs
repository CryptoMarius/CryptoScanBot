using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Market;
using OKX.Net.Objects.Trade;

namespace CryptoScanBot.Core.Exchange.Okx.Spot;

public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        //TickerGroup!.SocketClient ??= new OKXSocketClient();
        //var subscriptionResult = await ((OKXSocketClient)TickerGroup.SocketClient).UnifiedApi.Trading.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        //return subscriptionResult;
        return null;
    }

    private void OnOrderUpdate(DataEvent<OKXAlgoOrderUpdate> @event)
    {
        throw new NotImplementedException();
    }
}

