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

    // The candles for this interval
    public CryptoCandle LastCandle { get; set; } = default;
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

    // Cached ZigZag indicators, keyed by (TrendType, UseHighLow), shared across repeated calls to
    // TrendCalculator.CalculateBothAsync so candles are fed once instead of being replayed from
    // scratch on every stale-interval check (see ZigZagIndicator.LastFedCandleTime).
    public TrendZigZagIndicatorList ZigZagIndicators { get; } = [];

    // Cached ZigZag indicators used by ZoneDlz, keyed the same way as ZigZagIndicators above but
    // kept in a separate dictionary because DLZ's candle window (Settings.Signal.ZonesDlz.CandleCount)
    // can be larger than the trend window — sharing one cache entry between the two would either
    // shortchange DLZ's depth or force the trend calculation to carry history it doesn't need.
    public TrendZigZagIndicatorList DlzZigZagIndicators { get; } = [];

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
        TrendBosPrimary.Reset();
        TrendBosSecondary.Reset();
        ZigZagIndicators.Clear();
        DlzZigZagIndicators.Clear();
    }

    // **** experiment ****

    // For the new QuoteHub from Dave Skender — incremental indicator state (see IntervalIndicatorHub).
    public IntervalIndicatorHub? IndicatorHub = null;
    public CandleTime? IndicatorHubLastAdded = null;
    public SortedDictionary<CandleTime, CryptoData> Data = [];


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