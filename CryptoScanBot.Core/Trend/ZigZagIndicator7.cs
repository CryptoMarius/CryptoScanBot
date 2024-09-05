using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator7(SortedList<long, CryptoCandle> candleList, bool useHighLow)
{
    public bool UseHighLow = useHighLow;
    public int Depth { get; set; } = 12;
    public double Deviation { get; set; } = 5.0;
    public int BackStep { get; set; } = 3;

    public int CandleCount { get; set; } = 0;
    public readonly List<ZigZagResult> ZigZagList = [];
    private readonly SortedList<long, CryptoCandle> CandleList = candleList;

    // for remove duplicate highs/lows
    private ZigZagResult? Previous = null;


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetHighValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return (double)candle.High;
        else
            return (double)Math.Max(candle.Open, candle.Close);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetLowValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return (double)candle.Low;
        else
            return (double)Math.Min(candle.Open, candle.Close);
    }


    public void Calculate(CryptoCandle candle, int duration)
    {
        CandleCount++;

        // Get the low and high value from the last (count=depth) candles
        long key = candle.OpenTime - duration;
        double lowFromLastDepth = GetLowValue(candle);
        double highFromLastDepth = GetHighValue(candle);
        for (int count = 0; count < Depth - 1; count++)
        {
            if (CandleList.TryGetValue(key, out CryptoCandle? x))
            {
                double value = GetLowValue(x);
                if (value < lowFromLastDepth)
                    lowFromLastDepth = value;
                value = GetHighValue(x);
                if (value > highFromLastDepth)
                    highFromLastDepth = value;
            }
            key -= duration;
        }




        // is the current candle a new low?
        double lowFromLastCandle = GetLowValue(candle);
        if (Math.Abs(lowFromLastCandle - lowFromLastDepth) == 0)
        {
            if (Previous != null)
            {
                if (Previous.PointType == 'L' && lowFromLastDepth <= Previous.Value)
                {
                    Monitor.Enter(ZigZagList);
                    try
                    {
                        // System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
                        //ZigZagList.Remove(Previous); // repeated low, remove last zigzag point
                        int index = ZigZagList.IndexOf(Previous);
                        if (index >= 0)
                            ZigZagList.RemoveAt(index);

                        if (ZigZagList.Count > 0)
                            Previous = ZigZagList.Last();
                        else
                            Previous = null;
                    }
                    finally
                    {
                        Monitor.Exit(ZigZagList);
                    }
                }
                else if (Previous.PointType == 'L' && lowFromLastDepth > Previous.Value)
                    return; // repeated low, previous was lower
                else if (Previous.PointType == 'H' && Math.Abs(lowFromLastDepth - Previous.Value) < Deviation * lowFromLastDepth / 100)
                    return; // not past the threshold
                else if (Previous.Candle!.OpenTime + BackStep * duration >= candle!.OpenTime)
                    return; // no pivot allowed within X candles
            }

            ZigZagResult last = new()
            {
                PointType = 'L',
                Candle = candle,
                Value = lowFromLastDepth,
                Dominant = false,
            };
            ZigZagList.Add(last);
            Previous = last;
            return;
        }


        // is the current candle a new high?
        double highFromLastCandle = GetHighValue(candle);
        if (Math.Abs(highFromLastCandle - highFromLastDepth) == 0)
        {
            if (Previous != null)
            {
                if (Previous.PointType == 'H' && highFromLastDepth >= Previous.Value)
                {
                    // System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
                    //ZigZagList.Remove(Previous); // repeated high, remove the last zigzag point
                    Monitor.Enter(ZigZagList);
                    try
                    {
                        int index = ZigZagList.IndexOf(Previous);
                        if (index >= 0)
                            ZigZagList.RemoveAt(index);

                        if (ZigZagList.Count > 0)
                            Previous = ZigZagList.Last();
                        else
                            Previous = null;
                    }
                    finally
                    {
                        Monitor.Exit(ZigZagList);
                    }
                }
                else if (Previous.PointType == 'H' && highFromLastDepth < Previous.Value)
                    return; // repeated high, previous was higher
                else if (Previous.PointType == 'L' && Math.Abs(highFromLastDepth - Previous.Value) < Deviation * highFromLastDepth / 100)
                    return; // not past the threshold
                else if (Previous.Candle!.OpenTime + BackStep * duration >= candle!.OpenTime)
                    return; // no pivot allowed within X candles
            }

            ZigZagResult last = new()
            {
                PointType = 'H',
                Candle = candle,
                Value = highFromLastDepth,
                Dominant = false,
            };
            ZigZagList.Add(last);
            Previous = last;
            return;
        }
    }

}