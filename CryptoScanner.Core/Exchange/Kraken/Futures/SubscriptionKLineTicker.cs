using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

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
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    static double GetNextTimer()
    {
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }

    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SemaphoreSlim cacheListSemaphore = new(1, 1);
        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)TickerGroup!.SocketClient;
        var api = client.FuturesApi;

        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            throw new Exception("No exchange?");

        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
            throw new Exception("No interval?");

        SortedList<string, CryptoCandleList> symbolCandleCache = [];

        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            symbols.Add(symbol.ExchangeName);
            symbolCandleCache.Add(symbol.ExchangeName, []);
        }

        // Build 1m candles from the trade stream (Kraken Futures has no kline feed).
        var subscriptionResult = await api.SubscribeToTradeUpdatesAsync(symbols, data =>
        {
            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the socket
            // delivers messages, so trades for the same still-open candle are always applied in order.
            if (exchange.SymbolListExchangeName.TryGetValue(data.Symbol!, out CryptoSymbol? symbol))
            {
                cacheListSemaphore.Wait();
                try
                {
                    CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                    foreach (KrakenFuturesTradeUpdate trade in data.Data)
                    {
                        decimal quoteVolume = trade.Price * trade.Quantity;
                        CandleTime candleOpen = CandleTime.AlignFromDateTime(trade.Timestamp, 1);

                        // CryptoCandle is a struct → read a copy, update it, write it back.
                        if (!candleCache.TryGetValue(candleOpen, out CryptoCandle candle))
                        {
                            candle = new()
                            {
                                OpenTime = candleOpen,
                                TickDecimals = symbol.PriceDecimals,
                                Open = trade.Price,
                                High = trade.Price,
                                Low = trade.Price,
                                Close = trade.Price,
                                Volume = quoteVolume
                            };
                            candleCache.TryAdd(candleOpen, candle);
                        }
                        else
                        {
                            if (trade.Price > candle.High)
                                candle.High = trade.Price;
                            if (trade.Price < candle.Low)
                                candle.Low = trade.Price;
                            candle.Close = trade.Price;
                            candle.Volume += quoteVolume;
                            candleCache[candleOpen] = candle;
                        }
                    }
                }
                finally
                {
                    cacheListSemaphore.Release();
                }
            }
        }, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Timer: ~6s after each minute, flush the completed 1m candles to the analysis pipeline. The
        // trade feed only updates the CURRENT (incomplete) candle, so we emit a candle once its minute
        // has passed (same approach as the Spot kline subscription).
        if (subscriptionResult.Success)
        {
            System.Timers.Timer timerKline = new()
            {
                AutoReset = false,
            };
            timerKline.Elapsed += new System.Timers.ElapsedEventHandler(async (sender, e) =>
            {
                foreach (var symbol in SymbolList)
                {
                    try
                    {
                        await cacheListSemaphore.WaitAsync();
                        try
                        {
                            CryptoCandleList candleCache = symbolCandleCache[symbol.ExchangeName];
                            CandleTime expectedCandlesUpto = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - interval.Duration;

                            CryptoCandle candleLast = default;
                            foreach (CryptoCandle candle in candleCache.Values.ToList())
                            {
                                // Only the completed candles (whose minute has fully passed).
                                if (candle.OpenTime <= expectedCandlesUpto)
                                {
                                    candleCache.Remove(candle.OpenTime);
                                    Interlocked.Increment(ref TickerCount);
                                    if (TickerCount > 999999999)
                                        Interlocked.Exchange(ref TickerCount, 0);

                                    await CandleTools.Process1mCandleAsync(symbol, candle.Date,
                                        candle.Open, candle.High, candle.Low, candle.Close,
                                        candle.Volume);
                                    candleLast = candle;
                                }
                                else
                                    break;
                            }

                            // Queue the just-completed candle for analysis.
                            if (candleLast.OpenTime == expectedCandlesUpto)
                            {
                                if (!GlobalData.IsEmulatorMode)
                                    symbol.LastPrice = candleLast.Close;
                                GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candleLast);
                            }
                        }
                        finally
                        {
                            cacheListSemaphore.Release();
                        }
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, symbol.Name);
#if DEBUG
                        GlobalData.AddTextToLogTab($"KLine Ticker {symbol.Name} ERROR {error.Message}");
#endif
                    }
                }

                if (sender is System.Timers.Timer t)
                {
                    t.Interval = GetNextTimer();
                    t.Start();
                }
            });
            timerKline.Interval = GetNextTimer();
            timerKline.Start();
        }

        return subscriptionResult;
    }
}
