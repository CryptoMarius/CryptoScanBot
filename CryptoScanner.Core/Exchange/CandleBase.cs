using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

using System.Text.Json;

namespace CryptoScanner.Core.Exchange;

public class CandleBase(ExchangeBase api)
{
    private static readonly SemaphoreSlim GetCandlesSemaphore = new(1);

    private ExchangeBase Api { get; set; } = api;

    /// <summary>
    /// How often a candle request is repeated after the exchange refused it for rate limiting, and
    /// how long is waited in between. Bounded on purpose: an address that stays blocked must not
    /// keep a fetch thread here forever.
    /// </summary>
    private const int MaximumRateLimitAttempts = 5;

    /// <summary>
    /// Waited after the first refusal; the wait grows with the attempt (5, 10, 15, 20, 25 seconds),
    /// so a short throttle costs five seconds and a longer block still gets 75 seconds in total
    /// before the interval is left to the next refresh cycle.
    /// </summary>
    private static readonly TimeSpan RateLimitAttemptDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Answers whether the caller should ask again after a refused request, and does the waiting.
    ///
    /// <para>
    /// Every exchange needs this and each one used to carry its own copy - eight of the twenty-one
    /// had it, the rest simply gave up on the first refusal. Giving up returns the same fetchFrom to
    /// the caller, which stops the loop over that symbol and interval and leaves a hole in the
    /// history until the next refresh cycle. The exchange-specific part is only the typed api call
    /// itself, so the policy lives here and the call sites all read the same.
    /// </para>
    ///
    /// <para>
    /// The client-side limiters (LimitRate per exchange, plus the one inside the library) are what
    /// keep us under the published limits; this is for the refusals that happen anyway. Those are
    /// real: an exchange counts per IP address while a limiter counts per process, the published
    /// limit is not always the whole story (Bybit refused three requests on 19/20-08-2026 while we
    /// sat eleven times below its documented 600 per 5 seconds, every one of them during the
    /// maximum-size history backfill of a freshly listed symbol), and a limiter cannot see the
    /// traffic of other applications on the same address.
    /// </para>
    /// </summary>
    /// <param name="error">The error the exchange returned; anything but a rate limit is left alone.</param>
    /// <param name="prefix">Exchange, symbol and interval, for the log line.</param>
    /// <param name="attempt">Which attempt this would be, counting from 1.</param>
    protected static async Task<bool> RetryAfterRateLimitAsync(CryptoExchange.Net.Objects.Error? error,
        string prefix, int attempt)
    {
        // Not every exchange lands in ErrorType.RateLimitRequest. Mexc answers 429 when the weight of
        // an endpoint is exceeded and 418 once it decided to ban the address for ten minutes, and
        // those arrive as a plain http code - which is why the Mexc, Kucoin, Okx and BloFin fetches
        // each carried a second, near identical block for it.
        // Kucoin puts its own string in ErrorCode ("429000: Too Many Requests", seen 13-07-2023).
        bool rateLimited = error != null
            && (error.ErrorType == CryptoExchange.Net.Objects.Errors.ErrorType.RateLimitRequest
                || error.Code == 429
                || error.Code == 418
                || error.ErrorCode == "429000");
        if (!rateLimited)
            return false;
        if (attempt > MaximumRateLimitAttempts)
        {
            GlobalData.AddTextToLogTab($"{prefix} still rate limited after {MaximumRateLimitAttempts} attempts, leaving it to the next round");
            return false;
        }

        GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits (attempt {attempt})");
        await Task.Delay(RateLimitAttemptDelay * attempt);

        // Stopping (exchange switch, standby, shutdown) beats another attempt.
        return !ExchangeBase.CancellationToken.IsCancellationRequested;
    }

