using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

public class Trade() : TradeBase(), ITrade
{
    public Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult<int>(0);
    }

}
