using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Diagnostics;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Progress payload emitted by <see cref="TickRunner"/> after each replayed candle.
/// </summary>
public readonly record struct TickRunProgress(int Percent);


/// <summary>
/// Drives the emulator replay. Multi-symbol time-merged feed: the loop walks the replay window
/// minute by minute and, for every symbol that has a 1m candle at that minute, processes its tick
/// before the clock advances. This matches what a real exchange does — symbols don't run on
/// independent timelines — and is what lets cross-symbol strategies (barometer, trend filters that
/// read other symbols) behave the same way in the emulator as they do live.
///
/// The per-symbol replay candles are set aside as a <see cref="CryptoCandleList"/> keyed by
/// OpenTime (built by <see cref="IndicatorWarmup.PrepareSymbolAsync"/>); each minute the loop simply
/// looks the candle up by candle-time. Higher intervals are NOT delivered by anything else —
/// <see cref="SignalPrepare.Execute"/> only computes indicators and expects the higher-TF candle
/// to already be in <c>CandleList</c>. The emulator has no live KLine subscription, so
/// <see cref="ProcessTickAsync"/> hands each 1m candle to <see cref="CandleTools.Process1mCandleAsync"/>
/// — the exact live 1m handler — which adds the 1m candle and synthesises the higher timeframes
/// from it. Without this the higher CandleLists stay empty and the signal pipeline produces nothing.
/// </summary>
public sealed class TickRunner
{
    public IProgress<TickRunProgress>? Progress { get; init; }

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


