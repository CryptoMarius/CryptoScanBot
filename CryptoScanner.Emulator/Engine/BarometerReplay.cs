using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

using System.Collections.Concurrent;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// The market barometer during a replay.
/// <para>
/// Until now a replay had no barometer at all: the live scanner calculates it from the full symbol
/// pool of a quote coin on a timer, and that timer does not run here. The consequence was not that
/// conditions were skipped but that they silently passed - a barometer condition in a queue entry
/// was tested against a value that was never calculated, and every position of every run carried a
/// barometer of zero. This class removes both.
/// </para>
/// <para>
/// It measures over the symbols of the RUN, not over the whole exchange. That is a different index
/// than the live one (50 coins against several hundred), so a threshold measured here does not
/// transfer one to one to the live scanner - but it is the market this run actually traded in, and
/// it is the same formula (CryptoBarometerPrice), which is what makes runs comparable to each other.
/// Below <see cref="MinimumSymbols"/> coins nothing is stored at all: an average over three coins is
/// the price change of three coins with the word "market" written on it.
/// </para>
/// <para>
/// Calculated once per replayed minute, in the serial phase of the loop, so the value a signal reads
/// is the one from the previous minute - the same small lag the live scanner has, where the timer
/// also runs behind the candle that just closed.
/// </para>
/// </summary>
internal sealed class BarometerReplay
{
    /// <summary>
    /// The smallest number of coins a measurement may rest on. A barometer over fewer coins is not a
    /// market (the run report uses the same threshold of five; the live scanner has no such limit,
    /// see open point 40).
    /// </summary>
    public const int MinimumSymbols = 5;

    /// <summary>
    /// The intervals a signal and a position store on themselves (Barometer15m..Barometer1d). They
    /// are always calculated, whether a condition asks for them or not, because they cost almost
    /// nothing next to the rest of a replayed minute and they are what makes a finished run
    /// answerable afterwards.
    /// </summary>
    private static readonly CryptoIntervalPeriod[] StoredOnSignal =
    [
        CryptoIntervalPeriod.interval15m,
        CryptoIntervalPeriod.interval30m,
        CryptoIntervalPeriod.interval1h,
        CryptoIntervalPeriod.interval4h,
        CryptoIntervalPeriod.interval1d,
    ];

    // Spelled out in full: CryptoExchange is also a NAMESPACE, and a namespace wins from a type of
    // the same name (CS0118) - the whole file would stop compiling on the short spelling.
    private readonly Core.Model.CryptoExchange exchange;
    private readonly List<(CryptoQuoteData QuoteData, List<CryptoSymbol> Symbols)> perQuote = [];
    private readonly List<CryptoInterval> intervals = [];

    // One result object for the whole run, exactly as the live calculation does: the loop below runs
    // per minute per interval and a fresh object per measurement would be pure garbage.
    private readonly BarometerResult result = new();

    // Rows waiting to be written. Positions are created in the PARALLEL phase of the replay loop, so
    // this is filled from several threads and drained in the serial phase.
    private readonly ConcurrentQueue<CryptoBarometerSnapshot> pending = new();

    public BarometerReplay(Core.Model.CryptoExchange exchange, IEnumerable<CryptoSymbol> symbols)
    {
        this.exchange = exchange;

        foreach (CryptoSymbol symbol in symbols)
        {
            if (symbol.QuoteData == null || symbol.IsBarometerSymbol())
                continue;

            int index = perQuote.FindIndex(q => q.QuoteData == symbol.QuoteData);
            if (index < 0)
            {
                perQuote.Add((symbol.QuoteData, []));
                index = perQuote.Count - 1;
            }
            perQuote[index].Symbols.Add(symbol);
        }

        foreach (CryptoIntervalPeriod period in CollectIntervals())
        {
            if (GlobalData.IntervalListPeriod.TryGetValue(period, out CryptoInterval? interval))
                intervals.Add(interval);
        }
        intervals.Sort((a, b) => a.Duration.CompareTo(b.Duration));
    }


    /// <summary>
    /// The intervals to measure: the five that are stored on every signal, plus every interval a
    /// barometer condition asks about - on either side, for the analysis as well as for the trader.
    /// A condition on an interval that is not measured is the silent pass this class exists to
    /// remove, so the conditions decide what is calculated rather than the other way around.
    /// </summary>
    private static IEnumerable<CryptoIntervalPeriod> CollectIntervals()
    {
        HashSet<CryptoIntervalPeriod> periods = [.. StoredOnSignal];

        foreach (CryptoTradeSide side in Sides)
        {
            foreach (CryptoIntervalPeriod period in TradingConfig.Signals[side].Barometer.Keys)
                periods.Add(period);
            foreach (CryptoIntervalPeriod period in TradingConfig.Trading[side].Barometer.Keys)
                periods.Add(period);
        }

        return periods;
    }


