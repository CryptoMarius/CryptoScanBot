using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;
using System.Diagnostics;

namespace CryptoScanner.Core.Zones;

public class ZoneThreadCalculate
{

    private readonly BlockingCollection<(CryptoSymbol, CryptoInterval)> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        //GlobalData.AddTextToLogTab(string.Format("Stop calculating zones"));
    }


    public void AddToQueue(CryptoSymbol symbol, CryptoInterval interval)
    {
        Queue.Add((symbol, interval));
    }


    internal static async Task CalculateZones(CryptoSymbol symbol, CryptoInterval interval)
    {
        if (symbol.QuoteData!.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
        {
            if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
            {
                //GlobalData.AddTextToLogTab($"Calculation zones for {symbol.Name} {interval.Name}");

                // Per-kind gate: only run a calculation if this interval is actually configured
                // for that zone kind. Earlier this method blindly ran both ZoneDlz and ZoneFvg
                // for every queued (symbol, interval), which wasted work on intervals only
                // present in one of the two lists.
                // NOTE: zones that were created earlier on an interval that has since been
                // REMOVED from the IntervalList are NOT cleaned up here — that is a separate
                // concern (stale zones stay in memory/DB until manually purged).
                bool runDlz = Signal.SignalPrepare.IsDlzInterval(interval.Name);
                bool runFvg = GlobalData.Settings.Signal.ZonesFvg.IntervalList.Contains(interval.Name);
                if (!runDlz && !runFvg)
                    return;

                var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                var symbolDataInterval = symbol.Data.Get(interval.IntervalPeriod);

                //var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                //TrendZigZagIndicatorList trendZigZagIndicatorList = [];
                //trendZigZagIndicatorList.Add((trend.TrendType, trend.UseHighLow),
                //    new(trend.TrendType, trend.UseHighLow, 1.0m));

                ZoneCandleWindows loadedCandlesInMemory = new();

                // Hold ZoneLock for the entire load + recalculation so ScanForNew cannot
                // concurrently write to the same OrderedList (non-thread-safe List<T> inside).
                await symbol.Data.ZoneLock.WaitAsync();
                try
                {
                    // Scope to the active run (null when live) so a replay only ever sees its own zones.
                    // Only (re)load once per (symbol, run scope) — this resets the in-memory FVG/DLZ/SMC
                    // lists and re-reads them from the DB, which is needed once at the start of a run/session
                    // but is pure overhead (and breaks incremental calculation) if repeated on every drain.
                    if (!symbol.Data.ZonesLoaded || symbol.Data.ZonesLoadedRunId != GlobalData.CurrentEmulatorRunId)
                    {
                        ZoneDlz.LoadZonesForSymbol(symbol, GlobalData.CurrentEmulatorRunId);
                        symbol.Data.ZonesLoaded = true;
                        symbol.Data.ZonesLoadedRunId = GlobalData.CurrentEmulatorRunId;
                    }

                    int candleFetchCount = CandleTools.CandleCountFetch;
                    CandleTime maxDate = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, interval.Duration);
                    CandleTime minDate = maxDate - candleFetchCount * interval.Duration;
                    //await ZoneDlz.LoadHistoricCandles(symbol, interval, loadedCandlesInMemory);

                    if (runDlz)
                    {
                        // Measured here and not in SignalPrepare: this way the FVG call below stays
                        // out of the number, and both routes into a recalculation are covered - the
                        // emulator calls this method directly while the live scanner arrives through
                        // the queue drain. In the emulator the time inside lands in PrepareTicks
                        // ("indicators"), where it was indistinguishable from the indicator hub.
                        long profDlzStart = Stopwatch.GetTimestamp();
                        await ZoneDlz.CalculateZonesAsync(null, symbol, interval, loadedCandlesInMemory);
                        PipelineProfiler.RecordDlzInline(Stopwatch.GetTimestamp() - profDlzStart);
                    }
                    if (runFvg)
                        await ZoneFvg.CalculateZonesAsync(null, symbol, interval, loadedCandlesInMemory);

                    // Advance the exchange-level zone checkpoint so restarts
                    // only replay candles from this point forward.
                    if (!GlobalData.IsEmulatorMode)
                    {
                        var exchange = symbol.Exchange;
                        if (exchange.LastZoneCheckTime == null || maxDate > exchange.LastZoneCheckTime.Value)
                        {
                            exchange.LastZoneCheckTime = maxDate;
                            GlobalData.ThreadSaveObjects!.AddToQueue(exchange);
                        }
                    }

                    // Notify the symbol grid so the Distance column for this row is re-read
                    // immediately, instead of waiting for the 15-second refresh tick.
                    // SendMvvmMessage posts to the UI thread internally, so calling it from
                    // this background worker is safe.
                    GlobalData.SendMvvmMessage(new ZonesCalculatedForSymbolMessage(symbol));
                }
                finally
                {
                    // The write back is live-only. Not to keep the two engines apart, but because
                    // there is nothing to write: during a replay every candle in memory came out of
                    // candles.db to begin with, and the run persists once at the end. Writing per
                    // tick would take the shared write gate on every zone calculation.
                    //
                    // Ahead of the two lines below, deliberately: the save reads which intervals are
                    // still unsaved out of loadedCandlesInMemory, and clearing it first would hand it
                    // an empty list and silently write nothing.
                    if (!GlobalData.IsEmulatorMode)
                        await ZoneCandleEngine.SaveCandleDataToDiskAsync(symbol, loadedCandlesInMemory);

                    loadedCandlesInMemory.Clear();

                    // Pruning runs in the emulator too, since 23-08-2026. It used to be skipped there,
                    // and the replay then kept every candle the zoom had pulled in until the chunk
                    // boundary seven replay-days later - 23.6 million of them at chunk 30 of run 229.
                    // That is a different engine from the one the live scanner runs, and the emulator
                    // exists to predict the live scanner. Whatever the depth costs us here, the scanner
                    // pays it too; better to meet that in a replay than in production. The depth is
                    // per interval (CandleTools.GetCandleFetchStart): 500 candles for everything
                    // except 1m, and a day plus the barometer window for 1m.
                    //
                    // Awaited, where it used to be fire-and-forget. A replay has to produce the same
                    // answer twice, and a prune that lands somewhere in the next tick instead of in
                    // this one decides which candles that tick can still see. The lock order is
                    // unchanged - ZoneLock then CandleLock, the same way CalculatePivots takes them
                    // a few lines up - so waiting here cannot introduce a deadlock the old path did
                    // not already have.
                    await ZoneCandleEngine.CleanLoadedCandlesAsync(symbol);
                    symbol.Data.ZoneLock.Release();
                }
            }
        }
    }

    /// <summary>
    /// Synchronously processes everything currently in the queue and returns once it is empty.
    /// The live scanner runs <see cref="ExecuteAsync"/> on a background thread, but the emulator
    /// must keep zone calculation deterministic and on the replay thread. So instead of starting
    /// the worker, the emulator drains the queue inline at the end of each tick (right after
    /// PositionMonitor.NewCandleArrivedAsync) while the virtual clock is still pinned to the
    /// current bar — the queued (symbol, interval) items are exactly what SignalPrepare.Execute
    /// scheduled for this minute. TryTake is non-blocking, so this returns immediately when no
    /// zone work was queued for the tick.
    /// </summary>
    public async Task DrainQueueAsync()
    {
        while (Queue.TryTake(out (CryptoSymbol symbol, CryptoInterval interval) item))
        {
            try
            {
                await CalculateZones(item.symbol, item.interval);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddErrorToLogTab($"ThreadZoneCalculate (drain) ERROR {error.Message}");
            }
        }
    }


    public async Task ExecuteAsync()
    {
        //GlobalData.AddTextToLogTab("Starting task for calculating zones");
        try
        {
            foreach ((CryptoSymbol symbol, CryptoInterval interval) in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                try
                {
                    await CalculateZones(symbol, interval);
                }
                catch (OperationCanceledException)
                {
                    throw; // exit..
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddErrorToLogTab($"ThreadZoneCalculate ERROR {error.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"ThreadZoneCalculate ERROR {error.Message}");
        }

        GlobalData.AddTextToLogTab("ThreadZoneCalculate thread exit");
    }



    public static void CalculateZonesForAllSymbolsAsync()
    {
        if (GlobalData.ActiveExchange != null)
        {
            // Union of DLZ + FVG intervals. The queue carries (symbol, interval) without a
            // per-kind flag — the worker (CalculateZones) decides which of ZoneDlz / ZoneFvg
            // to run based on which IntervalList contains the interval. Earlier this loop
            // only iterated FVG.IntervalList, which meant DLZ-only intervals were never
            // queued by the "Calculate DLZ for all" command. Concat+Distinct keeps the
            // DLZ-configured intervals first (stable, easy to follow in the log).
            var intervalNames = Signal.SignalPrepare.EffectiveDlzIntervals
                .Concat(GlobalData.Settings.Signal.ZonesFvg.IntervalList)
                .Distinct()
                .ToList();

            foreach (var symbol in GlobalData.ActiveExchange.SymbolListName.Values)
            {
                foreach (var intervalName in intervalNames)
                {
                    if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    {
                        GlobalData.ThreadZoneCalculate?.AddToQueue(symbol, interval);
                    }
                }
            }
        }
    }

}
