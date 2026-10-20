using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// The outcome of a candle fetch: how many symbols of the run config were handled, whether a
/// Stop cut it short, and the error that ended it (null when it ran to the end or was stopped).
/// </summary>
public record CandleFetchResult(int Done, int Total, bool Cancelled, string? Error);


/// <summary>
/// Fetch symbols and fetch candles for the active exchange, without a window. The emulator's
/// buttons call these, and so does the console program that keeps the emulator databases up to
/// date by itself (CryptoScanBot.CandleSync). One implementation for both, so a scheduled update
/// cannot do anything else than what the button does.
/// <para>
/// Status lines go to <c>status</c> (the emulator puts them in its status bar, the console prints
/// them); everything else goes to the log through <see cref="GlobalData.AddTextToLogTab"/> as before.
/// </para>
/// </summary>
public static class EmulatorCandleSync
{
    /// <summary>
    /// Fetches the symbol list for the active exchange (REST call, no websocket subscriptions).
    /// Mirrors what ThreadLoadData does at scanner startup; needed before Configure can show
    /// quotes and before TickRunner has any symbols to drive. Returns the number of symbols.
    /// </summary>
    public static async Task<int> FetchSymbolsAsync(Action<string> status)
    {
        if (GlobalData.ActiveExchange == null)
            throw new Exception("No active exchange");

        status($"Fetching symbols for {GlobalData.ActiveExchange.Name}…");
        GlobalData.AddTextToLogTab($"Fetch symbols: {GlobalData.ActiveExchange.Name} — start");

        // CRITICAL: re-sync the in-memory SymbolListName from the DB before invoking the
        // REST fetch. The Symbol table has NO UNIQUE constraint on (ExchangeId, Name); the
        // entire dedup logic in IsSymbolAccepted (SymbolBase.cs) runs against this cache.
        // If the cache is empty or out of sync, every Fetch click reinserts the full list,
        // producing duplicates in the DB. Bootstrap already does this once at startup,
        // but explicitly repeating it here is cheap (AddSymbol skips known keys) and
        // covers state corruption scenarios (cache lost, parallel writes from elsewhere).
        int beforeCount = GlobalData.ActiveExchange.SymbolListName.Count;
        GlobalData.LoadSymbols();
        int afterLoadCount = GlobalData.ActiveExchange.SymbolListName.Count;
        GlobalData.AddTextToLogTab($"Fetch symbols: in-memory cache synced with DB ({beforeCount} → {afterLoadCount})");

        // Same call the live scanner makes (ThreadLoadData.cs). Hits the exchange REST
        // API, writes/updates rows in the Symbol table, sets LastTimeFetched.
        await GlobalData.ActiveExchange.GetApiInstance().Symbol.GetSymbolsAsync();

        // Refresh the in-memory symbol caches from the DB so Configure and run-time code
        // see what we just persisted.
        GlobalData.LoadSymbols();

        // Wire the freshly-known symbols into their quote-side index so Configure shows
        // the new per-quote counts immediately (otherwise it stays empty until next start).
        ThreadLoadData.IndexQuoteDataSymbols(GlobalData.ActiveExchange);

        // Same notification the live scanner sends so any future grid/Combo bindings
        // refresh themselves.
        GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());