    internal static void SaveCandleInfo(object exchangeInfo, string name)
    {
        // Save for debug
        try
        {
            string folderName = Path.Combine(GlobalData.AppDataFolder, ExchangeBase.ExchangeOptions.ExchangeName);
            Directory.CreateDirectory(folderName);
            string filename = Path.Combine(folderName, name);

            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
            File.WriteAllText(filename, text);
        }
        catch
        {
            // ignore
        }

    }

    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol)
    {
        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0 || symbol.IsBarometerSymbol())
            return;

        using IDisposable client = Api.GetClient();
        bool gapWasFilled = false;
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // The session was stopped (exchange switch, standby, shutdown). One symbol means one round
            // per interval, and on a cold start each of those catches up months of history, so waiting
            // for the symbol to finish is not an option. The cleanup below still runs: it only trims
            // candles that fall outside the fetch window, which is correct however far we got.
            if (ExchangeBase.CancellationToken.IsCancellationRequested)
                break;

            // LastCandleSynchronized only moves when this fetch actually brought candles in. While the
            // socket keeps up it is already at "now" and the loop inside returns without fetching, so
            // a move means the socket missed that stretch - a dead stream, a standby, a restart.
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            CandleTime? synchronizedBefore = symbolInterval.LastCandleSynchronized;

            await Api.Candle.GetCandlesForIntervalAsync(client, symbol, interval);

            if (symbolInterval.LastCandleSynchronized != synchronizedBefore)
                gapWasFilled = true;
        }

        if (gapWasFilled)
            await ResetDerivedStateAfterGapAsync(symbol);

        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }


    /// <summary>
    /// Throw away everything that was DERIVED from candles while a stretch of them was missing.
    /// <para>
    /// The candles themselves heal on their own: CandleTools.CreateCandle overwrites an existing
    /// entry with the real one and clears IsFilled, and BulkCalculateCandles rebuilds the higher
    /// intervals from the corrected minutes. What does not heal is anything built ON them while
    /// they were wrong. During an outage the flush timer keeps synthesising flat candles at the
    /// last known price and feeding them through the analysis, so the ZigZag has absorbed pivots
    /// that were decided on prices which never traded - and since CryptoCandle is a struct, each
    /// pivot holds its own COPY, so correcting the candle list does not reach them. Refreshing
    /// those values would not be enough either: which points became pivots at all was decided on
    /// the flat candles.
    /// </para>
    /// <para>
    /// So the ZigZag is dropped and rebuilt, the zone cursors go back to "never run" so the next
    /// calculation is a full rescan on the corrected history, and DlzAdmin is cleared so that
    /// rescan is actually queued. Guarded on a gap having been filled: this is the expensive kind
    /// of reset, and while the socket keeps up it must never run.
    /// </para>
    /// </summary>
    private static async Task ResetDerivedStateAfterGapAsync(CryptoSymbol symbol)
    {
        // Same lock the candle (re)load paths take before calling this - the analysis threads share
        // these objects, and the trend caches are rebuilt from the candle lists right after.
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            symbol.Data.ResetTrendDataAndCaches();
            symbol.Data.ResetZoneCalculationCursors();
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }

        // Only worth a line once the scanner is up. During startup every symbol legitimately fills
        // a gap - the scanner was off - and reporting that per symbol is three hundred lines saying
        // "we just started". The reset itself does run then, and costs nothing on an empty cache.
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Running)
            GlobalData.AddTextToLogTab($"{symbol.Name} candle gap filled, recalculating trend and zones");
    }


    /// <summary>
    /// Re-evaluate the "enough volume" decision for every symbol of the active exchange. Call it once
    /// per refresh cycle, right after GetSymbolsAsync refreshed the 24 hour volumes and before anything
    /// that reads EnoughVolume() - the subscription synchronisation and the candle fetch below have to
    /// agree on who qualifies, otherwise one of them works on the answer of the previous cycle.
    /// Calling it twice in the same cycle is harmless, the same volume gives the same answer.
    /// </summary>
    public static void UpdateVolumeDecisions()
    {
        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            return;

        foreach (var symbol in exchange.SymbolListName.Values)
        {
            if (symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
                continue;
            symbol.UpdateEnoughVolume();
        }
    }


    public virtual async Task GetCandlesForAllSymbolsAndIntervalsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Fetching {exchange.Name} information");
            try
            {
                await GetCandlesSemaphore.WaitAsync();
                try
                {
                    // The semaphore above already prevents a second fetch from running halfway through this
                    // one, so there is no need to disable the timer as well. Switching the timer off and on
                    // restarted its countdown at the end of every run, which pushed the effective refresh
                    // period well past the configured interval.
                    //GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    // Every caller (the hourly timer in ScannerSession and both refresh commands) already
                    // calls GetSymbolsAsync itself right before this method, and during startup ThreadLoadData
                    // does the same. Calling it again here fetched the ticker and instrument definitions of the
                    // exchange a second time per cycle, which also made every error appear twice in the log.
                    //if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                    //    await Api.Symbol.GetSymbolsAsync();

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"{exchange.Name} symbols={exchange.SymbolListName.Values.Count}");


                    // Safety net for callers that did not do this themselves (startup). Harmless when it
                    // already ran: the same volume gives the same answer a second time.
                    UpdateVolumeDecisions();

                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
                            continue;

                        // The not so interesting coins (saves a lot of memory)
                        if (!symbol.EnoughVolume() && !symbol.IsTrading())
                            continue;

                        //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                        queue.Enqueue(symbol);
                    }

                    int symbolTotal = queue.Count;
                    int symbolsDone = 0;

                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () =>
                        {
                            try
                            {
                                while (true)
                                {
                                    CryptoSymbol symbol;

                                    Monitor.Enter(queue);
                                    try
                                    {
                                        if (queue.Count > 0)
                                            symbol = queue.Dequeue();
                                        else
                                            break;
                                    }
                                    finally
                                    {
                                        Monitor.Exit(queue);
                                    }

                                    // The session was stopped (exchange switch, standby, shutdown). Without this
                                    // the remainder of the queue was still fetched, which kept the previous
                                    // exchange busy for minutes after the user had already chosen another one.
                                    if (ExchangeBase.CancellationToken.IsCancellationRequested)
                                        break;

                                    // Er is niet geswitched van exchange (omdat het ophalen zo lang duurt)
                                    if (symbol.ExchangeId == GlobalData.ActiveExchange!.Id)
                                    {
                                        int done = Interlocked.Increment(ref symbolsDone);
                                        GlobalData.CandleProgressText = $"{done} / {symbolTotal}  ({symbol.Name})";

                                        // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                                        // De datum en tijd tot na het activeren van beide streams (overlap)
                                        CandleTools.DetermineFetchStartDate(symbol);
                                        await GetCandlesForAllIntervalsAsync(symbol);
                                    }
                                }
                            }
                            catch (Exception error)
                            {
                                ScannerLog.Logger.Error(error, "");
                                GlobalData.AddErrorToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " +
                            }
                        });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);
                    GlobalData.CandleProgressText = "";

                    //GlobalData.AddTextToLogTab("Candles ophalen klaar");
                }
                finally
                {
                    // Enabled analysing
                    //GlobalData.SetCandleTimerEnable(true);

                    GetCandlesSemaphore.Release();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddErrorToLogTab("error get prices " + error.ToString());
            }
        }
    }


    public async Task GetCandlesForIntervalAsync(IDisposable client, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData!.FetchCandles)
            return;

        CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        bool intervalSupported = symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod);
        if (intervalSupported)
        {
            // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
            while (symbolInterval.LastCandleSynchronized < currentTime)
            {
                if (symbolInterval.LastCandleSynchronized + interval.Duration > currentTime)
                    break;

                // LastCandleSynchronized alway's has a value (minimum period start or last synched)
                CandleTime fetchFrom = symbolInterval.LastCandleSynchronized.Value;
                var (_, _, fetchedUpTo) = await Api.Candle.GetCandlesForInterval(client, symbol, interval, fetchFrom);
                symbolInterval.LastCandleSynchronized = fetchedUpTo;

                //await symbol.Data.CandleLock.WaitAsync();
                //try
                //{
                //    CandleTools.UpdateCandleFetched(symbol, interval);
                //}
                //finally
                //{
                //    symbol.Data.CandleLock.Release();
                //}

                if (symbolInterval.LastCandleSynchronized == fetchFrom) // not moving forward
                    break;

                // The session was stopped. Leaving the loop (instead of returning) on purpose: the
                // administration below has to run for the part that was fetched, exactly like it does
                // for the two breaks above. LastCandleSynchronized only moved up to what really
                // arrived, so the next start simply continues from there.
                if (ExchangeBase.CancellationToken.IsCancellationRequested)
                    break;

                currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
            }
        }


        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            // once
            CandleTools.UpdateCandleFetched(symbol, interval);

            // Add missing candles (the only place we know it can be done safely)
            CandleTools.BulkAddMissingCandles(symbol, interval);

            // Bulk calculate the higher interval candles
            if (interval.IntervalPeriod < Enum.GetValues(typeof(CryptoIntervalPeriod)).Cast<CryptoIntervalPeriod>().Last())
            {
                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, currentTime);
            }

            //// Adjust the administration for the not supported interval's
            //if (!intervalSupported)
            //{
            //    CandleTools.UpdateCandleFetched(symbol, interval);
            //}
            // twice
            CandleTools.UpdateCandleFetched(symbol, interval);
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }


    public async Task<(bool anythingAdded, CandleTime askedUpTo)> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, CandleTime unixLoop, CandleTime unixMax)
    {
        // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
        // Kind of the same as the CandleBase.GetCandlesForIntervalAsync, but also different because
        // of the symbolInterval.LastCandleSynchronized and calculation of higher interval candles

        //if (GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, " +
        //        $"{CandleTools.GetUnixDate(unixLoop).ToLocalTime()}, {CandleTools.GetUnixDate(unixMax).ToLocalTime()})");

        int totalFetched = 0;
        // Everything below this point has been requested by the time the loop ends. Kept separately
        // because unixLoop is also moved forward over candles that were already present, and because
        // the loop can break out early - then only the part up to here was really asked for.
        CandleTime askedUpTo = unixLoop;
        if (unixLoop < unixMax)
        {
            var api = symbol.Exchange.GetApiInstance();
            using IDisposable client = api.GetClient();
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            while (unixLoop < unixMax)
            {
                if (unixLoop + interval.Duration > unixMax)
                    break;

                CandleTime minTime = unixLoop;
                CandleTime maxTime = unixLoop + (ExchangeBase.ExchangeOptions.CandleLimit - 1) * interval.Duration;

                CandleTime lastDate = minTime;
                int countBefore = symbolInterval.CandleList.Count;
                var result = await symbol.Exchange.GetApiInstance().Candle.GetCandlesForInterval(client, symbol, interval, minTime);
                unixLoop = result.fetchedUpTo;

                int added = symbolInterval.CandleList.Count - countBefore;
                totalFetched += added;

                bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
                if (debug)
                    ScannerLog.Logger.Info($"Core.Exchange.FetchFrom({symbol.Name}, {interval!.Name}, " +
                        $"{minTime.ToDateTime()} .. {maxTime.ToDateTime()} limit={ExchangeBase.ExchangeOptions.CandleLimit} added={added}");


                //string text3 = $"{text} retrieved={added} total={candleList.Count}";
                //ScannerLog.Logger.Info(text3);
                //GlobalData.AddTextToLogTab(text3);CandleTime

                while (symbolInterval.CandleList!.ContainsKey(unixLoop))
                    unixLoop += interval.Duration;

                if (unixLoop == minTime) // not moving forward
                    break;

                askedUpTo = unixLoop;
            }
        }


        //if (GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, " +
        //        $"{CandleTools.GetUnixDate(unixLoop).ToLocalTime()}, {CandleTools.GetUnixDate(unixMax).ToLocalTime()}) fetched {totalFetched}");

        return (totalFetched > 0, askedUpTo);
    }


    internal static bool CheckFutureCandleReceived(DateTime openTime, CryptoSymbol symbol, CryptoInterval interval,
        decimal closePrice)
    {
        CandleTime candleTime = CandleTime.AlignFromDateTime(openTime, interval.Duration);
        CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, interval.Duration);
        if (candleTime + interval.Duration > currentTime)
        {
            // Report the values the condition above actually compares (the candle's close time
            // against the current candle), not openTime/candleTime - those are the same aligned
            // moment and printed a meaningless "15:55 > 15:55".
            ScannerLog.Logger.Debug($"Debug: future candle {symbol.Name} {interval.Name} " +
                $"close={(candleTime + interval.Duration).ToLocalTime()} > now={currentTime.ToLocalTime()}");
            return true;
        }

        if (closePrice <= 0)
        {
            ScannerLog.Logger.Debug($"Debug: candle with close price 0 {symbol.Name} {interval.Name} {openTime.ToLocalTime()} > {candleTime.ToLocalTime()}");
            return true;
        }
        return false;
    }
}
