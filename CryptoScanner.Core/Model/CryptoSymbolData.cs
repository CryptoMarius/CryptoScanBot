using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;


public class CryptoSymbolData
{
    /// <summary>
    /// Trend
    /// </summary>
    // Lock for trend data
    public SemaphoreSlim TrendLock { get; set; } = new(1, 1);

    // Primary and Secondary trend data (Dow Theory interpretation)
    public CryptoTrendData TrendPrimary = new();
    public CryptoTrendData TrendSecondary = new();
    // BOS/CHoCH trend data (Break of Structure / Change of Character) — separate slot,
    // see CryptoSymbolInterval for the rationale.
    public CryptoTrendData TrendBosPrimary = new();
    public CryptoTrendData TrendBosSecondary = new();


    /// <summary>
    /// Candles
    /// </summary>
    // Lock for manipulates candles
    public SemaphoreSlim CandleLock { get; set; } = new(1, 1);
    // Interval related data like candles, last candle fetched, zones
    public List<CryptoSymbolInterval> SymbolIntervalList { get; set; } = [];

    /// <summary>
    /// Set by <c>SymbolBase.IsSymbolAccepted</c> when the exchange reports a different instrument id
    /// than the one the stored candles were fetched with. The symbols are refreshed BEFORE the candles
    /// are loaded at startup, so without this flag <c>CandleDatabase.LoadSymbolIntervals</c> would
    /// restore the old LastSync and undo the detection. Cleared as soon as that load has honoured it.
    /// </summary>
    public bool InstrumentChanged { get; set; }


    /// <summary>
    /// DlzAdmin
    /// </summary>
    // Guards zone list writes: CalculateZones holds it exclusively (WaitAsync),
    // ScanForNew tries non-blocking (Wait(0)) and skips if unavailable,
    // preventing concurrent OrderedList corruption.
    public SemaphoreSlim ZoneLock { get; } = new(1, 1);

    // Guards ZoneDlz.LoadZonesForSymbol so it only re-reads the DB and resets the in-memory
    // FVG/DLZ/SMC lists once per (symbol, run scope) instead of on every zone-queue drain. The
    // in-memory zones are already kept current via the incremental calculation + ThreadSaveObjects
    // queue, so reloading from the DB on every tick was pure redundant I/O and also defeated any
    // incremental zone calculation (it wiped the per-call cursors every time). null = live scope.
    public bool ZonesLoaded { get; set; }
    public int? ZonesLoadedRunId { get; set; }

    // For display in the symbol grid
    // These are the closest DLZ zones (calculated from all the zones)
    // The closest dlz zones (calculated from all the active interval zones)
    // Display only (an initial hidden column in the symbol grid)
    public CryptoZoneDistance DlzZoneDistance { get; } = new();


    public CryptoSymbolData()
    {
        SymbolIntervalList = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = new()
            {
                Interval = interval,
                IntervalPeriod = interval.IntervalPeriod,
            };
            SymbolIntervalList.Add(symbolInterval);
        }
    }


    public CryptoSymbolInterval Get(CryptoIntervalPeriod intervalPeriod)
    {
        return SymbolIntervalList[(int)intervalPeriod];
    }

    public CryptoSymbolInterval Get(CryptoInterval interval)
    {
        return SymbolIntervalList[(int)interval.IntervalPeriod];
    }

    // NOTE: these three Reset*Data methods are called from ZoneDlz.LoadZonesForSymbol/LoadAllZones,
    // which runs every time a chart window opens/changes symbol (to scope the in-memory zones to the
    // viewed run) — NOT just on a genuine fresh start. They deliberately do NOT touch the incremental
    // cursors (DlzLastProcessedTime/FvgLastProcessedTime/SmcLastProcessedTime) below: LoadZonesForSymbol always
    // immediately repopulates the cleared lists from the DB in the same call, so the zone contents
    // stay correct either way — but nulling the cursors here would force the *live engine* (sharing
    // the same CryptoSymbolInterval objects) into a full historical rescan on its next tick just
    // because someone opened a chart on that symbol. Use ResetZoneCalculationCursors() for an actual
    // forced recalculation (run start, or the chart's "Calculate" force-recalc button).
    public void ResetFvgData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.FvgZones.Reset();
        }
    }

    public void ResetDlzData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.DlzZones.Reset();
        }
    }

    public void ResetSmcData()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.SmcZones.Clear();
        }
    }

    /// <summary>
    /// Forces every incremental zone-calculation cursor (DLZ/FVG/SMC) back to "never run" so the
    /// next call does a full historical rescan instead of continuing from where it left off. Call
    /// this for a genuine forced recalculation: a fresh emulator run
    /// (<see cref="Emulator.Engine.EmulatorDb.ClearZonesForSymbols"/>) or the chart's "Calculate"
    /// force-recalc button — NOT from the routine Reset*Data methods above, which run on every
    /// chart open/symbol switch and must stay cheap for the live engine.
    /// </summary>
    public void ResetZoneCalculationCursors()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.DlzLastProcessedTime = null;
            symbolInterval.FvgLastProcessedTime = null;
            symbolInterval.SmcLastProcessedTime = null;
        }
    }

    public void ResetTrendData()
    {
        TrendPrimary.Reset();
        TrendSecondary.Reset();
        TrendBosPrimary.Reset();
        TrendBosSecondary.Reset();
    }

    // Full reset including the per-interval trend state and cached ZigZag indicators
    // (CryptoSymbolInterval.ResetTrendData — TrendPrimary/Secondary/TrendBosPrimary/Secondary live
    // there too, separate from the symbol-level aggregate above). Deliberately NOT folded into
    // ResetTrendData() itself: that method is also called from ZoneDlz.LoadZonesForSymbol, which runs
    // on every zone-queue drain (hot path) — cascading a full per-interval ZigZag-cache wipe into that
    // call would defeat the trend cache almost as fast as it warms up. Use this version only for genuine
    // fresh-start scenarios: emulator run start, and candle history (re)load (where CandleList objects
    // are replaced, so any cached ZigZagResult.Candle references would otherwise go stale).
    public void ResetTrendDataAndCaches()
    {
        ResetTrendData();
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
            symbolInterval.ResetTrendData();
    }

}
