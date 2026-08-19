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
        SubscriptionBundle!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)SubscriptionBundle!.SocketClient;
        var api = client.SpotApi;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        // snapshot: false because Kraken otherwise answers every (re)connect with the last 10 closed
        // candles through this very same handler. Those minutes were already flushed, so they are put
        // back in the cache and pushed through Process1mCandleAsync a second time - work that produces
        // nothing new, for every symbol, on every reconnect.
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
                // Kraken provides base volume; the vwap it sends along with the candle converts it
                // to quote volume exactly, the same way the history does (see Candle.cs).
                decimal quoteVolume = kline.Vwap > 0
                    ? kline.Volume * kline.Vwap
                    : kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice);
                UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                    open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                    close: kline.ClosePrice, volume: quoteVolume);
                //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)} count={candleCache.Count}");
            }
        }, snapshot: false, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Kline timer implementation (fix)
        // Because a new candle is not always offered (like the "junk" coin TOMOUSDT) an additional
        // timer can be used that repeats the previous candle after all.
        // The idea is to do that every minute, 10 seconds after the normal kline event.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
