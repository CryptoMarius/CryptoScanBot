using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Kraken.Net.Clients;

namespace CryptoScanBot.Core.Exchange.KrakenSpot;

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
                    string symbolName = data.Topic?.Replace("/", "") ?? "";
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        TickerCount++;
                        symbol.LastPrice = tick.LastTrade.Price;
                        symbol.BidPrice = tick.BestBids.Price;
                        symbol.AskPrice = tick.BestAsks.Price;
                        symbol.Volume = tick.Volume.Value24H;
                    }
                }

                if (TickerCount > 999999999)
                    TickerCount = 0;
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}