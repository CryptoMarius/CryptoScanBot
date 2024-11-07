using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator9(SortedList<long, CryptoCandle> candleList, bool useHighLow, decimal deviation)
{
    public bool UseHighLow = useHighLow;
    //public int Depth { get; set; } = 12;
    public decimal Deviation { get; set; } = deviation;
    //public int BackStep { get; set; } = 3;
    //public bool OptimizeZigZag = optimizeZigZag;
    public bool PostponeFinish = false; // Delay finishing touch (for UI)



    public int CandleCount { get; set; } = 0;
    public List<ZigZagResult> ZigZagList { get; set; } = [];
    private readonly SortedList<long, CryptoCandle> CandleList = candleList;

    
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private decimal GetHighValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return candle.High;
        else
            return Math.Max(candle.Open, candle.Close);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private decimal GetLowValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return candle.Low;
        else
            return Math.Min(candle.Open, candle.Close);
    }


    private CryptoCandle? previous5 = null;
    private CryptoCandle? previous4 = null;
    private CryptoCandle? previous3 = null;
    private CryptoCandle? previous2 = null;
    private CryptoCandle? previous1 = null;

    private List<CryptoCandle> buffer = [];
    
    private ZigZagResult? Previous = null;
    private ZigZagResult? LastSwingLow = null;
    private ZigZagResult? LastSwingHigh = null;
    public ZigZagResult? AddedExtraZigZag1 { get; set; } = null;
    public ZigZagResult? AddedExtraZigZag2 { get; set; } = null;


    private bool GetLowFromBuffer(out CryptoCandle? swing)
    {
        if (buffer.Count == 0)
            swing = null;
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
        if (buffer.Count == 0)
            swing = null;
        swing = null;
        foreach (var x in buffer)
        {
            if (swing == null || GetHighValue(x) > GetHighValue(swing))
                swing = x;
        }
        return swing != null;
    }


    private ZigZagResult AddToZigZag(CryptoCandle candle, char pointType)
    {
        buffer.Clear();
        decimal value;
        if (pointType == 'L')
            value = GetLowValue(candle);
        else
            value = GetHighValue(candle);


        //if (Previous != null)
        //{
        //    if (Previous.PointType == 'H' && pointType == 'L' && value > Previous.Value)
        //    {
        //        Previous = Previous;
        //    }

        //    if (Previous.PointType == 'L' && pointType == 'H' && value < Previous.Value)
        //    {
        //        Previous = Previous;
        //    }
        //}


        if (Previous?.PointType == pointType)
        {
            if (pointType == 'L')
            {
                if (value <= Previous.Value)
                {
                    Previous.ReusePoint(candle, value); // repeated low
                    return Previous;
                }
                //else if (value == Previous.Value && candle1.Low < Previous.Candle.Low)
                //{
                //    Previous.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                //else if (value == Previous.Value)
                //{
                //    Previous.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                else
                    return Previous;
            }
            else
            {
                if (value >= Previous.Value)
                {
                    Previous.ReusePoint(candle, value); // repeated high
                    return Previous;
                }
                //else if (value == Previous.Value && candle1.High > Previous.Candle.High)
                //{
                //    Previous.ReusePoint(candle1, value); // prefer the one with the biggest wick
                //    return candle1;
                //}
                else
                    return Previous;
            }
        }

        ZigZagResult zigZag = new() { PointType = pointType, Candle = candle, Value = value, Dominant = false, };
        ZigZagList.Add(zigZag);
        Previous = zigZag;
        return Previous;
    }

    private void TestSwingHigh()
    {
        // Do we have a new high?
        var candleValue = GetHighValue(previous3!);
        if (GetHighValue(previous5!) <= candleValue && GetHighValue(previous4!) <= candleValue &&
            GetHighValue(previous2!) <= candleValue && GetHighValue(previous1!) <= candleValue)
        {
            if (LastSwingHigh == null)
                LastSwingHigh = AddToZigZag(previous3!, 'H');
            else if (candleValue > GetHighValue(LastSwingHigh.Candle)) // if the candle1 broke the last swing high we can calculate the lowest point
            {
                //decimal diff = candleValue - GetHighValue(LastSwingHigh.Candle);
                //decimal perc = 100 * diff / candleValue;
                //if (perc < (decimal)Deviation)
                //{
                //    buffer.Clear();
                //    LastSwingHigh.ReusePoint(previous3!, candleValue);
                //}
                //else
                {
                    if (GetLowFromBuffer(out CryptoCandle? swing) && GetLowValue(swing!) < LastSwingHigh.Value)
                        LastSwingLow = AddToZigZag(swing!, 'L');
                    LastSwingHigh = AddToZigZag(previous3!, 'H');
                }
            }
            else
                buffer.Add(previous3!); // for calculating high or low
        }
    }

    private void TestSwingLow()
    {
        // Do we have a new low?
        var candleValue = GetLowValue(previous3!);
        if (GetLowValue(previous5!) >= candleValue && GetLowValue(previous4!) >= candleValue &&
            GetLowValue(previous2!) >= candleValue && GetLowValue(previous1!) >= candleValue)
        {
            if (LastSwingLow == null)
                LastSwingLow = AddToZigZag(previous3!, 'L');
            else if (candleValue < GetLowValue(LastSwingLow.Candle)) // if the candle1 broke the last swing low we can calculate the highest point
            {
                //decimal diff = GetLowValue(LastSwingLow.Candle) - candleValue;
                //decimal perc = 100 * diff / candleValue;
                //if (perc < (decimal)Deviation)
                //    LastSwingLow.ReusePoint(previous3!, candleValue);
                //else
                {
                    if (GetHighFromBuffer(out CryptoCandle? swing) && GetHighValue(swing!) > LastSwingLow.Value)
                        LastSwingHigh = AddToZigZag(swing!, 'H');
                    LastSwingLow = AddToZigZag(previous3!, 'L');
                }
            }
            else
                buffer.Add(previous3!); // for calculating high or low
        }
    }



    private void Init()
    {
        // Remove the unnoticed BOS (see Finish)
        if (AddedExtraZigZag1 != null)
        {
            ZigZagList.Remove(AddedExtraZigZag1);
            AddedExtraZigZag1 = null;
        }
        if (AddedExtraZigZag2 != null)
        {
            ZigZagList.Remove(AddedExtraZigZag2);
            AddedExtraZigZag2 = null;
        }
    }

    private void Finish()
    {
        // Did we have an unnoticed BOS (because there didn't form a L or H in the last
        // 5 candles but the candle1 was lower/higher! (important for trend decisions)
        // Fix: add a dummy ZigZagResult and remove it in the next call
        if (ZigZagList.Count > 1 && CandleList.Count > 1)
        {
            if (LastSwingLow != null && LastSwingHigh != null)
            {
                ZigZagResult lastZigZag;
                if (LastSwingLow.Candle.OpenTime > LastSwingHigh.Candle.OpenTime)
                    lastZigZag = LastSwingLow;
                else
                    lastZigZag = LastSwingHigh;

                //ZigZagResult p1 = ZigZagList[^1];
                //ZigZagResult p2 = ZigZagList[^2];
                List<CryptoCandle> list = [];
                list.Add(CandleList.Values[^1]);
                list.Add(CandleList.Values[^2]);
                list.Add(CandleList.Values[^3]);


                foreach (CryptoCandle candle in list)
                {
                    if (candle.OpenTime <= lastZigZag.Candle.OpenTime)
                        break;

                    if (candle.GetLowValue(UseHighLow) < LastSwingLow.Value)
                    {
                        if (lastZigZag.PointType != 'H' && GetHighFromBuffer(out CryptoCandle? swing))
                        {
                            AddedExtraZigZag2 = new() { PointType = 'H', Candle = swing!, Value = swing!.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
                            ZigZagList.Add(AddedExtraZigZag2);
                        }
                        AddedExtraZigZag1 = new() { PointType = 'L', Candle = candle, Value = candle.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(AddedExtraZigZag1);
                        break;
                    }
                    else if (candle.GetHighValue(UseHighLow) > LastSwingHigh.Value)
                    {
                        if (lastZigZag.PointType != 'L' && GetLowFromBuffer(out CryptoCandle? swing))
                        {
                            AddedExtraZigZag2 = new() { PointType = 'L', Candle = swing!, Value = swing!.GetLowValue(UseHighLow), Dominant = false, Dummy = true, };
                            ZigZagList.Add(AddedExtraZigZag2);
                        }

                        AddedExtraZigZag1 = new() { PointType = 'H', Candle = candle, Value = candle.GetHighValue(UseHighLow), Dominant = false, Dummy = true, };
                        ZigZagList.Add(AddedExtraZigZag1);
                        break;
                    }
                }
            }
        }
    }

    public void OptimizeList()
    {
        int index = 1;
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

        // could have removed the last swing high/low, recalculate them
        if (recalculate)
        {
            LastSwingLow = null;
            LastSwingHigh = null;
            if (ZigZagList.Count > 0)
            {
                var l = ZigZagList[^1];
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
        CandleCount++;

        // Advance ... buffer of the last 5 candles, to detect swing low or swing high
        previous5 = previous4;
        previous4 = previous3;
        previous3 = previous2;
        previous2 = previous1;
        previous1 = candle; // current

        // We need 5 candles to start
        if (previous5 != null)
        {
            //if (candle!.DateLocal >= new DateTime(2024, 11, 07, 18, 00, 0, DateTimeKind.Local))
            //    candle = candle; // debug 

            if (!PostponeFinish)
                Init();
            TestSwingLow();
            TestSwingHigh();
            if (!PostponeFinish)
            {
                if (Deviation > 0)
                    OptimizeList();
                Finish();
            }
        }
    }


    public void FinishJob()
    {
        Init();
        if (Deviation > 0)
            OptimizeList();
        Finish();
    }

}