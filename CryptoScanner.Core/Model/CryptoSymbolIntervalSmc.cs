namespace CryptoScanner.Core.Model;

/// <summary>
/// Everything the SMC (Smart Money Concepts) calculation keeps per (symbol, interval), gathered in
/// one place. Same shape as <see cref="CryptoSymbolIntervalDlz"/>, for the same reason: these used
/// to sit loose on <see cref="CryptoSymbolInterval"/> between the candles, the signals and the
/// trend data, where the only thing tying them together was the Smc prefix on their names.
/// </summary>
public class CryptoSymbolIntervalSmc
{
    /// <summary>
    /// The order blocks for this interval - in-memory only for now (no DB persistence), rebuilt by
    /// ZoneSmc.Detect on demand. Long = bullish OB (demand) below a swing low; Short = bearish OB
    /// (supply) above a swing high. A flat list, unlike DLZ and FVG which split on side and state.
    /// </summary>
    public List<CryptoZone> Zones { get; internal set; } = [];

    /// <summary>
    /// Marks the candle up to and including which the zone scan has already run. Null means
    /// "never run, do a full historical scan"; on every later call only the candles after this
    /// point have to be scanned. See ZoneSmc.
    /// </summary>
    public CandleTime? ProcessedCandleMarker { get; set; }

    /// <summary>
    /// The AverageWindow/BaseMaxCandles settings the cursor above was built with. If the user
    /// changes these mid-run/session, the cached cursor is no longer valid and a full rescan is forced.
    /// </summary>
    public int CachedAverageWindow { get; set; } = -1;
    public int CachedBaseMaxCandles { get; set; } = -1;


    /// <summary>
    /// Empties the zone list without touching the cursor. Deliberately separate from
    /// <see cref="ResetCursor"/>: this one runs on every chart open/symbol switch, where the list
    /// is immediately refilled from the database in the same call, and nulling the cursor there
    /// would force the live engine into a full historical rescan. See the remarks on
    /// CryptoSymbolData.ResetSmcData.
    /// </summary>
    public void ResetZones()
    {
        Zones.Clear();
    }


    /// <summary>
    /// Puts the incremental cursor back to "never run", so the next call does a full historical
    /// scan. For a genuine forced recalculation only - see CryptoSymbolData.ResetZoneCalculationCursors.
    /// </summary>
    public void ResetCursor()
    {
        ProcessedCandleMarker = null;
    }


    /// <summary>
    /// Forgets which settings the cursor was built with, so the next Detect cannot mistake a stale
    /// cursor for a valid one. Kept next to the values themselves because ZoneSmc writes them as a
    /// pair and the emulator clears them as a pair - two call sites that used to be able to drift.
    /// </summary>
    public void ResetParameterCache()
    {
        CachedAverageWindow = -1;
        CachedBaseMaxCandles = -1;
    }
}
