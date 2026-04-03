using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

public class TrendZigZagIndicatorList : Dictionary<(TrendType trendType, bool useHighLow), ZigZagIndicator>;

public class ZigZagIndicator
{
    private bool UseHighLow { get; set; } = false; // Use High/Low or Open/Close
    private TrendType TrendType { get; set; } = TrendType.Primary; // see more than primary trend

    public bool UseOptimizing { get; set; } = true; // Debug
    //public CandleTime MaxTime { get; set; } = CandleTime.MinValue; // Debug
    public int CandleCount { get; set; } = 0; // Debug, count of candles added

    //public int Depth { get; set; } = 12; // from previous approach, but does not work
    public decimal Deviation { get; set; } // Optimizing (does not work for now)
    //public int BackStep { get; set; } = 3; // from previous approach, but does not work

    private readonly List<ZigZagResult> AddedDummyZigZag = []; // collected points for recreating a low/high after a BOS formed

    public List<ZigZagResult> PivotList = []; // All "raw" low and high pivot points (for determining high/low)
    public List<ZigZagResult> ZigZagList { get; set; } = []; // The resulting zigzag points

    public ZigZagResult? LastSwingLow = null; // the last Low Primary
    public ZigZagResult? LastSwingHigh = null; // the last High Primary
    public ZigZagResult? LastSwingPoint = null; // the last Primary added

    private readonly ZigZagLanceBeggs ZigZagLanceBeggs;


    public ZigZagIndicator(TrendType trendType, bool useHighLow, decimal deviation = 1.0m)
    {
        TrendType = trendType;
        UseHighLow = useHighLow;
        Deviation = deviation;
        ZigZagLanceBeggs = new(UseHighLow);
    }

    private decimal GetLowValue(CryptoCandle candle) => candle.GetLowValue(UseHighLow);
    private decimal GetHighValue(CryptoCandle candle) => candle.GetHighValue(UseHighLow);

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
        decimal value;
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


    private bool CanAddNewHigh(decimal candleValue)
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

    private bool CanAddNewLow(decimal candleValue)
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

                decimal value1;
                decimal value2;
                decimal value3;
                if (p1.PointType == 'L')
                {
                    value1 = GetLowValue(p1.Candle);
                    value2 = GetHighValue(p2.Candle);
                    value3 = GetLowValue(p3.Candle);
                }
                else
                {
                    value1 = GetHighValue(p1.Candle);
                    value2 = GetLowValue(p2.Candle);
                    value3 = GetHighValue(p3.Candle);
                }

                decimal diff1 = Math.Abs(value2 - value1);
                decimal perc1 = Math.Max(value1, value2) * Deviation / 100;

                decimal diff2 = Math.Abs(value3 - value2);
                decimal perc2 = Math.Max(value2, value3) * Deviation / 100;

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
        //if (candle!.Time >= new DateTime(2024, 11, 15, 5+2, 00, 0, DateTimeKind.Utc))
        //    candle = candle; // debug

        // we need buffer of 8 candles to detect a low or high point
        if (ZigZagLanceBeggs.Add(candle))
        {
            if (!batchProcess)
                RemoveDummyPoints();
            CheckNewLow(true, 5, false);
            CheckNewHigh(true, 5, false);
            if (!batchProcess)
                TryAddDummyPoints();
            OptimizeList();
        }
    }


    public void FinishBatch()
    {
        RemoveDummyPoints();
        TryAddDummyPoints();
        OptimizeList();
    }

}