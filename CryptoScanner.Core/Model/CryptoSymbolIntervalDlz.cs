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

    /// <summary>The closest zones, for the Distance column in the symbol grid.</summary>
    public CryptoZoneDistance ZoneDistance { get; } = new();


    /// <summary>
    /// The price range that decides WHETHER a recalculation is queued, kept apart from the swing
    /// values above on purpose.
    /// <para>
    /// The two used to share one pair of fields and pulled it in opposite directions. SignalPrepare
    /// widens this range on every candle that breaks out of it, so one move triggers one
    /// recalculation instead of one per candle. CalculatePivots then wrote the ZigZag swings back
    /// over that same pair, which sits INSIDE the widened range, so the next candle triggered again
    /// - and with no new pivot the swings were the same values as the round before. In the log that
    /// showed as a symbol starting nine hours in a row from identical bounds, 161 recalculations in
    /// eight hours of which 148 produced nothing at all.
    /// </para>
    /// <para>
    /// Split, each field does one job: the swings describe the trend, this range describes what has
    /// already been asked. It is re-seeded from the swings only when those actually moved, which is
    /// the one moment a recalculation can produce something new. See ApplySwingRange.
    /// </para>
    /// </summary>
    public decimal? TriggerRangeHigh { get; internal set; } = null;
    public decimal? TriggerRangeLow { get; internal set; } = null;

    // Automaticly calculate zones, recalculate if price is outside this range
    public CandleTime? TimeLastSwingPoint { get; set; }
    public decimal? LastSwingHigh { get; internal set; } = null;
    public decimal? LastSwingLow { get; internal set; } = null;

    /// <summary>
    /// Store the swing values a calculation just produced, and re-seed the trigger range when they
    /// moved. A null value means the indicator has no swing on that side yet and leaves the previous
    /// one standing, which is what the caller did before this method existed.
    /// <para>
    /// The second effect is deliberate and belongs here rather than at the call site: the rule is
    /// what keeps the two fields from drifting apart again, and a future writer of the swings would
    /// have to rediscover it.
    /// </para>
    /// </summary>
    public void ApplySwingRange(decimal? swingLow, decimal? swingHigh)
    {
        bool moved = (swingLow != null && swingLow != LastSwingLow)
                  || (swingHigh != null && swingHigh != LastSwingHigh);

        if (swingLow != null)
            LastSwingLow = swingLow;
        if (swingHigh != null)
            LastSwingHigh = swingHigh;

        // Unchanged swings mean this calculation stood still, so the widened range has to stay:
        // resetting it here is exactly what made the same symbol retrigger every hour.
        if (moved)
        {
            TriggerRangeLow = LastSwingLow;
            TriggerRangeHigh = LastSwingHigh;
        }
    }

    public void Reset()
    {
        LastSwingLow = null;
        LastSwingHigh = null;
        TriggerRangeLow = null;
        TriggerRangeHigh = null;
        TimeLastSwingPoint = null;
    }

}
