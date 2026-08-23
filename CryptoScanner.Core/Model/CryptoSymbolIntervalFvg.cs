namespace CryptoScanner.Core.Model;

/// <summary>
/// Everything the FVG (Fair Value Gap) calculation keeps per (symbol, interval), gathered in one
/// place. Same shape as <see cref="CryptoSymbolIntervalDlz"/>, for the same reason: these used to
/// sit loose on <see cref="CryptoSymbolInterval"/> between the candles, the signals and the trend
/// data, where the only thing tying them together was the Fvg prefix on their names.
/// </summary>
public class CryptoSymbolIntervalFvg
{
    /// <summary>All calculated zones for this interval, open and closed, per side.</summary>
    public CryptoSymbolIntervalZones Zones { get; internal set; } = new();

    /// <summary>
    /// Marks the candle up to and including which the zone scan has already run. Null means
    /// "never run, do a full historical scan"; on every later call only the candles after this
    /// point have to be scanned. See ZoneFvg.
    /// </summary>
    public CandleTime? ProcessedCandleMarker { get; set; }


    /// <summary>
    /// Empties the zone lists without touching the cursor. Deliberately separate from
    /// <see cref="ResetCursor"/>: this one runs on every chart open/symbol switch, where the
    /// lists are immediately refilled from the database in the same call, and nulling the cursor
    /// there would force the live engine into a full historical rescan. See the remarks on
    /// CryptoSymbolData.ResetFvgData.
    /// </summary>
    public void ResetZones()
    {
        Zones.Reset();
    }


    /// <summary>
    /// Puts the incremental cursor back to "never run", so the next call does a full historical
    /// scan. For a genuine forced recalculation only - see CryptoSymbolData.ResetZoneCalculationCursors.
    /// </summary>
    public void ResetCursor()
    {
        ProcessedCandleMarker = null;
    }
}
