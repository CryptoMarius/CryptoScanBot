using CryptoScanner.Core.Enums;

using System.Text;

namespace CryptoScanner.Core.Trend;


/// <summary>
/// Dow theory interpretation of a ZigZag pivot list.
/// A trend only flips after TWO consecutive contra-trend pivots (count > 1),
/// which naturally dampens single-pivot noise (including dummy pivots).
///
/// The window/indicator setup used to live here in CalculateAsync; that orchestration
/// is now shared with the BOS interpretation through <see cref="TrendCalculator.CalculateBothAsync"/>.
/// </summary>
public class TrendInterval
{
    /// <summary>
    /// Interpret the zigzag values en try to identify a trend
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(ZigZagIndicator indicator, StringBuilder? log)
    {
        var zigZagList = indicator.ZigZagList;
        CryptoTrendIndicator trend = CryptoTrendIndicator.Unknown;

        if (log != null)
        {
            log.AppendLine("");
            //log.AppendLine($"Deviation={indicator.Deviation}% Primary points={zigZagList.Count}:");
            log.AppendLine($"ZigZag points={zigZagList.Count}:");
        }

        // We need at least two points to make an assumption
        if (zigZagList.Count < 2)
        {
            log?.AppendLine($"Not enough zigzag points, trend={trend}");
            return trend;
        }


        // Configure a start value
        int count = 0;
        decimal lastLow;
        decimal lastHigh;
        if (zigZagList[1].Value > zigZagList[0].Value)
        {
            lastLow = zigZagList[0].Value;
            lastHigh = zigZagList[1].Value;
            trend = CryptoTrendIndicator.Bullish;
        }
        else
        {
            lastLow = zigZagList[1].Value;
            lastHigh = zigZagList[0].Value;
            trend = CryptoTrendIndicator.Bearish;
        }


        // Process from index 2 onward — points 0 and 1 were already used to initialise
        // lastLow/lastHigh above, so starting at 0 would double-count them and cause
        // a spurious trend flip on the very first two points.
        ZigZagResult zigZag;
        for (int i = 2; i < zigZagList.Count; i++)
        {
            zigZag = zigZagList[i];

            // Pickup last value
            decimal value = zigZag.PointType == 'H' ? lastHigh : lastLow;
            //decimal value;
            //if (zigZag.PointType == 'H')
            //    value = lastHigh;
            //else
            //    value = lastLow;

            // if the trend was bearish and the market was able to make a HH
            switch (trend)
            {
                case CryptoTrendIndicator.Bearish:
                    if (zigZag.Value > value)
                        count++;
                    else
                        count = 0;
                    break;
                case CryptoTrendIndicator.Bullish:
                    if (zigZag.Value < value)   // strictly less than: equal values are not a lower high/low
                        count++;
                    else
                        count = 0;
                    break;
            }

            // Save the last value
            if (zigZag.PointType == 'H')
                lastHigh = zigZag.Value;
            else
                lastLow = zigZag.Value;



            // switch trend if at least 2 values are present (Charles Dow theory)
            // (we could do the simple version and just use the last 2 points <weak?>)
            if (count > 1)
            {
                if (trend == CryptoTrendIndicator.Bearish)
                    trend = CryptoTrendIndicator.Bullish;
                else if (trend == CryptoTrendIndicator.Bullish)
                    trend = CryptoTrendIndicator.Bearish;

                log?.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} divergent={count}, trend={trend} Trend has switched");
                count = 0;
            }
            else log?.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} divergent={count}, trend={trend}");
        }

        log?.AppendLine("");
        return trend;
    }
}
