using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        throw new NotImplementedException();
    }

}

