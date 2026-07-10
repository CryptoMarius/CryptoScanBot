using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models.Socket.Futures;

namespace CryptoScanner.Core.Exchange.Kraken.Futures;

/// <summary>
/// Kraken FUTURES has no candle/kline websocket feed (unlike Kraken Spot, which has SubscribeToKline...).
/// So we build the 1m candles ourselves from the TRADE feed: every trade updates the running 1m candle
/// in a per-symbol cache (open on the first trade, high/low/close as trades arrive, quote-volume =
/// Σ price × quantity). A timer (~6s after each minute) flushes the just-completed candles through
/// CandleTools.Process1mCandleAsync and queues the last one for analysis — the same pattern as the Spot
/// kline subscription, which also needs a cache + timer because the live candle is incomplete.
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)TickerGroup!.SocketClient;
        var api = client.FuturesApi;

        InitializeCache(SymbolList);

        // Build 1m candles from the trade stream (Kraken Futures has no kline feed).
        var subscriptionResult = await api.SubscribeToTradeUpdatesAsync(SymbolNamesAsGenericArray, data =>
        {
            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the socket
            // delivers messages, so trades for the same still-open candle are always applied in order.
            foreach (KrakenFuturesTradeUpdate trade in data.Data)
            {
                decimal quoteVolume = trade.Price * trade.Quantity;
                UpdateCacheFromTrade(data.Symbol!, trade.Timestamp, trade.Price, quoteVolume);
            }
        }, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Timer: ~6s after each minute, flush the completed 1m candles to the analysis pipeline. The
        // trade feed only updates the CURRENT (incomplete) candle, so we emit a candle once its minute
        // has passed (same approach as the Spot kline subscription).
        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
