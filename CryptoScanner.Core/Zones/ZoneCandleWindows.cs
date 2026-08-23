using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// What one zone calculation has already pulled into memory, per interval: which candle windows were
/// read from candles.db, and whether that interval still has to be written back.
/// <para>
/// This replaces the SortedList&lt;CryptoIntervalPeriod, bool&gt; the zone engine used to pass around,
/// where the presence of a key meant "the whole series was read" and its value meant "and it still
/// has to be saved" - two answers on one field. The first of those was the expensive one: a key was
/// added the first time an interval was touched, so <see cref="ZoneCandleEngine.FetchFrom"/> read the
/// COMPLETE series for that interval while the caller had asked for a window of 60 candles, and did so
/// again on the next recalculation because this list is built fresh per call. Measured on emulator run
/// 229 (23-08-2026): a DLZ recalculation zooming down to 1m read ~744,000 rows to look at ~400 of them,
/// and because the query is bounded by the clock the cost grew with the position in the run -
/// 187 ms per recalculation in the first week against 1,014 ms in the last.
/// </para>
/// <para>
/// Remembering WINDOWS instead of a flag keeps the answer honest: a window that was read is not read
/// again, and one that was not is fetched on its own instead of dragging the whole history with it.
/// The zoom windows of separate pivots do not touch, so they stay separate ranges - two windows never
/// add up to a claim about the gap between them.
/// </para>
/// </summary>
public class ZoneCandleWindows
{
    private sealed class IntervalState
    {
        /// <summary>The candle windows established as present in the in-memory CandleList.</summary>
        public HistoryAskedRanges Read { get; } = new();

        /// <summary>True while this interval holds candles that are not in candles.db yet.</summary>
        public bool Changed { get; set; }
    }

    private readonly SortedList<CryptoIntervalPeriod, IntervalState> perInterval = [];


    public ZoneCandleWindows()
    {
    }


    /// <summary>
    /// An independent copy of <paramref name="other"/>: it starts from the same knowledge, but what
    /// one of the two learns afterwards does not reach the other. For a caller that wants to run
    /// several calculations from the same starting point.
    /// </summary>
    public ZoneCandleWindows(ZoneCandleWindows other)
    {
        lock (other.perInterval)
        {
            foreach (var entry in other.perInterval)
            {
                IntervalState copy = GetOrAdd(entry.Key);
                copy.Changed = entry.Value.Changed;
                foreach ((CandleTime from, CandleTime to) in entry.Value.Read.ToList())
                    copy.Read.Remember(from, to);
            }
        }
    }


    /// <summary>True when this interval was touched at all during this calculation.</summary>
    public bool Contains(CryptoIntervalPeriod period)
    {
        lock (perInterval)
            return perInterval.ContainsKey(period);
    }


    /// <summary>
    /// True when [from..to] was established as available earlier in this calculation, so going to
    /// candles.db for it again cannot add anything.
    /// </summary>
    public bool IsLoaded(CryptoIntervalPeriod period, CandleTime from, CandleTime to)
    {
        lock (perInterval)
            return perInterval.TryGetValue(period, out IntervalState? state) && state.Read.WasAsked(from, to);
    }


    /// <summary>Remember that [from..to] is available in memory for this interval.</summary>
    public void MarkLoaded(CryptoIntervalPeriod period, CandleTime from, CandleTime to)
    {
        lock (perInterval)
            GetOrAdd(period).Read.Remember(from, to);
    }


    /// <summary>
    /// Everything this interval can be asked for is already in the in-memory CandleList, so no window
    /// of it ever has to be read from disk during this calculation. For the places where the candles
    /// got there by another route than a disk read - built from a lower interval by
    /// BulkCalculateCandles, put there by the emulator's pre-flight fetch, or by a test.
    /// </summary>
    public void MarkAllLoaded(CryptoIntervalPeriod period)
    {
        lock (perInterval)
            GetOrAdd(period).Read.Remember(CandleTime.MinValue, CandleTime.MaxValue);
    }


    /// <summary>This interval holds candles that still have to be written to candles.db.</summary>
    public void MarkChanged(CryptoIntervalPeriod period)
    {
        lock (perInterval)
            GetOrAdd(period).Changed = true;
    }


    /// <summary>This interval was written to candles.db, so there is nothing left to save.</summary>
    public void MarkSaved(CryptoIntervalPeriod period)
    {
        lock (perInterval)
        {
            if (perInterval.TryGetValue(period, out IntervalState? state))
                state.Changed = false;
        }
    }


    /// <summary>True when this interval still has to be written to candles.db.</summary>
    public bool HasUnsavedChanges(CryptoIntervalPeriod period)
    {
        lock (perInterval)
            return perInterval.TryGetValue(period, out IntervalState? state) && state.Changed;
    }


    /// <summary>Nothing was read and nothing is pending any more.</summary>
    public void Clear()
    {
        lock (perInterval)
            perInterval.Clear();
    }


    private IntervalState GetOrAdd(CryptoIntervalPeriod period)
    {
        if (!perInterval.TryGetValue(period, out IntervalState? state))
        {
            state = new IntervalState();
            perInterval[period] = state;
        }
        return state;
    }
}
