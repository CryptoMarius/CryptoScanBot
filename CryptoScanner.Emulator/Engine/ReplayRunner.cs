using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Zones;

using System.Diagnostics;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Progress payload emitted by <see cref="ReplayRunner"/> after each replayed candle.
/// </summary>
public readonly record struct ReplayProgress(int Percent);


/// <summary>
/// Drives the emulator replay. Multi-symbol time-merged feed: the loop walks the replay window one
/// base interval at a time and, for every symbol that has a candle at that step, processes its tick
/// before the clock advances. This matches what a real exchange does — symbols don't run on
/// independent timelines — and is what lets cross-symbol strategies (barometer, trend filters that
/// read other symbols) behave the same way in the emulator as they do live.
///
/// Candles are HANDED OVER, not rebuilt. The fetch already computed and stored every interval, so
/// each chunk loads all of them per symbol (<see cref="SymbolReplay"/>) and the replay passes each
/// candle to the engine at the moment it closes. That keeps the intervals below the base interval
/// up to date as well — synthesis could only ever build upwards — and removes a second candle
/// implementation that can drift from what is stored.
///
/// Two rhythms: the analysis runs once per base candle, the order handling drops to minute
/// resolution as soon as an open position can be moved by that candle. See ProcessComputeAsync.
/// </summary>
public sealed class ReplayRunner
{
    public IProgress<ReplayProgress>? Progress { get; init; }

    /// <summary>
    /// When true, the symbols of each replay minute are processed in parallel (their per-symbol state
    /// is independent). The shared DB flush + zone drain still run serially after the parallel phase,
    /// per minute, so the outcome is deterministic. Set to false for a single-threaded baseline (to
    /// confirm parallel and serial produce the same signals/positions).
    /// </summary>
    public bool RunParallel { get; init; } = true;

    /// <summary>
    /// Number of days per chunk for the chunked replay loop. When the replay window exceeds this
    /// duration, candles are loaded and replayed one chunk at a time, keeping memory bounded for
    /// runs with many symbols. Set to 0 to disable chunking (load everything up front — original
    /// behaviour). Default is 7 days.
    /// </summary>
    public int ChunkDays { get; init; } = 7;

    // ───── Per-phase profiling accumulators ─────────────────────────────────────────
    // Raw Stopwatch ticks spent in each hot-loop phase, summed across every processed tick.
    // GetTimestamp() is a static QueryPerformanceCounter read (no allocation), so accumulating
    // per tick is negligible against the engine work it measures. Converted to wall-time and
    // logged once at the end of RunAsync, so a run tells you where its time actually went —
    // candle synthesis, the signal/trade pipeline, zone calculation, or DB flushing — instead
    // of having to guess before optimising.
    private long elapsedProcess1m;
    private long elapsedPipeline;
    private long elapsedZoneDrain;
    private long elapsedFlush;
    private long elapsedBarometer;

    // Wall-clock of the whole RunAsync, and the up-front warmup (PrepareSymbol per symbol). These let
    // the Timing line reconcile against the real run time: the four per-tick buckets above only cover
    // the instrumented phases, so wall − (buckets + warmup) is the "unaccounted" remainder (loop
    // overhead, logging, the per-tick debug block, etc.).
    private readonly Stopwatch runWall = new();
    private long elapsedWarmup;

    // Coarse outer-loop timer: measures the full body of each non-empty iteration (from after the
    // ticksThisMinute.Count == 0 guard to the end of the iteration). The difference between this
    // and the sum of the measured sub-phases (process1m + pipeline + zones + flush) is the overhead
    // that lives inside the loop but outside the fine-grained stopwatches — Parallel.ForEachAsync
    // scheduling, NLog writes, etc. Whatever remains after subtracting outerLoop from wall-warmup
    // is overhead that lives entirely outside the loop (startup/shutdown bookkeeping, etc.).
    private long elapsedOuterLoop;

    // Per-decile (0-9% .. 90-99%) wall-clock buckets to pinpoint where the run slows down.
    private readonly long[] decileWallTicks = new long[10];
    private int lastDecile = -1;
    private long decileStart;

    // Snapshot of every accumulator at the end of the previous chunk. The end-of-run report tells you
    // how the TOTAL time was divided, but not which part of it grows as the replay walks forward —
    // that is what the per-chunk delta below answers: every chunk logs the wall time it took plus the
    // increase of each bucket since the previous chunk, so a phase with quadratic behaviour shows up
    // as a steadily rising column instead of being averaged away over the whole run.
    private ProfileSnapshot lastChunkSnapshot;
    private long lastChunkWallTicks;

    // The resolved base interval for this run (set at the start of RunAsync).
    private CryptoInterval activeBaseInterval = null!;

    // The 1m interval, resolved once. Only used when the base interval is coarser than 1m, to keep
    // the 1m CandleList filled for the engine parts that read it directly.
    private CryptoInterval oneMinuteInterval = null!;

    // The market barometer of this run, or null when it is switched off (or when the run replays too
    // few coins to measure one). See BarometerReplay.
    private BarometerReplay? barometer;


    /// <summary>
    /// Per-symbol replay data for one chunk: the already-stored candles of EVERY interval, plus a
    /// cursor per interval saying how far they have been handed to the engine.
    ///
    /// <see cref="AdvanceTo"/> copies the candles that have CLOSED at the given clock time into the
    /// symbol's CandleList. No synthesis: the fetch already computed and stored every interval, so
    /// rebuilding them from a lower one during the replay is work that has been done before — and a
    /// second implementation that can drift away from the stored candles. It also means intervals
    /// below the base interval (2m..10m on a 15m run) simply keep up, which synthesis could not do
    /// because it only ever builds upwards.
    /// </summary>
    private sealed class SymbolReplay
    {
        public required CryptoSymbol Symbol { get; init; }
        public required Dictionary<CryptoIntervalPeriod, CryptoCandleList> Candles { get; init; }
        private readonly Dictionary<CryptoIntervalPeriod, CandleTime> cursor = [];

        public void ResetCursors(CandleTime windowFrom)
        {
            cursor.Clear();
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                // Start at the first boundary of this interval at or after the window start.
                uint aligned = windowFrom.Minutes - (windowFrom.Minutes % interval.Duration);
                cursor[interval.IntervalPeriod] = new CandleTime(aligned);
            }
        }

