using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;

namespace CryptoScanBot.Core.Trend;


public class TrendInterval
{
    private static bool ResolveStartAndEndDate(SortedList<long, CryptoCandle> candleList, AccountSymbolIntervalData accountSymbolIntervalData, ref long candleIntervalStart, ref long candleIntervalEnd)
    {
        // We cache the ZigZag indicator, this way we do not have to add all the candles again and again.
        // (We hope this makes the scanner a more less cpu hungry)
        // Question however: when is it ssave to clear the zigzag? to avoid memory overflow in the long run?
        // Anwer: We save and load the candles every 24 hours, perhaps there (TODO)
        //accountSymbolIntervalData.ZigZagIndicator ??= new(candleList, false);


        // start time
        if (candleIntervalStart == 0)
        {
            var candle = candleList.Values.First();
            if (candleIntervalEnd > 0)
            {
                // Need to set some limit or it will add 100.000 of candles (takes forever to initialize)
                candleIntervalStart = candleIntervalEnd - 5000 * accountSymbolIntervalData.Interval.Duration;
                if (candleIntervalStart < candle.OpenTime)
                    candleIntervalStart = candle.OpenTime;
            }
            else
            {
                candleIntervalStart = candle.OpenTime; // in the right interval
            }
        }
        else
            candleIntervalStart = IntervalTools.StartOfIntervalCandle(accountSymbolIntervalData.Interval, candleIntervalStart);
        // correct the start with what we previously added
        if (accountSymbolIntervalData.ZigZagLastCandleAdded.HasValue && accountSymbolIntervalData.ZigZagLastCandleAdded.Value >= candleIntervalStart)
            candleIntervalStart = (long)accountSymbolIntervalData.ZigZagLastCandleAdded;



        // end time
        if (candleIntervalEnd == 0)
        {
            var candle = candleList.Values.Last();
            candleIntervalEnd = candle.OpenTime; // in the right interval
        }
        else
            candleIntervalEnd = IntervalTools.StartOfIntervalCandle(accountSymbolIntervalData.Interval, candleIntervalEnd);
        // go 1 candle back (date parameter was a low interval candle and higher interval not yet closed)
        if (!candleList.ContainsKey(candleIntervalEnd))
            candleIntervalEnd -= accountSymbolIntervalData.Interval.Duration;


        return true;
    }





    /// <summary>
    /// Interpret the zigzag values en try to identify a trend
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(ZigZagIndicator7 indicator, StringBuilder? log)
    {
        var zigZagList = indicator.ZigZagList;
        CryptoTrendIndicator trend = CryptoTrendIndicator.Sideways;

        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine($"Deviation={indicator.Deviation}% ZigZag points={zigZagList.Count}:");
        }

        // We need at least two points to make an assumption
        if (zigZagList.Count < 2)
        {
            log?.AppendLine($"Not enough zigzag points, trend={trend}");
            return trend;
        }


        // Configure a start value
        int count = 0;
        double lastLow;
        double lastHigh;
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


