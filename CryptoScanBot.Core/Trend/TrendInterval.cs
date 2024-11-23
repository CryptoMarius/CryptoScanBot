using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;

namespace CryptoScanBot.Core.Trend;


public class TrendInterval
{
    private static bool ResolveStartAndEndDate(CryptoCandleList candleList, AccountSymbolIntervalData accountSymbolIntervalData, ref long candleIntervalStart, ref long candleIntervalEnd)
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
            candleIntervalStart = IntervalTools.StartOfIntervalCandle(candleIntervalStart, accountSymbolIntervalData.Interval.Duration);
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
            candleIntervalEnd = IntervalTools.StartOfIntervalCandle(candleIntervalEnd, accountSymbolIntervalData.Interval.Duration);
        // go 1 candle back (date parameter was a low interval candle and higher interval not yet closed)
        if (!candleList.ContainsKey(candleIntervalEnd))
            candleIntervalEnd -= accountSymbolIntervalData.Interval.Duration;


        return true;
    }



    //public static void Collapse(ZigZagIndicator9 indicator)
    //{
    //    // new idea... nice, not sure how..
    //    // https://youtu.be/plhWMmmgZj4?t=450
    //    // Reduces a lot, but a bit to much also..

    //    List<ZigZagResult> NewZigZagList = [];

    //    var zigZagList = indicator.ZigZagList;

    //    ZigZagResult zigZag;
    //    ZigZagResult? previousLow = null;
    //    ZigZagResult? previousHigh = null;
    //    for (int i = 0; i < zigZagList.Count; i++)
    //    {
    //        zigZag = zigZagList[i];
    //        zigZag.Index = i;
    //        if (zigZag.PointType == 'H')
    //        {
    //            if (previousHigh == null)
    //            {
    //                previousHigh = zigZag;
    //                NewZigZagList.Add(zigZag);
    //            }
    //            else
    //            {
    //                if (zigZag.Value > previousHigh.Value)
    //                {
    //                    // We have a new high, determine the lowest swingpoint, it will be the next low
    //                    if (previousLow != null)
    //                    {
    //                        ZigZagResult? swingLow = null;
    //                        for (int index = previousHigh.Index + 1; index < zigZag.Index; index++)
    //                        {
    //                            var zigZagTemp = zigZagList[index];

    //                            if (zigZagTemp.PointType == 'L')
    //                            {
    //                                if (swingLow == null || zigZagTemp.Value < swingLow.Value)
    //                                    swingLow = zigZagTemp;
    //                            }
    //                        }
    //                        if (swingLow != null)
    //                        {
    //                            previousLow = swingLow;
    //                            NewZigZagList.Add(swingLow);
    //                        }
    //                    }
    //                    previousHigh = zigZag;
    //                    NewZigZagList.Add(zigZag);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            if (previousLow == null)
    //            {
    //                previousLow = zigZag;
    //                NewZigZagList.Add(zigZag);
    //            }
    //            else
    //            {
    //                if (zigZag.Value < previousLow.Value)
    //                {
    //                    // We have a new high, determine the last swingpoint, it will be the next high
    //                    if (previousHigh != null)
    //                    {
    //                        ZigZagResult? swingHigh = null;
    //                        for (int index = previousLow.Index + 1; index < zigZag.Index; index++)
    //                        {
    //                            var zigZagTemp = zigZagList[index];

    //                            if (zigZagTemp.PointType == 'H')
    //                            {
    //                                if (swingHigh == null || zigZagTemp.Value > swingHigh.Value)
    //                                    swingHigh = zigZagTemp;
    //                            }
    //                        }
    //                        if (swingHigh != null)
    //                        {
    //                            previousHigh = swingHigh;
    //                            NewZigZagList.Add(swingHigh);
    //                        }
    //                    }
    //                    previousLow = zigZag;
    //                    NewZigZagList.Add(zigZag);
    //                }
    //            }
    //        }
    //    }
        
    //    indicator.ZigZagList = NewZigZagList;
    //}


    /// <summary>
    /// Interpret the zigzag values en try to identify a trend
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(ZigZagIndicator9 indicator, StringBuilder? log)
    {
        var zigZagList = indicator.ZigZagList;
        CryptoTrendIndicator trend = CryptoTrendIndicator.Sideways;

        if (log != null)
        {
            log.AppendLine("");
            //log.AppendLine($"Deviation={indicator.Deviation}% ZigZag points={zigZagList.Count}:");
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


        // Nieuwe bepaling [NB: Er is discussie over de laatste zigzag waarde (market value ipv een low/high)]
        // Je wilt naar pairs toe (l,h) zodat je kan vergelijken met de vorige (l,h)
        // (?verwarring of je een (l,h) of (h,l) gebruikt, beide zou kunnen, misschien vanwege start situatie?
        ZigZagResult zigZag;
        for (int i = 0; i < zigZagList.Count; i++)
        {
            zigZag = zigZagList[i];

            // Nope, the dummies are the most important as it can be a BOS (break of structure)
            //if (zigZag.Dummy)
            //    continue;

            // Pickup last value
            decimal value;
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



    public static void Calculate(CryptoSymbol symbol, CryptoCandleList candleList, AccountSymbolIntervalData accountSymbolIntervalData, 
        long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
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




        //// We cache the ZigZag indicator and we create a lot of them with different deviations
        if (accountSymbolIntervalData.ZigZagIndicators == null)
        {
            accountSymbolIntervalData.ZigZagIndicators = [];
            for (decimal deviation = 2.5m; deviation >= 0.5m; deviation -= 0.25m)
            {
                ZigZagIndicator9 indicator = new(candleList, GlobalData.Settings.General.UseHighLowInTrendCalculation, deviation, interval.Duration)
                {
                    Deviation = deviation
                };
                accountSymbolIntervalData.ZigZagIndicators.Add(indicator);
            }
        }
        //accountSymbolIntervalData.Indicator ??= new(candleList, GlobalData.Settings.General.UseHighLowInTrendCalculation, 1m);



        // Add candles to the ZigZag indicators
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            indicator.StartBatch();
        }

        int added = 0;
        long loop = candleIntervalStart;
        while (loop <= candleIntervalEnd)
        {
            if (candleList.TryGetValue(loop, out CryptoCandle? candle))
            {
                foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
                {
                    indicator.Calculate(candle);
                    added++;
                }
                //accountSymbolIntervalData.Indicator.Calculate(candle, accountSymbolIntervalData.Interval.Duration);
                accountSymbolIntervalData.ZigZagLastCandleAdded = loop;
            }
            //else log?.AppendLine($"unable to find candle {loop}");
            loop += accountSymbolIntervalData.Interval.Duration;
        }
        if (added > 0)
        {
            foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
            {
                indicator.FinishBatch();
            }
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
                if (GlobalData.Settings.General.DebugTrendCalculation)
                {
                    log?.AppendLine($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
                    ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
                }
            }
        }
        double avg = sum / countX;




        // What is the best? Technically we need at least 4 pivot points, would be nice if we have a lot of pivots.
        // On the other hand, we do not want small percentages als this can give fake trend & reversal signals.
        ZigZagIndicator9? bestIndicator = null;
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



        if (log != null)
        {
            log.AppendLine($"{bestIndicator.Deviation} {bestIndicator.ZigZagList} pivots");
            foreach (var zigZag in bestIndicator.ZigZagList)
            {
                string s = string.Format("date={0} H {1:N8} rsi={2:N8}", zigZag.Candle.Date.ToLocalTime(), zigZag.Value, zigZag.Candle.CandleData?.Rsi);
                log.AppendLine(s);
            }
        }


        // Interpret the pivot points and put Charles Dow theory at work
        //ZigZagIndicator9? bestIndicator = accountSymbolIntervalData.Indicator;
        CryptoTrendIndicator trendIndicator = InterpretZigZagPoints(bestIndicator, log);
        accountSymbolIntervalData.TrendIndicator = trendIndicator;
        accountSymbolIntervalData.TrendInfoUnix = candleIntervalEnd;
        accountSymbolIntervalData.TrendInfoDate = CandleTools.GetUnixDate(candleIntervalEnd);

        // Note: We could also do something like take the average trend over the last x zigzag indicators??
        // We still need to choose a proper indicator to do our analysis though on s/r & s/d and liquidity zones

        if (GlobalData.Settings.General.DebugTrendCalculation)
        {
            //string text = $"{symbol.Name} {interval.Name} candles={candleList.Count} calculated at {accountSymbolIntervalData.TrendInfoDate} " +
            //$"avg={avg} best={bestIndicator.Deviation}% zigzagcount={bestIndicator.ZigZagList.Count} {accountSymbolIntervalData.TrendIndicator} "
            string text = $"{symbol.Name} {interval.Name} candles={candleList.Count} calculated at {accountSymbolIntervalData.TrendInfoDate} " +
            $"zigzagcount={bestIndicator.ZigZagList.Count} {accountSymbolIntervalData.TrendIndicator} "
#if DEBUG
             + $"{candleIntervalStartDebug}..{candleIntervalEndDebug}"
#endif
            ;
            log?.AppendLine(text);
            ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
        }
        return;
    }

}
