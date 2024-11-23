using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator9(CryptoCandleList candleList, bool useHighLow, decimal deviation, int duration)
{
    public bool UseHighLow = useHighLow; // Use High/Low or Open/Close
    public bool ShowSecondary { get; set; } = false; // see more than primary trend
    private bool BatchProces { get; set; } = false; // bulk add candles


    public bool UseOptimizing = true; // Debug
    public long MaxTime { get; set; } = -1; // Debug
    public int CandleCount { get; set; } = 0; // Debug, count of candles added

    //public int Depth { get; set; } = 12; // from previous approach, but does not work
    public decimal Deviation { get; set; } = deviation; // Optimizing (does not work for now)
    //public int BackStep { get; set; } = 3; // from previous approach, but does not work

    private readonly CryptoCandleList CandleList = candleList; // reference to source candlelist (just for adding dummy points)
    public int Duration { get; set; } = duration; // Interval duration off source candlelist (just for adding dummy points)

    private readonly List<ZigZagResult> AddedDummyZigZag = []; // collected points for recreating a low/high after a BOS formed

    public readonly List<ZigZagResult> PivotList = []; // All l/h points (for determining high/low)
    public List<ZigZagResult> ZigZagList { get; set; } = []; // The resulting zigzag points

    public ZigZagResult? LastSwingLow = null; // the last Low ZigZag
    public ZigZagResult? LastSwingHigh = null; // the last High ZigZag
    public ZigZagResult? LastSwingPoint = null; // the last ZigZag added

    private readonly ZigZagLanceBeggs ZigZagLanceBeggs = new(useHighLow);



    private decimal GetLowValue(CryptoCandle candle) => candle.GetLowValue(UseHighLow);
    private decimal GetHighValue(CryptoCandle candle) => candle.GetHighValue(UseHighLow);

    private bool GetLowFromBuffer(out CryptoCandle? swing)
    {
        swing = null;
        if (ShowSecondary)
            return false;

        int index = 0;
        if (LastSwingHigh != null)
            index = LastSwingHigh.PivotIndex + 1;
        for (int i = index; i < PivotList.Count; i ++)
        {
            var zigZag = PivotList[i];
            if (zigZag.Candle.OpenTime < MaxTime)
            {
                if (swing == null || GetLowValue(zigZag.Candle) < GetLowValue(swing))
                    swing = zigZag.Candle;
            }
        }
        return swing != null;
    }


    private bool GetHighFromBuffer(out CryptoCandle? swing)
    {
        swing = null;
        if (ShowSecondary)
            return false;

        int index = 0;
        if (LastSwingLow != null)
            index = LastSwingLow.PivotIndex + 1;
        for (int i = index; i < PivotList.Count; i++)
        {
            var zigZag = PivotList[i];
            if (zigZag.Candle.OpenTime < MaxTime)
            {
                if (swing == null || GetHighValue(zigZag.Candle) > GetHighValue(swing))
                    swing = zigZag.Candle;
            }
        }
        return swing != null;
    }

    private char SavedLastSwingPointType;
    private decimal SavedLastSwingPointValue;
    private CryptoCandle? SavedLastSwingPointCandle;

    private void SaveSwingPoint()
    {
        SavedLastSwingPointType = LastSwingPoint!.PointType;
        SavedLastSwingPointValue = LastSwingPoint!.Value;
        SavedLastSwingPointCandle = LastSwingPoint!.Candle;
    }

    private void RestoreSwingPoint()
    {
        if (SavedLastSwingPointCandle != null && SavedLastSwingPointType == LastSwingPoint!.PointType)
        {
            LastSwingPoint!.Value = SavedLastSwingPointValue;
            LastSwingPoint!.Candle = SavedLastSwingPointCandle;
            SavedLastSwingPointCandle = null;
        }
    }


    private ZigZagResult AddZigZagPoint(CryptoCandle candle, char pointType, bool dummy, int pivotIndex)
    {
        decimal value;
        if (pointType == 'L')
            value = GetLowValue(candle);
        else
            value = GetHighValue(candle);


        if (LastSwingPoint?.PointType == pointType) //  && !dummy
        {
            if (pointType == 'L')
            {
                if (value == LastSwingPoint.Value && candle.Low > LastSwingPoint.Candle.Low) 
                {
                    if (dummy) SaveSwingPoint();
                    LastSwingPoint.ReusePoint(candle, value); // prefer the one with the biggest wick
                    return LastSwingPoint;
                }
                else if (value < LastSwingPoint.Value)
                {
                    if (dummy) SaveSwingPoint();
                    LastSwingPoint.ReusePoint(candle, value); // repeated low
                    return LastSwingPoint;
                }
                else
                    return LastSwingPoint;
            }
            else
            {
                if (value == LastSwingPoint.Value && candle.High < LastSwingPoint.Candle.High)
                {
                    if (dummy) SaveSwingPoint();
                    LastSwingPoint.ReusePoint(candle, value); // prefer the one with the biggest wick
                    return LastSwingPoint;
                }
                else if (value > LastSwingPoint.Value)
                {
                    if (dummy) SaveSwingPoint();
                    LastSwingPoint.ReusePoint(candle, value); // repeated high
                    return LastSwingPoint;
                }
                else
                    return LastSwingPoint;
            }
        }

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dominant = false, Dummy = dummy, PivotIndex = pivotIndex };
        ZigZagList.Add(zigZag);
        LastSwingPoint = zigZag;
        if (dummy)
            AddedDummyZigZag.Add(zigZag);
        return LastSwingPoint;
    }


    private bool CanAddNewHigh(decimal candleValue)
    {
        if (ShowSecondary)
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
                PivotList.Add(new() { PointType = 'H', Candle = candle, Value = candleValue * 1.005m });
            if (CanAddNewHigh(candleValue))
            {
                if (LastSwingHigh != null && GetLowFromBuffer(out CryptoCandle? swing))
                {
                    if (ShowSecondary || (!ShowSecondary && GetLowValue(swing!) < LastSwingHigh.Value))
                        LastSwingLow = AddZigZagPoint(swing!, 'L', dummy, PivotList.Count + 1);
                }
                LastSwingHigh = AddZigZagPoint(candle, 'H', dummy, PivotList.Count + 1);
                return true;
            }
            return false;
        }
        return false;
    }

    private bool CanAddNewLow(decimal candleValue)
    {
        if (ShowSecondary)
            return true;
        // no previous high
        if (LastSwingLow == null)
            return true;
        // It breaks the box
        var value = LastSwingLow.Value;
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
                PivotList.Add(new() { PointType = 'L', Candle = candle, Value = candleValue * 0.995m });
            if (CanAddNewLow(candleValue))
            {
                if (LastSwingLow != null && GetHighFromBuffer(out CryptoCandle? swing))
                {
                    if (ShowSecondary || (!ShowSecondary && GetHighValue(swing!) > LastSwingLow.Value))
                        LastSwingHigh = AddZigZagPoint(swing!, 'H', dummy, PivotList.Count + 1);
                }
                LastSwingLow = AddZigZagPoint(candle, 'L', dummy, PivotList.Count + 1);
                return true;
            }
            return false;
        }
        return false;
    }


    private void RemoveDummyPoints()
    {
        // Fixes because of unnoticed BOS at the right
        // Remove the dummy ZigZag points
        if (AddedDummyZigZag.Count > 0)
        {
            foreach (var zigZag in AddedDummyZigZag)
                ZigZagList.Remove(zigZag);
            AddedDummyZigZag.Clear();
            RecalculateSwingLowAndHigh();
        }
    }


    private void TryAddDummyPoints()
    {
        // Fixes because of unnoticed BOS at the end
        // If the last candle broke the L or H (BOS) we really need to add a temporary point
        // Did we have an unnoticed BOS (because there didn't form a L or H in the last 5 candles but the
        // LAST candle was a lower/higher then the previous H/L! (this is important for trend decisions)
        // okay, this might not be perfect, but close I think? (what a hassle btw...)

        if (ZigZagList.Count > 1 && CandleList.Count > 1 && LastSwingLow != null && LastSwingHigh != null)
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
                            added = CheckNewLow(false, 7, true);
                    }
                    else
                    {
                        if (!added)
                            added = CheckNewLow(false, 7, true);
                        if (!added)
                            added = CheckNewLow(false, 6, true);
                    }

                }
            }


            // old code, iterate all candles from the last swing point
            //if (!added)
            //{
            //    long unix = ZigZagLanceBeggs.queue[7].OpenTime;
            //    List<CryptoCandle> list = [];
            //    while (unix > LastSwingPoint.Candle.OpenTime)
            //    {
            //        if (unix < MaxTime && CandleList.TryGetValue(unix, out CryptoCandle? candle))
            //            list.Add(candle);
            //        unix -= Duration;
            //    }

            //    foreach (CryptoCandle candle in list)
            //    {
            //        if (candle.OpenTime <= LastSwingLow.Candle.OpenTime)
            //            break;

            //        candleValue = GetLowValue(candle);
            //        if (CanAddNewLow(candleValue))
            //        //if (candle.GetLowValue(UseHighLow) < LastSwingLow.Value)
            //        {
            //            if (LastSwingLow != null && GetHighFromBuffer(out CryptoCandle? swing))
            //            {
            //                if (ShowSecondary || (!ShowSecondary && GetHighValue(swing!) > LastSwingLow.Value))
            //                    LastSwingHigh = AddZigZagPoint(swing!, 'H', true, PivotList.Count + 1);
            //            }
            //            LastSwingLow = AddZigZagPoint(candle, 'L', true, PivotList.Count + 1);
            //            break;
            //            //if (LastSwingLow.PointType != 'H' && GetHighFromBuffer(out CryptoCandle? swing))
            //            //{
            //            //    ZigZagResult dummyHigh = new() { PointType = 'H', Candle = swing!, Value = swing!.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
            //            //    AddedDummyZigZag.Add(dummyHigh);
            //            //    ZigZagList.Add(dummyHigh);
            //            //    break;
            //            //}

            //            //if (LastSwingLow.PointType != 'L') // dont repeat it 
            //            //{
            //            //    ZigZagResult dummyLow = new() { PointType = 'L', Candle = candle, Value = candle.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
            //            //    AddedDummyZigZag.Add(dummyLow);
            //            //    ZigZagList.Add(dummyLow);
            //            //    break;
            //            //}
            //        }
            //        candleValue = GetHighValue(candle);
            //        if (CanAddNewHigh(candleValue))
            //        //else if (candle.GetHighValue(UseHighLow) > LastSwingHigh.Value)
            //        {
            //            if (LastSwingHigh != null && GetLowFromBuffer(out CryptoCandle? swing))
            //            {
            //                if (ShowSecondary || (!ShowSecondary && GetLowValue(swing!) < LastSwingHigh.Value))
            //                    LastSwingLow = AddZigZagPoint(swing!, 'L', true, PivotList.Count + 1);
            //            }
            //            LastSwingHigh = AddZigZagPoint(candle, 'H', true, PivotList.Count + 1);
            //            break;


            //            //if (LastSwingLow.PointType != 'L' && GetLowFromBuffer(out CryptoCandle? swing))
            //            //{
            //            //    ZigZagResult dummyLow = new() { PointType = 'L', Candle = swing!, Value = swing!.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
            //            //    AddedDummyZigZag.Add(dummyLow);
            //            //    ZigZagList.Add(dummyLow);
            //            //    break;
            //            //}

            //            //if (LastSwingLow.PointType != 'H') // dont repeat it 
            //            //{
            //            //    ZigZagResult dummyHigh = new() { PointType = 'H', Candle = candle, Value = candle.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
            //            //    AddedDummyZigZag.Add(dummyHigh);
            //            //    ZigZagList.Add(dummyHigh);
            //            //    break;
            //            //}
            //        }
            //    }
            //}
        }
    }

    public void OptimizeList()
    {
        // TODO: problem, we only check 2 points, there can be a huge jump before the first, invalidating everything!
        // NOT RIGHT, NEED FUNCTIONAL REFINEMENT...

        if (Deviation <= 0 || !UseOptimizing)
            return;

        // 3 purposes
        // fib (high/low)
        // coarse trend (market trend, using h/zigZag block) -> Primary trend
        // fine trend (dunno, just to look at?) -> Secondary trend
        // All three use this optimize thingy... oops


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
        LastSwingPoint = null;
        LastSwingLow = null;
        LastSwingHigh = null;

        int index = ZigZagList.Count;
        while (index > 0 && (LastSwingLow == null || LastSwingHigh == null))
        {
            index--;
            var zigZag = ZigZagList[index];
            if (!zigZag.Dummy)
            {
                if (zigZag.PointType == 'L' && LastSwingLow == null)
                    LastSwingLow = zigZag;
                if (zigZag.PointType == 'H' && LastSwingHigh == null)
                    LastSwingHigh = zigZag;
            }
        }
        
        if (LastSwingLow != null && LastSwingHigh != null)
        {
            if (LastSwingLow.Candle.OpenTime > LastSwingHigh.Candle.OpenTime)
                LastSwingPoint = LastSwingLow;
            else
                LastSwingPoint = LastSwingHigh;
        }
        else
        {
            // this will never be hit...
            if (LastSwingLow != null)
                LastSwingPoint = LastSwingLow;
            if (LastSwingHigh != null)
                LastSwingPoint = LastSwingHigh;
        }
    }



    public void Calculate(CryptoCandle candle)
    {
        CandleCount++;

        // we need buffer of 8 candles to detect a low or high point
        if (ZigZagLanceBeggs.Add(candle))
        {
            //if (candle!.Date >= new DateTime(2024, 11, 15, 5+2, 00, 0, DateTimeKind.Utc))
            //    candle = candle; // debug 
            if (!BatchProces)
            {
                RemoveDummyPoints();
                RestoreSwingPoint();
            }
            CheckNewLow(true, 5, false);
            CheckNewHigh(true, 5, false);
            if (!BatchProces)
                TryAddDummyPoints();
            OptimizeList();
        }
    }

    
    
    public void StartBatch()
    {
        BatchProces = true;
    }



    public void FinishBatch()
    {
        BatchProces = false;

        RemoveDummyPoints();
        RestoreSwingPoint();
        TryAddDummyPoints();
        OptimizeList();
    }


}