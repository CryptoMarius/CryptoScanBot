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
            symbolInterval.Dlz.Zones.Reset();
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
    /// (<see cref="Emulator.Engine.EmulatorDb.ClearZonesForSymbols"/>), the chart's "Calculate"
    /// force-recalc button, or a candle catch-up that filled a gap the socket missed
    /// (<see cref="Exchange.CandleBase.GetCandlesForAllIntervalsAsync"/>) — NOT from the routine
    /// Reset*Data methods above, which run on every chart open/symbol switch and must stay cheap
    /// for the live engine.
    /// <para>
    /// DlzAdmin goes with them. That is the price range deciding WHETHER a recalculation is queued
    /// at all (SignalPrepare), so leaving it filled means the forced rescan still waits for price to
    /// leave the range it held before — which after an outage it may never do, because price can
    /// have come back inside that range while we were not looking.
    /// </para>
    /// </summary>
    public void ResetZoneCalculationCursors()
    {
        foreach (CryptoSymbolInterval symbolInterval in SymbolIntervalList)
        {
            symbolInterval.Dlz.ProcessedCandleMarker = null;
            symbolInterval.FvgLastProcessedTime = null;
            symbolInterval.SmcLastProcessedTime = null;
            // The committed verdicts go with them. They are only meaningful against the pivot list
            // they were drawn from, and a reset is exactly the moment that list is rebuilt.
            symbolInterval.Dlz.CommittedPivotMarker = null;
            symbolInterval.Dlz.CommittedZones = [];
            symbolInterval.Dlz.Admin.Reset();
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
