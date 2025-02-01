using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Okx.Spot;

public class Trade() : TradeBase(), ITrade
{
    public Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        throw new NotImplementedException();
    }
}
