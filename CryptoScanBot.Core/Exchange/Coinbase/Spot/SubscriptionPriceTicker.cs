using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Coinbase.Net.Clients;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        // 1 symbol per ticker
        string symbolName = "";
        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            //Symbol = symbol;
            if (symbolName == "")
                symbolName = symbol.Base + "-" + symbol.Quote;
            else
                symbolName += "," + symbol.Base + "-" + symbol.Quote;
            symbols.Add(symbolName);
        }


        TickerGroup!.SocketClient ??= new CoinbaseSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = 
            await ((CoinbaseSocketClient)TickerGroup.SocketClient).AdvancedTradeApi.SubscribeToTickerUpdatesAsync(symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    string symbolName = tick.Symbol.Replace("-", "");
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        Interlocked.Increment(ref TickerCount);

                        if (!GlobalData.BackTest)
                        {

                            // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                            //symbol.OpenPrice = tick.OpenPrice;
                            //symbol.HighPrice = tick.HighPrice;
                            //symbol.LowPrice = tick.LowPrice;
                            //if (tick.LastPrice.HasValue)
                            symbol.LastPrice = tick.LastPrice;
                            //if (tick.BestBidPrice.HasValue)
                            //symbol.BidPrice = tick.BestBidPrice;
                            //if (tick.BestAskPrice.HasValue)
                            //    symbol.AskPrice = tick.BestAskPrice;
                            //symbol.Volume = tick.BaseVolume; //?
                            //if (tick.Turnover24h.HasValue)
                            if (tick.Volume24H.HasValue)
                                symbol.Volume = tick.Volume24H.Value; //= Quoted = het volume * de prijs
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
