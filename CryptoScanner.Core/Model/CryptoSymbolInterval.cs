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

    // The periods that were already requested from the exchange for this interval. The zone engine
    // asks this before it starts looking for missing candles, because on an exchange that skips a
    // minute without trades the candles can never answer "complete" and the same history would be
    // downloaded on every recalculation. See HistoryAskedRanges for the reasoning and the numbers.
    public HistoryAskedRanges HistoryAsked { get; } = new();


    /// <summary>True when this whole period was requested from the exchange before.</summary>
    public bool HistoryWasAsked(CandleTime from, CandleTime to) => HistoryAsked.WasAsked(from, to);


    /// <summary>Where a search still has to start, given what was requested before.</summary>
    public CandleTime SkipHistoryAlreadyAsked(CandleTime from) => HistoryAsked.SkipAsked(from);


    /// <summary>Remember that [from..to] was requested from the exchange.</summary>
    public void RememberHistoryAsked(CandleTime from, CandleTime to) => HistoryAsked.Remember(from, to);


    /// <summary>
    /// Candles up to and including <paramref name="newestRemoved"/> were removed from the store, so
    /// that part has to be fetched again when it is needed.
    /// </summary>
    public void ForgetHistoryUpTo(CandleTime newestRemoved) => HistoryAsked.ForgetUpTo(newestRemoved, Interval.Duration);


    /// <summary>Nothing is known about what was asked any more (the candles were thrown away).</summary>
    public void ForgetHistory() => HistoryAsked.Clear();

    // The candles for this interval
    public CryptoCandleList CandleList { get; set; } = [];

    // The signals generated for this interval
    public List<CryptoSignal> SignalList { get; set; } = [];

    // Everything DLZ keeps for this interval - zones, both markers, the committed store, the
    // trigger range and the distances. See CryptoSymbolIntervalDlz for why there are two markers.
    public CryptoSymbolIntervalDlz Dlz { get; } = new();

    // Everything FVG keeps for this interval - the calculated zones and its incremental marker.
    public CryptoSymbolIntervalFvg Fvg { get; } = new();

    // Everything SMC (Smart Money Concepts) keeps for this interval - the order blocks, its
    // incremental marker and the settings that marker was built with.
    public CryptoSymbolIntervalSmc Smc { get; } = new();

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

    // The Lux value belongs to a 5m candle, so it only changes when a new 5m candle closes. These
    // two remember which one was computed last and what came out, so the candles in between reuse it
    // instead of recomputing the same number. See IndicatorData.ApplyLux for what that was costing.
    public CandleTime? LuxCachedFor = null;
    public short? LuxCachedValue = null;
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