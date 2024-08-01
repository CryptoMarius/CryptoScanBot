using CryptoScanBot.Core.Model;

using System.Runtime.CompilerServices;

namespace CryptoScanBot.Core.Trend;

// Second version using dictionary
// https://ctrader.com/algos/indicators/show/157

public class ZigZagIndicator3(SortedList<long, CryptoCandle> candleList, bool useHighLow)
{
    public bool UseHighLow = useHighLow;
    public int Depth { get; set; } = 12;
    public int Deviation { get; set; } = 5;
    public int BackStep { get; set; } = 3;
    public SortedList<long, CryptoCandle> CandleList = candleList;


    // confusing, why so many variables?
    private double lastLow;
    private double lastHigh;

    // remove duplicate highs or lows
    private CryptoCandle? Previous = null;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetHighValue(CryptoCandle candle)
    {
        if (useHighLow)
            return (double)candle.High;
        else
            return (double)Math.Max(candle.Open, candle.Close);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetLowValue(CryptoCandle candle)
    {
        if (useHighLow)
            return (double)candle.Low;
        else
            return (double)Math.Min(candle.Open, candle.Close);
    }


    public void Calculate(CryptoCandle c, int duration)
    {
        c.ZigZagLow = 0;
        c.ZigZagHigh = 0;


        // returns the lowest and largest number in the last x candles
        // (Dictionary: Just go back count=Depth-1 and do comparison)
        long key = c.OpenTime - duration;
        double currentLow = GetLowValue(c);
        double currentHigh = GetHighValue(c);
        for (int count = 0; count < Depth - 1; count++)
        {
            if (CandleList.TryGetValue(key, out CryptoCandle? x))
            {
                double v = GetLowValue(x);
                if (v < currentLow)
                    currentLow = v;
                v = GetHighValue(x);
                if (v > currentHigh)
                    currentHigh = v;
            }
            key -= duration;
        }



        // low
        double low = GetLowValue(c);
        if (Math.Abs(currentLow - lastLow) < double.Epsilon) 
            currentLow = 0;
        else
        {
            lastLow = currentLow;
            if (low - currentLow > Deviation * currentLow / 100)
                currentLow = 0;
            else
            {
                key = c.OpenTime - duration;
                for (int count = 0; count < BackStep; count++)
                {
                    if (CandleList.TryGetValue(key, out CryptoCandle? x))
                    {
                        if (x.ZigZagLow > double.Epsilon && x.ZigZagLow > currentLow)
                            x.ZigZagLow = 0;
                    }
                    key -= duration;
                }
            }
        }
        if (Math.Abs(low - currentLow) < double.Epsilon)
            c.ZigZagLow = currentLow;
        else
            c.ZigZagLow = 0;



        // high
        double high = GetHighValue(c);
        if (Math.Abs(currentHigh - lastHigh) < double.Epsilon)
            currentHigh = 0;
        else
        {
            lastHigh = currentHigh;
            if (currentHigh - high > Deviation * currentHigh / 100)
                currentHigh = 0;
            else
            {
                key = c.OpenTime - duration;
                for (int count = 0; count < BackStep; count++)
                {
                    if (CandleList.TryGetValue(key, out CryptoCandle? x))
                    {
                        if (x.ZigZagHigh > double.Epsilon && x.ZigZagHigh < currentHigh)
                            x.ZigZagHigh = 0;
                    }
                    key -= duration;
                }
            }
        }
        if (Math.Abs(high - currentHigh) < double.Epsilon)
            c.ZigZagHigh = currentHigh;
        else
            c.ZigZagHigh = 0;


        // Remove repeated low/high points
        if (c.ZigZagHigh > 0 || c.ZigZagLow > 0)
        {
            if (Previous != null)
            {
                if (c.ZigZagLow > 0 && Previous.ZigZagLow > 0)
                    Previous.ZigZagLow = 0; // erase duplicate
                if (c.ZigZagHigh > 0 && Previous.ZigZagHigh > 0)
                    Previous.ZigZagHigh = 0; // erase duplicate
            }
            Previous = c;
        }

        // Woud be lovely though if it could build a proper zigzag list (without having to iterate the candlelist again)

    }
}