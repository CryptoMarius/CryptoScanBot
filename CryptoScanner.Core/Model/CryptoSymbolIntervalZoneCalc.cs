namespace CryptoScanner.Core.Model;

public class CryptoSymbolIntervalZoneCalc
{
    // Automaticly calculate zones
    // Based on the primary trend, recalculate if price is outside this range
    public CandleTime? TimeLastSwingPoint { get; set; }
    public decimal? LastSwingHigh { get; internal set; } = null;
    public decimal? LastSwingLow { get; internal set; } = null;

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
