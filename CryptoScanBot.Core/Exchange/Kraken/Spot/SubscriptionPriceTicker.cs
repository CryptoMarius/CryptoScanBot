using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kraken.Net.Clients;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        List<string> symbolList = [];
        foreach (var symbol in SymbolList)
        {
            symbolList.Add(symbol.Base + "/" + symbol.Quote);
        }

        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((KrakenSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToTickerUpdatesAsync(symbolList, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                var tick = data.Data;
                {
                    string symbolName = data.Symbol?.Replace("/", "") ?? "";
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        Interlocked.Increment(ref TickerCount);

                        if (!GlobalData.BackTest)
                        {
                            symbol.LastPrice = tick.LastPrice;
                            symbol.BidPrice = tick.BestBidPrice;
                            symbol.AskPrice = tick.BestAskPrice;
                            symbol.Volume = tick.Volume;
                        }
                    }
                }

                if (TickerCount > 999999999)
                    Interlocked.Exchange(ref TickerCount, 0);
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}