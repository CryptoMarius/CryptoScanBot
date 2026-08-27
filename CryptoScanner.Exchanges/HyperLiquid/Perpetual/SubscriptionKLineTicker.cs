using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Enums;
using HyperLiquid.Net.Objects.Models;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Perpetual;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new HyperLiquidSocketClient();
        var client = (HyperLiquidSocketClient)SubscriptionBundle.SocketClient;
        var api = client.FuturesApi.ExchangeData;

        // We verwachten (helaas) slechts 1 symbol per ticker
        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsCommaSeperatedString, KlineInterval.OneMinute, data =>
        {
            HyperLiquidKline kline = data.Data;
            //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
            //GlobalData.AddTextToLogTab($"kline ticker {data.ScannerSymbol} {json}");

            // Prossible change in flow:
            // Create some variables or temp candle
            // Update that candle until OpenTime is different
            // The last 1m candle added can be cached (avoiding the Last())
            // Then: Add the in between candles and the tempcandle
            // Finally add the tempcandle to the Analysis Queue / Monitoring Queue

            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the
            // socket delivers messages. The previous fire-and-forget Task.Run gave no
            // ordering guarantee, so a burst of pushes for the same still-open candle could
            // have an older message finish processing after a newer one and overwrite its
            // OHLC with stale values.
            UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: kline.Volume);
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Debug...
        //GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true));
        //            //var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime,
        //            //    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
        //            //    kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
        // Kline timer implementation (fix)
        // Because a new candle is not always offered (like the "junk" coin TOMOUSDT) an additional
        // timer can be used that repeats the previous candle after all.
        // The idea is to do that every minute, 10 seconds after the normal kline event.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
