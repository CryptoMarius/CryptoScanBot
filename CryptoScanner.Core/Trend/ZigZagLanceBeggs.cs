using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

// === INPUTS ===
// This Indicator Plots Swing Highs & Swing Lows based on Lance Beggs definition:
//
// A Swing High (SH) is a price bar high preceed by two lower highs (LH) and 
// followed by two lower highs (LH)
//
// In the event of multiple candles forming equal highs, this will still be defined as a swing high,
// provided that there are two candles with lower highs both preceding and following the multiple
// candle formation.

public class ZigZagLanceBeggs(bool useHighLow)
{
    public bool UseHighLow = useHighLow; // Use High/Low or Open/Close
    public readonly List<CryptoCandle> queue = []; // buffer with exactly 8 candles for testing H/L

    // The high/low of each buffered candle, worked out once when it enters the buffer instead of on
    // every comparison - the pattern checks below ask for the same value 34 times per candle.
    //
    // double, not decimal: a pivot only ever asks "is this higher or lower", never "how much money
    // is this", and decimal arithmetic is an order of magnitude slower in .NET. The conversion is
    // deterministic, so two equal decimals stay equal doubles and the ordering is preserved.
    private readonly List<double> queueHigh = [];
    private readonly List<double> queueLow = [];
    public bool UseIdenticalValues { get; set; } = true; // Use exact value matches or allow a margin
    public decimal MarginPercentage { get; set; } = 0.1m;  // Percentage margin for approximate equality

    private double GetLowValue(int offset) => queueLow[offset];
    private double GetHighValue(int offset) => queueHigh[offset];

    // Calculate distance percentage between two values
    private static double PercentageDistance(double value1, double value2) =>
        Math.Abs(value1 - value2) * 100 / Math.Max(value1, value2);

    // Check if two values are "equal" based on the input settings
    private bool ValuesAreEqual(double value1, double value2) =>
        UseIdenticalValues ? value1 == value2 : PercentageDistance(value1, value2) <= (double)MarginPercentage;

    // Check if the first value is a higher high compared to the second value
    private bool IsHigherHigh(double value, int offset)
    {
        var candleValue = GetHighValue(offset);
        return value > candleValue && !ValuesAreEqual(value, candleValue);
    }

    // Check if the first value is a lower low compared to the second value
    private bool IsLowerLow(double value, int offset)
    {
        var candleValue = GetLowValue(offset);
        return value < candleValue && !ValuesAreEqual(value, candleValue);
    }


    // === SWING HIGH ROUTINES ===

