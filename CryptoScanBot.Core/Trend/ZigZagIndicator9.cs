using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator9(SortedList<long, CryptoCandle> candleList, bool useHighLow, decimal deviation)
{
    public bool UseHighLow = useHighLow;
    //public int Depth { get; set; } = 12;
    public decimal Deviation { get; set; } = deviation;
    //public int BackStep { get; set; } = 3;

    public int CandleCount { get; set; } = 0;
    public List<ZigZagResult> ZigZagList { get; set; } = [];
    private readonly SortedList<long, CryptoCandle> CandleList = candleList;

    private CryptoCandle? previous5 = null;
    private CryptoCandle? previous4 = null;
    private CryptoCandle? previous3 = null;
    private CryptoCandle? previous2 = null;
    private CryptoCandle? previous1 = null;

    public ZigZagResult? LastSwingLow = null; // the last Low ZigZag
    public ZigZagResult? LastSwingHigh = null; // the last High ZigZag
    public ZigZagResult? LastSwingPoint = null; // the last ZigZag added

    private readonly List<CryptoCandle> buffer = []; // for creating a low/high after BOS
    //private readonly List<ZigZagResult> AddedDummyZigZag = []; // dummy points at the end because of BOS

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


    private ZigZagResult AddZigZagPoint(CryptoCandle candle, char pointType)
    {
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
                if (value <= LastSwingPoint.Value)
                {
                    LastSwingPoint.ReusePoint(candle, value); // repeated low
                    return LastSwingPoint;
                }
                //else if (value == LastSwingPoint.Value && candle1.Low < LastSwingPoint.Candle.Low)
                //{
                //    LastSwingPoint.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                //else if (value == LastSwingPoint.Value)
                //{
                //    LastSwingPoint.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                else
                    return LastSwingPoint;
            }
            else
            {
                if (value >= LastSwingPoint.Value)
                {
                    LastSwingPoint.ReusePoint(candle, value); // repeated high
                    return LastSwingPoint;
                }
                //else if (value == LastSwingPoint.Value && candle1.High > LastSwingPoint.Candle.High)
                //{
                //    LastSwingPoint.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                else
                    return LastSwingPoint;
            }
        }

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dominant = false, };
        ZigZagList.Add(zigZag);
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

        return false;
    }

    private void AddNewHigh(CryptoCandle candle, decimal candleValue)
    {
        //if (LastSwingHigh == null)
        //    LastSwingHigh = AddZigZagPoint(candle, 'H');
        //else
        if (CanAddNewHigh(candleValue)) //(candleValue > GetHighValue(LastSwingHigh.Candle)) // if the candle1 broke the last swing high we can calculate the lowest point
        {
            if (LastSwingHigh != null && GetLowFromBuffer(out CryptoCandle? swing) && GetLowValue(swing!) < LastSwingHigh.Value)
                LastSwingLow = AddZigZagPoint(swing!, 'L');
            LastSwingHigh = AddZigZagPoint(candle, 'H');
        }
        else
            buffer.Add(candle); // remember for calculating high or low after BOS
    }

    private void CheckNewHigh()
    {
        // Do we have a new high?
        var candleValue = GetHighValue(previous3!);
        if (GetHighValue(previous5!) <= candleValue && GetHighValue(previous4!) <= candleValue && GetHighValue(previous2!) <= candleValue && GetHighValue(previous1!) <= candleValue)
            AddNewHigh(previous3!, candleValue);
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

        return false;
    }

    private void AddNewLow(CryptoCandle candle, decimal candleValue)
    {
        //if (LastSwingLow == null)
        //    LastSwingLow = AddZigZagPoint(candle, 'L');
        //else
        if (CanAddNewLow(candleValue))  //(candleValue < GetLowValue(LastSwingLow.Candle)) // if the candle1 broke the last swing low we can calculate the highest point
        {
            if (LastSwingLow != null && GetHighFromBuffer(out CryptoCandle? swing) && GetHighValue(swing!) > LastSwingLow.Value)
                LastSwingHigh = AddZigZagPoint(swing!, 'H');
            LastSwingLow = AddZigZagPoint(candle, 'L');
        }
        else
            buffer.Add(candle); // remember for calculating high or low after BOS
    }

    private void CheckNewLow()
    {
        // Do we have a new low?
        var candleValue = GetLowValue(previous3!);
        if (GetLowValue(previous5!) >= candleValue && GetLowValue(previous4!) >= candleValue && GetLowValue(previous2!) >= candleValue && GetLowValue(previous1!) >= candleValue)
            AddNewLow(previous3!, candleValue);
    }


    //private void Init()
    //{
    //    // Fixes because of unnoticed BOS at the right
    //    // Remove the dummy ZigZag points
    //    foreach (var zigZag in AddedDummyZigZag)
    //        ZigZagList.Remove(zigZag);
    //    AddedDummyZigZag.Clear();
    //}


    //private void Finish()
    //{
    //    // Fixes because of unnoticed BOS at the right
    //    // Did we have an unnoticed BOS (because there didn't form a L or H in the last 5 candles but the
    //    // LAST candle was a lower/higher then the previous H/L! (this is important for trend decisions)
    //    // Fix: add a dummy ZigZagResult and remove it in the next call

    //    // Remark: What if we add it regulary? Do we need this? Simple comparison in the CheckNewHigh and CheckNewLow!
    //    // Or even better, add the last added candle 2 times extra? The Optimize will filter the point away ...
    //    // (no, if we add the candle the Previous* will be off, but the idea is still interesting).

    //    if (ZigZagList.Count > 1 && CandleList.Count > 1 && LastSwingLow != null && LastSwingHigh != null)
    //    {
    //        // This is the same as LastSwingPoint (we have a variable like that? Is it the same?)
    //        ZigZagResult lastZigZag;
    //        if (LastSwingLow.Candle.OpenTime > LastSwingHigh.Candle.OpenTime)
    //            lastZigZag = LastSwingLow;
    //        else
    //            lastZigZag = LastSwingHigh;

    //        List<CryptoCandle> list = [];
    //        list.Add(CandleList.Values[^1]);
    //        list.Add(CandleList.Values[^2]);
    //        list.Add(CandleList.Values[^3]);

    //        foreach (CryptoCandle candle in list)
    //        {
    //            if (candle.OpenTime <= lastZigZag.Candle.OpenTime)
    //                break;

    //            if (candle.GetLowValue(UseHighLow) < LastSwingLow.Value)
    //            {
    //                if (lastZigZag.PointType != 'H' && GetHighFromBuffer(out CryptoCandle? swing))
    //                {
    //                    ZigZagResult dummyHigh = new() { PointType = 'H', Candle = swing!, Value = swing!.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
    //                    ZigZagList.Add(dummyHigh);
    //                    AddedDummyZigZag.Add(dummyHigh);
    //                }

    //                ZigZagResult last = ZigZagList[^1];
    //                if (last.PointType != 'L') // dont repeat it 
    //                {
    //                    ZigZagResult dummyLow = new() { PointType = 'L', Candle = candle, Value = candle.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
    //                    ZigZagList.Add(dummyLow);
    //                    AddedDummyZigZag.Add(dummyLow);
    //                }
    //                break;
    //            }
    //            else if (candle.GetHighValue(UseHighLow) > LastSwingHigh.Value)
    //            {
    //                if (lastZigZag.PointType != 'L' && GetLowFromBuffer(out CryptoCandle? swing))
    //                {
    //                    ZigZagResult dummyLow = new() { PointType = 'L', Candle = swing!, Value = swing!.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
    //                    ZigZagList.Add(dummyLow);
    //                    AddedDummyZigZag.Add(dummyLow);
    //                }

    //                ZigZagResult last = ZigZagList[^1];
    //                if (last.PointType != 'H') // dont repeat it 
    //                {
    //                    ZigZagResult dummyHigh = new() { PointType = 'H', Candle = candle, Value = candle.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
    //                    ZigZagList.Add(dummyHigh);
    //                    AddedDummyZigZag.Add(dummyHigh);
    //                }
    //                break;
    //            }
    //        }
    //    }
    //}

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



    public void Calculate(CryptoCandle candle, int duration)
    {
        //if (candle!.DateLocal >= new DateTime(2024, 11, 07, 18, 00, 0, DateTimeKind.Local))
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
            //Init();
            CheckNewLow();
            CheckNewHigh();
            OptimizeList();
            //Finish();
        }
    }


    public void FinishJob()
    {
        // Afterwards, if the last candle broke the L or H (BOS) we need to add a temporary point
        // I'm not sure how this exactly works out, we make a temporary point more or less permanent!!
        // Chances are high a BOS will lead to another high/low, but technically if can go the other way..
        var candleValue = GetLowValue(previous1!);
        if (LastSwingLow != null && candleValue < LastSwingLow.Value && GetLowValue(previous2!) >= candleValue && GetLowValue(previous1!) >= candleValue)
            AddNewLow(previous1!, candleValue);

        candleValue = GetHighValue(previous1!);
        if (LastSwingHigh != null && candleValue > LastSwingHigh.Value && GetLowValue(previous2!) <= candleValue && GetLowValue(previous1!) <= candleValue)
            AddNewHigh(previous1!, candleValue);

        //Init();
        OptimizeList();
        //Finish();
    }

}