        // Nieuwe bepaling [NB: Er is discussie over de laatste zigzag waarde (market value ipv een low/high)]
        // Je wilt naar pairs toe (l,h) zodat je kan vergelijken met de vorige (l,h)
        // (?verwarring of je een (l,h) of (h,l) gebruikt, beide zou kunnen, misschien vanwege start situatie?
        ZigZagResult zigZag;
        for (int i = 0; i < zigZagList.Count; i++)
        {
            zigZag = zigZagList[i];

            // Pickup last value
            double value;
            if (zigZag.PointType == 'H')
                value = lastHigh;
            else
                value = lastLow;

            // if the trend was bearish and the market was able to make a HH

            switch (trend)
            {
                case CryptoTrendIndicator.Bearish:
                    if (zigZag.Value > value)
                        count++;
                    else count = 0;
                    break;
                case CryptoTrendIndicator.Bullish:
                    if (zigZag.Value <= value)
                        count++;
                    else count = 0;
                    break;

            }

            // Save the last value
            if (zigZag.PointType == 'H')
                lastHigh = zigZag.Value;
            else
                lastLow = zigZag.Value;



            // switch trend if 2 values are opposite
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



    public static void Calculate(CryptoSymbol symbol, SortedList<long, CryptoCandle> candleList, AccountSymbolIntervalData accountSymbolIntervalData, long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
    {
        var interval = accountSymbolIntervalData.Interval;

        log?.AppendLine("");
        log?.AppendLine("----");
        log?.AppendLine($"{symbol.Name} Interval {interval.Name}");
        log?.AppendLine("");

        // Unable to calculate - Note: in fact we need at least ~24 candles because of the zigzag parameters to identify H/L
        if (candleList.Count == 0)
        {
            // Hele discussies, maar als we niet genoeg candles hebben om een trend te berekenen
            // gebruiken we toch de sideway's om aan te geven dat het niet berekend kon worden.
            // Bij nieuwe munten, Flatliners (ethusdt) en andere gedrochten is het dus sideway's!
            //Signal.Reaction = string.Format("not enough quotes for {0} trend", interval.Name);
            accountSymbolIntervalData.Reset();

#if DEBUG
            log?.AppendLine($"{symbol.Name} {interval.Name} calculated at {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (no candles)");
            ScannerLog.Logger.Trace($"MarketTrend.Calculate {symbol.Name} {interval.Name} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (no candles)");
#endif
            return;
        }


        if (!ResolveStartAndEndDate(candleList, accountSymbolIntervalData, ref candleIntervalStart, ref candleIntervalEnd))
        {
            log?.AppendLine($"{symbol.Name} {interval.Name} calculated at {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (date period problem)");
            ScannerLog.Logger.Trace($"MarketTrend.Calculate {symbol.Name} {interval.Name} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (date period problem)");
            return;
        }
#if DEBUG
        DateTime candleIntervalStartDebug = CandleTools.GetUnixDate(candleIntervalStart);
        DateTime candleIntervalEndDebug = CandleTools.GetUnixDate(candleIntervalEnd);
#endif




        // We cache the ZigZag indicator and we create a lot of them with different deviations
        if (accountSymbolIntervalData.ZigZagIndicators == null)
        {
            accountSymbolIntervalData.ZigZagIndicators = [];
            //for (int deviation = 4; deviation <= 40; deviation++)
            for (double deviation = 10.0; deviation >= 1; deviation -= 0.25)
            {
                ZigZagIndicator7 indicator = new(candleList, false)
                {
                    Deviation = deviation
                };
                accountSymbolIntervalData.ZigZagIndicators.Add(indicator);
            }
        }



        // Add candles to the ZigZag indicators
        long loop = candleIntervalStart;
        while (loop <= candleIntervalEnd)
        {
            if (candleList.TryGetValue(loop, out CryptoCandle? candle))
            {
                foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
                {
                    indicator.Calculate(candle, accountSymbolIntervalData.Interval.Duration);
                }
                accountSymbolIntervalData.ZigZagLastCandleAdded = loop;
            }
            else log?.AppendLine($"unable to find candle {loop}");
            loop += accountSymbolIntervalData.Interval.Duration;
        }



        
        // Calculate the average amount of pivots for the valid zigzag indicators
        int countX = 0;
        double sum = 0;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            if (indicator.ZigZagList.Count > 4)
            {
                countX++;
                sum += indicator.ZigZagList.Count;
                log?.AppendLine($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
                ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
            }
        }
        double avg = sum / countX;




        // What is the best? Technically we need at least 4 pivot points, would be nice if we have a lot of pivots.
        // On the other hand, we do not want small percentages als this can give fake trend & reversal signals.
        ZigZagIndicator7? bestIndicator = null;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            int zigZagCount = indicator.ZigZagList.Count;
            if (indicator.ZigZagList.Count > 4 && zigZagCount > avg)
            {
                bestIndicator = indicator;
                break;
            }
        }
        // Fallback on the last ZigZag with deviation=1%, which should have the most pivots (not alway's the case!)
        bestIndicator ??= accountSymbolIntervalData.ZigZagIndicators.Last();
        accountSymbolIntervalData.BestIndicator = bestIndicator;



        //if (log != null)
        //{
        //    log.AppendLine($"{bestIndicator.Deviation} {bestIndicator.ZigZagList} pivots");
        //    foreach (var zigZag in bestIndicator.ZigZagList)
        //    {
        //        string s = string.Format("date={0} H {1:N8} rsi={2:N8}", zigZag.Candle.Date.ToLocalTime(), zigZag.Value, zigZag.Candle.CandleData?.Rsi);
        //        log.AppendLine(s);
        //    }
        //}

        // Interpret the pivot points and put Charles Dow theory at work
        CryptoTrendIndicator trendIndicator = InterpretZigZagPoints(bestIndicator, log);
        accountSymbolIntervalData.TrendIndicator = trendIndicator;
        accountSymbolIntervalData.TrendInfoUnix = candleIntervalEnd;
        accountSymbolIntervalData.TrendInfoDate = CandleTools.GetUnixDate(candleIntervalEnd);

        // Note: We could also do something like take the average trend over the last x zigzag indicators??
        // We still need to choose a proper indicator to do our analysis though on s/r & s/d and liquidity zones

#if DEBUG
        string text = $"{symbol.Name} {interval.Name} candles={candleList.Count} calculated at {accountSymbolIntervalData.TrendInfoDate} " +
            $"avg={avg} best={bestIndicator.Deviation}% zigzagcount={bestIndicator.ZigZagList.Count} {accountSymbolIntervalData.TrendIndicator} " +
            $"{candleIntervalStartDebug}..{candleIntervalEndDebug}";
        log?.AppendLine(text);
        ScannerLog.Logger.Trace("MarketTrend.Calculate "+ text);
#endif
        return;
    }

}
