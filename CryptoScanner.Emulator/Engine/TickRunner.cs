using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

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
/// OpenTime (built by <see cref="IndicatorWarmup.PrepareSymbol"/>); each minute the loop simply
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


    private static void ReceivedCreatedSignals(CryptoSignal signal)
    {
        //GlobalData.CreatedSignalCount++;
        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);
    }

    public async Task RunAsync(EmulatorRunConfig config, CancellationToken ct)
    {
        if (!GlobalData.ExchangeListName.TryGetValue(config.ExchangeName, out CryptoScanner.Core.Model.CryptoExchange? exchange))
            throw new InvalidOperationException($"Exchange '{config.ExchangeName}' is not registered in GlobalData.ExchangeListName.");

        // Bind ActiveExchange so the rest of Core (zone calculators, settings lookups, …)
        // sees the emulator's exchange. Restored on exit so unit-test re-entry is safe.
        CryptoScanner.Core.Model.CryptoExchange? previousActive = GlobalData.ActiveExchange;
        GlobalData.ActiveExchange = exchange;
        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;
        try
        {
            // Clear positions, assets etc
            exchange.Data.Clear();

            GlobalData.Settings.General.ExchangeName = config.ExchangeName;
            GlobalData.Settings.General.ActivateExchangeName = config.ExchangeName;


            if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
                throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName.");

            // The higher-timeframe synthesis is now done inside CandleTools.Process1mCandleAsync
            // (the live 1m handler) per tick, so the TickRunner no longer needs its own list of
            // higher intervals here — only the 1m driving interval to merge the per-symbol feeds.
            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, 1);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, 1);

            // ───── Warmup all symbols up-front ──────────────────────────────────────
            // PrepareSymbol fills the 1m CandleList up to replayFrom and aggregates higher
            // intervals so signal indicators have stable values on the very first tick. It hands
            // back the replay-window 1m candles as a CryptoCandleList keyed by OpenTime; the merge
            // loop below just looks each minute up by candle-time.
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
                CandleTime closeTime = openTime; // + interval1m.Duration;
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
            GlobalData.ActiveExchange = previousActive;
            GlobalData.AnalyzeSignalCreated = null;
        }
    }


    /// <summary>
    /// Standard per-tick processing for one symbol: insert the new 1m candle, mirror the
    /// state-mutations the live KLine ticker does (LastCandle, LastPrice), synthesise any
    /// higher-TF candles whose period closed on this minute, then run the live scanner
    /// analysis pipeline.
    /// </summary>
    private static async Task ProcessTickAsync(CryptoSymbol symbol, CryptoCandle candle)
    {
        // Reuse the canonical 1m-arrival handler instead of re-deriving it here. Process1mCandleAsync
        // is exactly what the live SubscriptionKLineTicker calls for every incoming 1m candle: it
        // adds the 1m candle to its CandleList, advances UpdateCandleFetched, and synthesises every
        // higher timeframe from 1m via CalculateCandleForInterval — using the look-ahead-safe
        // "targetComplete" check (StartOfIntervalCandle3) so an incomplete higher bucket is never
        // emitted. The pre-fetched higher candles in candles.db are the COMPLETE/closed bars; during
        // replay we must instead rebuild the CURRENT higher bar incrementally from the 1m candles
        // seen so far, otherwise the strategy would peek at the rest of the (future) bucket.
        await CandleTools.Process1mCandleAsync(symbol, candle.OpenTime.ToDateTime(),
            candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);

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
