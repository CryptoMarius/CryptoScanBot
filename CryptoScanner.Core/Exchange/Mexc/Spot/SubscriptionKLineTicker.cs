using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using Mexc.Net.Clients;
using Mexc.Net.Enums;
using Mexc.Net.Objects.Models.Spot;

namespace CryptoScanner.Core.Exchange.Mexc.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new MexcSocketClient();
        var client = (MexcSocketClient)SubscriptionBundle!.SocketClient;
        //SubscriptionBundle!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.SpotApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsGenericArray, KlineInterval.OneMinute, data =>
        {
            MexcStreamKline kline = data.Data;
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
            UpdateCacheFromKline(data.Symbol!, kline.StartTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: kline.QuoteVolume);
            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(ScannerSymbol, interval, ScannerSymbol.PriceDisplayFormat, true, true)}");
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
