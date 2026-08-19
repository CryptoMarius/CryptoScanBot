using BitMart.Net.Clients;
using BitMart.Net.Enums;
using BitMart.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;


namespace CryptoScanner.Core.Exchange.BitMart.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BitMartSocketClient();
        var client = (BitMartSocketClient)SubscriptionBundle!.SocketClient;
        //SubscriptionBundle!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.SpotApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        // BitMart wants its own pair names ("BTC_USDT"), not the scanner names ("BTCUSDT") that the
        // Symbols property carries - that one exists for the log line. Passing the scanner names fails
        // silently: the exchange accepts the topic and simply never sends anything, after which the
        // flush timer keeps synthesizing flat candles from the last known price.
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsGenericArray, KlineStreamInterval.OneMinute, data =>
        {
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
            foreach (BitMartKlineUpdate kline in data.Data)
            {
                // The volume of a kline is the amount in the BASE currency and the rest of the scanner
                // works in the quote currency, so it is converted the same way the fetched candles are
                // (which do carry a quote volume of their own). Without it the live candles are in
                // another unit than the history.
                // Each item carries its own name, the event covers more than one pair.
                UpdateCacheFromKline(kline.Symbol!, kline.Kline.OpenTime,
                    open: kline.Kline.OpenPrice, high: kline.Kline.HighPrice, low: kline.Kline.LowPrice,
                    close: kline.Kline.ClosePrice,
                    volume: kline.Kline.QuoteVolume ?? kline.Kline.Volume * 0.5m * (kline.Kline.HighPrice + kline.Kline.LowPrice));
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
