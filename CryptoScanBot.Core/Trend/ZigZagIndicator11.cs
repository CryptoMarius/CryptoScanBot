using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator11(CryptoCandleList candleList, bool useHighLow, decimal deviation, int duration)
{
    // unused for now
    public int Duration { get; set; } = duration;
    public long MaxTime { get; set; } = -1;
    public bool ShowSecondary { get; set; } = false;

    public bool UseHighLow = useHighLow;
    //public int Depth { get; set; } = 12;
    public decimal Deviation { get; set; } = deviation;
    //public int BackStep { get; set; } = 3;

    public int CandleCount { get; set; } = 0;
    public List<ZigZagResult> ZigZagList { get; set; } = [];
    private readonly CryptoCandleList CandleList = candleList;

    private CryptoCandle? candleminus5 = null;
    private CryptoCandle? candleminus4 = null;
    private CryptoCandle? candleminus3 = null;
    private CryptoCandle? candleminus2 = null;
    private CryptoCandle? candleminus1 = null;
    private CryptoCandle? candlecurrent = null; // the center for comparison
    private CryptoCandle? candleplus1 = null;
    private CryptoCandle? candleplus2 = null;

    public ZigZagResult? LastSwingLow = null; // the last Low ZigZag
    public ZigZagResult? LastSwingHigh = null; // the last High ZigZag
    public ZigZagResult? LastSwingPoint = null; // the last ZigZag added

    private readonly List<CryptoCandle> buffer = []; // for creating a low/high after a BOS formed
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

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dominant = false, };
        ZigZagList.Add(zigZag);
        LastSwingPoint = zigZag;
        return LastSwingPoint;
    }

    // === INPUTS ===
    // This Indicator Plots Swing Highs & Swing Lows based on Lance Beggs definition:
    //
    // A Swing High (SH) is a price bar high preceed by two lower highs (LH) and 
    // followed by two lower highs (LH)
    //
    // In the event of multiple candles forming equal highs, this will still be defined as a swing high,
    // provided that there are two candles with lower highs both preceding and following the multiple
    // candle formation.

    public bool UseIdenticalValues { get; set; } = false; // Use exact value matches or allow a margin
    public decimal MarginPercentage { get; set; } = 0.1m;  // Percentage margin for approximate equality

    // Calculate distance percentage between two values
    private static decimal PercentageDistance(decimal value1, decimal value2) =>
        Math.Abs(value1 - value2) * 100 / Math.Max(value1, value2);

    // Check if two values are "equal" based on the input settings
    private bool ValuesAreEqual(decimal value1, decimal value2) =>
        UseIdenticalValues? value1 == value2 : PercentageDistance(value1, value2) <= MarginPercentage;

    // Check if the first value is a higher high compared to the second value
    private bool IsHigherHigh(decimal value, CryptoCandle candle)
    {
        var candleValue = GetHighValue(candle);
        return value > candleValue && !ValuesAreEqual(value, candleValue);
    }

    // Check if the first value is a lower low compared to the second value
    private bool IsLowerLow(decimal value, CryptoCandle candle)
    {
        var candleValue = GetLowValue(candle);
        return value < candleValue && !ValuesAreEqual(value, candleValue);
    }


    // === SWING HIGH ROUTINES ===

    // Check for two previous bars and two next bars with Lower Highs 
    // 12321, 21312, 22322, etc...
    private bool Has_LH_LH_AND_LH_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus2!) &&
               IsHigherHigh(currentHigh, candleminus1!) &&
               IsHigherHigh(currentHigh, candleplus1!) &&
               IsHigherHigh(currentHigh, candleplus2!);
    }

    // Check for two previous bars with Lower Highs, one highest bar, one lower hight bar,
    // one equals to highest bar and two next bars with Lower Highs
    // 1232312, 1131311, 2231311, etc..
    // currentBar=Second EQ bar
    private bool Has_LH_LH_EQ_LH_EQ_LH_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus4!) &&
               IsHigherHigh(currentHigh, candleminus3!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus2!)) &&
               IsHigherHigh(currentHigh, candleminus1!) &&
               IsHigherHigh(currentHigh, candleplus1!) &&
               IsHigherHigh(currentHigh, candleplus2!);
    }

    // Check for two previous bars with Lower Highs, two equals highests bars, two next bars with Lower Highs
    // 123321, 213321, 113311, 113322, etc..
    // currentBar=Second EQ bar
    private bool Has_LH_LH_EQ_EQ_LH_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus3!) &&
               IsHigherHigh(currentHigh, candleminus2!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus1!)) &&
               IsHigherHigh(currentHigh, candleplus1!) &&
               IsHigherHigh(currentHigh, candleplus2!);
    }

    // Check for two previous bars with Lower Highs, three equals highests bars, two next bars with Lower Highs
    // 1233321, 2133321, 1133311, 1133322, etc..
    // currentBar=Third EQ bar
    private bool Has_LH_LH_EQ_EQ_EQ_LH_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus4!) &&
               IsHigherHigh(currentHigh, candleminus3!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus2!)) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus1!)) &&
               IsHigherHigh(currentHigh, candleplus1!) &&
               IsHigherHigh(currentHigh, candleplus2!);
    }

    // Check for two previous bars with Lower Highs, four equals highests bars, two next bars with Lower Highs
    // 12333321, 21333321, 11333311, 11333322, etc..
    // currentBar=Fourth EQ bar
    private bool Has_LH_LH_EQ_EQ_EQ_EQ_LH_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus5!) &&
               IsHigherHigh(currentHigh, candleminus4!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus3!)) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus2!)) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus1!)) &&
               IsHigherHigh(currentHigh, candleplus1!) &&
               IsHigherHigh(currentHigh, candleplus2!);
    }

    // Check for one bar with Lower Highs, one highest bar, one bar with Lower Highs, one equals
    // to highest bar, one with Lower Highs, one equals to highest bar and one with Lower Highs
    // 1313131, 2313231, etc...
    // currentBar=Third EQ bar
    private bool Has_LH_EQ_LH_EQ_LH_EQ_LH(int index)
    {
        decimal currentHigh = GetHighValue(candlecurrent!);
        return IsHigherHigh(currentHigh, candleminus5!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus4!)) &&
               IsHigherHigh(currentHigh, candleminus3!) &&
               ValuesAreEqual(currentHigh, GetHighValue(candleminus2!)) &&
               IsHigherHigh(currentHigh, candleminus1!) &&
               IsHigherHigh(currentHigh, candleplus1!);
    }

    // === SWING LOW ROUTINES ===

    // Check for two previous bars and two next bars with Higher Lows 
    // 32123, 23132, 22122, etc...
    private bool Has_HL_HL_AND_HL_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus2!) &&
               IsLowerLow(currentLow, candleminus1!) &&
               IsLowerLow(currentLow, candleplus1!) &&
               IsLowerLow(currentLow, candleplus2!);
    }

    // Check for two previous bars with Higher Lows, one lowest bar, one lower hight 
    // bar, one equals to lowest bar and two next bars with Higher Lows
    // 1232312, 1131311, 2231311, etc..
    // currentBar=Second EQ bar
    private bool Has_HL_HL_EQ_HL_EQ_HL_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus4!) &&
               IsLowerLow(currentLow, candleminus3!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus2!)) &&
               IsLowerLow(currentLow, candleminus1!) &&
               IsLowerLow(currentLow, candleplus1!) &&
               IsLowerLow(currentLow, candleplus2!);
    }

    // Check for two previous bars with Higher Lows, two equals lowests bars, two next bars with Higher Lows
    // 123321, 213321, 113311, 113322, etc..
    // currentBar=Second EQ bar
    private bool Has_HL_HL_EQ_EQ_HL_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus3!) &&
               IsLowerLow(currentLow, candleminus2!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus1!)) &&
               IsLowerLow(currentLow, candleplus1!) &&
               IsLowerLow(currentLow, candleplus2!);
    }

    // Check for two previous bars with Higher Lows, three equals lowests bars, two next bars with Higher Lows
    // 1233321, 2133321, 1133311, 1133322, etc..
    // currentBar=Third EQ bar
    private bool Has_HL_HL_EQ_EQ_EQ_HL_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus4!) &&
               IsLowerLow(currentLow, candleminus3!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus2!)) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus1!)) &&
               IsLowerLow(currentLow, candleplus1!) &&
               IsLowerLow(currentLow, candleplus2!);
    }

    // Check for two previous bars with Higher Lows, four equals lowests bars, two next bars with Higher Lows
    // 12333321, 21333321, 11333311, 11333322, etc..
    // currentBar=Fourth EQ bar
    private bool Has_HL_HL_EQ_EQ_EQ_EQ_HL_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus5!) &&
               IsLowerLow(currentLow, candleminus4!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus3!)) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus2!)) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus1!)) &&
               IsLowerLow(currentLow, candleplus1!) &&
               IsLowerLow(currentLow, candleplus2!);
    }

    // Check for one bar with Higher Lows, one lowest bar, one bar with Higher Lows, one equals
    // to lowest bar, one with Higher Lows, one equals to lowest bar and one with Higher Lows
    // 1313131, 2313231, etc...
    // currentBar=Third EQ bar
    private bool Has_HL_EQ_HL_EQ_HL_EQ_HL(int index)
    {
        decimal currentLow = GetLowValue(candlecurrent!);
        return IsLowerLow(currentLow, candleminus5!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus4!)) &&
               IsLowerLow(currentLow, candleminus3!) &&
               ValuesAreEqual(currentLow, GetLowValue(candleminus2!)) &&
               IsLowerLow(currentLow, candleminus1!) &&
               IsLowerLow(currentLow, candleplus1!);
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
        // Or if previous distance was really high say 50%?
        if (100 * Math.Abs(candleValue - value) / value > 25)
            return true;

        //if (LastSwingLow != null && candleValue < LastSwingLow.Value)
        //    return false;

        return false;
    }

    private void AddNewHigh(CryptoCandle candle, decimal candleValue)
    {
        if (ShowSecondary || CanAddNewHigh(candleValue))
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
        int index = 0;
        bool isSwingHigh = Has_LH_LH_AND_LH_LH(index);
        if (!isSwingHigh)
            isSwingHigh = Has_LH_LH_EQ_LH_EQ_LH_LH(index);
        if (!isSwingHigh)
            isSwingHigh = Has_LH_LH_EQ_EQ_LH_LH(index);
        if (!isSwingHigh)
            isSwingHigh = Has_LH_LH_EQ_EQ_EQ_LH_LH(index);
        if (!isSwingHigh)
            isSwingHigh = Has_LH_LH_EQ_EQ_EQ_EQ_LH_LH(index);
        if (!isSwingHigh)
            isSwingHigh = Has_LH_EQ_LH_EQ_LH_EQ_LH(index);

        var candleValue = GetHighValue(candleminus3!);
        if (isSwingHigh)
            //if (GetHighValue(candleminus5!) <= candleValue && GetHighValue(candleminus4!) <= candleValue && GetHighValue(candleminus2!) <= candleValue && GetHighValue(candleminus1!) <= candleValue)
            AddNewHigh(candleminus3!, candleValue);
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

        return false;
    }

    private void AddNewLow(CryptoCandle candle, decimal candleValue)
    {
        if (ShowSecondary || CanAddNewLow(candleValue))
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
        // ---- Swing Lows---
        int index = 0;
        bool isSwingLow = Has_HL_HL_AND_HL_HL(index);
        if (!isSwingLow)
            isSwingLow = Has_HL_HL_EQ_HL_EQ_HL_HL(index);
        if (!isSwingLow)
            isSwingLow = Has_HL_HL_EQ_EQ_HL_HL(index);
        if (!isSwingLow)
            isSwingLow = Has_HL_HL_EQ_EQ_EQ_HL_HL(index);
        if (!isSwingLow)
            isSwingLow = Has_HL_HL_EQ_EQ_EQ_EQ_HL_HL(index);
        if (!isSwingLow)
            isSwingLow = Has_HL_EQ_HL_EQ_HL_EQ_HL(index);

        // Do we have a new low?
        var candleValue = GetLowValue(candleminus3!);
        if (isSwingLow)
            //if (GetLowValue(candleminus5!) >= candleValue && GetLowValue(candleminus4!) >= candleValue && GetLowValue(candleminus2!) >= candleValue && GetLowValue(candleminus1!) >= candleValue)
            AddNewLow(candleminus3!, candleValue);
    }


    private void Init()
    {
        // Fixes because of unnoticed BOS at the right
        // Remove the dummy ZigZag points
        if (AddedDummyZigZag.Count > 0)
        {
            foreach (var zigZag in AddedDummyZigZag)
                ZigZagList.Remove(zigZag);
            AddedDummyZigZag.Clear();
        }
    }


    private void Finish()
    {
        // Fixes because of unnoticed BOS at the right
        // Did we have an unnoticed BOS (because there didn't form a L or H in the last 5 candles but the
        // LAST candle was a lower/higher then the previous H/L! (this is important for trend decisions)
        // Fix: add a dummy ZigZagResult and remove it in the next call

        // Remark: What if we add it regulary? Do we need this? Simple comparison in the IsHighPoint and IsLowPoint!
        // Or even better, add the last added candle 2 times extra? The Optimize will filter the point away ...
        // (no, if we add the candle the Previous* will be off, but the idea is still interesting).

        if (ZigZagList.Count > 1 && CandleList.Count > 1 && LastSwingLow != null && LastSwingHigh != null)
        {
            List<CryptoCandle> list = [candleminus1, candleminus2, candleminus3, candleminus4];
            foreach (CryptoCandle candle in list)
            {
                if (candle.OpenTime <= LastSwingLow.Candle.OpenTime)
                    break;

                if (candle.GetLowValue(UseHighLow) < LastSwingLow.Value)
                {
                    if (LastSwingLow.PointType != 'H' && GetHighFromBuffer(out CryptoCandle? swing))
                    {
                        ZigZagResult dummyHigh = new() { PointType = 'H', Candle = swing!, Value = swing!.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(dummyHigh);
                        AddedDummyZigZag.Add(dummyHigh);
                        break;
                    }

                    if (LastSwingLow.PointType != 'L') // dont repeat it 
                    {
                        ZigZagResult dummyLow = new() { PointType = 'L', Candle = candle, Value = candle.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(dummyLow);
                        AddedDummyZigZag.Add(dummyLow);
                        break;
                    }
                }
                else if (candle.GetHighValue(UseHighLow) > LastSwingHigh.Value)
                {
                    if (LastSwingLow.PointType != 'L' && GetLowFromBuffer(out CryptoCandle? swing))
                    {
                        ZigZagResult dummyLow = new() { PointType = 'L', Candle = swing!, Value = swing!.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(dummyLow);
                        AddedDummyZigZag.Add(dummyLow);
                        break;
                    }

                    if (LastSwingLow.PointType != 'H') // dont repeat it 
                    {
                        ZigZagResult dummyHigh = new() { PointType = 'H', Candle = candle, Value = candle.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(dummyHigh);
                        AddedDummyZigZag.Add(dummyHigh);
                        break;
                    }
                }
            }
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
        //if (candle!.DateLocal >= new DateTime(2024, 11, 07, 18, 00, 0, DateTimeKind.Local))
        //    candle = candle; // debug 
        CandleCount++;

        // we need buffer of 8 candles to detect a low or high point
        candleminus5 = candleminus4;
        candleminus4 = candleminus3;
        candleminus3 = candleminus2;
        candleminus2 = candleminus1;
        candleminus1 = candlecurrent;
        candlecurrent = candleplus1;
        candleplus1 = candleplus2;
        candleplus2 = candle;

        if (candleminus5 != null)
        {
            Init();
            CheckNewLow();
            CheckNewHigh();
            OptimizeList();
        }
    }


    public void FinishJob()
    {
        Finish();
        //// Afterwards, if the last candle broke the L or H (BOS) we need to add a temporary point
        //// I'm not sure how this exactly works out, we make a temporary point more or less permanent!!
        //// Chances are high a BOS will lead to another high/low, but technically if can go the other way..
        //var candleValue = GetLowValue(candleminus1!);
        //if (LastSwingLow != null && candleValue < LastSwingLow.Value && GetLowValue(candleminus2!) >= candleValue && GetLowValue(candleminus3!) >= candleValue)
        //    AddNewLow(candleminus1!, candleValue);
        //else
        //{
        //    candleValue = GetLowValue(candleminus2!);
        //    if (LastSwingLow != null && candleValue < LastSwingLow.Value && GetLowValue(candleminus3!) >= candleValue && GetLowValue(candleminus4!) >= candleValue)
        //        AddNewLow(candleminus1!, candleValue);
        //}


        //candleValue = GetHighValue(candleminus1!);
        //if (LastSwingHigh != null && candleValue > LastSwingHigh.Value && GetHighValue(candleminus2!) <= candleValue && GetHighValue(candleminus3!) <= candleValue)
        //    AddNewHigh(candleminus1!, candleValue);
        //else
        //{
        //    candleValue = GetLowValue(candleminus2!);
        //    if (LastSwingHigh != null && candleValue > LastSwingHigh.Value && GetHighValue(candleminus3!) <= candleValue && GetHighValue(candleminus4!) <= candleValue)
        //        AddNewHigh(candleminus1!, candleValue);
        //}

        //RemoveDummyPoints();
        OptimizeList();
        //TryAddDummyPoints();
    }

}