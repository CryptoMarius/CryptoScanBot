using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace CryptoScanner.Core.Exchange.Kucoin.Perpetual;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new KucoinSocketClient();
        var client = (KucoinSocketClient)SubscriptionBundle!.SocketClient;
        var api = client.FuturesApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        // Kucoin wants its own contract names ("XBTUSDCM"), not the scanner names ("XBTUSDC"). Passing the
        // scanner names fails silently: the exchange accepts the topic and simply never sends anything,
        // after which the flush timer keeps synthesizing flat candles from the last known price.
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsGenericArray, KlineInterval.OneMinute, data =>
        {
            var kline = data.Data;

            // The volume of a futures kline counts CONTRACTS and this stream carries no turnover field
            // of its own. QuantityTickSize holds the base amount of one contract (lotSize * multiplier,
            // see Symbol.cs), so this gives the same quote volume the fetched candles carry - without it
            // the live candles are in another unit than the history (and a factor multiplier apart).
            if (!SymbolByExchangeName.TryGetValue(data.Symbol!, out CryptoSymbol? symbol))
                return;
            decimal quoteVolume = kline.Volume * symbol.QuantityTickSize * kline.ClosePrice;
            //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
            //GlobalData.AddTextToLogTab($"kline ticker {data.ScannerSymbol} {json}");

            // Prossible change in flow:
            // Create some variables or temp candle
            // Update that candle until OpenTime is different
            // The last 1m candle added can be cached (avoiding the Last())
            // Then: Add the in between candles and the tempcandle
            // Finally add the tempcandle to the Analysis Queue / Monitoring Queue

            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the
            // socket delivers messages, so a burst of pushes for the same still-open candle
            // can never have an older message overwrite a newer one's OHLC.
            UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: quoteVolume);
            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Kline timer implementation (fix)
        // Because a new candle is not always offered (like the "junk" coin TOMOUSDT) an additional
        // timer can be used that repeats the previous candle after all.
        // The idea is to do that every minute, 10 seconds after the normal kline event.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
