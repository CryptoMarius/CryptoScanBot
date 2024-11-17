﻿using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class Trade(ExchangeBase api) : TradeBase(api), ITrade
{
    public Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult<int>(0);
    }

}
