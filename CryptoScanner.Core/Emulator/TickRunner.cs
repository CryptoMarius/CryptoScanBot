using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Progress payload emitted by <see cref="TickRunner"/> after each replayed candle.
/// </summary>
public readonly record struct TickRunProgress(string SymbolName, int ProcessedBars, int TotalBars);


/// <summary>
/// Drives the emulator replay. Multi-symbol time-merged feed: per replayed minute, every
/// symbol that has a 1m candle at that minute gets its tick processed (CandleList add,
/// higher-TF synthesis, PositionMonitor) before the clock advances to the next minute. This
/// matches what a real exchange does — symbols don't run on independent timelines — and is
/// what lets cross-symbol strategies (barometer, trend filters that read other symbols)
/// behave the same way in the emulator as they do live.
///
/// Higher intervals are NOT delivered by anything else. <see cref="SignalPrepare.Execute"/>
/// only computes indicators; it expects the higher-TF candle to already be in
/// <c>CandleList</c> (live KLineTicker subscribes per interval and provides them natively).
/// In the emulator that subscription doesn't exist, so the TickRunner synthesises higher
/// candles from 1m whenever a bucket-close aligns with the current minute. Without this
/// the higher CandleLists stay empty and signal pipeline produces nothing.
/// </summary>
public sealed class TickRunner
{
    public IProgress<TickRunProgress>? Progress { get; init; }


    public async Task RunAsync(EmulatorRunConfig config, CancellationToken ct)
    {
        if (!GlobalData.ExchangeListName.TryGetValue(config.ExchangeName, out Model.CryptoExchange? exchange))
            throw new InvalidOperationException($"Exchange '{config.ExchangeName}' is not registered in GlobalData.ExchangeListName.");

        // Bind ActiveExchange so the rest of Core (zone calculators, settings lookups, …)
        // sees the emulator's exchange. Restored on exit so unit-test re-entry is safe.
        Model.CryptoExchange? previousActive = GlobalData.ActiveExchange;
        GlobalData.ActiveExchange = exchange;
        try
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
                throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName.");

            // Maintained = strategy/zone intervals PLUS the trading-pause-rule intervals (e.g.
            // BTCUSDT 2m/5m). Must match what IndicatorWarmup.PrepareSymbol aggregated, otherwise
            // the higher-TF candle a rule/strategy reads would be missing during replay.
            List<CryptoInterval> activeIntervals = IndicatorWarmup.ResolveMaintainedIntervals();
            var higherIntervals = activeIntervals
                .Where(i => i.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                .ToList();

            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, 1);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, 1);

            // ───── Warmup all symbols up-front ──────────────────────────────────────
            // PrepareSymbol fills the 1m CandleList up to replayFrom and aggregates higher
            // intervals so signal indicators have stable values on the very first tick.
            // We keep one ReserveList per symbol; the merge loop below peeks across all of
            // them every minute.
            var reserves = new List<(CryptoSymbol Symbol, ReserveList Reserve)>();
            int totalBars = 0;
            foreach (string symbolName in config.Symbols)
            {
                ct.ThrowIfCancellationRequested();

                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    throw new InvalidOperationException($"Symbol '{symbolName}' not found on exchange '{config.ExchangeName}'.");

                List<CryptoCandle> replayCandles = IndicatorWarmup.PrepareSymbol(symbol, replayFrom, replayTo);
                reserves.Add((symbol, new ReserveList(symbol, replayCandles)));
                totalBars += replayCandles.Count;
            }

            // ───── Time-merged replay loop ──────────────────────────────────────────
            // Per iteration:
            //   1. Find the earliest pending candle across all symbols' reserves.
            //   2. Advance the EmulatorClock to that candle's close-time once.
            //   3. For each symbol whose next candle has that exact OpenTime, process it
            //      (CandleList add → higher-TF synthesis → NewCandleArrivedAsync).
            // Symbols without a candle at this minute simply don't tick; their next candle
            // will be picked up in a later iteration.
            EmulatorClock? emulatorClock = GlobalData.Clock as EmulatorClock;
            int processedBars = 0;