        /// <summary>
        /// Hands over every candle that is complete at <paramref name="clockTime"/>. A candle counts
        /// as complete when its close time has been reached — handing it over earlier would let the
        /// analysis read a bar that is still forming.
        /// </summary>
        public void AdvanceTo(CandleTime clockTime)
        {
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (!Candles.TryGetValue(interval.IntervalPeriod, out CryptoCandleList? source))
                    continue;

                CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);
                bool isDaily = interval.IntervalPeriod == CryptoIntervalPeriod.interval1d;
                CandleTime at = cursor[interval.IntervalPeriod];
                while (at + interval.Duration <= clockTime)
                {
                    if (source.TryGetValue(at, out CryptoCandle candle))
                    {
                        symbolInterval.CandleList.TryAdd(at, candle);

                        // The symbol's 24 hour volume, taken from the day being replayed instead of
                        // from the last "fetch symbols". See EmulatorVolume for why.
                        if (isDaily)
                            EmulatorVolume.ApplyDailyVolume(Symbol, candle);
                    }
                    at += interval.Duration;
                }
                cursor[interval.IntervalPeriod] = at;
            }
        }
    }


    private static void ReceivedCreatedSignals(CryptoSignal signal)
    {
        //GlobalData.CreatedSignalCount++;
        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);
    }

    public async Task RunAsync(EmulatorRunConfig config, CancellationToken ct)
    {
        var exchange = GlobalData.ActiveExchange!;
        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;

        // Enable the per-candle pipeline profiler for this run (off in the live scanner). It breaks
        // NewCandleArrivedAsync down into indicators / algorithms / trade handling / position check,
        // so the LogPhaseTimings summary can show where the dominant "pipeline" time actually goes.
        PipelineProfiler.Reset();
        // Same reason: a second run in the same queue has to report its own candle gaps, not stay
        // quiet because the previous run already mentioned that symbol.
        ZoneCandleGaps.Reset();
        PipelineProfiler.Enabled = true;
        runWall.Restart();
        try
        {
            // Resolve the base interval from the run config (default "1m" for full precision).
            string baseIntervalName = config.BaseInterval ?? "1m";
            if (!GlobalData.IntervalListPeriodName.TryGetValue(baseIntervalName, out CryptoInterval? baseInterval))
                throw new InvalidOperationException($"Base interval '{baseIntervalName}' not registered in GlobalData.IntervalListPeriodName.");
            activeBaseInterval = baseInterval;

            if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
                throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName.");
            oneMinuteInterval = interval1m;

            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, baseInterval.Duration);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, baseInterval.Duration);

            // Advance the emulator clock to the END of the replay window before loading candles.
            // LoadCandlesInRange applies an "OpenTime <= Clock.UtcNow" filter when
            // CurrentEmulatorRunId is set (which StartRun already did). Without this, a stale
            // clock left over from a cancelled previous run (or a fresh app start) would clip the
            // warmup and replay candle loads to whatever date the clock happened to be frozen at.
            // The replay loop resets the clock minute-by-minute anyway once it starts.
            if (GlobalData.Clock is EmulatorClock preClock)
                preClock.UtcNow = replayTo.ToDateTime();

            // ───── Resolve symbols ──────────────────────────────────────────────────
            var symbols = new List<CryptoSymbol>();
            foreach (string symbolName in config.Symbols)
            {
                ct.ThrowIfCancellationRequested();
                // Configs and the hand-edited queue JSON hold bare pairs ("BTCUSDT") from before
                // the product moved into the name; TryGetSymbolByPair accepts both spellings.
                if (!exchange.TryGetSymbolByPair(symbolName, out CryptoSymbol? symbol))
                    throw new InvalidOperationException($"Symbol '{symbolName}' not found on exchange '{config.ExchangeName}'.");
                symbols.Add(symbol);
            }

            // Sorted by name so the processing order does not depend on how the symbols happen to be
            // ordered in the run config — reordering that list must not change the outcome of a run.
            // Ordinal, not culture-aware, so the order is the same on every machine.
            symbols.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));


            // ───── Reset all transient state from any previous run ────────────────
            ResetRunState(exchange, symbols);
            AssertCleanState(exchange, symbols);

            // ───── Hand out the paper-trading start capital ─────────────────────────
            // After the clean-state assert on purpose: the assert wants an empty asset list, and the
            // seeding is what fills it. Every run therefore starts with exactly the same amount of
            // money, so two runs over the same period stay comparable.
            decimal startCapital = config.StartCapital > 0 ? config.StartCapital : GlobalData.Settings.Trading.PaperAssetStartCapital;
            PaperAssets.ResetAssets(exchange, startCapital);
            // A filled default asset list is the starting point of the run, and then this amount is
            // not used at all - worth saying, because the run label still carries it.
            int assetDefaults = GlobalData.Settings.Trading.PaperAssetDefaults.Count;
            if (assetDefaults > 0)
                GlobalData.AddTextToLogTab($"Start capital: the {assetDefaults} default asset(s) of the settings");
            else
                GlobalData.AddTextToLogTab($"Start capital: {startCapital:N2} per quote coin, " +
                    "except the ones with a start capital of their own");

            // The first point of the capital line: the start capital, stamped with the first day of
            // the run. The date is handed in because the clock is parked on the END of the replay
            // window at this moment (see the preClock above), and a snapshot stamped there would put
            // the start capital behind the result of the run.
            AssetSnapshotTools.Capture(exchange, replayFrom.ToDateTime());

            // ───── Warmup all symbols up-front ──────────────────────────────────────
            long warmupStart = Stopwatch.GetTimestamp();
            EmulatorVolume.ResetDiagnostics();
            foreach (var symbol in symbols)
            {
                ct.ThrowIfCancellationRequested();
                IndicatorWarmup.WarmupSymbol(symbol, replayFrom);
            }
            elapsedWarmup = Stopwatch.GetTimestamp() - warmupStart;
            GlobalData.AddTextToLogTab($"Warmup ({symbols.Count} symbol(s)): " +
                $"{(double)elapsedWarmup / Stopwatch.Frequency:F1}s " +
                $"[base interval: {baseInterval.Name}]");

            // The volume follows the replayed day from here on, so say so - the minimum-volume check
            // is the symbol selection in practice, and a silent switch of what feeds it would make
            // this run incomparable with every earlier one without a trace in the log.
            GlobalData.AddTextToLogTab($"Volume: taken from the daily candle being replayed" +
                (EmulatorVolume.SymbolsWithoutDailyVolume > 0
                    ? $" ({EmulatorVolume.SymbolsWithoutDailyVolume} symbol(s) start at zero, no daily candle in the warmup)"
                    : ""));


            // ───── Market barometer ─────────────────────────────────────────────────
            // A replay used to run without one: the live calculation walks the full symbol pool on a
            // timer and that timer does not run here. The result was not that barometer conditions
            // were skipped but that they passed against a value that was never calculated, and that
            // every position of every run carried a barometer of zero. Both are fixed here, and a
            // condition that cannot be measured now stops the run instead of quietly deciding it.
            List<string> barometerConditions = BarometerReplay.ActiveConditions();
            if (config.CalculateBarometer)
            {
                barometer = new BarometerReplay(exchange, symbols);
                if (!barometer.HasEnoughSymbols)
                {
                    if (barometerConditions.Count > 0)
                        throw new InvalidOperationException(
                            $"This run replays too few coins for a barometer ({barometer.SymbolCountText}) while " +
                            $"{barometerConditions.Count} barometer condition(s) are set: {string.Join(", ", barometerConditions)}. " +
                            $"A barometer needs at least {BarometerReplay.MinimumSymbols} coins per quote coin.");

                    GlobalData.AddTextToLogTab($"Barometer: too few coins to measure one ({barometer.SymbolCountText}), " +
                        "so it stays empty for this run");
                    barometer = null;
                }
                else
                {
                    GlobalData.AddTextToLogTab($"Barometer: {barometer.SymbolCountText} coin(s), intervals {barometer.IntervalText}" +
                        (barometerConditions.Count > 0 ? $", conditions: {string.Join(", ", barometerConditions)}" : ", no conditions"));
                }
            }
            else if (barometerConditions.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The barometer is switched off for this run while {barometerConditions.Count} barometer condition(s) are set: " +
                    $"{string.Join(", ", barometerConditions)}. Those would be tested against a value that is never calculated, " +
                    "which lets everything through or refuses everything without saying so.");
            }
            else
            {
                GlobalData.AddTextToLogTab("Barometer: switched off for this run");
            }

            if (barometer != null)
                GlobalData.PositionCreated += barometer.PositionCreated;

            // ───── Determine chunks ─────────────────────────────────────────────────
            uint chunkMinutes = ChunkDays > 0 ? (uint)ChunkDays * 24 * 60 : 0;
            bool useChunks = chunkMinutes > 0 && (replayTo.Minutes - replayFrom.Minutes) > chunkMinutes;

            EmulatorClock? emulatorClock = GlobalData.Clock as EmulatorClock;
            int processedBars = 0;
            int lastReportedPercent = -1;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            // Estimate total bars for progress: replay steps × symbols
            int totalBars = (int)((replayTo.Minutes - replayFrom.Minutes) / baseInterval.Duration) * symbols.Count;

            // ───── Chunk loop (or single pass when chunking is off) ──────────────────
            int chunkIndex = 0;
            CandleTime windowFrom = replayFrom;
            while (windowFrom < replayTo)
            {
                if (ct.IsCancellationRequested)
                    break;

                // See ReplayChunk for what From / LastBaseOpen / End mean and why they differ.
                ReplayChunk chunk = ReplayChunk.Resolve(windowFrom, replayTo, chunkMinutes, baseInterval.Duration);
                CandleTime windowTo = chunk.LastBaseOpen;

                // Advance the clock to the end of this chunk BEFORE loading its candles.
                // LoadCandlesInRange clips every read to "OpenTime <= Clock.UtcNow" while a run is
                // active, and after the previous chunk the clock is parked at that chunk's last
                // replayed minute — which would clip the entire new chunk to (almost) nothing and
                // starve the replay from chunk 2 onwards. The replay loop below resets the clock
                // minute-by-minute anyway once it starts.
                //
                // chunk.End, not windowTo: windowTo is the OPEN time of the last base candle, and
                // this clip is applied to the loading below. Parking the clock there silently undid
                // the wider load window (chunk.LoadTo) — the 1m candles of the last base candle were
                // requested but clipped away again, so the newest 1m candle stayed a base interval
                // stale and orders derived from it were stamped into the past.
                if (emulatorClock != null)
                    emulatorClock.UtcNow = chunk.End.ToDateTime();

                // Load the stored candles of EVERY interval for this chunk. The replay hands them to
                // the engine as they close (SymbolReplay.AdvanceTo) instead of rebuilding the higher
                // timeframes from a lower one — the fetch computed them once and wrote them away, so
                // recomputing them here is duplicated work AND a second implementation that can drift
                // from what is stored.
                var replays = new List<SymbolReplay>();
                int chunkBars = 0;
                long loadStart = Stopwatch.GetTimestamp();
                foreach (var symbol in symbols)
                {
                    Dictionary<CryptoIntervalPeriod, CryptoCandleList> perInterval = [];
                    foreach (CryptoInterval interval in GlobalData.IntervalList)
                    {
                        CryptoCandleList list = IndicatorWarmup.LoadReplayCandles(
                            symbol, chunk.LoadFrom(interval.Duration), chunk.LoadTo, interval);
                        perInterval[interval.IntervalPeriod] = list;
                        if (interval.IntervalPeriod == baseInterval.IntervalPeriod)
                            chunkBars += list.Count;
                    }
                    var replay = new SymbolReplay { Symbol = symbol, Candles = perInterval };
                    replay.ResetCursors(windowFrom);
                    replays.Add(replay);
                }
                long loadElapsed = Stopwatch.GetTimestamp() - loadStart;

                if (useChunks)
                {
                    chunkIndex++;
                    GlobalData.AddTextToLogTab(
                        $"Chunk {chunkIndex}: {windowFrom.ToDateTime():yyyy-MM-dd} → {windowTo.ToDateTime():yyyy-MM-dd}, " +
                        $"{chunkBars} bars loaded in {(double)loadElapsed / Stopwatch.Frequency:F1}s");
                }

                // ───── Time-merged replay loop for this chunk ────────────────────────
                for (CandleTime openTime = windowFrom; openTime <= windowTo; openTime += baseInterval.Duration)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    CandleTime closeTime = openTime + baseInterval.Duration;
                    if (emulatorClock != null)
                        emulatorClock.UtcNow = closeTime.ToDateTime();

                    List<(SymbolReplay Replay, CryptoCandle Candle)> ticksThisMinute = [];
                    foreach (var replay in replays)
                    {
                        if (replay.Candles[activeBaseInterval.IntervalPeriod].TryGetValue(openTime, out CryptoCandle candle))
                            ticksThisMinute.Add((replay, candle));
                    }
                    if (ticksThisMinute.Count == 0)
                        continue;

                    long iterStart = Stopwatch.GetTimestamp();

                    // ── Phase A: per-symbol compute (indicators + signal/trade pipeline) ──────
                    if (RunParallel && ticksThisMinute.Count > 1)
                    {
                        await Parallel.ForEachAsync(ticksThisMinute, parallelOptions,
                            async (item, _) => await ProcessComputeAsync(item.Replay, item.Candle, openTime, closeTime));
                    }
                    else
                    {
                        foreach (var item in ticksThisMinute)
                            await ProcessComputeAsync(item.Replay, item.Candle, openTime, closeTime);
                    }

                    // ── Phase B: persist + zones (serial, deterministic order) ────────────────
                    await PersistAndCalculateZonesAsync();

                    // The capital of this replayed day, once per day. Deliberately here in the
                    // serial part: it walks the position list, and in phase A every symbol thread is
                    // busy changing its own position. Costs a comparison of two dates per minute.
                    AssetSnapshotTools.CaptureIfDue(exchange);

                    // The barometer over the minute that has just closed, plus the rows the positions
                    // of this minute queued. Also in the serial part on purpose: it reads the candle
                    // lists of every symbol of the run, which phase A was busy filling.
                    if (barometer != null)
                    {
                        long barometerStart = Stopwatch.GetTimestamp();
                        barometer.Execute(closeTime - 1u);
                        barometer.Flush();
                        elapsedBarometer += Stopwatch.GetTimestamp() - barometerStart;
                    }

                    processedBars += ticksThisMinute.Count;
                    int percent = totalBars > 0 ? Math.Min(100, 100 * processedBars / totalBars) : 0;
                    if (percent != lastReportedPercent)
                    {
                        lastReportedPercent = percent;
                        Progress?.Report(new ReplayProgress(percent));
                    }

                    int decile = Math.Min(percent / 10, 9);
                    if (decile != lastDecile)
                    {
                        long now = Stopwatch.GetTimestamp();
                        if (lastDecile >= 0)
                            decileWallTicks[lastDecile] += now - decileStart;
                        lastDecile = decile;
                        decileStart = now;
                    }

                    elapsedOuterLoop += Stopwatch.GetTimestamp() - iterStart;
                }

                // ───── Between chunks: prune old candles to keep memory bounded ──────
                // Not after a Stop: this exists to let the NEXT chunk start with bounded memory, and
                // after a Stop there is no next chunk. It was the one piece of work between pressing
                // Stop and the run being written away that had no cancellation check - 618.775
                // candles plus their indicator data and pivots on the run it was measured on.
                if (useChunks && windowTo < replayTo && !ct.IsCancellationRequested)
                {
                    // The boundary, per interval. Count back from chunk.End, NOT from windowTo
                    // (= chunk.LastBaseOpen). LastBaseOpen is the OPEN time of the last base candle,
                    // so it sits one base interval before the chunk boundary and therefore moves with
                    // the base interval: 1 minute earlier on a 1m run, 15 on a 15m run. chunk.End is
                    // the boundary itself and is identical for every base interval.
                    //
                    // Aligning the cutoff on the interval below only hides that for intervals COARSER
                    // than the difference. For 1m/2m/3m/5m/10m it does not, so those kept a different
                    // number of candles per base interval — and that is exactly the set whose trend
                    // drifted apart, with the differences piling up in the hours around a chunk
                    // boundary.
                    CandleTime CutoffFor(CryptoInterval interval)
                    {
                        int keepDepth = IndicatorWarmup.WarmupDepth(interval);
                        uint cutoffMinutes = chunk.End.Minutes - (uint)keepDepth * interval.Duration;
                        return new CandleTime(cutoffMinutes - cutoffMinutes % interval.Duration);
                    }

                    // The scanner's own routine, with our deeper boundary handed in. This used to be
                    // a loop here that removed candles and nothing else, which meant a replay never
                    // pruned the indicator data and never trimmed the ZigZag pivots - the two other
                    // things CleanCandleDataAsync does against the same boundary. Measured on run 220:
                    // 8.8 million indicator-data entries and 14 GB in one process, while a live
                    // scanner that does run this sits at 600-900 MB. The pivot lists mattered just as
                    // much for speed: ZoneDlz walks the whole list on every recalculation, and in a
                    // seven-month replay that list only ever grew.
                    int pruned = 0;
                    foreach (var symbol in symbols)
                        pruned += await CandleTools.CleanCandleDataAsync(symbol, null, CutoffFor);
                    GlobalData.AddTextToLogTab($"Chunk {chunkIndex}: pruned {pruned} old candles from memory");
                }

                // Per-chunk delta report — the "where does it slow down" measurement.
                LogChunkTimings(chunkIndex, exchange, symbols);

                // Advance to next chunk
                windowFrom = useChunks ? chunk.NextFrom : replayTo;
            }

            // The last point of the capital line: the balances as the run leaves them. The clock is
            // still on the last replayed minute here - FinishRun puts it back on real time - so this
            // overwrites the snapshot of that day with the end result of the run.
            AssetSnapshotTools.Capture(exchange, GlobalData.Clock.UtcNow);

            Progress?.Report(new ReplayProgress(100));

            if (lastDecile >= 0)
                decileWallTicks[lastDecile] += Stopwatch.GetTimestamp() - decileStart;
        }
        finally
        {
            GlobalData.AnalyzeSignalCreated = null;
            if (barometer != null)
            {
                GlobalData.PositionCreated -= barometer.PositionCreated;
                barometer.Flush(); // the rows of the last minute, and of a run that ended early
                barometer = null;
            }
            LogPhaseTimings();
            PipelineProfiler.Enabled = false;
        }
    }


    /// <summary>
    /// Point-in-time copy of every accumulator the per-chunk report compares. Only the counters that
    /// can plausibly grow with the replay position are captured — the end-of-run report keeps covering
    /// the full breakdown.
    /// </summary>
    private readonly record struct ProfileSnapshot(
        long Process1m, long Pipeline, long ZoneDrain, long Flush,
        long Prepare, long Execute, long Trade, long PositionCheck,
        long SeStrategy, long SeEvaluations, long SeSignals,
        long Trend, long TrendCalls, long FvgInline, long SmcInline,
        long DbFlush, long DbFlushItems, long CandleArrivals,
        long DlzInline, long DlzInlineCalls, long DlzFeed, long DlzJudge, long DlzMerge,
        long DlzBroken, long DlzFullRuns, long DlzIncrementalRuns)
    {
        public static ProfileSnapshot Capture(ReplayRunner runner) => new(
            runner.elapsedProcess1m, runner.elapsedPipeline, runner.elapsedZoneDrain, runner.elapsedFlush,
            PipelineProfiler.PrepareTicks, PipelineProfiler.ExecuteTicks, PipelineProfiler.TradeTicks,
            PipelineProfiler.PositionCheckTicks,
            PipelineProfiler.SeStrategyTicks, PipelineProfiler.SeEvaluations, PipelineProfiler.SeSignals,
            PipelineProfiler.TrendTicks, PipelineProfiler.TrendCalls,
            PipelineProfiler.FvgInlineTicks, PipelineProfiler.SmcInlineTicks,
            PipelineProfiler.DbFlushOpenTicks + PipelineProfiler.DbFlushWriteTicks + PipelineProfiler.DbFlushCommitTicks,
            PipelineProfiler.DbFlushItems, PipelineProfiler.CandleArrivals,
            PipelineProfiler.DlzInlineTicks, PipelineProfiler.DlzInlineCalls,
            PipelineProfiler.DlzFeedTicks, PipelineProfiler.DlzJudgeTicks,
            PipelineProfiler.DlzMergeTicks, PipelineProfiler.DlzBrokenTicks,
            PipelineProfiler.DlzFullRuns, PipelineProfiler.DlzIncrementalRuns);
    }


    /// <summary>
    /// Logs what this chunk cost compared to the previous one. The end-of-run "Timing"/"Decile" lines
    /// say how the total was divided and that the run slows down; this says WHICH phase is responsible,
    /// while the run is still going. Two blocks per chunk:
    /// <list type="bullet">
    ///   <item>the wall time of the chunk plus the increase of every timing bucket since the previous
    ///         chunk (a phase with quadratic behaviour keeps rising chunk after chunk, a constant-cost
    ///         phase stays flat);</item>
    ///   <item>the size of the state that could be causing it — in-memory candles, cached indicator
    ///         data, zones, open positions — so a rising bucket can be tied to the structure that grows
    ///         along with it.</item>
    /// </list>
    /// </summary>
    private void LogChunkTimings(int chunkIndex, CryptoScanner.Core.Model.CryptoExchange exchange, List<CryptoSymbol> symbols)
    {
        static double Seconds(long ticks) => (double)ticks / Stopwatch.Frequency;

        ProfileSnapshot now = ProfileSnapshot.Capture(this);
        ProfileSnapshot prev = lastChunkSnapshot;
        long wallNow = runWall.ElapsedTicks;
        double chunkWall = Seconds(wallNow - lastChunkWallTicks);
        lastChunkSnapshot = now;
        lastChunkWallTicks = wallNow;

        GlobalData.AddTextToLogTab(
            $"Chunk {chunkIndex} timing — wall {chunkWall:F1}s | " +
            $"candles {Seconds(now.Process1m - prev.Process1m):F1}s, " +
            $"pipeline {Seconds(now.Pipeline - prev.Pipeline):F1}s, " +
            $"zones {Seconds(now.ZoneDrain - prev.ZoneDrain):F1}s, " +
            $"flush {Seconds(now.Flush - prev.Flush):F1}s " +
            $"|| indicators {Seconds(now.Prepare - prev.Prepare):F1}s, " +
            $"algorithms {Seconds(now.Execute - prev.Execute):F1}s " +
            $"(strategy {Seconds(now.SeStrategy - prev.SeStrategy):F1}s over {now.SeEvaluations - prev.SeEvaluations} eval, " +
            $"{now.SeSignals - prev.SeSignals} signal), " +
            $"trade {Seconds(now.Trade - prev.Trade):F1}s, " +
            $"positionCheck {Seconds(now.PositionCheck - prev.PositionCheck):F1}s " +
            $"|| trend {Seconds(now.Trend - prev.Trend):F1}s over {now.TrendCalls - prev.TrendCalls} call(s), " +
            $"fvgInline {Seconds(now.FvgInline - prev.FvgInline):F1}s, " +
            $"smcInline {Seconds(now.SmcInline - prev.SmcInline):F1}s, " +
            $"db {Seconds(now.DbFlush - prev.DbFlush):F1}s over {now.DbFlushItems - prev.DbFlushItems} item(s) " +
            $"|| candles processed {now.CandleArrivals - prev.CandleArrivals}");

        // DLZ on its own line, because in the emulator it runs inline in SignalPrepare and therefore
        // disappears into the "indicators" bucket above - which was routinely 99% of the pipeline,
        // leaving DLZ and the indicator hub indistinguishable. The "zones" bucket is no help: it
        // measures the queue drain the emulator never uses and prints 0.0 every chunk.
        //
        // Only printed when there is something to report, so a run without zone intervals does not
        // get a line of zeroes on every chunk.
        long dlzCalls = now.DlzInlineCalls - prev.DlzInlineCalls;
        if (dlzCalls > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Chunk {chunkIndex} dlz — inline {Seconds(now.DlzInline - prev.DlzInline):F1}s " +
                $"over {dlzCalls} recalc " +
                $"({now.DlzFullRuns - prev.DlzFullRuns} full / " +
                $"{now.DlzIncrementalRuns - prev.DlzIncrementalRuns} incr) | " +
                $"feed {Seconds(now.DlzFeed - prev.DlzFeed):F1}s, " +
                $"judge {Seconds(now.DlzJudge - prev.DlzJudge):F1}s, " +
                $"merge {Seconds(now.DlzMerge - prev.DlzMerge):F1}s, " +
                $"broken {Seconds(now.DlzBroken - prev.DlzBroken):F1}s");
        }

        // State that survives the chunk boundary. A bucket above that keeps rising while one of these
        // keeps rising too is the pair to look at: the phase re-walks a structure that never stops
        // growing. Counted per chunk over all replayed symbols, so the cost is negligible.
        long candleCount = 0;
        long dataCount = 0;
        long zonesOpen = 0;
        long zonesClosed = 0;
        long signalCount = 0;
        foreach (var symbol in symbols)
        {
            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                candleCount += symbolInterval.CandleList.Count;
                dataCount += symbolInterval.Data.Count;
                signalCount += symbolInterval.SignalList.Count;
                // SMC keeps a single flat list, DLZ/FVG split theirs into open and closed lists.
                zonesOpen += symbolInterval.Dlz.Zones.LongOpen.Count + symbolInterval.Dlz.Zones.ShortOpen.Count
                    + symbolInterval.Fvg.Zones.LongOpen.Count + symbolInterval.Fvg.Zones.ShortOpen.Count
                    + symbolInterval.Smc.Zones.Count;
                zonesClosed += symbolInterval.Dlz.Zones.LongClosed.Count + symbolInterval.Dlz.Zones.ShortClosed.Count
                    + symbolInterval.Fvg.Zones.LongClosed.Count + symbolInterval.Fvg.Zones.ShortClosed.Count;
            }
        }

        GlobalData.AddTextToLogTab(
            $"Chunk {chunkIndex} state — candles in memory {candleCount}, indicator data {dataCount}, " +
            $"signals {signalCount}, zones open {zonesOpen} / closed {zonesClosed}, " +
            $"positions {exchange.Data.PositionList.Count}, " +
            $"managed memory {GC.GetTotalMemory(false) / (1024 * 1024)} MB, " +
            $"GC gen0/1/2 {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
    }


    /// <summary>
    /// Writes the per-phase profiling totals (candle synthesis / signal+trade pipeline / zone
    /// calculation / DB flush) collected during the run to the log, so a run reveals where its
    /// time actually went before any optimisation is attempted. Raw Stopwatch ticks are converted
    /// to seconds via <see cref="Stopwatch.Frequency"/>.
    /// </summary>
    private void LogPhaseTimings()
    {
        static double Seconds(long ticks) => (double)ticks / Stopwatch.Frequency;

        double process1m = Seconds(elapsedProcess1m);
        double pipeline = Seconds(elapsedPipeline);
        double zoneDrain = Seconds(elapsedZoneDrain);
        double flush = Seconds(elapsedFlush);
        double barometerTime = Seconds(elapsedBarometer);
        double total = process1m + pipeline + zoneDrain + flush + barometerTime;
        if (total <= 0)
            return;

        // Wall-clock of the whole run and the up-front warmup, so the buckets reconcile with the real
        // duration. The outer-loop timer covers each non-empty iteration in full; its overhead-in-loop
        // slice (outerLoop − measured sub-phases) is what lives inside the loop but outside the fine
        // stopwatches (Parallel.ForEachAsync scheduling, NLog writes, etc.). The remainder after
        // subtracting outerLoop from wall−warmup is overhead outside the loop entirely.
        double wall = runWall.Elapsed.TotalSeconds;
        double warmup = Seconds(elapsedWarmup);
        double outerLoop = Seconds(elapsedOuterLoop);
        double overheadInLoop = outerLoop - total;
        double overheadOutsideLoop = wall - warmup - outerLoop;
        double unaccounted = wall - total - warmup;
        GlobalData.AddTextToLogTab(
            $"Wall-clock — run {wall:F1}s | warmup {warmup:F1}s, measured phases {total:F1}s, " +
            $"unaccounted {unaccounted:F1}s ({(wall > 0 ? unaccounted / wall : 0):P0})");
        GlobalData.AddTextToLogTab(
            $"Unaccounted split — outer loop {outerLoop:F1}s total | " +
            $"overhead-in-loop {overheadInLoop:F1}s (Parallel/NLog/etc), " +
            $"overhead-outside-loop {overheadOutsideLoop:F1}s (startup/shutdown/bookkeeping)");

        GlobalData.AddTextToLogTab(
            $"Timing — measured {total:F1}s | " +
            $"candles {process1m:F1}s ({process1m / total:P0}), " +
            $"pipeline {pipeline:F1}s ({pipeline / total:P0}), " +
            $"zones {zoneDrain:F1}s ({zoneDrain / total:P0}), " +
            $"flush {flush:F1}s ({flush / total:P0}), " +
            $"barometer {barometerTime:F1}s ({barometerTime / total:P0})");

        // Sub-breakdown of the "pipeline" phase from the PipelineProfiler (NewCandleArrivedAsync).
        // Percentages are of the pipeline total so they line up with the line above. Only emitted
        // when the profiler actually accumulated something this run.
        double prepare = Seconds(PipelineProfiler.PrepareTicks);
        double execute = Seconds(PipelineProfiler.ExecuteTicks);
        double trade = Seconds(PipelineProfiler.TradeTicks);
        double posCheck = Seconds(PipelineProfiler.PositionCheckTicks);
        double pipelineMeasured = prepare + execute + trade + posCheck;
        if (pipelineMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Pipeline — measured {pipelineMeasured:F1}s over {PipelineProfiler.CandleArrivals} candle(s) | " +
                $"indicators(SignalPrepare) {prepare:F1}s ({prepare / pipelineMeasured:P0}), " +
                $"algorithms(SignalExecute) {execute:F1}s ({execute / pipelineMeasured:P0}), " +
                $"trade+rules+position {trade:F1}s ({trade / pipelineMeasured:P0}), " +
                $"positionCheck {posCheck:F1}s ({posCheck / pipelineMeasured:P0})");
        }

        // Sub-breakdown of the indicator (SignalPrepare) bucket. Two parts now that the batch path
        // is gone: building the history window for a warm-up, and the per-candle incremental work.
        // The incremental part is split further in the "Hub incremental" line below.
        double collect = Seconds(PipelineProfiler.PrepCollectTicks);
        double incremental = Seconds(PipelineProfiler.HubAddTicks + PipelineProfiler.HubBuildTicks
            + PipelineProfiler.HubDataInsertTicks + PipelineProfiler.HubApplyLuxTicks);
        double prepMeasured = collect + incremental;
        if (prepMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Indicators — measured {prepMeasured:F1}s | " +
                $"collectCandles(warmup) {collect:F1}s ({collect / prepMeasured:P0}), " +
                $"hubIncremental {incremental:F1}s ({incremental / prepMeasured:P0})");
        }

        // The warm-up branch as a whole, which the line above only covered the first step of. The
        // ratio between warmups and incremental calls is the number to read first: a hub that is
        // rebuilt on every candle pays a full history rebuild per candle, and the reason column says
        // whether that is a startup effect or something structural.
        double warmupTotal = Seconds(PipelineProfiler.PrepWarmupTicks);
        double hubFeed = Seconds(PipelineProfiler.PrepHubFeedTicks);
        double bandRange = Seconds(PipelineProfiler.PrepBandRangeTicks);
        long prepCalls = PipelineProfiler.PrepCalls;
        long warmupCalls = PipelineProfiler.PrepWarmupCalls;
        if (prepCalls > 0)
        {
            double perWarmup = warmupCalls > 0 ? 1000.0 * warmupTotal / warmupCalls : 0;
            double candlesPerWarmup = warmupCalls > 0 ? (double)PipelineProfiler.PrepHubFeedCandles / warmupCalls : 0;
            GlobalData.AddTextToLogTab(
                $"PrepareIndicators — {prepCalls} call(s) | " +
                $"alreadyPresent {PipelineProfiler.PrepAlreadyPresent}, " +
                $"warmup {warmupCalls}, incremental {PipelineProfiler.HubIncrementalCalls}, " +
                $"notEnoughHistory {PipelineProfiler.PrepNotEnoughHistory}");
            GlobalData.AddTextToLogTab(
                $"Warmup — {warmupTotal:F1}s over {warmupCalls} call(s) ({perWarmup:F1} ms each, " +
                $"{candlesPerWarmup:F0} candles each) | " +
                $"collect {collect:F1}s, hubFeed {hubFeed:F1}s, bandRange {bandRange:F1}s");
            GlobalData.AddTextToLogTab(
                $"Warmup reason — hubNull {PipelineProfiler.PrepWarmupHubNull}, " +
                $"gap {PipelineProfiler.PrepWarmupGap}, " +
                $"explicitWindow {PipelineProfiler.PrepWarmupExplicit}, " +
                $"configChanged {PipelineProfiler.PrepWarmupConfig}");
        }

        // Sub-breakdown of the algorithms (SignalExecute) bucket: normal-strategy evaluation vs.
        // zone-touch (FVG/DLZ/SMC) vs. the rest (barometer + loop overhead, derived from the total).
        // The eval/signal counters reveal whether the cost scales with evaluations or with signals.
        double seStrategy = Seconds(PipelineProfiler.SeStrategyTicks);
        double seZoneTouch = Seconds(PipelineProfiler.SeZoneTouchTicks);
        double seOther = execute - seStrategy - seZoneTouch;
        if (seOther < 0)
            seOther = 0;
        if (execute > 0)
        {
            GlobalData.AddTextToLogTab(
                $"SignalExecute — {execute:F1}s over {PipelineProfiler.SeEvaluations} eval(s), {PipelineProfiler.SeSignals} signal(s) | " +
                $"strategy {seStrategy:F1}s ({seStrategy / execute:P0}), " +
                $"zoneTouch {seZoneTouch:F1}s ({seZoneTouch / execute:P0}), " +
                $"other(barometer+loop) {seOther:F1}s ({seOther / execute:P0})");
        }

        // Carve-outs — these OVERLAP the buckets above (not additive to the total). They isolate
        // pieces that are otherwise hidden: trend (inside the strategy bucket) and the inline FVG/SMC
        // scans (inside the indicators bucket).
        double trend = Seconds(PipelineProfiler.TrendTicks);
        double fvgInline = Seconds(PipelineProfiler.FvgInlineTicks);
        double smcInline = Seconds(PipelineProfiler.SmcInlineTicks);
        if (trend + fvgInline + smcInline > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Carve-outs (overlap above) — " +
                $"trend {trend:F1}s over {PipelineProfiler.TrendCalls} call(s), " +
                $"fvgInline {fvgInline:F1}s, " +
                $"smcInline {smcInline:F1}s");
        }

        // Sub-breakdown of the trend carve-out: where inside CalculateMarketTrendAsync the time goes —
        // the per-symbol lock wait, CalculateBothAsync (one call per stale interval), and inside that,
        // the candle-ingest loop (+ how many candles it actually processed, confirming the emulator
        // window clamp) versus the Dow/BOS interpretation passes.
        double trendLockWait = Seconds(PipelineProfiler.TrendLockWaitTicks);
        double trendCalcBoth = Seconds(PipelineProfiler.TrendCalcBothTicks);
        double trendIngest = Seconds(PipelineProfiler.TrendIngestTicks);
        double trendDow = Seconds(PipelineProfiler.TrendDowTicks);
        double trendBos = Seconds(PipelineProfiler.TrendBosTicks);
        if (trendCalcBoth > 0)
        {
            long ingestCandles = PipelineProfiler.TrendIngestCandles;
            double avgCandlesPerCall = PipelineProfiler.TrendCalcBothCalls > 0
                ? (double)ingestCandles / PipelineProfiler.TrendCalcBothCalls : 0;
            GlobalData.AddTextToLogTab(
                $"Trend internals — {trend:F1}s over {PipelineProfiler.TrendCalls} call(s) | " +
                $"lockWait {trendLockWait:F1}s ({trendLockWait / trend:P0}), " +
                $"calculateBoth {trendCalcBoth:F1}s ({trendCalcBoth / trend:P0}) over {PipelineProfiler.TrendCalcBothCalls} call(s) | " +
                $"ingest {trendIngest:F1}s ({trendIngest / trendCalcBoth:P0}) over {ingestCandles} candle(s) ({avgCandlesPerCall:F0}/call), " +
                $"dow {trendDow:F1}s ({trendDow / trendCalcBoth:P0}), " +
                $"bos {trendBos:F1}s ({trendBos / trendCalcBoth:P0})");
        }

        // Sub-breakdown of the DLZ judge phase (CalculateDlzAsync): whether the cost is the NUMBER of
        // pivots that get a verdict or what one verdict costs. Walked counts every pivot the sliding
        // window steps over, skipped the ones whose verdict is settled and already committed, judged
        // the ones that actually went through the zoom.
        double dlzZoom = Seconds(PipelineProfiler.DlzZoomTicks);
        double dlzGrade = Seconds(PipelineProfiler.DlzGradeTicks);
        double dlzJudgeTotal = Seconds(PipelineProfiler.DlzJudgeTicks);
        if (PipelineProfiler.DlzPivotsWalked > 0)
        {
            long judgedPivots = PipelineProfiler.DlzPivotsJudged;
            double perJudged = judgedPivots > 0 ? 1000.0 * dlzZoom / judgedPivots : 0;
            double stepsPerZoom = judgedPivots > 0 ? (double)PipelineProfiler.DlzZoomSteps / judgedPivots : 0;
            double walkedPerCall = PipelineProfiler.DlzInlineCalls > 0
                ? (double)PipelineProfiler.DlzPivotsWalked / PipelineProfiler.DlzInlineCalls : 0;
            GlobalData.AddTextToLogTab(
                $"Dlz judge — {dlzJudgeTotal:F1}s | pivots walked {PipelineProfiler.DlzPivotsWalked} " +
                $"({walkedPerCall:F0}/recalc), skipped {PipelineProfiler.DlzPivotsSkipped}, judged {judgedPivots} | " +
                $"zoom {dlzZoom:F1}s ({perJudged:F1} ms each, {stepsPerZoom:F1} interval(s) down), " +
                $"gradeIntro {dlzGrade:F1}s, " +
                $"other(walk+bookkeeping) {dlzJudgeTotal - dlzZoom - dlzGrade:F1}s");
        }

        // Where ApplyLux got its number. A "computed" is a full 260-candle recalculation of the Lux
        // over the 5m candles; the other two are lookups. On a base interval finer than 5m the vast
        // majority should be cache hits - if it is not, the cache key is wrong.
        long luxTotal = PipelineProfiler.LuxFromCache + PipelineProfiler.LuxFrom5mData + PipelineProfiler.LuxComputed;
        if (luxTotal > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Lux — {luxTotal} call(s) | from cache {PipelineProfiler.LuxFromCache} " +
                $"({(double)PipelineProfiler.LuxFromCache / luxTotal:P0}), from 5m data " +
                $"{PipelineProfiler.LuxFrom5mData}, recalculated {PipelineProfiler.LuxComputed}");
        }

        // Candles the zone walks expected and did not find. Until 24-08-2026 a missing candle fell
        // through the if and read as "nothing happened", so a run could produce a different zone set
        // without a single line saying why. Interrupted is the subset longer than
        // ZoneCandleGaps.ToleratedGap - those are the ones that can change which zones survive, and
        // a run with any of them has not measured the strategy but the holes in its candle history.
        if (PipelineProfiler.ZoneGapWalks > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Zone candle gaps — {PipelineProfiler.ZoneGapWalks} walk(s) found " +
                $"{PipelineProfiler.ZoneGapCandles} missing candle(s), longest run " +
                $"{PipelineProfiler.ZoneGapWorst} | interruptions (> {ZoneCandleGaps.ToleratedGap} " +
                $"in a row) {PipelineProfiler.ZoneGapInterrupted} | read back in " +
                $"{PipelineProfiler.ZoneGapRefetches} time(s) over " +
                $"{PipelineProfiler.ZoneGapRefetchCandles} candle(s)");
        }

        // Every read of candles out of candles.db. Rows per call is the number the bounded read was
        // built for: it used to be the complete series for the interval whatever window was asked.
        double candleRead = Seconds(PipelineProfiler.CandleReadTicks);
        if (PipelineProfiler.CandleReadCalls > 0)
        {
            double rowsPerCall = (double)PipelineProfiler.CandleReadRows / PipelineProfiler.CandleReadCalls;
            GlobalData.AddTextToLogTab(
                $"Candle reads (candles.db) — {candleRead:F1}s over {PipelineProfiler.CandleReadCalls} call(s), " +
                $"{PipelineProfiler.CandleReadRows} row(s) ({rowsPerCall:F0}/call)");
        }

        // Sub-breakdown of the positionCheck bucket: where inside CalculatePositionResultsViaOrders
        // the time goes — the DB load of orders/trades, the per-order processing loop, the
        // profit/break-even recalculation, or the final persist transaction.
        double posLoad = Seconds(PipelineProfiler.PosLoadOrdersTicks);
        double posLoop = Seconds(PipelineProfiler.PosOrderLoopTicks);
        double posCalc = Seconds(PipelineProfiler.PosCalcProfitTicks);
        double posPersist = Seconds(PipelineProfiler.PosPersistTicks);
        double posMeasured = posLoad + posLoop + posCalc + posPersist;
        if (posMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"PositionResults — measured {posMeasured:F1}s over {PipelineProfiler.PosCalls} call(s) | " +
                $"loadOrders {posLoad:F1}s ({posLoad / posMeasured:P0}), " +
                $"orderLoop {posLoop:F1}s ({posLoop / posMeasured:P0}), " +
                $"calcProfit {posCalc:F1}s ({posCalc / posMeasured:P0}), " +
                $"persist {posPersist:F1}s ({posPersist / posMeasured:P0})");
        }

        // Sub-breakdown of the positionCheck bucket's OTHER path: PositionMonitor.CheckThePosition,
        // run via ThreadCheckFinishedPosition.ProcessPosition -> PositionOpenAsUsual on every candle
        // that has an open position (not gated behind ForceCheckPosition like the PositionResults
        // breakdown above). Cancel = stale/timeout/reposition order cleanup, dca = the fixed-percentage
        // DCA check, handle = placing/modifying the live buy/sell orders (+ optional LockProfits).
        double checkCancel = Seconds(PipelineProfiler.CheckPosCancelTicks);
        double checkDca = Seconds(PipelineProfiler.CheckPosDcaTicks);
        double checkHandle = Seconds(PipelineProfiler.CheckPosHandleTicks);
        double checkMeasured = checkCancel + checkDca + checkHandle;
        if (checkMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"CheckThePosition — measured {checkMeasured:F1}s over {PipelineProfiler.CheckPosCalls} call(s) | " +
                $"cancel {checkCancel:F1}s ({checkCancel / checkMeasured:P0}), " +
                $"dca {checkDca:F1}s ({checkDca / checkMeasured:P0}), " +
                $"handle {checkHandle:F1}s ({checkHandle / checkMeasured:P0})");
        }

        // Cross-check of the positionCheck bucket itself: a dedicated wrap of the exact same statement
        // (PositionMonitor.NewCandleArrivedAsync's AddToQueue call) that positionCheck above derives via
        // subtraction. The two totals should match closely; a gap here would mean the diff-based
        // timestamps are off. cleanCandle is the method's tail (CleanCandleDataAsync), gated behind
        // !IsEmulatorMode so it stays ~0s in emulator runs.
        double addToQueue = Seconds(PipelineProfiler.PcAddToQueueTicks);
        double cleanCandle = Seconds(PipelineProfiler.PcCleanCandleTicks);
        if (addToQueue > 0 || cleanCandle > 0)
        {
            GlobalData.AddTextToLogTab(
                $"PositionCheck cross-check — addToQueue {addToQueue:F1}s (vs. positionCheck bucket {posCheck:F1}s), " +
                $"cleanCandle {cleanCandle:F1}s (live scanner only)");
        }

        // Sub-breakdown of ThreadCheckFinishedPosition.ProcessPosition — the body AddToQueue runs
        // synchronously in emulator mode, i.e. what addToQueue/positionCheck above actually measures.
        // forceCheck and PositionResults should track each other (same call site); ready was never
        // instrumented before, so this is what should explain the gap between positionCheck and
        // PositionResults + CheckThePosition.
        double ppTotal = Seconds(PipelineProfiler.PpTotalTicks);
        double ppForceCheck = Seconds(PipelineProfiler.PpForceCheckTicks);
        double ppStatusNew = Seconds(PipelineProfiler.PpStatusNewTicks);
        double ppReady = Seconds(PipelineProfiler.PpReadyTicks);
        double ppOpenAsUsual = Seconds(PipelineProfiler.PpOpenAsUsualTicks);
        if (ppTotal > 0)
        {
            GlobalData.AddTextToLogTab(
                $"ProcessPosition — measured {ppTotal:F1}s over {PipelineProfiler.PpCalls} call(s) | " +
                $"forceCheck {ppForceCheck:F1}s ({ppForceCheck / ppTotal:P0}), " +
                $"statusNew {ppStatusNew:F1}s ({ppStatusNew / ppTotal:P0}), " +
                $"ready {ppReady:F1}s ({ppReady / ppTotal:P0}) over {PipelineProfiler.PpReadyCalls} call(s), " +
                $"openAsUsual {ppOpenAsUsual:F1}s ({ppOpenAsUsual / ppTotal:P0}) over {PipelineProfiler.PpOpenAsUsualCalls} call(s)");
        }

        // Real database activity, consolidated from every instrumented call site regardless of which
        // pipeline bucket it is nested in: the per-tick Flush() (signals/positions/zones), the order+
        // trade load on ForceCheckPosition, and the final position-result persist transaction.
        double dbOpen = Seconds(PipelineProfiler.DbFlushOpenTicks);
        double dbWrite = Seconds(PipelineProfiler.DbFlushWriteTicks);
        double dbCommit = Seconds(PipelineProfiler.DbFlushCommitTicks);
        double dbFlushMeasured = dbOpen + dbWrite + dbCommit;
        if (dbFlushMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Flush (DB) — {dbFlushMeasured:F1}s over {PipelineProfiler.DbFlushCalls} flush(es), {PipelineProfiler.DbFlushItems} item(s) | " +
                $"open {dbOpen:F1}s ({dbOpen / dbFlushMeasured:P0}), " +
                $"write {dbWrite:F1}s ({dbWrite / dbFlushMeasured:P0}), " +
                $"commit {dbCommit:F1}s ({dbCommit / dbFlushMeasured:P0})");
        }

        double dbTotal = dbFlushMeasured + posLoad + posPersist;
        if (dbTotal > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Database total — {dbTotal:F1}s across the run | " +
                $"flush {dbFlushMeasured:F1}s, loadOrders {posLoad:F1}s, persist {posPersist:F1}s");
        }

        // Hub incremental sub-breakdown (the part of SignalPrepare that runs every candle in hub mode)
        double hubAdd = Seconds(PipelineProfiler.HubAddTicks);
        double hubBuild = Seconds(PipelineProfiler.HubBuildTicks);
        double hubDataInsert = Seconds(PipelineProfiler.HubDataInsertTicks);
        double hubApplyLux = Seconds(PipelineProfiler.HubApplyLuxTicks);
        double hubTotal = hubAdd + hubBuild + hubDataInsert + hubApplyLux;
        if (hubTotal > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Hub incremental — {hubTotal:F1}s over {PipelineProfiler.HubIncrementalCalls} call(s) | " +
                $"hubAdd {hubAdd:F1}s ({hubAdd / hubTotal:P0}), " +
                $"buildCurrent {hubBuild:F1}s ({hubBuild / hubTotal:P0}), " +
                $"dataInsert {hubDataInsert:F1}s ({hubDataInsert / hubTotal:P0}), " +
                $"applyLux {hubApplyLux:F1}s ({hubApplyLux / hubTotal:P0})");
        }

        // Trigger-price skip diagnostic
        GlobalData.AddTextToLogTab(
            $"TriggerSkip — hasPosition {PipelineProfiler.SkipHasPosition}, " +
            $"triggersNull {PipelineProfiler.SkipTriggersNull}, " +
            $"forceCheck {PipelineProfiler.SkipForceCheck}, " +
            $"priceOutside {PipelineProfiler.SkipPriceOutside}, " +
            $"skipped {PipelineProfiler.SkipSuccess}");

        // Per-decile wall-clock breakdown — shows where the run slows down
        var decileParts = new string[10];
        for (int i = 0; i < 10; i++)
            decileParts[i] = $"{i * 10}-{i * 10 + 9}%:{Seconds(decileWallTicks[i]):F1}s";
        GlobalData.AddTextToLogTab($"Decile wall-clock — {string.Join(", ", decileParts)}");
    }


    /// <summary>
    /// Phase A — per-symbol compute (parallel-safe). Hands the engine the candles that have closed
    /// at this point in the replay, then runs the live scanner analysis pipeline (signals,
    /// paper-trade, position eval).
    ///
    /// Two rhythms. The ANALYSIS always runs once per base candle — that is what makes a coarser
    /// base interval faster. The ORDER handling drops to minute resolution as soon as this base
    /// candle can move an open position, because a fill has to happen on the minute it is really
    /// reached: only then do the follow-up orders (take profit, DCA, stop loss) exist from that
    /// moment on. Handling it on the coarse candle stamps the fill at the END of that candle, which
    /// is what made a 5m run miss a take profit a 1m run did take.
    /// </summary>
    private async Task ProcessComputeAsync(SymbolReplay replay, CryptoCandle candle, CandleTime openTime, CandleTime closeTime)
    {
        long t0 = Stopwatch.GetTimestamp();
        CryptoSymbol symbol = replay.Symbol;

        // Can this base candle move an open position? Asked BEFORE handing over new candles, so the
        // trigger prices are those the position was left with.
        var exchange = GlobalData.ActiveExchange!;
        bool descend = activeBaseInterval.Duration > 1
            && exchange.Data.PositionList.TryGetValue(symbol.Name, out CryptoPosition? position)
            && PositionMonitor.CandleCanMovePosition(position!, candle, closeTime);

        bool ordersHandledPerMinute = false;
        if (descend)
        {
            CryptoSymbolInterval oneMinute = symbol.GetSymbolInterval(oneMinuteInterval.IntervalPeriod);
            for (CandleTime minute = openTime; minute < closeTime; minute += 1)
            {
                // Hand over everything that closes at this minute, so the position reacts to the
                // same picture it would see in a 1m run.
                replay.AdvanceTo(minute + 1);
                if (!oneMinute.CandleList.TryGetValue(minute, out CryptoCandle minuteCandle))
                    continue;

                symbol.LastPrice = minuteCandle.Close;

                // Put the clock on this minute's close instead of leaving it at the end of the base
                // candle. PaperTradingCheckOrders walks the 1m candles up to Clock.UtcNow, so a clock
                // that already stands at the end of the base candle makes it fill orders on minutes
                // this iteration has not reached yet — a look-ahead inside the base candle that also
                // put every log line and every clock-derived timeout up to a base interval too late.
                using (EmulatorClock.Scoped((minute + 1).ToDateTime()))
                {
                    using PositionMonitor minuteMonitor = new(symbol, minuteCandle, 1);
                    await minuteMonitor.ProcessOrdersAsync();
                }
            }
            ordersHandledPerMinute = true;
        }

        // Catch up to the end of the base candle (a no-op for the intervals the loop above already
        // advanced) and let the base candle dictate the last price, exactly as the live 1m handler
        // does with its own candle.
        replay.AdvanceTo(closeTime);
        symbol.LastPrice = candle.Close;

        long t1 = Stopwatch.GetTimestamp();
        Interlocked.Add(ref elapsedProcess1m, t1 - t0);

        // Drive the exact same pipeline as the live ThreadMonitorCandle.Execute() loop:
        // SignalPrepare → SignalExecute → PaperTrading → TradingRules → CreateOrExtendPosition.
        using PositionMonitor positionMonitor = new(symbol, candle, activeBaseInterval.Duration)
        {
            OrdersAlreadyProcessed = ordersHandledPerMinute,
        };
        await positionMonitor.NewCandleArrivedAsync();
        Interlocked.Add(ref elapsedPipeline, Stopwatch.GetTimestamp() - t1);
    }


    /// <summary>
    /// Phase B — runs ONCE per minute, serially, after all symbols of the minute are computed.
    /// Persists everything the compute phase queued (created signals/positions plus the inline FVG
    /// (ScanForNew) and SMC (Detect) zone diffs), then drains the DLZ zone-calculation queue, then
    /// flushes again. Order matters: ZoneDlz.LoadZonesForSymbol (first thing in CalculateZones) resets
    /// all in-memory zones and reloads them from the DB, so the queued diffs must be flushed BEFORE
    /// the drain, and the drain's own diffs flushed after. Serial + deterministic order, so the
    /// persisted result does not depend on the (parallel) compute order.
    /// </summary>
    private async Task PersistAndCalculateZonesAsync()
    {
        long t0 = Stopwatch.GetTimestamp();
        GlobalData.ThreadSaveObjects?.Flush();
        long t1 = Stopwatch.GetTimestamp();
        elapsedFlush += t1 - t0;

        if (GlobalData.ThreadZoneCalculate != null)
            await GlobalData.ThreadZoneCalculate.DrainQueueAsync();
        long t2 = Stopwatch.GetTimestamp();
        elapsedZoneDrain += t2 - t1;

        GlobalData.ThreadSaveObjects?.Flush();
        elapsedFlush += Stopwatch.GetTimestamp() - t2;
    }


    /// <summary>
    /// Wipes all transient in-memory state so a run starts from a guaranteed clean slate.
    /// Without this, leftover positions, signals, trends, zones, indicator hubs, barometer
    /// values and paper-trading assets from a previous run leak into the next one, producing
    /// non-reproducible results.
    /// </summary>
    private static void ResetRunState(CryptoScanner.Core.Model.CryptoExchange exchange, List<CryptoSymbol> symbols)
    {
        // ── Exchange-level state ────────────────────────────────────────────────

        // Positions — a leftover position blocks signal generation for that symbol
        exchange.Data.PositionList.Clear();

        // Paper-trading asset balances
        exchange.Data.AssetList.Clear();

        // Which day the capital line last recorded (the previous run, or an earlier period)
        AssetSnapshotTools.Reset();

        // Pause rule (price-drop circuit breaker)
        exchange.Data.PauseTrading.Clear();

        // Zone-check timestamp
        exchange.LastZoneCheckTime = null;

        // Per-quote transient state: barometer values and pause-barometer timers
        foreach (var quoteData in exchange.Data.QuoteDataList.Values)
        {
            foreach (var barometer in quoteData.BarometerDataList.Values)
                barometer.Clear();
            foreach (var pauseBarometer in quoteData.PauseBarometerList.Values)
            {
                pauseBarometer.Calculated = null;
                pauseBarometer.Until = null;
                pauseBarometer.Text = null;
            }
        }

        // ── Global counters ─────────────────────────────────────────────────────

        GlobalData.CreatedSignalCount = 0;
        CryptoScanner.Core.Signal.SignalExecute.ResetAnalyseCount();
        GlobalData.LiveDataQueue.Clear();
        GlobalData.LiveDataQueueAdded.Clear();

        // ── Per-symbol state ────────────────────────────────────────────────────

        foreach (var symbol in symbols)
        {
            symbol.LastPrice = null;
            symbol.LastTradeDate = null;
            symbol.LastLossDate = null;
            symbol.LastTradeFetched = null;
            symbol.LastTradeIdFetched = null;

            // Per-interval transient state
            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                // Signals
                symbolInterval.SignalList.Clear();

                // Indicator hub (Skender incremental state + Lux + plugin extensions)
                symbolInterval.IndicatorHub = null;
                symbolInterval.IndicatorHubLastAdded = null;
                symbolInterval.IndicatorHubAddCount = 0;
                symbolInterval.BandRange = null;

                // Indicator data cache (CryptoData per candle)
                symbolInterval.Data.Clear();

                // Candle sync cursor
                symbolInterval.LastCandleSynchronized = null;

                // DLZ swing tracking
                symbolInterval.Dlz.Reset();

                // SMC parameter cache (forces full rescan)
                symbolInterval.Smc.ResetParameterCache();
            }

            // Trend data (Dow + BOS, symbol-level and per-interval, including ZigZag caches)
            symbol.Data.ResetTrendDataAndCaches();

            // Zone data (DLZ, FVG, SMC) and their incremental cursors
            symbol.Data.ResetDlzData();
            symbol.Data.ResetFvgData();
            symbol.Data.ResetSmcData();
            symbol.Data.ResetZoneCalculationCursors();
            symbol.Data.ZonesLoaded = false;
            symbol.Data.ZonesLoadedRunId = null;
        }
    }


    /// <summary>
    /// Verifies that transient state is actually clean after <see cref="ResetRunState"/>.
    /// Throws if anything was missed, so a forgotten reset produces a loud crash instead of
    /// silently corrupted results. This is the safety net that prevents a repeat of the
    /// original non-determinism bug.
    /// </summary>
    private static void AssertCleanState(CryptoScanner.Core.Model.CryptoExchange exchange, List<CryptoSymbol> symbols)
    {
        if (!exchange.Data.PositionList.IsEmpty)
            throw new InvalidOperationException(
                $"PositionList not empty at run start: {exchange.Data.PositionList.Count} leftover position(s).");

        if (exchange.Data.AssetList.Count > 0)
            throw new InvalidOperationException(
                $"AssetList not empty at run start: {exchange.Data.AssetList.Count} leftover asset(s).");

        foreach (var symbol in symbols)
        {
            if (symbol.LastPrice != null)
                throw new InvalidOperationException(
                    $"{symbol.Name}: LastPrice not null at run start.");

            if (symbol.LastTradeDate != null)
                throw new InvalidOperationException(
                    $"{symbol.Name}: LastTradeDate not null at run start.");

            if (symbol.LastLossDate != null)
                throw new InvalidOperationException(
                    $"{symbol.Name}: LastLossDate not null at run start.");

            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                if (symbolInterval.SignalList.Count > 0)
                    throw new InvalidOperationException(
                        $"{symbol.Name} {symbolInterval.Interval!.Name}: " +
                        $"{symbolInterval.SignalList.Count} leftover signal(s).");

                if (symbolInterval.IndicatorHub != null)
                    throw new InvalidOperationException(
                        $"{symbol.Name} {symbolInterval.Interval!.Name}: " +
                        "IndicatorHub not null — stale incremental indicator state.");

                if (symbolInterval.Data.Count > 0)
                    throw new InvalidOperationException(
                        $"{symbol.Name} {symbolInterval.Interval!.Name}: " +
                        $"{symbolInterval.Data.Count} leftover indicator data entries.");

                // The trigger range is checked alongside the swings: it is a second pair of fields
                // that survives a chunk boundary just as easily, and a stale one decides whether the
                // next chunk queues a recalculation at all.
                if (symbolInterval.Dlz.LastSwingHigh != null || symbolInterval.Dlz.LastSwingLow != null
                    || symbolInterval.Dlz.TriggerRangeHigh != null || symbolInterval.Dlz.TriggerRangeLow != null)
                    throw new InvalidOperationException(
                        $"{symbol.Name} {symbolInterval.Interval!.Name}: " +
                        "DlzAdmin swing points not null.");
            }
        }
    }

}
