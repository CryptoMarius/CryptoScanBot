using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class Order(ExchangeBase api) : OrderBase(api), IOrder
{
    public Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult<int>(0);
    }

}
