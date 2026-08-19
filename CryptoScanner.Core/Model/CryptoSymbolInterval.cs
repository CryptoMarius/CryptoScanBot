using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Indicators;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Model;

public class CryptoSymbolInterval
{
    // Administration (not really needed but it is what is is)
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last synchronized candle with the exchange (without gaps)
    public CandleTime? LastCandleSynchronized { get; set; }

    // "Already asked the exchange for this period". The zone engine needs the deep history and
    // decides whether it still has to fetch by walking the series and looking for the first missing
    // candle. On an exchange that only produces a candle for a minute in which something was traded
    // that minute never arrives, so the walk finds the same hole every hour and downloads the same
    // history again - measured on Bitvavo Spot 19-08-2026: 255 requests, 203,471 candles, 7% of them
    // new. What the candles cannot say, this pair says: the period below was requested at least once,
    // so a candle missing inside it is missing at the exchange as well.
    //
    // ONE uninterrupted period, never a set of loose pieces: two zoom fetches around different pivots
    // must not add up to a claim about the gap between them.
    //
    // Deliberately NOT persisted. After a restart it is empty and the whole history is checked once
    // more, which is the escape hatch if it is ever wrong. Whoever removes candles from the store
    // shortens it - see CandleDatabase.CleanCandlesForSymbol.
    public CandleTime? HistoryAskedFrom { get; private set; }
    public CandleTime? HistoryAskedTo { get; private set; }


    /// <summary>
    /// True when this whole period was requested from the exchange before, so nothing can be gained
    /// by asking again: whatever is missing inside it does not exist at the exchange either.
    /// </summary>
    public bool HistoryWasAsked(CandleTime from, CandleTime to)
    {
        if (!HistoryAskedFrom.HasValue || !HistoryAskedTo.HasValue)
            return false;
        return from >= HistoryAskedFrom.Value && to <= HistoryAskedTo.Value;
    }


    /// <summary>
    /// The moment from which a search still has to look. Everything between <paramref name="from"/>
    /// and the returned moment was requested before, so examining it again cannot turn up anything.
    /// Returns <paramref name="from"/> unchanged when nothing is remembered about it.
    /// <para>
    /// This is what makes the note usable hour after hour: the period the zone engine asks for slides
    /// forward with the clock, so it never fits inside the note completely. Only its tail is new.
    /// </para>
    /// </summary>
    public CandleTime SkipHistoryAlreadyAsked(CandleTime from)
    {
        if (!HistoryAskedFrom.HasValue || !HistoryAskedTo.HasValue)
            return from;
        if (from < HistoryAskedFrom.Value || from > HistoryAskedTo.Value)
            return from;
        return HistoryAskedTo.Value;
    }


    /// <summary>
    /// Remember that [from..to] was requested from the exchange. Only merged with what is already
    /// remembered when the two touch or overlap; a period that does not connect REPLACES it, because
    /// the stretch in between was never requested and the pair may only describe one uninterrupted
    /// period. The newest one wins - that is the one the zone engine keeps coming back for.
    /// </summary>
    public void RememberHistoryAsked(CandleTime from, CandleTime to)
    {
        if (to < from)
            return;

        if (HistoryAskedFrom.HasValue && HistoryAskedTo.HasValue
            && from <= HistoryAskedTo.Value && to >= HistoryAskedFrom.Value)
        {
            if (HistoryAskedFrom.Value < from)
                from = HistoryAskedFrom.Value;
            if (HistoryAskedTo.Value > to)
                to = HistoryAskedTo.Value;
        }

        HistoryAskedFrom = from;
        HistoryAskedTo = to;
    }


    /// <summary>
    /// Candles up to and including <paramref name="newestRemoved"/> were removed from the store. The
    /// part below it is unknown again, so the remembered period resumes after it and the zone engine
    /// will fetch the older part again - which is exactly what has to happen once the candles are
    /// gone. The remembered period can only ever get shorter this way, never longer, so "never fetched
    /// again" cannot happen.
    /// </summary>
    public void ForgetHistoryUpTo(CandleTime newestRemoved)
    {
        if (!HistoryAskedFrom.HasValue || !HistoryAskedTo.HasValue)
            return;

        CandleTime resumeFrom = newestRemoved + Interval.Duration;
        if (resumeFrom >= HistoryAskedTo.Value)
        {
            ForgetHistory();
            return;
        }

        if (resumeFrom > HistoryAskedFrom.Value)
            HistoryAskedFrom = resumeFrom;
    }


