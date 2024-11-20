using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator10(CryptoCandleList candleList, bool useHighLow, decimal deviation, int duration)
{
    public bool UseHighLow = useHighLow;
    public int Duration { get; set; } = duration;
    //public int Depth { get; set; } = 12;
    public decimal Deviation { get; set; } = deviation;
    //public int BackStep { get; set; } = 3;
    public bool ShowSecondary { get; set; } = false;
    public long MaxTime { get; set; } = -1;

    public int CandleCount { get; set; } = 0;
    public List<ZigZagResult> ZigZagList { get; set; } = [];
    private readonly CryptoCandleList CandleList = candleList;

    private CryptoCandle? previous5 = null;
    private CryptoCandle? previous4 = null;
    private CryptoCandle? previous3 = null;
    private CryptoCandle? previous2 = null;
    private CryptoCandle? previous1 = null;

    public ZigZagResult? LastSwingLow = null; // the last Low ZigZag
    public ZigZagResult? LastSwingHigh = null; // the last High ZigZag
    public ZigZagResult? LastSwingPoint = null; // the last ZigZag added

    private readonly List<CryptoCandle> buffer = []; // for creating a low/high after BOS
    private readonly List<ZigZagResult> AddedDummyZigZag = []; // dummy points at the end because of BOS

    private decimal GetLowValue(CryptoCandle candle) => candle.GetLowValue(UseHighLow);
    private decimal GetHighValue(CryptoCandle candle) => candle.GetHighValue(UseHighLow);

    private bool GetLowFromBuffer(out CryptoCandle? swing)
    {
        swing = null;
        foreach (var x in buffer)
        {
            if (swing == null || GetLowValue(x) < GetLowValue(swing))
                swing = x;
        }
        return swing != null;
    }


    private bool GetHighFromBuffer(out CryptoCandle? swing)
    {
        swing = null;
        foreach (var x in buffer)
        {
            if (swing == null || GetHighValue(x) > GetHighValue(swing))
                swing = x;
        }
        return swing != null;
    }


    private ZigZagResult AddZigZagPoint(CryptoCandle candle, char pointType, bool dummy)
    {
        if (!dummy)
            buffer.Clear();
        decimal value;
        if (pointType == 'L')
            value = GetLowValue(candle);
        else
            value = GetHighValue(candle);


        if (LastSwingPoint?.PointType == pointType)
        {
            if (pointType == 'L')
            {
                if (value == LastSwingPoint.Value && candle.Low < LastSwingPoint.Candle.Low)
                {
                    LastSwingPoint.ReusePoint(candle, value); // prefer the one with the biggest wick
                    return LastSwingPoint;
                }
                else if (value < LastSwingPoint.Value)
                {
                    LastSwingPoint.ReusePoint(candle, value); // repeated low
                    return LastSwingPoint;
                }
                else
                    return LastSwingPoint;
            }
            else
            {
                if (value == LastSwingPoint.Value && candle.High > LastSwingPoint.Candle.High)
                {
                    LastSwingPoint.ReusePoint(candle, value); // prefer the one with the biggest wick
                    return LastSwingPoint;
                }
                else if (value > LastSwingPoint.Value)
                {
                    LastSwingPoint.ReusePoint(candle, value); // repeated high
                    return LastSwingPoint;
                }
                else
                    return LastSwingPoint;
            }
        }

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dominant = false, Dummy = dummy };
        ZigZagList.Add(zigZag);
        if (dummy)
            AddedDummyZigZag.Add(zigZag);
        LastSwingPoint = zigZag;
        return LastSwingPoint;
    }

    private bool CanAddNewHigh(decimal candleValue)
    {
        // no previous high
        if (LastSwingHigh == null)
            return true;
        // It breaks the box
        var value = GetHighValue(LastSwingHigh!.Candle);
        if (candleValue > value)
            return true;
        // or if previous distance was really high say 50%?
        if (100 * Math.Abs(candleValue - value) / value > 25)
            return true;

        //if (LastSwingLow != null && candleValue < LastSwingLow.Value)
        //    return false;

        return ShowSecondary;
    }

    private void AddNewHigh(CryptoCandle candle, decimal candleValue, bool dummy)
    {
        if (CanAddNewHigh(candleValue)) // if the candle1 broke the last swing high we can calculate the lowest point
        {
            if (LastSwingHigh != null && GetLowFromBuffer(out CryptoCandle? swing) && GetLowValue(swing!) < LastSwingHigh.Value)
                LastSwingLow = AddZigZagPoint(swing!, 'L', dummy);
            LastSwingHigh = AddZigZagPoint(candle, 'H', dummy);
        }
        else
            buffer.Add(candle); // remember for calculating high or low after BOS
    }

    private (bool success, bool absent) SurroundedByLower(CryptoCandle center, int duration)
    {
        // Do we have a new low?
        var candleValue = GetHighValue(center);

        int limit = 2;
        int found = 0;
        int present = 0;
        long unix = center.OpenTime;
        while (limit-- > 0)
        {
            unix += duration;
            if (CandleList.TryGetValue(unix, out CryptoCandle? candle))
            {
                if (MaxTime > 0 && candle.OpenTime > MaxTime)
                    break;
                present++;
                var value = GetHighValue(candle);
                if (value > candleValue)
                    break;
                if (value <= candleValue) // should not be equal
                    found++;
                // continue if equal
            }
        }
        return (found >= 2, present == 0);
    }

    private void CheckNewHigh(CryptoCandle center)
    {
        // Do we have a new high?
        var leftOkay = SurroundedByLower(center, -Duration);
        var rightOkay = SurroundedByLower(center, Duration);
        if (leftOkay.success && !leftOkay.absent && rightOkay.success && !rightOkay.absent)
        {
            var candleValue = GetHighValue(center);
            AddNewHigh(center, candleValue, rightOkay.absent);
        }
    }

    private bool CanAddNewLow(decimal candleValue)
    {
        // no previous high
        if (LastSwingLow == null)
            return true;
        // It breaks the box
        var value = GetLowValue(LastSwingLow!.Candle);
        if (candleValue < value)
            return true;
        // or if previous distance was really high
        if (100 * Math.Abs(candleValue - value) / value > 25)
            return true;

        //if (LastSwingHigh != null && candleValue > LastSwingHigh.Value)
        //    return false;

        return ShowSecondary;
    }

    private void AddNewLow(CryptoCandle candle, decimal candleValue, bool dummy)
    {
        if (CanAddNewLow(candleValue)) // if the candle1 broke the last swing low we can calculate the highest point
        {
            if (LastSwingLow != null && GetHighFromBuffer(out CryptoCandle? swing) && GetHighValue(swing!) > LastSwingLow.Value)
                LastSwingHigh = AddZigZagPoint(swing!, 'H', dummy);
            LastSwingLow = AddZigZagPoint(candle, 'L', dummy);
        }
        else
            buffer.Add(candle); // remember for calculating high or low after BOS
    }

    private (bool success, bool absent) SurroundedByHigher(CryptoCandle center, int duration)
    {
        // Do we have a new low?
        var candleValue = GetLowValue(center);

        int limit = 2;
        int found = 0;
        int present = 0;
        long unix = center.OpenTime;
        while (limit-- > 0)
        {
            unix += duration;
            if (CandleList.TryGetValue(unix, out CryptoCandle? candle))
            {
                if (MaxTime > 0 && candle.OpenTime > MaxTime)
                    break;
                present++;
                var value = GetLowValue(candle);
                if (value < candleValue)
                    break;
                if (value >= candleValue) // should not be equal
                    found++;
                // continue if equal
            }
        }
        return (found >= 2, present == 0);
    }


    private void CheckNewLow(CryptoCandle center)
    {
        // Do we have a new low?
        var leftOkay = SurroundedByHigher(center, -Duration);
        var rightOkay = SurroundedByHigher(center, Duration);
        if (leftOkay.success && !leftOkay.absent && rightOkay.success && !rightOkay.absent)
        {
            var candleValue = GetLowValue(center);
            AddNewLow(center, candleValue, rightOkay.absent);
        }
    }


    public void OptimizeList()
    {
        if (Deviation <= 0)
            return;

        // Dont need to iterate all, the last couple of points are enough
        int index = ZigZagList.Count - 10;
        if (index < 1)
            index = 1;

        bool recalculate = false;
        while (index < ZigZagList.Count)
        {
            ZigZagResult? p1 = ZigZagList[index - 0];
            ZigZagResult? p2 = ZigZagList[index - 1];

            if ((p1.PointType == 'L' && p2.PointType == 'H') || p1.PointType == 'H' && p2.PointType == 'L')
            {
                decimal value1;
                decimal value2;
                if (p1.PointType == 'L')
                {
                    value1 = GetLowValue(p1.Candle);
                    value2 = GetHighValue(p2.Candle);
                }
                else
                {
                    value1 = GetHighValue(p1.Candle);
                    value2 = GetLowValue(p2.Candle);
                }

                decimal diff = Math.Abs(value2 - value1);
                decimal perc = Math.Max(value1, value2) * Deviation / 100;
                if (diff < perc)
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
        {
            LastSwingPoint = null;
            LastSwingLow = null;
            LastSwingHigh = null;
            if (ZigZagList.Count > 0)
            {
                var l = ZigZagList[^1];
                LastSwingPoint = l;
                if (l.PointType == 'L')
                    LastSwingLow = l;
                else
                    LastSwingHigh = l;
            }
            if (ZigZagList.Count > 1)
            {
                var l = ZigZagList[^2];
                if (l.PointType == 'L')
                    LastSwingLow = l;
                else
                    LastSwingHigh = l;
            }
        }
    }



    public void Calculate(CryptoCandle candle)
    {
        //if (candle!.DateLocal >= new DateTime(2024, 11, 16, 23, 00, 0, DateTimeKind.Utc))
        //    candle = candle; // debug 
        CandleCount++;

        // we need buffer of 5 candles to detect a low or high point
        previous5 = previous4;
        previous4 = previous3;
        previous3 = previous2;
        previous2 = previous1;
        previous1 = candle;

        if (previous5 != null)
        {
            if (AddedDummyZigZag.Count > 0)
            {
                foreach (var zigzag in AddedDummyZigZag)
                    ZigZagList.Remove(zigzag);
                AddedDummyZigZag.Clear();
            }

            CheckNewLow(previous3!);
            CheckNewHigh(previous3!);
            OptimizeList();
        }
    }


    public void FinishJob()
    {
        // Afterwards, if the last candle broke the L or H (BOS) we need to add a temporary point
        // I'm not sure how this exactly works out, we make a temporary point more or less permanent!!
        // Chances are high a BOS will lead to another high/low, but technically if can go the other way..
        var candleValue = GetLowValue(previous1!);
        if (LastSwingLow != null && candleValue < LastSwingLow.Value && GetLowValue(previous2!) >= candleValue && GetLowValue(previous3!) >= candleValue)
            AddNewLow(previous1!, candleValue, true);
        else
        {
            candleValue = GetLowValue(previous2!);
            if (LastSwingLow != null && candleValue < LastSwingLow.Value && GetLowValue(previous3!) >= candleValue && GetLowValue(previous4!) >= candleValue)
                AddNewLow(previous1!, candleValue, true);
        }


        candleValue = GetHighValue(previous1!);
        if (LastSwingHigh != null && candleValue > LastSwingHigh.Value && GetHighValue(previous2!) <= candleValue && GetHighValue(previous3!) <= candleValue)
            AddNewHigh(previous1!, candleValue, true);
        else
        {
            candleValue = GetLowValue(previous2!);
            if (LastSwingHigh != null && candleValue > LastSwingHigh.Value && GetHighValue(previous3!) <= candleValue && GetHighValue(previous4!) <= candleValue)
                AddNewHigh(previous1!, candleValue, true);
        }

        //Init();
        OptimizeList();
        //Finish();
    }

    //public void FinishJob()
    //{
    //    // Afterwards, if the last candle formed a BOS we need to add a temporary point

    //    if (candleminus1 == null)
    //        return;


    //    List<CryptoCandle> list = [candleminus1, candleminus2, candleminus3, candleminus4];
    //    foreach (var candle in list)
    //    {
    //        if (LastSwingPoint != null && candle.OpenTime <= LastSwingPoint.Candle.OpenTime)
    //            break;

    //        if (candleminus1!.Date == new DateTime(2024, 11, 17, 01, 00, 0, DateTimeKind.Utc))
    //            candleminus1 = candleminus1; // debug 

    //        {
    //            var leftOkay = SurroundedByHigher(candle, -Duration);
    //            var rightOkay = SurroundedByHigher(candle, Duration);
    //            if (leftOkay.success && !leftOkay.absent && !rightOkay.success && rightOkay.absent)
    //            {
    //                var candleValue = GetLowValue(candle);
    //                AddNewLow(candle, candleValue, true);
    //                break;
    //            }
    //        }

    //        {
    //            var leftOkay = SurroundedByLower(candle, -Duration);
    //            var rightOkay = SurroundedByLower(candle, Duration);
    //            if (leftOkay.success && !leftOkay.absent && !rightOkay.success && rightOkay.absent)
    //            {
    //                var candleValue = GetHighValue(candle);
    //                AddNewHigh(candle, candleValue, true);
    //                break;
    //            }
    //        }
    //    }

    //    OptimizeList();
    //}


}