using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Progress payload emitted by <see cref="TickRunner"/> after each replayed candle.
/// </summary>
public readonly record struct TickRunProgress(string SymbolName, int ProcessedBars, int TotalBars);


/// <summary>
/// Drives the emulator replay. For each symbol in <see cref="EmulatorRunConfig"/>:
///  1. <see cref="IndicatorWarmup.PrepareSymbol"/> loads 1m history and aggregates higher intervals
///     up to the start of the replay window.
///  2. Replay 1m candles one-by-one from the <see cref="ReserveList"/>. For each candle the
///     <see cref="GlobalData.Clock"/> is advanced to the candle's close-time and the candle is
///     pushed into the symbol's 1m CandleList — exactly the same state mutation the live
///     KLine ticker performs in production.
///  3. Whenever the 1m close-time aligns on a higher-interval boundary the higher candle is
///     synthesized via <see cref="CandleTools.CalculateCandleForInterval"/>. No exchange
///     calls for higher intervals; the emulator builds them itself.
///
/// The scanner analysis pipeline call is intentionally left as a TODO for the next iteration —
/// this class only proves the feed mechanics work in isolation. Multi-symbol time-merged
/// replay (single timeline across all symbols) is also a follow-up; for now symbols replay
/// sequentially.
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

            List<CryptoInterval> activeIntervals = IndicatorWarmup.ResolveActiveIntervals();
            var higherIntervals = activeIntervals
                .Where(i => i.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                .ToList();

            CandleTime replayFrom = CandleTime.AlignFromDateTime(config.FromDate, 1);
            CandleTime replayTo = CandleTime.AlignFromDateTime(config.ToDate, 1);

            foreach (string symbolName in config.Symbols)
            {
                ct.ThrowIfCancellationRequested();

                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    throw new InvalidOperationException($"Symbol '{symbolName}' not found on exchange '{config.ExchangeName}'.");

                await ReplaySymbolAsync(symbol, interval1m, higherIntervals, replayFrom, replayTo, ct);
            }
        }
        finally
        {
            GlobalData.ActiveExchange = previousActive;
        }
    }


    private async Task ReplaySymbolAsync(CryptoSymbol symbol,
        CryptoInterval interval1m, List<CryptoInterval> higherIntervals,
        CandleTime replayFrom, CandleTime replayTo, CancellationToken ct)
    {
        // Warmup: fills the 1m CandleList up to replayFrom and aggregates higher intervals.
        List<CryptoCandle> replayCandles = IndicatorWarmup.PrepareSymbol(symbol, replayFrom, replayTo);
        var reserve = new ReserveList(symbol, replayCandles);
        int totalBars = reserve.RemainingCount;
        int processedBars = 0;

        EmulatorClock? emulatorClock = GlobalData.Clock as EmulatorClock;
        CryptoSymbolInterval symbolInterval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);

        while (!ct.IsCancellationRequested && reserve.TryPop(out CryptoCandle candle))
        {

            // CloseTime = OpenTime + duration. For 1m candles that is OpenTime + 1 minute.
            uint closeMinutes = candle.OpenTime.Minutes + interval1m.Duration;

            // Advance the wall-clock the rest of Core sees. Everything that reads
            // GlobalData.Clock.UtcNow now observes the candle's close-time.
            if (emulatorClock != null)
                emulatorClock.UtcNow = new CandleTime(closeMinutes).ToDateTime();

            // Same state mutations the live KLine ticker performs on close.
            symbolInterval1m.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval1m.LastCandle = candle;
            symbol.LastPrice = candle.Close;

            // Whenever this 1m close-time aligns on a higher-interval boundary, build the
            // higher-interval candle from the 1m candles already in the CandleList.
            foreach (CryptoInterval higher in higherIntervals)
            {
                if (closeMinutes % higher.Duration == 0)
                {
                    var higherOpen = new CandleTime(closeMinutes - higher.Duration);
                    CandleTools.CalculateCandleForInterval(symbol, interval1m, higher, higherOpen);
                }
            }

            // TODO: invoke the scanner analysis pipeline here. Needs a dedicated entry point
            // that does not assume the live ScannerSession is running (current flow goes
            // through ThreadMonitorCandle → SignalPrepare → SignalCreate). Phase 3 closer.

            processedBars++;
            Progress?.Report(new TickRunProgress(symbol.Name, processedBars, totalBars));

            // Yield occasionally so a UI thread or test harness stays responsive — engine work
            // itself is synchronous and CPU-bound.
            if ((processedBars & 0xFF) == 0)
                await Task.Yield();
        }
    }
}
