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
        string symbolName = "";
        foreach (var symbol in SymbolList)
        {
            //if (symbolName == "")
            //    symbolName = symbol.Base + "-" + symbol.Quote;
            //else
            //    symbolName += "," + symbol.Base + "-" + symbol.Quote;
            if (symbolName == "")
                symbolName = symbol.Name;
            else
                symbolName += "," + symbol.Name;
        }


        TickerGroup!.SocketClient ??= new MexcSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((MexcSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToMiniTickerUpdatesAsync(symbolName, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                var tick = data.Data;
                {
                    if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol? symbol))
                    {
                        TickerCount++;

                        if (!GlobalData.BackTest)
                        {
                            symbol.LastPrice = tick.LastPrice;
                            symbol.Volume = tick.QuoteVolume; //= Base = het volume * de prijs                                
                        }
                    }
                }

                if (TickerCount > 999999999)
                    TickerCount = 0;
            }
        }, null, ExchangeHelper.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