            while (!ct.IsCancellationRequested)
            {
                uint? nextMinute = FindNextMinute(reserves);
                if (nextMinute == null)
                    break; // all reserves drained

                // Clock advances to the close-time of the current tick BEFORE the symbols
                // process — that way any "now" read inside SignalPrepare/SignalExecute
                // points at the end of the current bar (same as live ticker behavior).
                uint closeMinutes = nextMinute.Value + interval1m.Duration;
                if (emulatorClock != null)
                    emulatorClock.UtcNow = new CandleTime(closeMinutes).ToDateTime();

                foreach (var (symbol, reserve) in reserves)
                {
                    if (!reserve.TryPeek(out CryptoCandle peek))
                        continue;
                    if (peek.OpenTime.Minutes != nextMinute.Value)
                        continue;

                    reserve.TryPop(out CryptoCandle candle);
                    await ProcessTickAsync(symbol, candle, interval1m, higherIntervals, closeMinutes);

                    processedBars++;

                    // Throttle progress reporting. Reporting every bar posts hundreds of thousands
                    // of updates to the UI thread on a multi-week 1m replay, which floods the
                    // dispatcher and dominates the run time. Once per 256 bars is smooth enough for
                    // a progress bar; the final count is reported after the loop.
                    if ((processedBars & 0xFF) == 0)
                    {
                        Progress?.Report(new TickRunProgress(symbol.Name, processedBars, totalBars));

                        // Yield occasionally so a UI thread or test harness stays responsive —
                        // engine work itself is synchronous and CPU-bound.
                        await Task.Yield();
                    }
                }
            }

            // Final progress report so the bar lands on 100% / the exact processed count even when
            // the last batch didn't hit the 256-bar boundary.
            Progress?.Report(new TickRunProgress("", processedBars, totalBars));
        }
        finally
        {
            GlobalData.ActiveExchange = previousActive;
        }
    }


    /// <summary>
    /// Returns the smallest OpenTime.Minutes value sitting at the head of any reserve, or
    /// null when every reserve is empty.
    /// </summary>
    private static uint? FindNextMinute(List<(CryptoSymbol Symbol, ReserveList Reserve)> reserves)
    {
        uint? earliest = null;
        foreach (var (_, reserve) in reserves)
        {
            if (!reserve.TryPeek(out CryptoCandle peek))
                continue;
            if (earliest == null || peek.OpenTime.Minutes < earliest.Value)
                earliest = peek.OpenTime.Minutes;
        }
        return earliest;
    }


    /// <summary>
    /// Standard per-tick processing for one symbol: insert the new 1m candle, mirror the
    /// state-mutations the live KLine ticker does (LastCandle, LastPrice), synthesise any
    /// higher-TF candles whose period closed on this minute, then run the live scanner
    /// analysis pipeline.
    /// </summary>
    private static async Task ProcessTickAsync(CryptoSymbol symbol, CryptoCandle candle,
        CryptoInterval interval1m, List<CryptoInterval> higherIntervals, uint closeMinutes)
    {
        CryptoSymbolInterval symbolInterval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);

        symbolInterval1m.CandleList.TryAdd(candle.OpenTime, candle);
        symbolInterval1m.LastCandle = candle;
        symbol.LastPrice = candle.Close;

        // Whenever this 1m close-time aligns on a higher-interval boundary, build the
        // higher-interval candle from the 1m candles already in the CandleList. Required
        // because SignalPrepare.Execute only checks the higher CandleList — it does NOT
        // synthesise the missing candle. Without this every check for the higher interval
        // fails silently and no signal/position can fire on those timeframes.
        foreach (CryptoInterval higher in higherIntervals)
        {
            if (closeMinutes % higher.Duration == 0)
            {
                var higherOpen = new CandleTime(closeMinutes - higher.Duration);
                CandleTools.CalculateCandleForInterval(symbol, interval1m, higher, higherOpen);
            }
        }

        // Drive the exact same pipeline as the live ThreadMonitorCandle.Execute() loop:
        // SignalPrepare → SignalExecute → PaperTrading → TradingRules → CreateOrExtendPosition.
        // Synchronous (await) so the next replay tick can never observe a half-processed
        // state — multi-symbol parallelism is intentionally out of scope here.
        PositionMonitor positionMonitor = new(symbol, candle);
        await positionMonitor.NewCandleArrivedAsync();

        // Persist everything NewCandleArrivedAsync queued — created signals/positions plus the
        // inline FVG (ScanForNew) and SMC (Detect) zone diffs. This must happen BEFORE the DLZ
        // drain below, because ZoneDlz.LoadZonesForSymbol (first thing in CalculateZones) resets
        // all in-memory zones and reloads them from the DB; anything still sitting in the save
        // queue would be reset away. Live this is fine on a 250 ms background flush, but the
        // emulator's tick boundaries collapse that timing so we flush synchronously here.
        GlobalData.ThreadSaveObjects?.Flush();

        // DLZ zones are queued (not computed) by SignalPrepare.Execute via
        // ThreadZoneCalculate.AddToQueue — the live scanner has a background worker draining
        // that queue. The emulator has no such worker (it would race the virtual clock and the
        // shared CandleList), so we drain synchronously here, on the replay thread, while the
        // clock is still pinned to this bar.
        if (GlobalData.ThreadZoneCalculate != null)
            await GlobalData.ThreadZoneCalculate.DrainQueueAsync();

        // Persist the DLZ zone diffs the drain just produced, so the next tick's
        // LoadZonesForSymbol reload sees them instead of resetting them away.
        GlobalData.ThreadSaveObjects?.Flush();
    }
}
