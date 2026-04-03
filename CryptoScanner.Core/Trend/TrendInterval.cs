using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Text;

namespace CryptoScanner.Core.Trend;


public class TrendInterval
{
    private static bool ResolveStartAndEndDate(CryptoInterval interval,
        CryptoCandleList candleList, ref CandleTime minDate, ref CandleTime maxDate)
    {
        // We cache the Primary indicator, this way we do not have to add all the candles again and again.
        // (We hope this makes the scanner a more less cpu hungry)
        // Question however: when is it save to clear the zigzag? to avoid memory overflow in the long run?
        // Anwer: We save and load the candles every 24 hours, perhaps there (TODO)
        //intervalTrend.ZigZagIndicator ??= new(candleList, false);

        // start time
        if (minDate == 0)
        {
            // Use the thread-safe helper — candleList.Values.First() enumerates without the read lock
            // and throws InvalidOperationException when another thread calls Add() concurrently.
            if (!candleList.TryGetFirstCandle(out var candle))
                return false;
            if (maxDate > 0)
            {
                // Need to set some limit or it will add 100.000 of candles (takes forever to initialize)
                minDate = maxDate - 5000 * interval.Duration;
                if (minDate < candle.OpenTime)
                    minDate = candle.OpenTime;
            }
            else
            {
                minDate = candle.OpenTime; // in the right interval
            }
        }
        else
            minDate = IntervalTools.StartOfIntervalCandle(minDate, interval.Duration);
        // correct the start with what we previously added
        //if (intervalTrend.ZigZagLastCandleAdded.HasValue && intervalTrend.ZigZagLastCandleAdded.Value >= minDate)
        //    minDate = (long)intervalTrend.ZigZagLastCandleAdded;



        // end time
        if (maxDate == 0)
        {
            if (!candleList.TryGetLastCandle(out var candle))
                return false;
            maxDate = candle.OpenTime; // in the right interval
        }
        else
            maxDate = IntervalTools.StartOfIntervalCandle(maxDate, interval.Duration);
        // go 1 candle back (date parameter was a low interval candle and higher interval not yet closed)
        if (!candleList.ContainsKey(maxDate))
            maxDate -= interval.Duration;


        return true;
    }



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



    public static async Task CalculateAsync(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList,
        CryptoTrendData intervalTrend, SettingsZigZag trend, StringBuilder? log = null)
    {
        log?.AppendLine("");
        log?.AppendLine("----");
        log?.AppendLine($"{symbol.Name} Interval {interval.Name}");
        log?.AppendLine("");

        // Unable to calculate - Note: in fact we need at least ~24 candles because of the zigzag parameters to identify H/L
        if (candleList.Count == 0)
        {
            // Lots of discussion, but if we dont have candles it really cannot be up or down so we choose sideway's
            intervalTrend.Reset();
#if DEBUG
            if (intervalTrend.Time != null)
            {
                log?.AppendLine($"{symbol.Name} {interval.Name} calculated at {intervalTrend.Time?.ToDateTime()} {intervalTrend.Trend} (no candles)");
                //ScannerLog.Logger.Debug($"MarketTrend.Calculate {symbol.Name} {interval.Name} {intervalTrend.Time?.ToDateTime()} {intervalTrend.Trend} (no candles)");
            }
#endif
            return;
        }


        // Determine the period (but limited <not 10000+ candles back>)
        CandleTime minDate = CandleTime.MinValue;
        CandleTime maxDate = CandleTime.MinValue;
        if (!ResolveStartAndEndDate(interval, candleList, ref minDate, ref maxDate))
        {
            log?.AppendLine($"{symbol.Name} {interval.Name} calculated at {intervalTrend.Time?.ToDateTime()} {intervalTrend.Trend} (date period problem)");
            //ScannerLog.Logger.Debug($"MarketTrend.Calculate {symbol.Name} {interval.Name} {intervalTrend.Time?.ToDateTime()} {intervalTrend.Trend} (date period problem)");
            return;
        }
        //#if DEBUG
        //        DateTime candleIntervalStartDebug = CandleTools.GetUnixDate(minDate);
        //        DateTime candleIntervalEndDebug = CandleTools.GetUnixDate(maxDate);
        //#endif

        // Add candles to the indicator
        ZigZagIndicator indicator = new(trend.TrendType, trend.UseHighLow, 1.0m);
        await TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, minDate, maxDate);

        // Interpret the pivot points and put Charles Dow theory at work
        var bestIndicator = indicator;
        CryptoTrendIndicator trendIndicator = InterpretZigZagPoints(bestIndicator, log);

        intervalTrend.PrevTrend = intervalTrend.Trend;
        intervalTrend.PrevTime = intervalTrend.Time;
        intervalTrend.Trend = trendIndicator;
        intervalTrend.Time = maxDate;

        // Note: We could also do something like take the average trend over the last x zigzag indicators??
        // We still need to choose a proper indicator to do our analysis though on s/r & s/d and liquidity zones

        if (GlobalData.Settings.General.DebugTrendCalculation)
        {
            //string text = $"{symbol.Name} {interval.Name} candles={candleList.Count} calculated at {intervalTrend.TrendInfoDate} " +
            //$"avg={avg} best={bestIndicator.Deviation}% zigzagcount={bestIndicator.ZigZagList.Count} {intervalTrend.TrendInterval} "
            string text = $"{symbol.Name} {interval.Name} candles={candleList.Count} calculated at {intervalTrend.Time?.ToDateTime()} " +
            $"zigzagcount={bestIndicator.ZigZagList.Count} {intervalTrend.Trend} "
            //#if DEBUG
            //             + $"{candleIntervalStartDebug}..{candleIntervalEndDebug}"
            //#endif
            ;
            log?.AppendLine(text);
            //ScannerLog.Logger.Debug("MarketTrend.Calculate " + text);
        }
        return;
    }

}
