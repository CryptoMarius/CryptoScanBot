using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models.Socket.Futures;

namespace CryptoScanner.Core.Exchange.Kraken.Perpetual;

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
        SubscriptionBundle!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)SubscriptionBundle!.SocketClient;
        var api = client.FuturesApi;

        InitializeCache(SymbolList);

        // Build 1m candles from the trade stream (Kraken Perpetual has no kline feed).
        var subscriptionResult = await api.SubscribeToTradeUpdatesAsync(SymbolNamesAsGenericArray, data =>
        {
            // Kraken sends a snapshot of the most recent (historical) trades on every connect AND
            // on every reconnect, through this very same handler - only the update type tells them
            // apart. Merging those into the cache is harmful twice over: trades from minutes that
            // were already flushed rebuild those candles from a partial set of trades and overwrite
            // correct history through Process1mCandleAsync, and the trades of the current minute get
            // counted a second time so its volume keeps growing on each reconnect. Only live updates
            // may reach the cache.
            if (data.UpdateType == SocketUpdateType.Snapshot)
                return;

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
