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
/// <para>
/// Measured ONCE per coin list. Every measurement is written into the candles of the $BMP/$BMX
/// symbols, where the live scanner has always put it, and a later run over the same coins reads them
/// back instead of computing them again - which takes the barometer out of the replay loop entirely.
/// That it is the same answer is not an assumption: runs 802 and 803 had the same period and the same
/// 66 coins, and on all 39.365 shared moments the values were identical to the cent.
/// </para>
/// <para>
/// This replaced the BarometerSnapshot table on 04-09-2026. That table stored the same measurement a
/// second time, per run, and was not covered by the purge before a run - 5.566.145 rows over 151
/// runs, most of the 1,27 GB the database had grown to, and the write-ahead log that came with it
/// made a run fail on "database is locked".
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

    /// <summary>
    /// Whether the stored series may be read back instead of measured again. True when the marker in
    /// the candle store says the $BMP/$BMX candles were measured over exactly this coin list and this
    /// volume threshold; false the first time, and after coins are added or removed.
    /// </summary>
    private readonly bool reuse;

    /// <summary>The Meta key that says which coin list a quote coin's barometer series belongs to.</summary>
    internal static string MarkerKey(string quoteName) => "Barometer:" + Constants.SymbolNameBarometerPrice + quoteName;

    /// <summary>
    /// What the stored series has to match: the coins it was measured over and the volume threshold
    /// that decided which of them took part at each moment. Both change the outcome, so both are in
    /// the marker - a run with the threshold at 0 instead of 15 million measures a different market
    /// over the same coins, which is exactly what happened on 02-09-2026 without anyone noticing.
    /// </summary>
    internal static string MarkerFor(CryptoQuoteData quoteData, List<CryptoSymbol> symbols)
    {
        List<string> names = [.. symbols.Select(s => s.Name)];
        names.Sort(StringComparer.Ordinal);
        return $"volume={quoteData.MinimalVolume};coins={string.Join(",", names)}";
    }

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

        // One decision for the whole run, taken here rather than per minute: every quote coin has to
        // match, otherwise this run measures again and writes a fresh series. Deliberately blunt -
        // half a series from one coin list and half from another is the kind of mixture that reads
        // as a real measurement and is not one.
        reuse = perQuote.Count > 0 && perQuote.TrueForAll(q =>
            CandleDatabase.ReadMeta(exchange, MarkerKey(q.QuoteData.Name)) == MarkerFor(q.QuoteData, q.Symbols));

        if (reuse)
            GlobalData.AddTextToLogTab("Barometer: reading the series measured by an earlier run over the same coins");
        else
        {
            foreach ((CryptoQuoteData quoteData, List<CryptoSymbol> quoteSymbols) in perQuote)
                CandleDatabase.WriteMeta(exchange, MarkerKey(quoteData.Name), MarkerFor(quoteData, quoteSymbols));
            GlobalData.AddTextToLogTab("Barometer: measuring it for this coin list, later runs read it back");
        }
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
            {
                // Already measured by an earlier run over the same coins? Then read it back instead
                // of computing it again. The barometer is a function of the candles, the coin list
                // and the volume threshold, and none of those changed - runs 802 and 803 proved it
                // on their own numbers: same period, same 66 coins, and on all 39.365 shared moments
                // the values were identical to the cent.
                if (reuse && ReadCandles(quoteData.Name, interval, lastClosedMinute))
                    continue;

                BarometerTools.CalculateForSymbols(exchange, quoteData, symbols, interval, lastClosedMinute, MinimumSymbols, result);
                StoreCandles(quoteData.Name, interval);
            }
        }
    }


    /// <summary>
    /// Put the stored measurement of this minute back into the exchange data, so everything that
    /// reads the barometer - the trading conditions, and the five columns a signal records - sees
    /// exactly what the run that measured it saw. False when there is no candle for this minute,
    /// which puts the caller back on calculating.
    /// </summary>
    private bool ReadCandles(string quoteName, CryptoInterval interval, CandleTime at)
    {
        if (!exchange.TryGetSymbolByPair(Constants.SymbolNameBarometerPrice + quoteName, out CryptoSymbol? primary)
            || primary == null)
            return false;

        CryptoCandleList candles = primary.GetSymbolInterval(interval.IntervalPeriod).CandleList;
        CryptoCandle candle;
        lock (candles)
        {
            if (!candles.TryGetValue(at, out candle))
                return false;
        }

        CryptoCandle extra = default;
        bool hasExtra = false;
        if (exchange.TryGetSymbolByPair(Constants.SymbolNameBarometerExtra + quoteName, out CryptoSymbol? second) && second != null)
        {
            CryptoCandleList extraCandles = second.GetSymbolInterval(interval.IntervalPeriod).CandleList;
            lock (extraCandles)
                hasExtra = extraCandles.TryGetValue(at, out extra);
        }

        CryptoBarometerData data = exchange.Data.GetBarometer(quoteName, interval.IntervalPeriod);
        data.PriceDateTime = at;
        data.PriceBarometer = BarometerCandleFields.Read(candle, BarometerGraphValue.Average);
        data.PriceMedian = BarometerCandleFields.Read(candle, BarometerGraphValue.Median);
        data.PricePercentageRising = BarometerCandleFields.Read(candle, BarometerGraphValue.Rising);
        data.PriceSpread = BarometerCandleFields.Read(candle, BarometerGraphValue.Spread);
        data.PriceSymbolCount = (int)BarometerCandleFields.Read(candle, BarometerGraphValue.SymbolCount);
        if (hasExtra)
        {
            data.PriceMovement = BarometerCandleFields.Read(extra, BarometerGraphValue.Movement);
            data.PriceBitcoinVersusMarket = BarometerCandleFields.Read(extra, BarometerGraphValue.BitcoinVersusMarket);
            data.PriceOutlierCount = (int)extra.High;
        }
        return true;
    }


    /// <summary>
    /// Write one measurement into the candles of the two barometer symbols, exactly where the live
    /// scanner puts it - $BMP&lt;quote&gt; for the average, median, breadth, spread and coin count,
    /// $BMX&lt;quote&gt; for movement, bitcoin-against-the-market and the outlier count.
    /// <para>
    /// This replaced the BarometerSnapshot table on 04-09-2026. That table existed for one reason,
    /// spelled out in its own summary: "used to live only in the candles of the barometer symbols -
    /// WHICH THE EMULATOR DOES NOT WRITE". It does now, so the table was storing a second copy of
    /// something the candle format already holds - and it never shrank, because the purge before a
    /// run did not cover it. It reached 5.566.145 rows over 151 runs, most of the 1,27 GB the
    /// database had grown to, and the 1443 MB write-ahead log that made run 807 fail with
    /// "database is locked".
    /// </para>
    /// <para>
    /// Written at the heartbeat rather than per minute, which is what the snapshot did too. The live
    /// scanner writes a candle per minute because it draws a seven-hour graph; a seven-month replay
    /// on that rate would be 1,5 million candles for a figure that moves slowly on the intervals it
    /// is measured over. The series therefore has hourly spacing - it is read as points, not as
    /// candlesticks, the same way the graph already treats these symbols.
    /// </para>
    /// <para>
    /// There are no rows per position any more either. A position already carries the average per
    /// interval in Barometer15m..Barometer1d, filled by SignalCreate, and the rest of the
    /// measurement is a lookup in this series at the position's open time.
    /// </para>
    /// </summary>
    private void StoreCandles(string quoteName, CryptoInterval interval)
    {
        CryptoBarometerData data = exchange.Data.GetBarometer(quoteName, interval.IntervalPeriod);
        if (!data.PriceBarometer.HasValue || !data.PriceDateTime.HasValue)
            return; // nothing measured (yet) - a candle of zeroes would read as a flat market

        CandleTime at = data.PriceDateTime.Value;
        StoreOne(Constants.SymbolNameBarometerPrice + quoteName, interval, at, data, extra: false);
        StoreOne(Constants.SymbolNameBarometerExtra + quoteName, interval, at, data, extra: true);
    }


    private void StoreOne(string symbolName, CryptoInterval interval, CandleTime at, CryptoBarometerData data, bool extra)
    {
        if (!exchange.TryGetSymbolByPair(symbolName, out CryptoSymbol? symbol) || symbol == null)
            return;

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolInterval.CandleList;

        lock (candles)
        {
            if (!candles.TryGetValue(at, out CryptoCandle candle))
                candle = new CryptoCandle { OpenTime = at };

            // TickDecimals FIRST: CryptoCandle keeps its prices as integer ticks, so a value written
            // before the scale is set is rounded to whole numbers - a barometer of 0,89 would come
            // back as 1. Same reason Store() takes the candle by ref: it is a struct, and assigning
            // to a copy would store an all-zero candle.
            candle.TickDecimals = symbol.PriceDecimals;
            if (extra)
                BarometerCandleFields.StoreExtra(ref candle, data);
            else
                BarometerCandleFields.Store(ref candle, data);

            candles[at] = candle;

            if (at > symbolInterval.LastCandleSynchronized)
                symbolInterval.LastCandleSynchronized = at;
        }
    }


    /// <summary>The intervals being measured, for the line the run logs about itself.</summary>
    public string IntervalText => string.Join(", ", intervals.Select(i => i.Name));

    /// <summary>The number of coins the barometer of each quote coin rests on.</summary>
    public string SymbolCountText => string.Join(", ", perQuote.Select(q => $"{q.QuoteData.Name} {q.Symbols.Count}"));

    /// <summary>True when at least one quote coin has enough coins for a barometer that means anything.</summary>
    public bool HasEnoughSymbols => perQuote.Exists(q => q.Symbols.Count >= MinimumSymbols);
}
