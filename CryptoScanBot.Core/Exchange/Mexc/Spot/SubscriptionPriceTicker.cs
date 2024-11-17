using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        //string symbolName = "";
        foreach (var symbol in SymbolList)
        {
            //if (symbolName == "")
            //    symbolName = symbol.Name;
            //else
            //    symbolName += "," + symbol.Name;
            symbols.Add(symbol.Name);
        }


        TickerGroup!.SocketClient ??= new MexcSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((MexcSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToMiniTickerUpdatesAsync(symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                var tick = data.Data;
                {
                    if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol? symbol))
                    {
                        Interlocked.Increment(ref TickerCount);

                        if (!GlobalData.BackTest)
                        {
                            symbol.LastPrice = tick.LastPrice;
                            symbol.Volume = tick.QuoteVolume; //= Base = het volume * de prijs                                
                        }
                    }
                }

                if (TickerCount > 999999999)
                    Interlocked.Exchange(ref TickerCount, 0);
            }
        }, null, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