    /// <summary>Nothing is known about what was asked any more (the candles were thrown away).</summary>
    public void ForgetHistory()
    {
        HistoryAskedFrom = null;
        HistoryAskedTo = null;
    }

    // The candles for this interval
    public CryptoCandleList CandleList { get; set; } = [];

    // The signals generated for this interval
    public List<CryptoSignal> SignalList { get; set; } = [];

    // All the calculated zones
    public CryptoSymbolIntervalZones DlzZones { get; internal set; } = new();
    public CryptoSymbolIntervalZones FvgZones { get; internal set; } = new();

    // SMC (Smart Money Concepts) Order Blocks — in-memory only for now (no DB persistence),
    // rebuilt by ZoneSmc.Detect on demand. Long  = bullish OB (demand) below a swing low;
    // Short = bearish OB (supply) above a swing high.
    public List<CryptoZone> SmcZones { get; internal set; } = [];

    // Incremental zone-calculation cursors: the candle time up to (and including) which the
    // zone scan has already run. Null means "never run, do a full historical scan". On every
    // later call only candles after this point need to be scanned — see ZoneFvg/ZoneSmc.
    public CandleTime? DlzLastProcessedTime { get; set; }
    public CandleTime? FvgLastProcessedTime { get; set; }
    public CandleTime? SmcLastProcessedTime { get; set; }
    // The AverageWindow/BaseMaxCandles settings the SMC cursor above was built with. If the user
    // changes these mid-run/session, the cached cursor is no longer valid and a full rescan is forced.
    public int SmcCachedAverageWindow { get; set; } = -1;
    public int SmcCachedBaseMaxCandles { get; set; } = -1;

    // Zone administration (calculation and distances)
    public CryptoSymbolIntervalZoneCalc DlzAdmin { get; internal set; } = new();

    // For display in the symbol grid
    // These are the closest DLZ zones (calculated from all the zones)
    public CryptoZoneDistance DlzZoneDistance { get; } = new();

    // Primary and Secondary trend data (Dow Theory interpretation)
    public CryptoTrendData TrendPrimary = new();
    public CryptoTrendData TrendSecondary = new();

    // BOS/CHoCH trend data (Break of Structure / Change of Character).
    // Kept separate from TrendPrimary/Secondary because TrendIntervalBos and TrendInterval (Dow)
    // both write to the same fields (Trend, PrevTrend, LastPivot, …) and would overwrite each other.
    // Same ZigZag source data, different interpretation rules.
    public CryptoTrendData TrendBosPrimary = new();
    public CryptoTrendData TrendBosSecondary = new();

    // Cached ZigZag indicators, keyed by (TrendType, UseHighLow), shared across both trend
    // calculation (TrendCalculator) and zone calculation (ZoneDlz). Candles are fed once
    // incrementally instead of being replayed from scratch (see ZigZagIndicator.LastFedCandleTime).
    public TrendZigZagIndicatorList ZigZagIndicators { get; } = [];

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
        TrendBosPrimary.Reset();
        TrendBosSecondary.Reset();
        ZigZagIndicators.Clear();
    }

    // **** experiment ****

    // For the new QuoteHub from Dave Skender — incremental indicator state (see IntervalIndicatorHub).
    public IntervalIndicatorHub? IndicatorHub = null;
    public CandleTime? IndicatorHubLastAdded = null;
    public int IndicatorHubAddCount = 0;
    public SortedDictionary<CandleTime, CryptoData> Data = [];

    // Band width and excursion statistics — how much room there is between the Bollinger bands and
    // what the price does after touching one (see BandRangeTracker). Rebuilt together with the
    // indicator hub, but from a longer window than the hub's warm-up uses.
    public BandRangeTracker? BandRange = null;


    public bool TryGetCandle(CandleTime time, out MyData? myData)
    {
        if (CandleList.TryGetValue(time, out CryptoCandle candle) &&
            Data.TryGetValue(time, out CryptoData? indicator))
        {
            myData = new()
            {
                Candle = candle!,
                CandleData = indicator!
            };
            return true;
        }
        else
        {
            myData = null;
            return false;
        }
    }
}