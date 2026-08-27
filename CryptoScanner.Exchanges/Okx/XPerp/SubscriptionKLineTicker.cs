using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Market;

namespace CryptoScanner.Core.Exchange.Okx.XPerp;

/// <summary>
/// Monitor the 1m candles that the exchange pushes
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, OKXKline kline)
    {
        // The base volume is the volume in terms of the first currency of the pair,
        // the quote volume is the volume in terms of the second currency of the pair.
        if (string.IsNullOrEmpty(symbolName))
            return;

        if (SymbolByExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();

            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Time,
                kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                kline.VolumeCurrencyQuote);
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }

    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new OKXSocketClient();
        var client = (OKXSocketClient)SubscriptionBundle!.SocketClient;
        var api = client.UnifiedApi;

        // Okx expects the instrument id ("BTC-USD_UM_XPERP-310404"), not the scanner name ("BTCUSD").
        // Use ExchangeName so it matches both the REST candle fetch and the SymbolByExchangeName lookup
        // below. Pass the names as a list: the overload taking a single string treats a comma separated
        // text as one instrument id, which silently breaks as soon as a subscription serves more than
        // one symbol. Checked on 27-08-2026 that this channel does deliver for these instruments.
        var subscriptionResult = await api.ExchangeData.SubscribeToKlineUpdatesAsync(SymbolList.Select(s => s.ExchangeName).ToList(), KlineInterval.OneMinute, data =>
        {
            OKXKline kline = data.Data;
            {
                if (kline.Confirm) // It is a final candle
                    Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
