using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// Base class for exchanges that deliver kline data as a continuous stream of partial (open-candle)
/// updates rather than a single definitive "final" event per closed candle (e.g. HyperLiquid, Kraken
/// Perpetual). Encapsulates the per-symbol candle cache, the minute-boundary timer, the flush logic, and
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

    // Counters behind Subscription.ActivityDiagnostics, reset in InitializeCache so every number
    // describes the period since the last (re)subscribe. Interlocked because the socket callback, the
    // minute timer and the health check each touch them from their own thread.
    //
    // What they separate. A subscription that reports no activity has exactly three possible causes,
    // and until now all three produced the same "inactive for N minutes" line:
    //   flush == 0 or far below the number of minutes  -> the minute timer did not run (a dead timer,
    //       or a machine too busy to serve it), so the fault is in the timer, not in the exchange.
    //   flush runs but ws == 0 and noprice > 0         -> the socket delivers nothing AND there is no
    //       price to repeat, the one path that marks no activity at all.
    //   flush runs and flat > 0                        -> activity WAS marked every minute, so the
    //       subscription cannot be inactive - the fault would then be in the bookkeeping itself.
    // wsbad and wsdrop name the two ways an incoming update is thrown away before it reaches the cache.
    private int _flushTicks;           // times the minute timer actually ran
    private int _flushRealCandles;     // candles flushed out of the cache
    private int _flushFlatCandles;     // minutes synthesized because nothing was traded
    private int _flushNoPrice;         // minutes that could not even be synthesized (no price to repeat)
    private int _flushErrors;          // per-symbol failures inside the flush
    private int _socketUpdates;        // kline/trade updates merged into the cache
    private int _socketRejected;       // updates dropped by the zero/invalid OHLC guard
    private int _socketUnknownSymbol;  // updates dropped, the name was not in the cache
    private long _lastFlushTicks;      // moment the minute timer last ran, UTC ticks


    public override string ActivityDiagnostics
    {
        get
        {
            long lastFlush = Interlocked.Read(ref _lastFlushTicks);
            string flushAge = lastFlush == 0
                ? "never"
                : $"{(GlobalData.Clock.UtcNow - new DateTime(lastFlush, DateTimeKind.Utc)).TotalMinutes:N0}m";
            return $"flush={Volatile.Read(ref _flushTicks)} last={flushAge} " +
                $"real={Volatile.Read(ref _flushRealCandles)} flat={Volatile.Read(ref _flushFlatCandles)} " +
                $"noprice={Volatile.Read(ref _flushNoPrice)} err={Volatile.Read(ref _flushErrors)} " +
                $"ws={Volatile.Read(ref _socketUpdates)} wsbad={Volatile.Read(ref _socketRejected)} " +
                $"wsdrop={Volatile.Read(ref _socketUnknownSymbol)}";
        }
    }

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
        // A new round starts here: the counters have to describe the period since THIS subscribe,
        // otherwise the numbers in the restart line still carry the previous round's totals.
        Interlocked.Exchange(ref _flushTicks, 0);
        Interlocked.Exchange(ref _flushRealCandles, 0);
        Interlocked.Exchange(ref _flushFlatCandles, 0);
        Interlocked.Exchange(ref _flushNoPrice, 0);
        Interlocked.Exchange(ref _flushErrors, 0);
        Interlocked.Exchange(ref _socketUpdates, 0);
        Interlocked.Exchange(ref _socketRejected, 0);
        Interlocked.Exchange(ref _socketUnknownSymbol, 0);
        Interlocked.Exchange(ref _lastFlushTicks, 0);

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
        {
            Interlocked.Increment(ref _socketUnknownSymbol);
            return;
        }

        // Guard against empty/invalid kline updates. A minute without trades (or an incomplete
        // update) can arrive with OHLC = 0; caching+flushing that produces the reported all-zero
        // OHLC candles (and corrupts the higher timeframes). Skip it — a genuinely missing minute
        // is back-filled as a flat candle (previous close) by CandleTools.BulkAddMissingCandles.
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
        {
            Interlocked.Increment(ref _socketRejected);
            return;
        }

        Interlocked.Increment(ref _socketUpdates);
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
    /// trade-stream callback (e.g. Kraken Perpetual which has no kline feed).
    /// Acquires the cache semaphore synchronously so trade ordering is preserved.
    /// </summary>
    protected void UpdateCacheFromTrade(string exchangeName,
        DateTime tradeTime, decimal price, decimal quoteVolume)
    {
        if (!_cache.TryGetValue(exchangeName, out var entry))
        {
            Interlocked.Increment(ref _socketUnknownSymbol);
            return;
        }

        Interlocked.Increment(ref _socketUpdates);
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
    /// The price a synthesized flat candle repeats: the last price this market delivered, and failing
    /// that the close of the newest 1m candle already held for the symbol.
    /// <para>
    /// That second source is what keeps an instrument alive that has not traded since the scanner
    /// started. <see cref="CryptoSymbol.LastPrice"/> is cleared when the candles are loaded and is only
    /// ever assigned by <see cref="CandleTools.Process1mCandleAsync"/>, so on a market without a price
    /// ticker - HyperLiquid has none - it stays null until the websocket pushes a kline. Without a
    /// price there is no flat candle AND no <see cref="Subscription.IncrementTickerCount"/>, so the
    /// subscription never reports activity, <see cref="SubscriptionManager.NeedsRestart"/> declares it
    /// dead every cycle, and the restart cannot repair anything because there was nothing wrong with
    /// it. On HyperLiquid Perpetual, 29-08-2026, RIVNUSDC.XYZ and VSTUSDC.PARA were rebuilt every ten
    /// minutes from the moment the scanner started and wrote no minute of their own for an hour and a
    /// half; the only candles they got came from the hourly REST gap fill.
    /// </para>
    /// <para>
    /// The candle list is the same value by another route - LastPrice is assigned the close of the
    /// candle that goes into that list - so this widens where the price may come from and changes
    /// nothing about what is stored.
    /// </para>
    /// </summary>
    internal static bool TryGetPriceToRepeat(CryptoSymbol symbol, out decimal price)
    {
        if (symbol.LastPrice.HasValue)
        {
            price = symbol.LastPrice.Value;
            return true;
        }

        if (symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.TryGetLastCandle(out CryptoCandle candle)
            && candle.Close > 0)
        {
            price = candle.Close;
            return true;
        }

        price = 0;
        return false;
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
            // Counted before any work: this is the number that says whether the minute timer is being
            // served at all. A machine that cannot keep up shows fewer ticks than elapsed minutes.
            Interlocked.Increment(ref _flushTicks);
            Interlocked.Exchange(ref _lastFlushTicks, GlobalData.Clock.UtcNow.Ticks);

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

                                Interlocked.Increment(ref _flushRealCandles);
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
                        if (candleLast.OpenTime != expectedUpto && TryGetPriceToRepeat(symbol, out decimal lastPrice))
                        {
                            Interlocked.Increment(ref _flushFlatCandles);
                            IncrementTickerCount();

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
                        else if (candleLast.OpenTime != expectedUpto)
                        {
                            // Nothing flushed and no price to repeat. This is the ONLY path through the
                            // flush that calls no IncrementTickerCount at all, so it is the only one that
                            // can leave a subscription looking dead to the health check.
                            Interlocked.Increment(ref _flushNoPrice);
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
                    Interlocked.Increment(ref _flushErrors);
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
