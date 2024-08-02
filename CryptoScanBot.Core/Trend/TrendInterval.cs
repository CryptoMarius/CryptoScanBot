using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;

namespace CryptoScanBot.Core.Trend;


public class TrendInterval
{
    private static bool ResolveStartAndEndDate(SortedList<long, CryptoCandle> candleList, AccountSymbolIntervalData accountSymbolIntervalData, ref long candleIntervalStart, ref long candleIntervalEnd)
    {
        // TODO - de parameter candleIntervalStart controleren! (staat nu nog op twee plekken op 0)

        // We cache the ZigZag indicator, this way we do not have to add all the candles again and again.
        // (We hope this makes the scanner a more less cpu hungry)
        // Question however: when is it ssave to clear the zigzag? to avoid memory overflow in the long run?
        // Anwer: We save and load the candles every 24 hours, perhaps there (TODO)
        accountSymbolIntervalData.ZigZagIndicator ??= new(candleList, true);


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
    /// collect the lows en highs in 1 single list
    /// (unfortunately a full sweep)
    /// </summary>
    public static List<ZigZagResult> PickupZigZagPoints(SortedList<long, CryptoCandle> candleList)
    {
        List<ZigZagResult> zigZagList = [];
        //ZigZagResult? last; // = null;

        for (int x = 0; x < candleList.Count; x++)
        {
            CryptoCandle candle = candleList.Values[x];

            if (candle.ZigZagHigh != 0)
            {
                // remove repeated high values
                //if (last != null && last.PointType == 'H' && candle.ZigZagHigh >= last.Value)
                //{
                //    last.Candle = candle;
                //    last.Value = candle.ZigZagHigh;
                //}
                //else
                {
                    ZigZagResult last = new()
                    {
                        PointType = 'H',
                        Candle = candle,
                        Value = candle.ZigZagHigh
                    };
                    zigZagList.Add(last);
                }
            }

            if (candle.ZigZagLow != 0)
            {
                // remove repeated low values
                //if (last != null && last.PointType == 'L' && candle.ZigZagLow <= last.Value)
                //{
                //    last.Candle = candle;
                //    last.Value = candle.ZigZagLow;
                //}
                //else
                {
                    ZigZagResult last = new()
                    {
                        PointType = 'L',
                        Candle = candle,
                        Value = candle.ZigZagLow
                    };
                    zigZagList.Add(last);
                }
            }
        }

        return zigZagList;
    }



    /// <summary>
    /// Interpret the zigzag values en try to identify a trend
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(List<ZigZagResult> zigZagList, StringBuilder? log)
    {
        CryptoTrendIndicator trend = CryptoTrendIndicator.Sideways;

        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine($"ZigZag points ({zigZagList.Count}) & interpretation:");
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

        return trend;
    }


    public static void Calculate(CryptoSymbol symbol, SortedList<long, CryptoCandle> candleList, AccountSymbolIntervalData accountSymbolIntervalData, long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
    {
#if DEBUG
        string debugText = $"MarketTrend.Calculate {symbol.Name} {accountSymbolIntervalData.Interval.Name}";
#endif

        // Unable to calculate - Note: in fact we need at least ~24 candles because of the zigzag parameters to identify H/L
        if (candleList.Count == 0)
        {
            // Hele discussies, maar als we niet genoeg candles hebben om een trend te berekenen
            // gebruiken we toch de sideway's om aan te geven dat het niet berekend kon worden.
            // Bij nieuwe munten, Flatliners (ethusdt) en andere gedrochten is het dus sideway's!
            //Signal.Reaction = string.Format("not enough quotes for {0} trend", interval.Name);
            accountSymbolIntervalData.Reset();

#if DEBUG
            log?.AppendLine($"{debugText} calculated at {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (no candles)");
            ScannerLog.Logger.Trace($"{debugText} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (no candles)");
#endif
            return;
        }


        if (!ResolveStartAndEndDate(candleList, accountSymbolIntervalData, ref candleIntervalStart, ref candleIntervalEnd))
            return;
#if DEBUG
        int count = 0;
        DateTime candleIntervalStartDebug = CandleTools.GetUnixDate(candleIntervalStart);
        DateTime candleIntervalEndDebug = CandleTools.GetUnixDate(candleIntervalEnd);
#endif


        // We cache the ZigZag indicator, this way we do not have to add all the candles again and again.
        accountSymbolIntervalData.ZigZagIndicator ??= new(candleList, true);



        // Avoid additions of removals.. (indexed)
        //Monitor.Enter(symbol.CandleList); - possible removals
        //try
        //{
        // Add to the ZigZag indicator
        long loop = candleIntervalStart;
        while (loop <= candleIntervalEnd)
        {
#if DEBUG
            count++;
#endif

            if (candleList.TryGetValue(loop, out CryptoCandle? candle))
            {
                accountSymbolIntervalData.ZigZagIndicator.Calculate(candle, accountSymbolIntervalData.Interval.Duration);
                accountSymbolIntervalData.ZigZagLastCandleAdded = loop;
            }
            else log?.AppendLine($"unable to find candle {loop}");
            loop += accountSymbolIntervalData.Interval.Duration;
        }
#if DEBUG
        log?.AppendLine($"Added {count} candles");
#endif
        //}
        //finally
        //{
        //    //Monitor.Exit(symbol.CandleList);
        //}


        // Make a list (without the empry values)
        List<ZigZagResult> zigZagList = PickupZigZagPoints(candleList);

        // Interpret the zigzag list
        CryptoTrendIndicator trendIndicator = InterpretZigZagPoints(zigZagList, log);
        accountSymbolIntervalData.TrendIndicator = trendIndicator;
        accountSymbolIntervalData.TrendInfoUnix = candleIntervalEnd;
        accountSymbolIntervalData.TrendInfoDate = CandleTools.GetUnixDate(candleIntervalEnd);

#if DEBUG
        log?.AppendLine($"{debugText} candles={candleList.Count} calculated at {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} added={count} {candleIntervalStartDebug}..{candleIntervalEndDebug} (executed)");
        ScannerLog.Logger.Trace($"{debugText} candles={candleList.Count} calculated at {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} added={count} {candleIntervalStartDebug}..{candleIntervalEndDebug} (executed)");
#endif
        return;
    }

}