    private static readonly CryptoTradeSide[] Sides = [CryptoTradeSide.Long, CryptoTradeSide.Short];


    /// <summary>
    /// Every barometer condition that is set right now, whichever side it is on. Used by the guard
    /// that refuses to start a run with conditions it cannot measure.
    /// </summary>
    public static List<string> ActiveConditions()
    {
        List<string> names = [];
        foreach (CryptoTradeSide side in Sides)
        {
            foreach (CryptoIntervalPeriod period in TradingConfig.Signals[side].Barometer.Keys)
                names.Add($"analyze {side} {period}");
            foreach (CryptoIntervalPeriod period in TradingConfig.Trading[side].Barometer.Keys)
                names.Add($"trader {side} {period}");
            if (TradingConfig.Signals[side].BarometerConsensusActive && TradingConfig.Signals[side].BarometerMinConsensus > 0)
                names.Add($"analyze {side} consensus");
        }
        return names;
    }


    /// <summary>
    /// Measure every interval for every quote coin of the run. Called once per replayed minute from
    /// the serial phase, with the last minute that has fully closed.
    /// </summary>
    public void Execute(CandleTime lastClosedMinute)
    {
        foreach ((CryptoQuoteData quoteData, List<CryptoSymbol> symbols) in perQuote)
        {
            foreach (CryptoInterval interval in intervals)
                BarometerTools.CalculateForSymbols(exchange, quoteData, symbols, interval, lastClosedMinute, MinimumSymbols, result);
        }

        // The market context of the run itself, so a finding like "this strategy earns in a falling
        // market" has a denominator: how often a falling market occurred at all.
        if (lastClosedMinute.Minutes % Constants.BarometerHeartbeatMinutes == 0)
        {
            foreach ((CryptoQuoteData quoteData, _) in perQuote)
            {
                foreach (CryptoInterval interval in intervals)
                {
                    CryptoBarometerSnapshot? row = BuildRow(quoteData.Name, interval, null);
                    if (row != null)
                        pending.Enqueue(row);
                }
            }
        }
    }


    /// <summary>
    /// Remember what the market looked like when this position was opened. Called from the parallel
    /// phase: GlobalData.PositionCreated fires right after the position is inserted, so it has an Id.
    /// </summary>
    public void PositionCreated(CryptoPosition position)
    {
        foreach (CryptoInterval interval in intervals)
        {
            CryptoBarometerSnapshot? row = BuildRow(position.Symbol.Quote, interval, position.Id);
            if (row != null)
                pending.Enqueue(row);
        }
    }


    private CryptoBarometerSnapshot? BuildRow(string quoteName, CryptoInterval interval, int? positionId)
    {
        CryptoBarometerData data = exchange.Data.GetBarometer(quoteName, interval.IntervalPeriod);
        if (!data.PriceBarometer.HasValue || !data.PriceDateTime.HasValue)
            return null; // nothing measured (yet) - a row of zeroes would read as a flat market

        return new CryptoBarometerSnapshot
        {
            EmulatorRunId = GlobalData.CurrentEmulatorRunId,
            PositionId = positionId,
            MeasureDate = data.PriceDateTime.Value.ToDateTime(),
            Quote = quoteName,
            Interval = interval.Name,
            Average = data.PriceBarometer.Value,
            Median = data.PriceMedian ?? 0,
            PercentageRising = data.PricePercentageRising ?? 0,
            Spread = data.PriceSpread ?? 0,
            Movement = data.PriceMovement ?? 0,
            BitcoinVersusMarket = data.PriceBitcoinVersusMarket,
            SymbolCount = data.PriceSymbolCount ?? 0,
            OutlierCount = data.PriceOutlierCount ?? 0,
        };
    }


    /// <summary>
    /// Write what is waiting. Called from the serial phase, so no position is being created while
    /// the queue is drained.
    /// </summary>
    public void Flush()
    {
        if (pending.IsEmpty)
            return;

        try
        {
            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();

            while (pending.TryDequeue(out CryptoBarometerSnapshot? row))
                database.Connection.Insert(row, transaction);

            transaction.Commit();
        }
        catch (Exception error)
        {
            // A missing measurement costs a row in an analysis, it may never cost a trade
            ScannerLog.Logger.Error(error, "BarometerReplay.Flush");
        }
    }


    /// <summary>The intervals being measured, for the line the run logs about itself.</summary>
    public string IntervalText => string.Join(", ", intervals.Select(i => i.Name));

    /// <summary>The number of coins the barometer of each quote coin rests on.</summary>
    public string SymbolCountText => string.Join(", ", perQuote.Select(q => $"{q.QuoteData.Name} {q.Symbols.Count}"));

    /// <summary>True when at least one quote coin has enough coins for a barometer that means anything.</summary>
    public bool HasEnoughSymbols => perQuote.Exists(q => q.Symbols.Count >= MinimumSymbols);
}
