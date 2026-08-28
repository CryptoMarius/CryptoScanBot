using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

/// <summary>
/// Real-time 1-minute candle subscription via the Bitvavo WebSocket API.
/// WebSocket endpoint: wss://ws.bitvavo.com/v2/
///
/// Subscribe message:
///   {"action":"subscribe","channels":[{"name":"candles","interval":["1m"],"markets":["BTC-EUR","ETH-EUR"]}]}
///
/// Received candle message:
///   {"event":"candle","market":"BTC-EUR","interval":"1m","candle":[[timestamp_ms,"open","high","low","close","volume"]]}
///
/// No authentication required for public market data.
///
/// Candle processing uses the shared SubscriptionKLineCachedTicker cache + timer pattern.
///
/// <para>
/// Until 19-08-2026 this class owned a ClientWebSocket of its own, with its own receive loop, its own
/// json parsing and its own StartAsync/StopAsync, because there is no Bitvavo package from JKorf. That
/// turned out to be the wrong conclusion: CryptoExchange.Net is a framework, and an exchange can be
/// built on it without a ready made package. Everything below the surface now lives in the Socket
/// folder next to this file, and this class looks like every other exchange again.
/// </para>
/// <para>
/// What that buys, and it is the reason for the change: a keep alive that also NOTICES a missing
/// answer, reconnecting with a policy, resubscribing by itself after a reconnect, and the
/// ConnectionLost / ConnectionRestored / ResubscribingFailed events that Exchange/Subscription.cs
/// already handles for every other market. The hand written socket had none of that, so a half open
/// connection was only caught by the inactivity check five minutes later, and every repair to the shared
/// path had to be redone here by hand.
/// </para>
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BitvavoSocketClient();
        var client = (BitvavoSocketClient)SubscriptionBundle.SocketClient;

        InitializeCache(SymbolList);

        // The EXCHANGE names ("BTC-EUR"), not the scanner names ("BTCEUR"). Subscribing with the wrong
        // one fails silently at Bitvavo: the topic is accepted and nothing is ever delivered, after
        // which the flush timer invents flat candles. That is the trap that Kucoin and Coinbase were
        // caught in before, so it is worth naming here.
        List<string> markets = [.. SymbolNamesAsGenericArray];

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await client.SpotApi.SubscribeToKlineUpdatesAsync(markets, "1m", data =>
        {
            var update = data.Data;
            if (update.Candle.Length == 0)
                return;

            // Bitvavo sends an array of candles; in practice one per message, but read them all rather
            // than assume. A candle that cannot be read is skipped, not thrown - see BitvavoSocketCandle.From.
            foreach (var values in update.Candle)
            {
                var candle = BitvavoSocketCandle.From(values);
                if (candle == null)
                    continue;

                // Handled synchronously, in the exact order the socket delivers messages, so a burst
                // of pushes for the same still-open candle can never have an older message overwrite
                // a newer one's OHLC.
                UpdateCacheFromKline(update.Market, candle.Value.OpenTimeUtc,
                    open: candle.Value.Open, high: candle.Value.High, low: candle.Value.Low,
                    close: candle.Value.Close, volume: candle.Value.QuoteVolume);
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
