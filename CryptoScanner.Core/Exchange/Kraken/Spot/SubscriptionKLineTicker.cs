using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models.Socket;

namespace CryptoScanner.Core.Exchange.Kraken.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)TickerGroup!.SocketClient;
        var api = client.SpotApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsGenericArray, KlineInterval.OneMinute, data =>
        {
            //var kline = data;
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
            foreach (KrakenKlineUpdate kline in data.Data)
            {
                // Kraken provides base volume; derive quote volume as approximation.
                decimal quoteVolume = kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice);
                UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                    open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                    close: kline.ClosePrice, volume: quoteVolume);
                //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)} count={candleCache.Count}");
            }
        }, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Implementatie kline timer (fix)
        // Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
        // kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
        // De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
