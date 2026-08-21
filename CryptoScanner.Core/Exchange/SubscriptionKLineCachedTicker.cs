using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// Base class for exchanges that deliver kline data as a continuous stream of partial (open-candle)
/// updates rather than a single definitive "final" event per closed candle (e.g. HyperLiquid, Kraken
/// Futures). Encapsulates the per-symbol candle cache, the minute-boundary timer, the flush logic, and
/// the flat-candle synthesis for minutes with no trades — eliminating the need to duplicate that
/// machinery in every exchange-specific subclass.
///
/// Subclasses only need to implement <see cref="Subscribe"/>: set up the socket subscription, call
/// <see cref="InitializeCache"/> once with the symbol list, and call either
/// <see cref="UpdateCacheFromKline"/> or <see cref="UpdateCacheFromTrade"/> on each incoming event.
/// After a successful subscription call <see cref="StartFlushTimer"/> to activate the timer.
/// </summary>
public abstract class SubscriptionKLineCachedTicker(ExchangeOptions exchangeOptions)
    : Subscription(exchangeOptions)
{
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    private System.Timers.Timer? _flushTimer;

    // Combined per-symbol entry: symbol metadata + its running candle cache, keyed by exchange name.
    private Dictionary<string, (CryptoSymbol Symbol, CryptoCandleList Candles)> _cache = [];

    // Raw list of exchange names — for exchanges that take a List<string>.
    protected IReadOnlyList<string> SymbolNamesAsGenericArray => [.. _cache.Keys];
    // Comma-separated exchange names — for exchanges that accept a combined subscription string.
    protected string SymbolNamesAsCommaSeperatedString => string.Join(",", _cache.Keys);


    // Milliseconds until ~6 s past the next UTC minute boundary.
    private static double GetNextTimerInterval()
    {
        DateTime now = DateTime.Now;
        return 6000 + ((60 - now.Second) * 1000 - now.Millisecond);
    }

    /// <summary>
    /// Register every symbol in the cache and build the exchange-name collections
    /// (<see cref="SymbolNamesAsCommaSeperatedString"/>, <see cref="SymbolNamesAsGenericArray"/>).
    /// Call once at the start of <see cref="Subscribe"/> before wiring up the socket callback.
    /// </summary>
    protected void InitializeCache(IEnumerable<CryptoSymbol> symbols)
    {
        _cache = [];
        foreach (var symbol in symbols)
            _cache.TryAdd(symbol.ExchangeName, (symbol, []));
    }

    /// <summary>
    /// Merge an incoming kline update into the running candle for <paramref name="exchangeName"/>.
    /// The volume field is treated as cumulative (the exchange already reports the total for the
    /// open candle), so we take the max rather than adding. Call this from a kline-stream callback.
    /// Acquires the cache semaphore synchronously so message ordering is preserved.
    /// </summary>
    protected void UpdateCacheFromKline(string exchangeName, DateTime openTime,
        decimal open, decimal high, decimal low, decimal close, decimal volume)
    {
        if (!_cache.TryGetValue(exchangeName, out var entry))
            return;

        // Guard against empty/invalid kline updates. A minute without trades (or an incomplete
        // update) can arrive with OHLC = 0; caching+flushing that produces the reported all-zero
        // OHLC candles (and corrupts the higher timeframes). Skip it — a genuinely missing minute
        // is back-filled as a flat candle (previous close) by CandleTools.BulkAddMissingCandles.
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
            return;

        _cacheSemaphore.Wait();
        try
        {
            var (symbol, candles) = entry;
            CandleTime candleOpen = CandleTime.AlignFromDateTime(openTime, 1);

            if (candles.TryGetValue(candleOpen, out CryptoCandle candle))
            {
                candle.High = Math.Max(candle.High, high);
                candle.Low = Math.Min(candle.Low, low);
                candle.Close = close;
                candle.Volume = Math.Max(candle.Volume, volume);
                candles[candleOpen] = candle;
            }
            else
            {
                candles.TryAdd(candleOpen, new CryptoCandle
                {
                    // Coarsen the tick size when this candle's prices do not fit the int the candle
                    // stores them in (see CryptoCandle.FitTickDecimals). A live price sits close to
                    // the price the symbol's tick size was chosen against, so this practically never
                    // bites here - but a coin that multiplies between two symbol refreshes would.
                    // Only on the branch that CREATES the candle: the update branch above runs per
                    // kline tick and may not change the tick size under an already stored Open.
                    TickDecimals = CryptoCandle.FitTickDecimals(symbol.PriceDecimals, open, high, low, close),
                    OpenTime = candleOpen,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                });
            }
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <summary>
    /// Merge an individual trade into the running candle for <paramref name="exchangeName"/>.
    /// Volume is additive (each trade contributes its own quote volume). Call this from a
    /// trade-stream callback (e.g. Kraken Futures which has no kline feed).
    /// Acquires the cache semaphore synchronously so trade ordering is preserved.
    /// </summary>
    protected void UpdateCacheFromTrade(string exchangeName,
        DateTime tradeTime, decimal price, decimal quoteVolume)
    {
        if (!_cache.TryGetValue(exchangeName, out var entry))
            return;

        _cacheSemaphore.Wait();
        try
        {
            var (symbol, candles) = entry;
            CandleTime candleOpen = CandleTime.AlignFromDateTime(tradeTime, 1);

            if (candles.TryGetValue(candleOpen, out CryptoCandle candle))
            {
                if (price > candle.High)
                    candle.High = price;
                if (price < candle.Low)
                    candle.Low = price;
                candle.Close = price;
                candle.Volume += quoteVolume;
                candles[candleOpen] = candle;
            }
            else
            {
                candles.TryAdd(candleOpen, new CryptoCandle
                {
                    // See the kline variant above for why the tick size is fitted here.
                    TickDecimals = CryptoCandle.FitTickDecimals(symbol.PriceDecimals, price, price, price, price),
                    OpenTime = candleOpen,
                    Open = price,
                    High = price,
                    Low = price,
                    Close = price,
                    Volume = quoteVolume,
                });
            }
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <summary>
    /// Start the minute-boundary flush timer. Call after a successful socket subscription.
    /// The timer fires ~6 s past each UTC minute, flushes all completed candles from the cache
    /// through <see cref="CandleTools.Process1mCandleAsync"/>, and synthesizes a flat candle for
    /// any minute that had no trades (keeping <c>CandleList.Count</c> above the 260-candle minimum
    /// that <c>CollectCandles</c> requires — without this, sparse symbols accumulate fewer than
    /// 260 real candles and trigger "Error collecting history").
    /// </summary>
    protected void StartFlushTimer()
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
            throw new Exception("interval1m not found");

        _flushTimer?.Stop();
        _flushTimer?.Dispose();
        _flushTimer = new System.Timers.Timer()
        {
            AutoReset = false
        };
        _flushTimer.Elapsed += async (sender, _) =>
        {
            foreach (var (symbol, candles) in _cache.Values)
            {
                try
                {
                    await _cacheSemaphore.WaitAsync();
                    try
                    {
                        CryptoCandleList cache = candles;
                        CandleTime expectedUpto = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1) - interval.Duration;

                        CryptoCandle candleLast = default;
                        foreach (CryptoCandle candle in cache.Values.ToList())
                        {
                            if (candle.OpenTime <= expectedUpto)
                            {
                                cache.Remove(candle.OpenTime);

                                // Skip all-zero candles that slipped through validation.
                                if (candle.Close <= 0)
                                    continue;

                                IncrementTickerCount();

                                await CandleTools.Process1mCandleAsync(symbol, candle.Date,
                                    candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
                                candleLast = candle;
                            }
                            else break;
                        }

                        // If the expected minute had no trades the cache had no entry for it, and
                        // Process1mCandleAsync was never called. Synthesize a flat candle so the 1m
                        // CandleList stays contiguous and CollectCandles keeps finding >= 260 candles.
                        if (candleLast.OpenTime != expectedUpto && symbol.LastPrice.HasValue)
                        {
                            IncrementTickerCount();

                            decimal lastPrice = symbol.LastPrice.Value;
                            await CandleTools.Process1mCandleAsync(symbol, expectedUpto.ToDateTime(),
                                lastPrice, lastPrice, lastPrice, lastPrice, 0, isFilled: true);
                            candleLast = new CryptoCandle
                            {
                                TickDecimals = symbol.PriceDecimals,
                                OpenTime = expectedUpto,
                                Open = lastPrice,
                                High = lastPrice,
                                Low = lastPrice,
                                Close = lastPrice,
                                Volume = 0,
                                IsFilled = true,
                            };
                        }

                        if (candleLast.OpenTime == expectedUpto)
                        {
                            symbol.LastPrice = candleLast.Close;
                            GlobalData.ThreadMonitorCandle?.AddToQueue(symbol, candleLast);
                        }
                    }
                    finally
                    {
                        _cacheSemaphore.Release();
                    }
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, symbol.Name);
#if DEBUG
                    GlobalData.AddErrorToLogTab($"KLine Ticker {symbol.Name} ERROR {error.Message}");
#endif
                }
            }

            if (sender is System.Timers.Timer t)
            {
                t.Interval = GetNextTimerInterval();
                t.Start();
            }
        };
        _flushTimer.Interval = GetNextTimerInterval();
        _flushTimer.Start();
    }

    protected void StopFlushTimer()
    {
        _flushTimer?.Stop();
        _flushTimer?.Dispose();
        _flushTimer = null;
    }

    public override async Task StopAsync()
    {
        StopFlushTimer();
        await base.StopAsync();
    }
}
