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
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(Symbols, KlineStreamInterval.OneMinute, data =>
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
                UpdateCacheFromKline(data.Symbol!, kline.Kline.OpenTime,
                    open: kline.Kline.OpenPrice, high: kline.Kline.HighPrice, low: kline.Kline.LowPrice,
                    close: kline.Kline.ClosePrice, volume: kline.Kline.Volume);
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
