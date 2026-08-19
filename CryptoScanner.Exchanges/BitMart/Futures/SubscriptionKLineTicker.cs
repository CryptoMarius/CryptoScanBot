using BitMart.Net.Clients;
using BitMart.Net.Enums;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Model;


namespace CryptoScanner.Core.Exchange.BitMart.Futures;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BitMartSocketClient();
        var client = (BitMartSocketClient)SubscriptionBundle!.SocketClient;
        //SubscriptionBundle!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.UsdFuturesApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        // BitMart wants its own contract names, not the scanner names that the Symbols property carries
        // - that one exists for the log line. They happen to be the same string on the futures side, but
        // passing the wrong one fails silently: the exchange accepts the topic and simply never sends
        // anything, after which the flush timer keeps synthesizing flat candles from the last price.
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsGenericArray, FuturesStreamKlineInterval.OneMinute, data =>
        {
            //var kline = data.Data;
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
            // The volume of a futures kline counts CONTRACTS and this stream carries no turnover field
            // of its own. QuantityTickSize holds the base amount of one contract (see Symbol.cs), so
            // this gives the same quote volume the fetched candles carry - without it the live candles
            // are in another unit than the history (and a factor contract size apart).
            if (!SymbolByExchangeName.TryGetValue(data.Data.Symbol!, out CryptoSymbol? symbol))
                return;

            foreach (var kline in data.Data.Klines)
            {
                if (kline.Timestamp == null)
                    continue;

                UpdateCacheFromKline(data.Data.Symbol!, kline.Timestamp.Value,
                    open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                    close: kline.ClosePrice,
                    volume: kline.Volume * symbol.QuantityTickSize * 0.5m * (kline.HighPrice + kline.LowPrice));
                //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(ScannerSymbol, interval, ScannerSymbol.PriceDisplayFormat, true, true)}");
            }
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