    private static void ReceivedCreatedSignals(CryptoSignal signal)
    {
        //GlobalData.CreatedSignalCount++;
        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);
    }

    public async Task RunAsync(EmulatorRunConfig config, CancellationToken ct)
    {
        var exchange = GlobalData.ActiveExchange!;
        GlobalData.Settings.Signal.UseIndicatorHub = true;
        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;

        // Enable the per-candle pipeline profiler for this run (off in the live scanner). It breaks
        // NewCandleArrivedAsync down into indicators / algorithms / trade handling / position check,
        // so the LogPhaseTimings summary can show where the dominant "pipeline" time actually goes.
        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = true;
        runWall.Restart();
        try
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
                throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName.");

            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, 1);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, 1);

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
                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    throw new InvalidOperationException($"Symbol '{symbolName}' not found on exchange '{config.ExchangeName}'.");
                symbols.Add(symbol);
            }


            // ───── Warmup all symbols up-front ──────────────────────────────────────
            long warmupStart = Stopwatch.GetTimestamp();
            foreach (var symbol in symbols)
            {
                ct.ThrowIfCancellationRequested();
                IndicatorWarmup.WarmupSymbol(symbol, replayFrom);

                symbol.LastPrice = null;
                symbol.LastTradeDate = null;
                symbol.LastTradeFetched = null;
                symbol.LastTradeIdFetched = null;
            }
            elapsedWarmup = Stopwatch.GetTimestamp() - warmupStart;
            GlobalData.AddTextToLogTab($"Warmup ({symbols.Count} symbol(s)): " +
                $"{(double)elapsedWarmup / Stopwatch.Frequency:F1}s");


            // ───── Determine chunks ─────────────────────────────────────────────────
            uint chunkMinutes = ChunkDays > 0 ? (uint)ChunkDays * 24 * 60 : 0;
            bool useChunks = chunkMinutes > 0 && (replayTo.Minutes - replayFrom.Minutes) > chunkMinutes;

            EmulatorClock? emulatorClock = GlobalData.Clock as EmulatorClock;
            int processedBars = 0;
            int lastReportedPercent = -1;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            // Estimate total bars for progress: replay minutes × symbols (refined per chunk)
            int totalBars = (int)((replayTo.Minutes - replayFrom.Minutes) / interval1m.Duration) * symbols.Count;

            // ───── Chunk loop (or single pass when chunking is off) ──────────────────
            int chunkIndex = 0;
            CandleTime windowFrom = replayFrom;
            while (windowFrom < replayTo)
            {
                if (ct.IsCancellationRequested)
                    break;

                CandleTime windowTo = useChunks
                    ? new CandleTime(Math.Min(windowFrom.Minutes + chunkMinutes, replayTo.Minutes))
                    : replayTo;

                // Advance the clock to the end of this chunk BEFORE loading its candles.
                // LoadCandlesInRange clips every read to "OpenTime <= Clock.UtcNow" while a run is
                // active, and after the previous chunk the clock is parked at that chunk's last
                // replayed minute — which would clip the entire new chunk to (almost) nothing and
                // starve the replay from chunk 2 onwards. The replay loop below resets the clock
                // minute-by-minute anyway once it starts.
                if (emulatorClock != null)
                    emulatorClock.UtcNow = windowTo.ToDateTime();

                // Load replay candles for this chunk
                var replays = new List<(CryptoSymbol Symbol, CryptoCandleList Replay)>();
                int chunkBars = 0;
                long loadStart = Stopwatch.GetTimestamp();
                foreach (var symbol in symbols)
                {
                    CryptoCandleList replayCandles = IndicatorWarmup.LoadReplayCandles(symbol, windowFrom, windowTo);
                    replays.Add((symbol, replayCandles));
                    chunkBars += replayCandles.Count;
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
                for (CandleTime openTime = windowFrom; openTime <= windowTo; openTime += interval1m.Duration)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    CandleTime closeTime = openTime + interval1m.Duration;
                    if (emulatorClock != null)
                        emulatorClock.UtcNow = closeTime.ToDateTime();

                    List<(CryptoSymbol Symbol, CryptoCandle Candle)> ticksThisMinute = [];
                    foreach (var (symbol, replay) in replays)
                    {
                        if (replay.TryGetValue(openTime, out CryptoCandle candle))
                            ticksThisMinute.Add((symbol, candle));
                    }
                    if (ticksThisMinute.Count == 0)
                        continue;

                    long iterStart = Stopwatch.GetTimestamp();

                    // ── Phase A: per-symbol compute (indicators + signal/trade pipeline) ──────
                    if (RunParallel && ticksThisMinute.Count > 1)
                    {
                        await Parallel.ForEachAsync(ticksThisMinute, parallelOptions,
                            async (item, _) => await ProcessComputeAsync(item.Symbol, item.Candle));
                    }
                    else
                    {
                        foreach (var item in ticksThisMinute)
                            await ProcessComputeAsync(item.Symbol, item.Candle);
                    }

                    // ── Phase B: persist + zones (serial, deterministic order) ────────────────
                    await PersistAndCalculateZonesAsync();

                    processedBars += ticksThisMinute.Count;
                    int percent = totalBars > 0 ? Math.Min(100, 100 * processedBars / totalBars) : 0;
                    if (percent != lastReportedPercent)
                    {
                        lastReportedPercent = percent;
                        Progress?.Report(new TickRunProgress(percent));
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
                if (useChunks && windowTo < replayTo)
                {
                    int pruned = 0;
                    foreach (var symbol in symbols)
                    {
                        foreach (CryptoInterval interval in GlobalData.IntervalList)
                        {
                            int keepDepth = IndicatorWarmup.WarmupDepth(interval);
                            CandleTime cutoff = new(windowTo.Minutes - (uint)keepDepth * interval.Duration);

                            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                            pruned += symbolInterval.CandleList.RemoveBefore(cutoff);
                        }
                    }
                    GlobalData.AddTextToLogTab($"Chunk {chunkIndex}: pruned {pruned} old candles from memory");
                }

                // Advance to next chunk
                windowFrom = useChunks ? new CandleTime(windowTo.Minutes + interval1m.Duration) : replayTo;
            }

            Progress?.Report(new TickRunProgress(100));

            if (lastDecile >= 0)
                decileWallTicks[lastDecile] += Stopwatch.GetTimestamp() - decileStart;
        }
        finally
        {
            GlobalData.AnalyzeSignalCreated = null;
            LogPhaseTimings();
            PipelineProfiler.Enabled = false;
        }
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
        double total = process1m + pipeline + zoneDrain + flush;
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
            $"flush {flush:F1}s ({flush / total:P0})");

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

        // Sub-breakdown of the indicator (SignalPrepare) bucket: where inside CalculateIndicators
        // the time goes — candle-list building, Skender batch math, the per-candle fill loop, or Lux.
        // This decides whether an incremental rewrite is worthwhile or just cheaper bookkeeping.
        double collect = Seconds(PipelineProfiler.PrepCollectTicks);
        double skender = Seconds(PipelineProfiler.PrepSkenderTicks);
        double fill = Seconds(PipelineProfiler.PrepFillTicks);
        double lux = Seconds(PipelineProfiler.PrepLuxTicks);
        double prepMeasured = collect + skender + fill + lux;
        if (prepMeasured > 0)
        {
            GlobalData.AddTextToLogTab(
                $"Indicators — measured {prepMeasured:F1}s | " +
                $"collectCandles {collect:F1}s ({collect / prepMeasured:P0}), " +
                $"skender {skender:F1}s ({skender / prepMeasured:P0}), " +
                $"fillLoop {fill:F1}s ({fill / prepMeasured:P0}), " +
                $"lux {lux:F1}s ({lux / prepMeasured:P0})");
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

        // Per-decile wall-clock breakdown — shows where the run slows down
        var decileParts = new string[10];
        for (int i = 0; i < 10; i++)
            decileParts[i] = $"{i * 10}-{i * 10 + 9}%:{Seconds(decileWallTicks[i]):F1}s";
        GlobalData.AddTextToLogTab($"Decile wall-clock — {string.Join(", ", decileParts)}");
    }


    /// <summary>
    /// Phase A — per-symbol compute (parallel-safe): insert the new 1m candle, synthesise the higher
    /// timeframes, then run the live scanner analysis pipeline (signals, paper-trade, position eval).
    /// Touches only this symbol's own state plus thread-safe queues (ThreadSaveObjects /
    /// ThreadZoneCalculate AddToQueue). The shared DB flush + zone drain are NOT done here — they run
    /// once per minute in <see cref="PersistAndCalculateZonesAsync"/> after all symbols are computed.
    /// elapsed* are accumulated with Interlocked because several symbols run this concurrently.
    /// </summary>
    private async Task ProcessComputeAsync(CryptoSymbol symbol, CryptoCandle candle)
    {
        // Reuse the canonical 1m-arrival handler instead of re-deriving it here. Process1mCandleAsync
        // is exactly what the live SubscriptionKLineTicker calls for every incoming 1m candle: it
        // adds the 1m candle to its CandleList, advances UpdateCandleFetched, and synthesises every
        // higher timeframe from 1m via CalculateCandleForInterval — using the look-ahead-safe
        // "targetComplete" check (StartOfIntervalCandle3) so an incomplete higher bucket is never
        // emitted. The pre-fetched higher candles in candles.db are the COMPLETE/closed bars; during
        // replay we must instead rebuild the CURRENT higher bar incrementally from the 1m candles
        // seen so far, otherwise the strategy would peek at the rest of the (future) bucket.
        long t0 = Stopwatch.GetTimestamp();
        await CandleTools.Process1mCandleAsync(symbol, candle.OpenTime.ToDateTime(),
            candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
        long t1 = Stopwatch.GetTimestamp();
        Interlocked.Add(ref elapsedProcess1m, t1 - t0);

        // Drive the exact same pipeline as the live ThreadMonitorCandle.Execute() loop:
        // SignalPrepare → SignalExecute → PaperTrading → TradingRules → CreateOrExtendPosition.
        using PositionMonitor positionMonitor = new(symbol, candle);
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

}
