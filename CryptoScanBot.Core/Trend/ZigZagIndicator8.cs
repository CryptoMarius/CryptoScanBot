using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicator8(SortedList<long, CryptoCandle> candleList, bool useHighLow)
{
    public bool UseHighLow = useHighLow;
    public int Depth { get; set; } = 12;
    public decimal Deviation { get; set; } = 5.0m;
    public int BackStep { get; set; } = 3;

    public int CandleCount { get; set; } = 0;
    public List<ZigZagResult> ZigZagList { get; set; } = [];
    private readonly SortedList<long, CryptoCandle> CandleList = candleList;

    // for remove duplicate highs/lows
    private ZigZagResult? Previous = null;


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


    public void Calculate(CryptoCandle candle, int duration)
    {
        CandleCount++;

        // Get the low and high value from the last (count=depth) candles
        long key = candle.OpenTime - duration;
        decimal lowFromLastDepth = GetLowValue(candle);
        decimal highFromLastDepth = GetHighValue(candle);
        for (int count = 0; count < Depth - 1; count++)
        {
            if (CandleList.TryGetValue(key, out CryptoCandle? x))
            {
                decimal value = GetLowValue(x);
                if (value < lowFromLastDepth)
                    lowFromLastDepth = value;
                value = GetHighValue(x);
                if (value > highFromLastDepth)
                    highFromLastDepth = value;
            }
            key -= duration;
        }


        //if (candle.DateLocal >= new DateTime(2024, 10, 17, 17, 0, 0, DateTimeKind.Local))
        //    candle = candle; // debug 

        // is the current candle a new low?
        decimal lowFromLastCandle = GetLowValue(candle);
        if (Math.Abs(lowFromLastCandle - lowFromLastDepth) == 0)
        {
            bool Addzigzag = true;
            if (Previous != null)
            {
                // ignore multiple low's after each other (reuse/remove the previous low)
                if (Previous.PointType == 'L' && lowFromLastDepth <= Previous.Value)
                {
                    //if (candle.Low < Previous.Candle.Low)
                    Previous.ReusePoint(candle, lowFromLastDepth);
                    return;
                }
                else if (Previous.PointType == 'L' && lowFromLastDepth > Previous.Value)
                    Addzigzag = false; // repeated low, previous was lower
                else if (Previous.PointType == 'H' && Math.Abs(lowFromLastDepth - Previous.Value) < Deviation * lowFromLastDepth / 100)
                    Addzigzag = false; // not past the threshold
                else if (Previous.Candle!.OpenTime + BackStep * duration >= candle!.OpenTime)
                    Addzigzag = false; // no pivot allowed within X candles
            }

            if (Addzigzag)
            {
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
        }


        // is the current candle a new high?
        decimal highFromLastCandle = GetHighValue(candle);
        if (Math.Abs(highFromLastCandle - highFromLastDepth) == 0)
        {
            bool Addzigzag = true;
            if (Previous != null)
            {
                // ignore multiple high's after each other (reuse/remove the previous high)
                if (Previous.PointType == 'H' && highFromLastDepth >= Previous.Value)
                {
                    //if (candle.High > Previous.Candle.High)
                        Previous.ReusePoint(candle, highFromLastDepth);
                    return;
                }
                else if (Previous.PointType == 'H' && highFromLastDepth < Previous.Value)
                    Addzigzag = false; // repeated high, previous was higher
                else if (Previous.PointType == 'L' && Math.Abs(highFromLastDepth - Previous.Value) < Deviation * highFromLastDepth / 100)
                    Addzigzag = false; // not past the threshold
                else if (Previous.Candle!.OpenTime + BackStep * duration >= candle!.OpenTime)
                    Addzigzag = false; // no pivot allowed within X candles
            }

            if (Addzigzag)
            {
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

}