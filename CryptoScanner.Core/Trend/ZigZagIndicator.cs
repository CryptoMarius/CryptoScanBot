using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

/// <summary>
/// The cached ZigZag indicators of one symbol+interval, keyed by (TrendType, UseHighLow).
/// <para>
/// Concurrent, not a plain Dictionary, because four different places touch this one instance and they
/// do not share a lock: ZoneDlz.CalculatePivots and CandleTools.CleanCandleDataAsync read it under
/// symbol.Data.CandleLock, TrendCalculator.CalculateBothAsync ADDS to it without that lock, and
/// CryptoSymbolInterval.ResetTrendData clears it from the candle loading paths. On the night of
/// 17/18-08-2026 that threw "Collection was modified; enumeration operation may not execute" five
/// times on three exchanges, each time silently aborting the zone calculation for that symbol.
/// </para>
/// <para>
/// Putting them all under CandleLock is not an option as it stands: TrendTools.AddCandlesToIndicatorsAsync
/// takes that same semaphore, and SemaphoreSlim is not re-entrant, so the writer would deadlock against
/// itself. A concurrent dictionary needs no lock discipline at all - its enumerator walks a snapshot,
/// so an entry added or removed halfway through simply does not throw.
/// </para>
/// </summary>
public class TrendZigZagIndicatorList : System.Collections.Concurrent.ConcurrentDictionary<(TrendType trendType, bool useHighLow), ZigZagIndicator>;

public class ZigZagIndicator
{
    private bool UseHighLow { get; set; } = false; // Use High/Low or Open/Close
    private TrendType TrendType { get; set; } = TrendType.Primary; // see more than primary trend

    public bool UseOptimizing { get; set; } = true; // Debug
    //public CandleTime MaxTime { get; set; } = CandleTime.MinValue; // Debug
    public int CandleCount { get; set; } = 0; // Debug, count of candles added

    //public int Depth { get; set; } = 12; // from previous approach, but does not work
    public double Deviation { get; set; } // Optimizing (does not work for now)
    //public int BackStep { get; set; } = 3; // from previous approach, but does not work

    private readonly List<ZigZagResult> AddedDummyZigZag = []; // collected points for recreating a low/high after a BOS formed

    public List<ZigZagResult> PivotList = []; // All "raw" low and high pivot points (for determining high/low)
    public List<ZigZagResult> ZigZagList { get; set; } = []; // The resulting zigzag points

    public ZigZagResult? LastSwingLow = null; // the last Low Primary
    public ZigZagResult? LastSwingHigh = null; // the last High Primary
    public ZigZagResult? LastSwingPoint = null; // the last Primary added

    // Marks the last candle fed into this instance. Lets a caller that caches this indicator across
    // calls (e.g. TrendCalculator, ZoneDlz) feed only the candles since the last call instead of
    // rebuilding from scratch every time — Calculate() is already incremental per candle, so resuming
    // is safe as long as candles are fed in order with no gap.
    public CandleTime? LastFedCandleTime { get; set; }

    private readonly ZigZagLanceBeggs ZigZagLanceBeggs;


    public ZigZagIndicator(TrendType trendType, bool useHighLow, double deviation = 1.0)
    {
        TrendType = trendType;
        UseHighLow = useHighLow;
        Deviation = deviation;
        ZigZagLanceBeggs = new(UseHighLow);
    }

    private double GetLowValue(CryptoCandle candle) => (double)candle.GetLowValue(UseHighLow);
    private double GetHighValue(CryptoCandle candle) => (double)candle.GetHighValue(UseHighLow);

    private bool GetLowFromBuffer(int minIndex, int maxIndex, out ZigZagResult? swing)
    {
        swing = null;
        if (TrendType == TrendType.Secondary)
            return false;
        for (int i = minIndex; i <= maxIndex; i++)
        {
            var zigZag = PivotList[i];
            //if (MaxTime == CandleTime.MinValue || (MaxTime > CandleTime.MinValue && zigZag.Candle.OpenTime < MaxTime))
            {
                if (swing == null || GetLowValue(zigZag.Candle) < GetLowValue(swing.Candle))
                    swing = zigZag;
            }
        }
        return swing != null;
    }