    // Check for two previous bars and two next bars with Lower Highs 
    // 12321, 21312, 22322, etc...
    private bool Has_LH_LH_AND_LH_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 2) &&
               IsHigherHigh(currentHigh, offset - 1);
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1) &&
               IsHigherHigh(currentHigh, offset + 2);
        return result;
    }

    // Check for two previous bars with Lower Highs, one highest bar, one lower hight bar,
    // one equals to highest bar and two next bars with Lower Highs
    // 1232312, 1131311, 2231311, etc..
    // currentBar=Second EQ bar
    private bool Has_LH_LH_EQ_LH_EQ_LH_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 4) &&
               IsHigherHigh(currentHigh, offset - 3) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 2)) &&
               IsHigherHigh(currentHigh, offset - 1);
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1) &&
               IsHigherHigh(currentHigh, offset + 2);
        return result;
    }

    // Check for two previous bars with Lower Highs, two equals highests bars, two next bars with Lower Highs
    // 123321, 213321, 113311, 113322, etc..
    // currentBar=Second EQ bar
    private bool Has_LH_LH_EQ_EQ_LH_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 3) &&
               IsHigherHigh(currentHigh, offset - 2) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 1));
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1) &&
               IsHigherHigh(currentHigh, offset + 2);
        return result;
    }

    // Check for two previous bars with Lower Highs, three equals highests bars, two next bars with Lower Highs
    // 1233321, 2133321, 1133311, 1133322, etc..
    // currentBar=Third EQ bar
    private bool Has_LH_LH_EQ_EQ_EQ_LH_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 4) &&
               IsHigherHigh(currentHigh, offset - 3) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 2)) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 1));
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1) &&
               IsHigherHigh(currentHigh, offset + 2);
        return result;
    }

    // Check for two previous bars with Lower Highs, four equals highests bars, two next bars with Lower Highs
    // 12333321, 21333321, 11333311, 11333322, etc..
    // currentBar=Fourth EQ bar
    private bool Has_LH_LH_EQ_EQ_EQ_EQ_LH_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 5) &&
               IsHigherHigh(currentHigh, offset - 4) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 3)) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 2)) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 1));
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1) &&
               IsHigherHigh(currentHigh, offset + 2);
        return result;
    }

    // Check for one bar with Lower Highs, one highest bar, one bar with Lower Highs, one equals
    // to highest bar, one with Lower Highs, one equals to highest bar and one with Lower Highs
    // 1313131, 2313231, etc...
    // currentBar=Third EQ bar
    private bool Has_LH_EQ_LH_EQ_LH_EQ_LH(bool compareRight, int offset)
    {
        double currentHigh = GetHighValue(offset);
        bool result = IsHigherHigh(currentHigh, offset - 5) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 4)) &&
               IsHigherHigh(currentHigh, offset - 3) &&
               ValuesAreEqual(currentHigh, GetHighValue(offset - 2)) &&
               IsHigherHigh(currentHigh, offset - 1);
        if (compareRight)
            result &= IsHigherHigh(currentHigh, offset + 1);
        return result;
    }

    // === SWING LOW ROUTINES ===

    // Check for two previous bars and two next bars with Higher Lows 
    // 32123, 23132, 22122, etc...
    private bool Has_HL_HL_AND_HL_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result =
            IsLowerLow(currentLow, offset - 2) &&
            IsLowerLow(currentLow, offset - 1);

        if (compareRight)
            result &=
                IsLowerLow(currentLow, offset + 1) &&
                IsLowerLow(currentLow, offset + 2);
        return result;
    }

    // Check for two previous bars with Higher Lows, one lowest bar, one lower hight 
    // bar, one equals to lowest bar and two next bars with Higher Lows
    // 1232312, 1131311, 2231311, etc..
    // currentBar=Second EQ bar
    private bool Has_HL_HL_EQ_HL_EQ_HL_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result = IsLowerLow(currentLow, offset - 4) &&
               IsLowerLow(currentLow, offset - 3) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 2)) &&
               IsLowerLow(currentLow, offset - 1);
        if (compareRight)
            result &= IsLowerLow(currentLow, offset + 1) &&
               IsLowerLow(currentLow, offset + 2);
        return result;
    }

    // Check for two previous bars with Higher Lows, two equals lowests bars, two next bars with Higher Lows
    // 123321, 213321, 113311, 113322, etc..
    // currentBar=Second EQ bar
    private bool Has_HL_HL_EQ_EQ_HL_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result = IsLowerLow(currentLow, offset - 3) &&
               IsLowerLow(currentLow, offset - 2) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 1));
        if (compareRight)
            result &= IsLowerLow(currentLow, offset + 1) &&
               IsLowerLow(currentLow, offset + 2);
        return result;
    }

    // Check for two previous bars with Higher Lows, three equals lowests bars, two next bars with Higher Lows
    // 1233321, 2133321, 1133311, 1133322, etc..
    // currentBar=Third EQ bar
    private bool Has_HL_HL_EQ_EQ_EQ_HL_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result = IsLowerLow(currentLow, offset - 4) &&
               IsLowerLow(currentLow, offset - 3) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 2)) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 1));
        if (compareRight)
            result &= IsLowerLow(currentLow, offset + 1) &&
               IsLowerLow(currentLow, offset + 2);
        return result;
    }

    // Check for two previous bars with Higher Lows, four equals lowests bars, two next bars with Higher Lows
    // 12333321, 21333321, 11333311, 11333322, etc..
    // currentBar=Fourth EQ bar
    private bool Has_HL_HL_EQ_EQ_EQ_EQ_HL_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result = IsLowerLow(currentLow, offset - 5) &&
               IsLowerLow(currentLow, offset - 4) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 3)) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 2)) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 1));
        if (compareRight)
            result &=
                IsLowerLow(currentLow, offset + 1) &&
               IsLowerLow(currentLow, offset + 2);
        return result;
    }

    // Check for one bar with Higher Lows, one lowest bar, one bar with Higher Lows, one equals
    // to lowest bar, one with Higher Lows, one equals to lowest bar and one with Higher Lows
    // 1313131, 2313231, etc...
    // currentBar=Third EQ bar
    private bool Has_HL_EQ_HL_EQ_HL_EQ_HL(bool compareRight, int offset)
    {
        double currentLow = GetLowValue(offset);
        bool result = IsLowerLow(currentLow, offset - 5) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 4)) &&
               IsLowerLow(currentLow, offset - 3) &&
               ValuesAreEqual(currentLow, GetLowValue(offset - 2)) &&
               IsLowerLow(currentLow, offset - 1);
        if (compareRight)
            result &= IsLowerLow(currentLow, offset + 1);
        return result;
    }


    internal bool IsHighPoint(bool compareRight, int offset)
    {
        // Do we have a new high?
        return Has_LH_LH_AND_LH_LH(compareRight, offset) ||
            Has_LH_LH_EQ_LH_EQ_LH_LH(compareRight, offset) ||
            Has_LH_LH_EQ_EQ_LH_LH(compareRight, offset) ||
            Has_LH_LH_EQ_EQ_EQ_LH_LH(compareRight, offset) ||
            Has_LH_LH_EQ_EQ_EQ_EQ_LH_LH(compareRight, offset) ||
            Has_LH_EQ_LH_EQ_LH_EQ_LH(compareRight, offset);
    }

    internal bool IsLowPoint(bool compareRight, int offset)
    {
        // Do we have a new low?
        return Has_HL_HL_AND_HL_HL(compareRight, offset) ||
            Has_HL_HL_EQ_HL_EQ_HL_HL(compareRight, offset) ||
            Has_HL_HL_EQ_EQ_HL_HL(compareRight, offset) ||
            Has_HL_HL_EQ_EQ_EQ_HL_HL(compareRight, offset) ||
            Has_HL_HL_EQ_EQ_EQ_EQ_HL_HL(compareRight, offset) ||
            Has_HL_EQ_HL_EQ_HL_EQ_HL(compareRight, offset);
    }

    internal bool Add(CryptoCandle candle)
    {
        queue.Add(candle);
        queueHigh.Add((double)candle.GetHighValue(UseHighLow));
        queueLow.Add((double)candle.GetLowValue(UseHighLow));
        if (queue.Count > 8)
        {
            queue.RemoveAt(0);
            queueHigh.RemoveAt(0);
            queueLow.RemoveAt(0);
        }
        return queue.Count == 8;
    }
}
