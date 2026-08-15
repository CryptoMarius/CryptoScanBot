using Coinbase.Net.Clients;
using Coinbase.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

namespace CryptoScanner.Core.Exchange.Coinbase.Spot;

/// <summary>
/// Coinbase has a kline websocket channel, but it is fixed at a 5 minute interval - the api states so
/// itself ("Klines are always at a 5 minute interval"), there is no granularity to choose. Feeding those
/// into the 1m pipeline stores five minutes of range under one minute stamp and leaves the four minutes
/// in between to be filled in as flat candles, which corrupts the 1m series and everything built on it.
///
/// So the 1m candles are built from the TRADE feed instead: every trade updates the running 1m candle in
/// a per-symbol cache (open on the first trade, high/low/close as trades arrive, quote-volume =
/// sum of price x quantity). A timer (~6s after each minute) flushes the just-completed candles through
/// CandleTools.Process1mCandleAsync and queues the last one for analysis. Same approach as Kraken
/// Futures, which has no kline feed at all.
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new CoinbaseSocketClient();
        var client = (CoinbaseSocketClient)SubscriptionBundle.SocketClient;
        var api = client.AdvancedTradeApi;

        InitializeCache(SymbolList);

        // Coinbase wants its own symbol names ("BTC-USD"), not the scanner names ("BTCUSD"). The names
        // the cache is keyed on come from that same list, so both sides stay in step.
        var subscriptionResult = await api.SubscribeToTradeUpdatesAsync(SymbolNamesAsGenericArray, data =>
        {
            // On subscribe (and on every reconnect) Coinbase first sends a snapshot of the most recent
            // historical trades through this same handler. Merging those into the cache rebuilds
            // already flushed minutes from a partial set of trades and counts the trades of the current
            // minute a second time, so its volume grows on each reconnect. Only live updates may pass.
            if (data.UpdateType == SocketUpdateType.Snapshot)
                return;

            // Handled synchronously, in the exact order the socket delivers messages, so trades for the
            // same still-open candle are always applied in order.
            foreach (CoinbaseTrade trade in data.Data)
            {
                // Per trade its own symbol, not data.Symbol: the package fills that one from the FIRST
                // trade of the message while the message covers every symbol of this subscription, so
                // using it would book the trades of all the others onto that one symbol.
                decimal quoteVolume = trade.Price * trade.Quantity;
                UpdateCacheFromTrade(trade.Symbol, trade.Timestamp, trade.Price, quoteVolume);
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Timer: ~6s after each minute, flush the completed 1m candles to the analysis pipeline. The
        // trade feed only updates the CURRENT (incomplete) candle, so we emit a candle once its minute
        // has passed.
        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