    private bool GetHighFromBuffer(int minIndex, int maxIndex, out ZigZagResult? swing)
    {
        swing = null;
        if (TrendType == TrendType.Secondary)
            return false;
        for (int i = minIndex; i <= maxIndex; i++)
        {
            var zigZag = PivotList[i];
            //if (MaxTime == CandleTime.MinValue || (MaxTime > CandleTime.MinValue && zigZag.Candle.OpenTime < MaxTime))
            {
                if (swing == null || GetHighValue(zigZag.Candle) > GetHighValue(swing.Candle))
                    swing = zigZag;
            }
        }
        return swing != null;
    }


    private void RestoreSwingPoint()
    {
        LastSwingLow?.Restore();
        LastSwingHigh?.Restore();
    }


    private ZigZagResult AddZigZagPoint(CryptoCandle candle, char pointType, bool dummy, int pivotIndex)
    {
        double value;
        if (pointType == 'L')
            value = GetLowValue(candle);
        else
            value = GetHighValue(candle);


        if (LastSwingPoint?.PointType == pointType) // && !dummy)
        {
            if (pointType == 'L')
            {
                if (value == LastSwingPoint.Value && candle.Low > LastSwingPoint.Candle.Low)
                    LastSwingPoint.ReusePoint(candle, value, dummy, pivotIndex); // prefer the one with the biggest wick
                else if (value < LastSwingPoint.Value)
                    LastSwingPoint.ReusePoint(candle, value, dummy, pivotIndex); // repeated low
                return LastSwingPoint;
            }
            else
            {
                if (value == LastSwingPoint.Value && candle.High < LastSwingPoint.Candle.High)
                    LastSwingPoint.ReusePoint(candle, value, dummy, pivotIndex); // prefer the one with the biggest wick
                else if (value > LastSwingPoint.Value)
                    LastSwingPoint.ReusePoint(candle, value, dummy, pivotIndex); // repeated high
                return LastSwingPoint;
            }
        }

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dummy = dummy, PivotIndex = pivotIndex };
        ZigZagList.Add(zigZag);
        LastSwingPoint = zigZag;
        if (dummy)
            AddedDummyZigZag.Add(zigZag);
        else
            LastSwingPoint.Backup();
        return LastSwingPoint;
    }


    private bool CanAddNewHigh(double candleValue)
    {
        if (TrendType == TrendType.Secondary)
            return true;
        // no previous high
        if (LastSwingHigh == null)
            return true;
        // It breaks the box
        var value = LastSwingHigh.Value;
        if (candleValue > value)
            return true;
        // Or if previous distance was really high say 50%?
        if (100 * Math.Abs(candleValue - value) / value > 25)
            return true;
        return false;
    }

    private bool CheckNewHigh(bool compareRight, int offset, bool dummy)
    {
        // Do we have a new high?
        if (ZigZagLanceBeggs.IsHighPoint(compareRight, offset))
        {
            var candle = ZigZagLanceBeggs.queue[offset];
            var candleValue = GetHighValue(candle);
            if (!dummy)
                PivotList.Add(new() { PointType = 'H', Candle = candle, Value = candleValue, PivotIndex = PivotList.Count });
            if (CanAddNewHigh(candleValue))
            {
                // The new high should be higher than the last low, if not ignore the point
                if (LastSwingLow != null && candleValue < LastSwingLow.Value)
                {
                    return false; // so ignore it completely..
                }
                else
                {
                    // Create a new inbetween low calculated from the buffer
                    if (LastSwingHigh != null && GetLowFromBuffer(LastSwingHigh.PivotIndex + 1, PivotList.Count - 2, out ZigZagResult? swing))
                    {
                        if (TrendType == TrendType.Secondary || (TrendType == TrendType.Primary && GetLowValue(swing!.Candle!) < LastSwingHigh.Value))
                            LastSwingLow = AddZigZagPoint(swing!.Candle!, 'L', dummy, swing.PivotIndex);
                    }
                    LastSwingHigh = AddZigZagPoint(candle, 'H', dummy, PivotList.Count - 1);
                    return true;
                }
            }
            return false;
        }
        return false;
    }

    private bool CanAddNewLow(double candleValue)
    {
        if (TrendType == TrendType.Secondary)
            return true;
        // no previous high
        if (LastSwingLow == null)
            return true;
        // It breaks the box
        var value = LastSwingLow.Value;
        if (value == 0)
            return true;
        if (candleValue < value)
            return true;
        // or if previous distance was really high
        if (100 * Math.Abs(candleValue - value) / value > 25)
            return true;
        return false;
    }


    private bool CheckNewLow(bool compareRight, int offset, bool dummy)
    {
        // Do we have a new low?
        if (ZigZagLanceBeggs.IsLowPoint(compareRight, offset))
        {
            //return AddNewLow(ZigZagLanceBeggs.queue[offset], dummy);
            var candle = ZigZagLanceBeggs.queue[offset];
            var candleValue = GetLowValue(candle);
            if (!dummy)
                PivotList.Add(new() { PointType = 'L', Candle = candle, Value = candleValue, PivotIndex = PivotList.Count });
            if (CanAddNewLow(candleValue))
            {
                // The new low should be lower than the last high, if not ignore the point
                if (LastSwingHigh != null && candleValue > LastSwingHigh.Value)
                {
                    return false; // so ignore it completely..
                }
                else
                {
                    // Create a new inbetween high calculated from the buffer
                    if (LastSwingLow != null && GetHighFromBuffer(LastSwingLow.PivotIndex + 1, PivotList.Count - 2, out ZigZagResult? swing))
                    {
                        if (TrendType == TrendType.Secondary || (TrendType == TrendType.Primary && GetHighValue(swing!.Candle) > LastSwingLow.Value))
                            LastSwingHigh = AddZigZagPoint(swing!.Candle!, 'H', dummy, swing.PivotIndex);
                    }
                    LastSwingLow = AddZigZagPoint(candle, 'L', dummy, PivotList.Count - 1);
                    return true;
                }
            }
            return false;
        }
        return false;
    }


    private void RemoveDummyPoints()
    {
        // Fixes because of unnoticed BOS at the right
        // Remove the dummy Primary points
        if (AddedDummyZigZag.Count > 0)
        {
            foreach (var zigZag in AddedDummyZigZag)
                ZigZagList.Remove(zigZag);
            AddedDummyZigZag.Clear();
        }

        // The two calls below run even when nothing was removed, and that is NOT redundant however
        // much it looks it. RecalculateSwingLowAndHigh walks back through ZigZagList and calls
        // Restore() on EVERY non-dummy point it passes, not only on the two it ends up returning -
        // so this is also the moment a reused pivot gets its backed-up Value/Candle/PivotIndex put
        // back (see ZigZagResult.ReusePoint/Backup).
        //
        // Skipping them when AddedDummyZigZag is empty looks like free speed - Calculate() calls
        // this on every candle now - but it was tried on 2026-08-12 and broke the block-size
        // independence again: 6 of the 12 tests in ZigZagIncrementalTests failed. Leave it.
        RecalculateSwingLowAndHigh();
        RestoreSwingPoint();
    }


    private void TryAddDummyPoints()
    {
        // Fixes because of unnoticed BOS at the end
        // If the last candle broke the L or H (BOS) we really need to add a temporary point
        // Did we have an unnoticed BOS (because there didn't form a L or H in the last 5 candles but the
        // LAST candle was a lower/higher then the previous H/L! (this is important for trend decisions)
        // okay, this might not be perfect, but close I think? (what a hassle btw...)
        if (ZigZagList.Count > 1 && LastSwingLow != null && LastSwingHigh != null)
        {
            bool added = false;
            if (!added)
            {
                var candleValue6 = GetHighValue(ZigZagLanceBeggs.queue[6]);
                var candleValue7 = GetHighValue(ZigZagLanceBeggs.queue[7]);
                var candleValue = Math.Max(candleValue6, candleValue7);
                if (candleValue > LastSwingHigh.Value)
                {
                    if (candleValue7 > candleValue6)
                    {
                        if (!added)
                            added = CheckNewHigh(false, 7, true);
                        if (!added)
                            added = CheckNewHigh(false, 6, true);
                    }
                    else
                    {
                        if (!added)
                            added = CheckNewHigh(false, 6, true);
                        if (!added)
                            added = CheckNewHigh(false, 7, true);
                    }
                }
            }

            if (!added)
            {
                var candleValue6 = GetLowValue(ZigZagLanceBeggs.queue[6]);
                var candleValue7 = GetLowValue(ZigZagLanceBeggs.queue[7]);
                var candleValue = Math.Min(candleValue6, candleValue7);
                if (candleValue < LastSwingLow.Value)
                {
                    if (candleValue6 < candleValue7)
                    {
                        if (!added)
                            added = CheckNewLow(false, 6, true);
                        if (!added)
                            CheckNewLow(false, 7, true);
                    }
                    else
                    {
                        if (!added)
                            added = CheckNewLow(false, 7, true);
                        if (!added)
                            CheckNewLow(false, 6, true);
                    }

                }
            }
        }
    }

    private void OptimizeList()
    {
        // TODO: problem, we only check 2 points...
        // there can be a huge jump before the first, invalidating everything!
        // NEED (some) FUNCTIONAL REFINEMENT...

        if (Deviation <= 0 || !UseOptimizing)
            return;


        // Worked out once instead of twice per iteration: Deviation does not change while the loop runs.
        double deviationFactor = Deviation / 100;

        // Dont need to iterate all, the last couple of points are enough
        int index = ZigZagList.Count - 10;
        if (index < 2)
            index = 2;

        bool recalculate = false;
        while (index < ZigZagList.Count)
        {
            ZigZagResult? p1 = ZigZagList[index - 0];
            ZigZagResult? p2 = ZigZagList[index - 1];
            ZigZagResult? p3 = ZigZagList[index - 2];

            if ((p1.PointType == 'L' && p2.PointType == 'H' && p3.PointType == 'L') || p1.PointType == 'H' && p2.PointType == 'L' && p3.PointType == 'H')
            {
                // Are we in the back of the list...?
                if (p1.Dummy || p2.Dummy || p3.Dummy || index == ZigZagList.Count - 1)
                {
                    index++;
                    continue;
                }

                // A point's Value is by construction the low of its candle for an 'L' and the high for
                // an 'H' (AddZigZagPoint sets them together, ReusePoint and Restore move them together),
                // and the triple check above already established the L/H/L or H/L/H alternation. So the
                // values are simply there - recomputing Math.Max(Open, Close) from the candle was the
                // same answer worked out again on every candle.
                double value1 = p1.Value;
                double value2 = p2.Value;
                double value3 = p3.Value;

                double diff1 = Math.Abs(value2 - value1);
                double perc1 = Math.Max(value1, value2) * deviationFactor;

                double diff2 = Math.Abs(value3 - value2);
                double perc2 = Math.Max(value2, value3) * deviationFactor;

                if (diff1 < perc1 && diff2 < perc2)
                {
                    ZigZagList.Remove(p1);
                    ZigZagList.Remove(p2);
                    recalculate = true;
                }
                else index++;
            }
            else index++;
        }

        // We could have removed the last swing high/low?
        // Recalculate them (dummy points are not present)
        if (recalculate)
            RecalculateSwingLowAndHigh();
    }


    private void RecalculateSwingLowAndHigh()
    {
        LastSwingLow = null;
        LastSwingHigh = null;
        LastSwingPoint = null;

        int index = ZigZagList.Count;
        while (index > 0 && (LastSwingLow == null || LastSwingHigh == null))
        {
            index--;
            var zigZag = ZigZagList[index];
            if (!zigZag.Dummy)
            {
                zigZag.Restore();
                if (zigZag.PointType == 'L' && LastSwingLow == null)
                {
                    LastSwingLow = zigZag;
                    LastSwingPoint ??= zigZag;
                }
                if (zigZag.PointType == 'H' && LastSwingHigh == null)
                {
                    LastSwingHigh = zigZag;
                    LastSwingPoint ??= zigZag;
                }
            }
        }
    }


    public void Calculate(CryptoCandle candle, bool batchProcess)
    {
        CandleCount++;

        // we need buffer of 8 candles to detect a low or high point
        if (ZigZagLanceBeggs.Add(candle))
        {
            // A dummy point must NEVER be in the list while OptimizeList runs. It is a provisional
            // marker for the right-hand edge (see TryAddDummyPoints), not part of the structure, and
            // OptimizeList skips any triple containing one. Leaving it in therefore made the result
            // depend on how often the caller happened to settle the indicator: a candle at a time
            // (the live scanner) produced 67 points where 15 at a time produced 193, on the very
            // same candles, with the last swing high nearly four months apart.
            //
            // So: clear the provisional point, extend the structure, optimise on a clean list, and
            // only then put the edge marker back. batchProcess now only defers that last step,
            // which is what it was meant to do - it was never meant to change the outcome.
            RemoveDummyPoints();
            CheckNewLow(true, 5, false);
            CheckNewHigh(true, 5, false);
            OptimizeList();
            if (!batchProcess)
                TryAddDummyPoints();
        }
    }


    public void FinishBatch()
    {
        // Same order as Calculate: optimise on a dummy-free list, then place the edge marker.
        RemoveDummyPoints();
        OptimizeList();
        TryAddDummyPoints();
    }


    /// <summary>
    /// Drops pivot/zigzag points whose candle is older than <paramref name="cutoff"/> — the same
    /// window CandleTools.CleanCandleDataAsync uses to trim CandleList/Data. Without this, PivotList
    /// and ZigZagList keep referencing CryptoCandle objects forever (the indicator instance itself is
    /// cached for the whole run, see CryptoSymbolInterval.ZigZagIndicators), so candles already removed
    /// from CandleList stay alive and unreachable for the GC purely because of these lists.
    /// Never removes past LastSwingLow/LastSwingHigh (or ZigZagList's own optimize window) so PivotIndex
    /// based lookups (GetLowFromBuffer/GetHighFromBuffer) stay valid.
    /// </summary>
    public void TrimBefore(CandleTime cutoff)
    {
        TrimPivotList(cutoff);
        TrimZigZagList(cutoff);
    }


    private void TrimPivotList(CandleTime cutoff)
    {
        int safeLimit = PivotList.Count;
        if (LastSwingLow != null)
            safeLimit = Math.Min(safeLimit, LastSwingLow.PivotIndex);
        if (LastSwingHigh != null)
            safeLimit = Math.Min(safeLimit, LastSwingHigh.PivotIndex);
        // RecalculateSwingLowAndHigh (called from OptimizeList) can later restore any non-dummy
        // ZigZagList entry as the new LastSwingLow/LastSwingHigh, not just the ones active right now.
        // So the shift below must never push any ZigZagList entry's PivotIndex/BackupIndex below 0,
        // otherwise that entry blows up GetLowFromBuffer/GetHighFromBuffer once it becomes a swing point.
        foreach (ZigZagResult zigZag in ZigZagList)
        {
            safeLimit = Math.Min(safeLimit, zigZag.PivotIndex);
            if (zigZag.BackupIndex.HasValue)
                safeLimit = Math.Min(safeLimit, zigZag.BackupIndex.Value);
        }

        int removeCount = 0;
        while (removeCount < safeLimit && PivotList[removeCount].Candle.OpenTime < cutoff)
            removeCount++;

        if (removeCount == 0)
            return;

        PivotList.RemoveRange(0, removeCount);
        // PivotIndex values are positions into PivotList — shift them after the removal above,
        // both on the remaining pivots and on every ZigZagList entry that points back into PivotList.
        // BackupIndex must shift too: Restore() copies it back into PivotIndex (see ZigZagResult),
        // so a stale BackupIndex would undo the shift on the next RestoreSwingPoint/RecalculateSwingLowAndHigh.
        foreach (ZigZagResult pivot in PivotList)
        {
            pivot.PivotIndex -= removeCount;
            if (pivot.BackupIndex.HasValue)
                pivot.BackupIndex -= removeCount;
        }
        foreach (ZigZagResult zigZag in ZigZagList)
        {
            zigZag.PivotIndex -= removeCount;
            if (zigZag.BackupIndex.HasValue)
                zigZag.BackupIndex -= removeCount;
        }
    }


    private void TrimZigZagList(CandleTime cutoff)
    {
        // Keep whatever OptimizeList still scans (its own trailing window) plus the live swing points.
        int safeLimit = Math.Max(0, ZigZagList.Count - 10);
        if (LastSwingLow != null)
            safeLimit = Math.Min(safeLimit, ZigZagList.IndexOf(LastSwingLow));
        if (LastSwingHigh != null)
            safeLimit = Math.Min(safeLimit, ZigZagList.IndexOf(LastSwingHigh));

        int removeCount = 0;
        while (removeCount < safeLimit && ZigZagList[removeCount].Candle.OpenTime < cutoff)
            removeCount++;

        if (removeCount > 0)
            ZigZagList.RemoveRange(0, removeCount);
    }

}