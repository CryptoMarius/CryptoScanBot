using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Everything the DLZ calculation keeps per (symbol, interval), gathered in one place.
/// <para>
/// These used to sit loose on <see cref="CryptoSymbolInterval"/> between the candles, the signals
/// and the trend data, where it was no longer obvious which field belonged to which mechanism -
/// and DLZ has the most of them by a distance. Same shape as
/// <see cref="CryptoSymbolIntervalZoneCalc"/> already had for the trigger range, one level up.
/// </para>
/// <para>
/// The two markers below are NOT interchangeable and the difference is the reason the zones once
/// depended on how often the caller happened to ask. See ZoneDlzIncrementalTests.
/// </para>
/// </summary>
public class CryptoSymbolIntervalDlz
{
    /// <summary>All calculated zones for this interval, open and closed, per side.</summary>
    public CryptoSymbolIntervalZones Zones { get; internal set; } = new();

    /// <summary>
    /// Marks the candle up to and including which the broken-zone scan has walked. Null means
    /// "never run, do a full historical scan". This one is about CANDLES, and it exists because
    /// that scan is not idempotent: TouchCount counts up, so replaying a candle would count its
    /// touch twice.
    /// </summary>
    public CandleTime? ProcessedCandleMarker { get; set; }

    /// <summary>
    /// Marks the confirming pivot up to which the dominance verdicts are FINAL. This one is about
    /// PIVOTS, and a verdict only becomes final once the pivot carrying it has left the ZigZag's
    /// mutable tail (<see cref="ZigZagIndicator.SettledCount"/>).
    /// <para>
    /// A candle marker cannot express "this triple has been judged", because the pivot list keeps
    /// changing at its right edge: the pivot that confirms a triple today need not be the one that
    /// confirms it tomorrow. That is why there are two.
    /// </para>
    /// </summary>
    public CandleTime? CommittedPivotMarker { get; set; }

    /// <summary>
    /// The zones that came out of settled verdicts, kept so they never have to be recomputed. The
    /// mutable tail is deliberately NOT in here: a verdict about a pivot that can still move is not
    /// something to remember.
    /// </summary>
    public List<CryptoZone> CommittedZones { get; set; } = [];

    /// <summary>
    /// The price range that decides WHETHER a recalculation is queued at all (SignalPrepare): the
    /// last confirmed swing low and high. Price leaving that range is the same event as a pivot
    /// becoming dominant, which is why it works as the trigger.
    /// </summary>
    public CryptoSymbolIntervalZoneCalc Admin { get; internal set; } = new();

    /// <summary>The closest zones, for the Distance column in the symbol grid.</summary>
    public CryptoZoneDistance ZoneDistance { get; } = new();
}
