using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Spot;

namespace CryptoScanner.Core.Exchange.Kucoin.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KucoinSocketClient();
        var client = (KucoinSocketClient)TickerGroup!.SocketClient;
        //TickerGroup!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.SpotApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(Symbols, KlineInterval.OneMinute, data =>
        {
            KucoinKline kline = data.Data.Candles;
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
            UpdateCacheFromKline(data.Data.Symbol!, kline.OpenTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: kline.QuoteVolume);
            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(ScannerSymbol, interval, ScannerSymbol.PriceDisplayFormat, true, true)}");
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Implementatie kline timer (fix)
        // Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
        // kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
        // De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