        int count = GlobalData.ActiveExchange.SymbolListName.Count;
        status($"Fetched {count} symbols for {GlobalData.ActiveExchange.Name}.");
        GlobalData.AddTextToLogTab($"Fetch symbols: {GlobalData.ActiveExchange.Name} — done ({count} symbols)");
        return count;
    }


    /// <summary>
    /// For every symbol listed in CryptoScanBot-Emulator.json, fetch the 1m candles needed for
    /// the backtest window from the exchange, then derive all higher intervals locally via
    /// <see cref="CandleTools.BulkCalculateCandles"/>. Only one REST stream per symbol instead of
    /// one per interval, which is the main driver of fetch time on long windows.
    ///
    /// The 1m warmup window is the largest of what the indicators need
    /// (<see cref="IndicatorWarmup.ComputeWarmupMinutes"/>) and what the chart needs
    /// (WindowMarginCandles + WindowCalcWarmupCandles expressed in minutes). Higher intervals are
    /// derived in ascending-duration order — IntervalList is already sorted that way — so a 4h
    /// candle built from 1h finds the 1h list already populated when its turn comes.
    /// <para>
    /// The window ends at <paramref name="until"/>, or at the run config's ToDate when that is null.
    /// An update passes the current moment: it adds every candle that appeared since the last fetch.
    /// What is in the candle database already is read from disk and not asked again, so only the new
    /// tail goes to the exchange. Run that regularly on an exchange that only serves its newest
    /// candles: HyperLiquid hands out the newest 5000 per interval, 3.5 days of 1m.
    /// </para>
    /// </summary>
    public static async Task<CandleFetchResult> FetchCandlesAsync(DateTime? until, Action<string> status, CancellationToken cancellationToken)
    {
        if (GlobalData.ActiveExchange == null)
            return new CandleFetchResult(0, 0, false, "No active exchange — restart and pick one in the Setup dialog.");

        // A run parks the clock on the moment it last replayed. FinishRun puts it back on real time,
        // but a run that never reached it - a crash, a kill - leaves it standing. Repeat it here,
        // because this is the routine that suffers: ZoneCandleEngine will not ask an exchange for
        // candles beyond the clock, so a stale one quietly limits the synchronisation to that old
        // date and reports "done" without having fetched a thing.
        if (GlobalData.Clock is EmulatorClock clock)
            clock.UtcNow = DateTime.UtcNow;

        EmulatorRunConfig config;
        try
        {
            config = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            return new CandleFetchResult(0, 0, false, $"Failed to read run config: {ex.Message}");
        }

        if (config.Symbols.Count == 0)
            return new CandleFetchResult(0, 0, false, "Run config has no symbols — edit CryptoScanBot-Emulator.json first.");

        // The end of the window: the run config's ToDate, or the moment of an update
        return await FetchCandlesForSymbolsAsync(config.Symbols, config.FromDate, until ?? config.ToDate, until.HasValue, status, cancellationToken);
    }


    /// <summary>
    /// The fetch itself, for a given list of symbols and a given window instead of the run config's.
    /// CryptoScanBot.CandleSync calls it with the symbols of a candle database and the start date
    /// it was given; <see cref="FetchCandlesAsync"/> with the run config.
    /// </summary>
    public static async Task<CandleFetchResult> FetchCandlesForSymbolsAsync(IReadOnlyList<string> symbolNames,
        DateTime fromDate, DateTime fetchUntil, bool isUpdate, Action<string> status, CancellationToken cancellationToken)
    {
        if (GlobalData.ActiveExchange == null)
            return new CandleFetchResult(0, 0, false, "No active exchange — restart and pick one in the Setup dialog.");

        if (GlobalData.Clock is EmulatorClock clock)
            clock.UtcNow = DateTime.UtcNow;

        if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
            return new CandleFetchResult(0, 0, false, "1m interval not registered — cannot fetch candles.");

        int total = symbolNames.Count;
        int symbolIdx = 0;
        bool cancelled = false;

        List<CryptoInterval> intervals = GlobalData.IntervalList;
        GlobalData.AddTextToLogTab(
            $"Fetch candles: {total} symbol(s), window {fromDate:yyyy-MM-dd HH:mm}..{fetchUntil:yyyy-MM-dd HH:mm} UTC" +
            (isUpdate ? " (update to now)" : "") + ", 1m from exchange + higher derived locally");

        try
        {
            // Run the whole fetch on a background thread so the UI stays responsive, same as the
            // replay run. ZoneCandleEngine.FetchFrom is async (network I/O) but the work between
            // the awaits — IsDataLocal walks, candle aggregation, candles.db reads/writes — is
            // synchronous CPU/disk work that would otherwise stutter the UI on a big fetch.
            // Status is a UI-bound property, so every assignment inside is marshalled back to the
            // UI thread; AddTextToLogTab already marshals itself via the Log tab.
            await Task.Run(async () =>
            {
                // One connection for the whole fetch; each symbol is persisted (and its memory
                // released) as soon as it is complete, see step 3 below. InitializeSchema is
                // normally done by the bootstrap but is cheap and covers a fresh candle folder.
                CandleDatabase.InitializeSchema(GlobalData.ActiveExchange);
                using var candleDb = new CandleDatabase(GlobalData.ActiveExchange);
                candleDb.Open();
                // Which periods were asked from the exchange before: asked once counts as accepted
                // (history before a listing, holes in the exchange's data). Emulator-only table.
                EmulatorHistoryAskedStore.EnsureTable(candleDb.Connection);

                foreach (string symbolName in symbolNames)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        GlobalData.AddTextToLogTab($"Fetch candles: stopped by user after {symbolIdx}/{total} symbol(s)");
                        break;
                    }

                    symbolIdx++;
                    // Config names may be bare pairs from before the product moved into the name
                    if (!GlobalData.ActiveExchange.TryGetSymbolByPair(symbolName, out CryptoSymbol? symbol))
                    {
                        status($"Symbol '{symbolName}' not in exchange list — did you Fetch symbols first?");
                        GlobalData.AddTextToLogTab($"Fetch candles: SKIP {symbolName} — not in exchange list");
                        continue;
                    }

                    string symbolStatus = $"Fetching candles for {symbol.Name} ({symbolIdx}/{total})…";
                    status(symbolStatus);
                    // The archive download reports per file; shown behind the symbol so the status moves
                    // during the twenty-odd seconds a symbol can take.
                    GlobalData.CandleFetchProgress = text => status($"{symbolStatus}  {text}");

                    // One dict per symbol, exactly like the DLZ zone-calculation path: it tells
                    // ZoneCandleEngine.FetchFrom which intervals it has already materialised from
                    // disk/candles.db into the in-memory CandleList, so the IsDataLocal verify
                    // step sees the full picture instead of only the bounded startup load.
                    ZoneCandleWindows loadedCandlesInMemory = new();
                    EmulatorHistoryAskedStore.Load(candleDb.Connection, symbol);


                    // ── Step 1: determine the fetch start per interval ───────────────────────
                    // Exactly what the live scanner does before GetCandlesForAllIntervalsAsync
                    // (see CandleBase.GetCandlesForAllSymbolsAndIntervalsAsync). It applies
                    // GetCandleFetchStart per interval — 500 bars of that interval's own
                    // resolution, and InitialCandleCountFetch for 1m — and then walks the
                    // ConstructFrom chain for every interval this exchange does NOT serve, pulling
                    // the source interval's start date back far enough to build it. That walk is a
                    // loop, so a chain of several missing intervals cascades correctly, and it
                    // takes the MINIMUM over all consumers so a source is deepened once for
                    // everyone instead of once per dependent interval.
                    //
                    // The only difference with the scanner is the reference point: it measures back
                    // from "now", the emulator from the START of its replay window.
                    CandleTools.DetermineFetchStartDate(symbol, fromDate);

                    CandleTime fetchEnd = IntervalTools.StartOfIntervalCandle(
                        CandleTime.AlignFromDateTime(fetchUntil, interval1m.Duration), interval1m.Duration);

                    // ── Step 2: fetch + derive, interval by interval ─────────────────────────
                    // Mirrors CandleBase.GetCandlesForIntervalAsync: fetch the interval when the
                    // exchange serves it, then fill gaps and bulk-calculate the NEXT interval from
                    // its ConstructFrom. IntervalList runs ascending, so by the time an interval is
                    // reached its source is complete — including the ones the exchange does not
                    // serve, which are produced here rather than fetched.
                    foreach (CryptoInterval interval in intervals)
                    {
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        CandleTime fetchStart = symbolInterval.LastCandleSynchronized
                            ?? IntervalTools.StartOfIntervalCandle(
                                CandleTools.GetCandleFetchStart(symbol, interval, fromDate), interval.Duration);

                        if (symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod) && fetchEnd > fetchStart)
                        {
                            int candleFetchCount = (int)((fetchEnd.Minutes - fetchStart.Minutes) / interval.Duration);
                            GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} {interval.Name} — " +
                                $"want {fetchStart.ToDateTime():yyyy-MM-dd HH:mm}..{fetchEnd.ToDateTime():yyyy-MM-dd HH:mm} ({candleFetchCount} bars)");
                            try
                            {
                                await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval, fetchStart, candleFetchCount);
                            }
                            catch (Exception sx)
                            {
                                GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} {interval.Name} — FAILED ({sx.Message})");
                            }
                        }

                        await symbol.Data.CandleLock.WaitAsync();
                        try
                        {
                            CandleTools.UpdateCandleFetched(symbol, interval);
                            CandleTools.BulkAddMissingCandles(symbol, interval);

                            // Build the next interval up from its own ConstructFrom (which is not
                            // necessarily THIS interval — 5m is built from 1m, not from 3m).
                            if (interval.IntervalPeriod < Enum.GetValues<CryptoIntervalPeriod>().Last())
                            {
                                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                                CandleTools.BulkCalculateCandles(symbol, targetInterval.ConstructFrom!, targetInterval, fetchEnd);
                                // Changed, so it still has to be written back - but NOT marked as
                                // loaded. MarkAllLoaded claims MinValue..MaxValue: the whole timeline,
                                // for ever. What was actually built here is only what could be derived
                                // from the source candles that happened to be in memory at this moment,
                                // and that is a good deal less than what candles.db already holds.
                                //
                                // With the claim in place, FetchFrom skipped its bounded disk read when
                                // the loop reached this interval, found the holes the derivation had
                                // left, and asked the EXCHANGE to fill them - one request per symbol per
                                // interval, for candles that were sitting in candles.db all along. That
                                // is why a fetch kept downloading history that was never deleted, and
                                // why 1m was the only interval that stayed quiet: nothing derives 1m, so
                                // it was the only one that still did its disk read.
                                //
                                // Reading from disk cannot undo the derivation either: the read uses
                                // TryAdd, so a candle already in memory wins over the stored one.
                                loadedCandlesInMemory.MarkChanged(targetInterval.IntervalPeriod);
                            }
                            CandleTools.UpdateCandleFetched(symbol, interval);
                        }
                        finally
                        {
                            symbol.Data.CandleLock.Release();
                        }
                    }

                    // ── Step 3: persist this symbol to {Exchange}.db and release its memory ──
                    // Per symbol instead of one SaveCandlesAsync at the end: with the full
                    // interval ladder a symbol-year is ~1.2M in-memory candles (~100 MB), so a
                    // fetch of many symbols over a long window would otherwise accumulate
                    // gigabytes. The replay never needs these lists — TickRunner reloads per
                    // chunk from candles.db — and ClearCandles leaves LastCandleSynchronized
                    // intact, so a next fetch still resumes where this one ended. Bonus: a Stop
                    // (or crash) loses at most the symbol in progress, everything before it is
                    // already on disk.
                    status($"Saving {symbol.Name} to database ({symbolIdx}/{total})…");
                    CandleDatabase.SaveCandlesForSymbol(candleDb.Connection, symbol);
                    EmulatorHistoryAskedStore.Save(candleDb.Connection, symbol);
                    symbol.ClearCandles();

                    GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} — done (saved, memory released)");
                }
            });

            // On a Stop the candles fetched so far are already persisted (each symbol is saved
            // right after its fetch), so a re-run of the fetch resumes where it left off.
            if (cancelled)
            {
                status($"Fetch candles: stopped after {symbolIdx}/{total} symbol(s), fetched data saved.");
                GlobalData.AddTextToLogTab($"Fetch candles: stopped ({symbolIdx}/{total} symbol(s) done)");
            }
            else
            {
                status($"Fetch candles: completed for {total} symbol(s).");
                GlobalData.AddTextToLogTab($"Fetch candles: completed ({total} symbol(s))");
            }
            return new CandleFetchResult(symbolIdx, total, cancelled, null);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Fetch candles: FAILED — {ex.Message}");
            return new CandleFetchResult(symbolIdx, total, cancelled, $"Fetch candles failed: {ex.Message}");
        }
        finally
        {
            GlobalData.CandleFetchProgress = null;
        }
    }
}
