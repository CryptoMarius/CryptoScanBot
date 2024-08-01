using CryptoScanBot.Core.Model;

using System.Runtime.CompilerServices;

// Original, but does not seem to work properly and there is lot of unneeded(?) iterations (heavy on the cpu)
// https://github.com/StockSharp/StockSharp/blob/master/Algo/Indicators/ZigZag.cs

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator1(SortedList<long, CryptoCandle> candleList, bool useHighLow)
{
    // data
    private readonly SortedList<long, CryptoCandle> CandleList = candleList;

    // params
    public bool UseHighLow { get; set; } = useHighLow;
    public int BackStep { get; set; } = 3;
    public int Depth { get; set; } = 12;
    public int Deviation { get; set; } = 5;

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetHighValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return (double)candle.High;
        else
            return (double)Math.Max(candle.Open, candle.Close);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetLowValue(CryptoCandle candle)
    {
        if (UseHighLow)
            return (double)candle.Low;
        else
            return (double)Math.Min(candle.Open, candle.Close);
    }


    public void Calculate(int candleIndex)
    {
        var c = CandleList.Values[candleIndex];
        c.ZigZagLow = 0;
        c.ZigZagHigh = 0;

        if (candleIndex <= Depth)
            return;


        // Determine limit, go "back" 3 zigzag points (?)
        int zigzagPoints = 0;
        var j = candleIndex;
        while (j > Depth)
        {
            c = CandleList.Values[j];
            if (c.ZigZagLow != 0 || c.ZigZagHigh != 0)
            {
                zigzagPoints--;
                if (zigzagPoints >= 3)
                    break;
            }
            j--;
        }
        var limit = candleIndex - j - 1;


        // Main loop
        double lastLow = 0;
        double lastHigh = 0;
        for (var i = candleIndex - limit; i <= candleIndex; i++)
        {
            c = CandleList.Values[i];

            double low = (double)c.Low;
            double high = (double)c.High;
            for (int depth = 1; depth < Depth; depth++)
            {
                CryptoCandle b = CandleList.Values[i - depth];
                double v = GetLowValue(b);
                if (v < low) low = v;
                v = GetHighValue(b);
                if (v > high) high = v;
            }

            // Calculate low
            if (low == lastLow)
                low = 0;
            else
            {
                lastLow = low;
                if (GetLowValue(c) - low > Deviation * low / 100)
                    low = 0;
                else
                {
                    for (int backStep = 1; backStep <= BackStep; backStep++)
                    {
                        if (i - backStep >= 0)
                        {
                            var b = CandleList.Values[i - backStep];
                            var res = b.ZigZagLow;
                            if (res != 0 && res > low)
                                b.ZigZagLow = 0.0;
                        }
                    }
                }
            }
            if (GetLowValue(c) == low)
                c.ZigZagLow = low;
            else
                c.ZigZagLow = 0;



            // Calculate high
            if (high == lastHigh)
                high = 0;
            else
            {
                lastHigh = high;
                if (high - GetHighValue(c) > Deviation * high / 100)
                    high = 0;
                else
                {
                    for (int backStep = 1; backStep <= BackStep; backStep++)
                    {
                        if (i - backStep >= 0)
                        {
                            var b = CandleList.Values[i - backStep];
                            var res = b.ZigZagHigh;
                            if (res != 0 && res < high)
                                b.ZigZagHigh = 0.0;
                        }
                    }
                }
            }
            if (GetHighValue(c) == high)
                c.ZigZagHigh = high;
            else
                c.ZigZagHigh = 0;
        }



        // final cutting
        lastLow = -1;
        lastHigh = -1;
        var lastLowPos = -1;
        var lastHighPos = -1;

        for (var i = candleIndex - limit; i <= candleIndex; i++)
        {
            c = CandleList.Values[i];

            var curLow = c.ZigZagLow;
            var curHigh = c.ZigZagHigh;
            if (curLow == 0 && curHigh == 0)
                continue;

            if (curHigh != 0)
            {
                if (lastHigh > 0)
                {
                    if (lastHigh < curHigh)
                        CandleList.Values[lastHighPos].ZigZagHigh = 0;
                    else
                        c.ZigZagHigh = 0;
                }
                if (lastHigh < curHigh || lastHigh < 0)
                {
                    lastHigh = curHigh;
                    lastHighPos = i;
                }
                lastLow = -1;
            }

            if (curLow != 0)
            {
                if (lastLow > 0)
                {
                    if (lastLow > curLow)
                        CandleList.Values[lastLowPos].ZigZagLow = 0;
                    else
                        c.ZigZagLow = 0;
                }
                if (curLow < lastLow || lastLow < 0)
                {
                    lastLow = curLow;
                    lastLowPos = i;
                }
                lastHigh = -1;
            }
        }
    }

}