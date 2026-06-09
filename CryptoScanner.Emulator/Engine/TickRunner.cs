using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Diagnostics;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Progress payload emitted by <see cref="TickRunner"/> after each replayed candle.
/// </summary>
public readonly record struct TickRunProgress(string SymbolName, int ProcessedBars, int TotalBars);


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
        PipelineProfiler.Enabled = true;
        try
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
                throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName.");

            // The higher-timeframe synthesis is now done inside CandleTools.Process1mCandleAsync
            // (the live 1m handler) per tick, so the TickRunner no longer needs its own list of
            // higher intervals here — only the 1m driving interval to merge the per-symbol feeds.
            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, 1);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, 1);

            // ───── Warmup all symbols up-front ──────────────────────────────────────
            // PrepareSymbol loads ~270 candles of EACH interval (1m + higher) before replayFrom
            // straight from the candles.db, so every timeframe has real history for its indicators.
            // It hands back ONLY the 1m replay window as a CryptoCandleList keyed by OpenTime; the
            // higher intervals are extended by Process1mCandleAsync as the replay progresses.
            var replays = new List<(CryptoSymbol Symbol, CryptoCandleList Replay)>();
            int totalBars = 0;
            foreach (string symbolName in config.Symbols)
            {
                ct.ThrowIfCancellationRequested();

                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    throw new InvalidOperationException($"Symbol '{symbolName}' not found on exchange '{config.ExchangeName}'.");

                CryptoCandleList replayCandles = IndicatorWarmup.PrepareSymbol(symbol, replayFrom, replayTo);
                replays.Add((symbol, replayCandles));
                totalBars += replayCandles.Count;

                // Reset trade data
                symbol.LastPrice = null;
                symbol.LastTradeDate = null;
                symbol.LastTradeFetched = null;
                symbol.LastTradeIdFetched = null;
            }



            // ───── Time-merged replay loop ──────────────────────────────────────────
            // Walk the replay window minute by minute (the exchange's own clock). For each minute:
            //   1. Advance the EmulatorClock to that minute's close-time once, so any "now" read
            //      inside SignalPrepare/SignalExecute points at the end of the current bar.
            //   2. For every symbol that HAS a 1m candle at this minute (TryGetValue), process it.
            // Symbols without a candle at this minute simply don't tick. No queue / peek / pop —
            // the per-symbol CryptoCandleList is the set-aside feed and we index it by candle-time.
            EmulatorClock? emulatorClock = GlobalData.Clock as EmulatorClock;
            int processedBars = 0;

            for (CandleTime openTime = replayFrom; openTime <= replayTo; openTime += interval1m.Duration)
            {
                if (ct.IsCancellationRequested)
                    break;

                // Clock advances to the close-time of the current minute BEFORE the symbols process.
                CandleTime closeTime = openTime + interval1m.Duration;
                if (emulatorClock != null)
                    emulatorClock.UtcNow = closeTime.ToDateTime();

                foreach (var (symbol, replay) in replays)
                {
                    if (replay.TryGetValue(openTime, out CryptoCandle candle))
                    {
                        await ProcessTickAsync(symbol, candle);

                        // Throttle progress reporting. Reporting every bar posts hundreds of thousands
                        // of updates to the UI thread on a multi-week 1m replay, which floods the
                        // dispatcher and dominates the run time. Once per 256 bars is smooth enough for
                        // a progress bar; the final count is reported after the loop.
                        if ((++processedBars & 0xFF) == 0)
                        {
                            Progress?.Report(new TickRunProgress(symbol.Name, processedBars, totalBars));

                            // Yield occasionally so a UI thread or test harness stays responsive —
                            // engine work itself is synchronous and CPU-bound.
                            await Task.Yield();
                        }
                    }
                }
            }

            // Final progress report so the bar lands on 100% / the exact processed count even when
            // the last batch didn't hit the 256-bar boundary.
            Progress?.Report(new TickRunProgress("", processedBars, totalBars));
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

        GlobalData.AddTextToLogTab(
            $"Timing — total {total:F1}s | " +
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
    }


    /// <summary>
    /// Standard per-tick processing for one symbol: insert the new 1m candle, mirror the
    /// state-mutations the live KLine ticker does (LastCandle, LastPrice), synthesise any
    /// higher-TF candles whose period closed on this minute, then run the live scanner
    /// analysis pipeline.
    /// </summary>
    private async Task ProcessTickAsync(CryptoSymbol symbol, CryptoCandle candle)
    {
        // keep please for debugging!!!
        var symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        var c = symbolPeriod.CandleList.Count;
        if (c > 0)
        {
        }
        var first = symbolPeriod.CandleList.FirstOrDefault();
        var last = symbolPeriod.CandleList.LastOrDefault();

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
        elapsedProcess1m += t1 - t0;

        // Drive the exact same pipeline as the live ThreadMonitorCandle.Execute() loop:
        // SignalPrepare → SignalExecute → PaperTrading → TradingRules → CreateOrExtendPosition.
        // Synchronous (await) so the next replay tick can never observe a half-processed
        // state — multi-symbol parallelism is intentionally out of scope here.
        PositionMonitor positionMonitor = new(symbol, candle);
        await positionMonitor.NewCandleArrivedAsync();
        long t2 = Stopwatch.GetTimestamp();
        elapsedPipeline += t2 - t1;

        // Persist everything NewCandleArrivedAsync queued — created signals/positions plus the
        // inline FVG (ScanForNew) and SMC (Detect) zone diffs. This must happen BEFORE the DLZ
        // drain below, because ZoneDlz.LoadZonesForSymbol (first thing in CalculateZones) resets
        // all in-memory zones and reloads them from the DB; anything still sitting in the save
        // queue would be reset away. Live this is fine on a 250 ms background flush, but the
        // emulator's tick boundaries collapse that timing so we flush synchronously here.
        GlobalData.ThreadSaveObjects?.Flush();
        long t3 = Stopwatch.GetTimestamp();
        elapsedFlush += t3 - t2;

        // DLZ zones are queued (not computed) by SignalPrepare.Execute via
        // ThreadZoneCalculate.AddToQueue — the live scanner has a background worker draining
        // that queue. The emulator has no such worker (it would race the virtual clock and the
        // shared CandleList), so we drain synchronously here, on the replay thread, while the
        // clock is still pinned to this bar.
        if (GlobalData.ThreadZoneCalculate != null)
            await GlobalData.ThreadZoneCalculate.DrainQueueAsync();
        long t4 = Stopwatch.GetTimestamp();
        elapsedZoneDrain += t4 - t3;

        // Persist the DLZ zone diffs the drain just produced, so the next tick's
        // LoadZonesForSymbol reload sees them instead of resetting them away.
        GlobalData.ThreadSaveObjects?.Flush();
        elapsedFlush += Stopwatch.GetTimestamp() - t4;
    }

}
