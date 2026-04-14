using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Text;

namespace CryptoScanner.Core.Trend;


/// <summary>
/// Trend interpretation using BOS (Break of Structure) and CHoCH (Change of Character).
///
/// Unlike Dow Theory which requires two consecutive confirming swing points (e.g. HH + HL),
/// a single structural break is sufficient here:
///   - In a bearish trend: a Higher High → CHoCH → switches trend to Bullish
///   - In a bullish trend: a Lower Low  → CHoCH → switches trend to Bearish
///   - Same direction:     Higher High in uptrend / Lower Low in downtrend → BOS (continuation)
///
/// This makes BOS/CHoCH faster than Dow Theory, at the cost of potentially more reversals.
/// </summary>
public class TrendIntervalBos
{
    private static bool ResolveStartAndEndDate(CryptoInterval interval,
        CryptoCandleList candleList, ref CandleTime minDate, ref CandleTime maxDate)
    {
        // start time
        if (minDate == 0)
        {
            if (!candleList.TryGetFirstCandle(out var candle))
                return false;
            if (maxDate > 0)
            {
                minDate = maxDate - 5000 * interval.Duration;
                if (minDate < candle.OpenTime)
                    minDate = candle.OpenTime;
            }
            else
            {
                minDate = candle.OpenTime;
            }
        }
        else
            minDate = IntervalTools.StartOfIntervalCandle(minDate, interval.Duration);

        // end time
        if (maxDate == 0)
        {
            if (!candleList.TryGetLastCandle(out var candle))
                return false;
            maxDate = candle.OpenTime;
        }
        else
            maxDate = IntervalTools.StartOfIntervalCandle(maxDate, interval.Duration);

        if (!candleList.ContainsKey(maxDate))
            maxDate -= interval.Duration;

        return true;
    }


    /// <summary>
    /// Interpret zigzag swing points using BOS/CHoCH logic.
    /// Returns the resulting trend (Bullish/Bearish/Unknown).
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(ZigZagIndicator indicator, StringBuilder? log)
    {
        var zigZagList = indicator.ZigZagList;
        CryptoTrendIndicator trend = CryptoTrendIndicator.Unknown;

        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine($"BOS/CHoCH ZigZag points={zigZagList.Count}:");
        }

        if (zigZagList.Count < 2)
        {
            log?.AppendLine($"Not enough zigzag points, trend={trend}");
            return trend;
        }


        // Determine initial trend and seed the last known high/low from the first two points
        decimal lastHigh;
        decimal lastLow;
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

        for (int i = 2; i < zigZagList.Count; i++)
        {
            var zigZag = zigZagList[i];
            CryptoStructureEvent structureEvent = CryptoStructureEvent.None;

            if (zigZag.PointType == 'H')
            {
                if (zigZag.Value > lastHigh)
                {
                    if (trend == CryptoTrendIndicator.Bearish)
                    {
                        // Higher High in a downtrend = Change of Character → reversal to Bullish
                        structureEvent = CryptoStructureEvent.ChoCh;
                        trend = CryptoTrendIndicator.Bullish;
                    }
                    else
                    {
                        // Higher High in an uptrend = Break of Structure (continuation)
                        structureEvent = CryptoStructureEvent.Bos;
                    }
                }
                lastHigh = zigZag.Value;
            }
            else // 'L'
            {
                if (zigZag.Value < lastLow)
                {
                    if (trend == CryptoTrendIndicator.Bullish)
                    {
                        // Lower Low in an uptrend = Change of Character → reversal to Bearish
                        structureEvent = CryptoStructureEvent.ChoCh;
                        trend = CryptoTrendIndicator.Bearish;
                    }
                    else
                    {
                        // Lower Low in a downtrend = Break of Structure (continuation)
                        structureEvent = CryptoStructureEvent.Bos;
                    }
                }
                lastLow = zigZag.Value;
            }

            if (log != null)
            {
                if (structureEvent != CryptoStructureEvent.None)
                    log.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} {structureEvent}, trend={trend}");
                else
                    log.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} trend={trend}");
            }
        }

        log?.AppendLine("");
        return trend;
    }


    public static async Task CalculateAsync(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList,
        CryptoTrendData intervalTrend, SettingsZigZag trendSettings, StringBuilder? log = null)
    {
        log?.AppendLine("");
        log?.AppendLine("----");
        log?.AppendLine($"{symbol.Name} Interval {interval.Name} [BOS/CHoCH]");
        log?.AppendLine("");

        if (candleList.Count == 0)
        {
            intervalTrend.Reset();
            return;
        }

        CandleTime minDate = CandleTime.MinValue;
        CandleTime maxDate = CandleTime.MinValue;
        if (!ResolveStartAndEndDate(interval, candleList, ref minDate, ref maxDate))
            return;

        ZigZagIndicator indicator = new(trendSettings.TrendType, trendSettings.UseHighLow, 1.0m);
        await TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, minDate, maxDate);

        CryptoTrendIndicator trendIndicator = InterpretZigZagPoints(indicator, log);

        intervalTrend.PrevTrend = intervalTrend.Trend;
        intervalTrend.PrevTime = intervalTrend.Time;
        intervalTrend.Trend = trendIndicator;
        intervalTrend.Time = maxDate;

        if (GlobalData.Settings.General.DebugTrendCalculation)
        {
            string text = $"{symbol.Name} {interval.Name} [BOS] candles={candleList.Count} " +
                $"calculated at {intervalTrend.Time?.ToDateTime()} " +
                $"zigzagcount={indicator.ZigZagList.Count} {intervalTrend.Trend}";
            log?.AppendLine(text);
            ScannerLog.Logger.Debug("TrendIntervalBos.Calculate " + text);
        }
    }
}
