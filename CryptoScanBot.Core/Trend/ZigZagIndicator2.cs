using CryptoScanBot.Core.Model;

using System.Runtime.CompilerServices;

namespace CryptoScanBot.Core.Trend;

// First version using indexes
// https://ctrader.com/algos/indicators/show/157

public class ZigZagIndicator2(SortedList<long, CryptoCandle> candleList, bool useHighLow)
{
    public bool UseHighLow = useHighLow;
    public int Depth { get; set; } = 12;
    public int Deviation { get; set; } = 5;
    public int BackStep { get; set; } = 3;
    public SortedList<long, CryptoCandle> CandleList = candleList;


    // confusing, why so many variables?
    //private double _low;
    //private double _high;
    private double lastLow;
    private double lastHigh;

    //private int pivotType = 0;

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


    public void Calculate(int index)
    {
        // Hint: This process would be faster if we use the dictionary and use the time to lookup values
        // this removes the need for locking (I think)

        CryptoCandle c = CandleList.Values[index];
        c.ZigZagLow = 0;
        c.ZigZagHigh = 0;

        // Warming up
        if (index < Depth)
            return;

        // returns the lowest and largest number in the last x candles
        // (Dictionary: Just go back count=Depth-1 and do comparison)
        double currentLow = GetLowValue(c);
        double currentHigh = GetHighValue(c);
        for (int i = index - Depth + 1; i < index; i++)
        {
            CryptoCandle x = CandleList.Values[i];
            double v = GetLowValue(x);
            if (v < currentLow)
                currentLow = v;
            v = GetHighValue(x);
            if (v > currentHigh)
                currentHigh = v;
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
                // (Dictionary: Just go back count=BackStep and erase last lows)
                for (int i = 1; i <= BackStep; i++)
                {
                    CryptoCandle x = CandleList.Values[index - i];
                    if (x.ZigZagLow > double.Epsilon && x.ZigZagLow > currentLow)
                        x.ZigZagLow = 0;
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
                // (Dictionary: Just go back count=BackStep and erase last highs)
                for (int i = 1; i <= BackStep; i++)
                {
                    CryptoCandle x = CandleList.Values[index - i];
                    if (x.ZigZagHigh > double.Epsilon && x.ZigZagHigh < currentHigh)
                        x.ZigZagHigh = 0;
                }
            }
        }
        if (Math.Abs(high - currentHigh) < double.Epsilon)
            c.ZigZagHigh = currentHigh;
        else
            c.ZigZagHigh = 0;


        // Code is for ignoring a repeated low/high but it does not work properly..
        // (the use of the backstep also works against this piece of code, so just ignore for now)
        // Further in the process we loop the candlelist (again) and remove repeated L and H's.
        // KISS!

        //switch (pivotType)
        //{
        //    case 0: // once after warmup
        //        if (_low < double.Epsilon && _high < double.Epsilon)
        //        {
        //            if (c.ZigZagLow > double.Epsilon)
        //            {
        //                pivotType = 1;
        //                _low = low;
        //                c.ZigZagLow = low;
        //            }
        //            if (c.ZigZagHigh > double.Epsilon)
        //            {
        //                pivotType = -1;
        //                _high = high;
        //                c.ZigZagHigh = high;
        //            }
        //        }
        //        break;
        //    case 1:
        //        if (c.ZigZagLow > double.Epsilon && c.ZigZagLow < _low && c.ZigZagHigh < double.Epsilon)
        //        {
        //            // does not work
        //            //lastLowCandle.ZigZagLow = double.NaN;
        //            //lastLowCandle.ZigZagValue = double.NaN;
        //            _low = c.ZigZagLow;
        //        }
        //        if (c.ZigZagHigh > double.Epsilon && c.ZigZagLow < double.Epsilon)
        //        {
        //            pivotType = -1;
        //            _high = c.ZigZagHigh;
        //        }
        //        break;
        //    case -1:
        //        if (c.ZigZagHigh > double.Epsilon && c.ZigZagHigh > _high && c.ZigZagLow < double.Epsilon)
        //        {
        //            // does not work
        //            //lastHighCandle.ZigZagLow = double.NaN;
        //            //lastHighCandle.ZigZagValue = double.NaN;
        //            _high = c.ZigZagHigh;
        //        }
        //        if (c.ZigZagLow > double.Epsilon && c.ZigZagHigh <= double.Epsilon)
        //        {
        //            pivotType = 1;
        //            _low = c.ZigZagLow;
        //        }
        //        break;
        //}

    }
}