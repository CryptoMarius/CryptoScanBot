using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;

namespace CryptoScanBot.Core.Trend;


public class TrendInterval
{
    private static List<ZigZagResult> PickupZigZagValues(ZigZagIndicator zigZagTest, StringBuilder? log)
    {
        List<ZigZagResult> zigZagList = [];

        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine("ZigZag points:");
        }

        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("ZigZag points:");
        // De lows en highs in 1 lijst zetten voor interpretatie verderop
        // Deze indicator zet de candles net andersom (voila)
        for (int x = zigZagTest.Candles.Count - 1; x >= 0; x--)
        {
            CryptoCandle candle = zigZagTest.Candles[x];

            ZigZagResult zigZagResult;
            if (zigZagTest._highBuffer[x] != 0)
            {
                if (log != null)
                {
                    string s = string.Format("date={0} H {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest._highBuffer[x], candle.CandleData?.Rsi);
                    log.AppendLine(s);
                }

                zigZagResult = new ZigZagResult
                {
                    PointType = "H",
                    Date = candle.Date,
                    Rsi = candle.CandleData?.Rsi,
                    Value = (double)zigZagTest._highBuffer[x]
                };
                zigZagList.Add(zigZagResult);
            }

            if (zigZagTest._lowBuffer[x] != 0)
            {
                if (log != null)
                {
                    string s = string.Format("date={0} L {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest._lowBuffer[x], candle.CandleData?.Rsi);
                    log.AppendLine(s);
                }

                zigZagResult = new ZigZagResult
                {
                    PointType = "L",
                    Date = candle.Date,
                    Rsi = candle.CandleData?.Rsi,
                    Value = (double)zigZagTest._lowBuffer[x]
                };
                zigZagList.Add(zigZagResult);
            }
        }
        return zigZagList;
    }



    /// <summary>
    /// Interpreteer de zigzag values (P&T) en identificeer de trend
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagValues(List<ZigZagResult> zigZagList, StringBuilder? log)
    {
        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine("ZigZag interpretation:");
        }

        CryptoTrendIndicator trend = CryptoTrendIndicator.Sideways;

        // Zijn er meer dan 1 punt?, zoniet laat dan maar, want dan is er niets uit te extraheren
        if (zigZagList.Count < 2)
            return trend;


        // Pak de 1e 2 punten en bepaal een start situatie (de sideway's komt dan niet meer voor).
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
        // Maar maakt het uit, zolang je maar genoeg zigzag punten hebt boeit het niet?)
        ZigZagResult zigZagResult;
        for (int i = 2; i < zigZagList.Count; i++)
        {
            zigZagResult = zigZagList[i];

            // Pickup last value
            double value;
            if (zigZagResult.PointType == "H")
                value = lastHigh;
            else
                value = lastLow;

            // Check Values
            switch (trend)
            {
                case CryptoTrendIndicator.Bearish:
                    if (zigZagResult.Value > value)
                        count++;
                    else count = 0;
                    break;
                case CryptoTrendIndicator.Bullish:
                    if (zigZagResult.Value <= value)
                        count++;
                    else count = 0;
                    break;

            }

            // Save the last value
            if (zigZagResult.PointType == "H")
                lastHigh = zigZagResult.Value;
            else
                lastLow = zigZagResult.Value;


            log?.AppendLine(string.Format("date={0} {1} {2:N8} rsi={3:N8} count={4}, trend={5}", zigZagResult.Date.ToLocalTime(),
                    zigZagResult.PointType, zigZagResult.Value, zigZagResult.Rsi, count, trend));

            // switch trend if 2 values are opposite
            if (count > 1)
            {
                count = 0;
                if (trend == CryptoTrendIndicator.Bearish)
                    trend = CryptoTrendIndicator.Bullish;
                else if (trend == CryptoTrendIndicator.Bullish)
                    trend = CryptoTrendIndicator.Bearish;

                log?.AppendLine("The trend switched");
            }
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
            accountSymbolIntervalData.TrendInfoUnix = null;
            accountSymbolIntervalData.TrendInfoDate = null;
            accountSymbolIntervalData.TrendIndicator = CryptoTrendIndicator.Sideways;
#if DEBUG
            ScannerLog.Logger.Trace($"{debugText} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (no candles)");
#endif
            return;
        }


        // TODO - de parameter candleIntervalStart controleren! (staat nu nog op twee plekken op 0)

        // We cache the ZigZag indicator, this way we do not have to add all the candles again and again.
        // (We hope this makes the scanner a more less cpu hungry)
        // Question however: when is it ssave to clear the zigzag? to avoid memory overflow in the long run?
        // Anwer: We save and load the candles every 24 hours, perhaps there (TODO)
        if (accountSymbolIntervalData.ZigZagCache == null)
            accountSymbolIntervalData.ZigZagCache = new();
        var cache = accountSymbolIntervalData.ZigZagCache; // alias


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
        if (cache.LastCandleAdded.HasValue && cache.LastCandleAdded.Value >= candleIntervalStart)
            candleIntervalStart = cache.LastCandleAdded.Value;



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


        // Already calculated?
        //accountSymbolIntervalData.TrendIndicator != CryptoTrendIndicator.Sideways &&
        if (accountSymbolIntervalData.TrendInfoUnix.HasValue && candleIntervalEnd == accountSymbolIntervalData.TrendInfoUnix)
        {
#if DEBUG
            ScannerLog.Logger.Trace($"{debugText} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} (reused)");
#endif
            return;
        }



#if DEBUG
        int count = 0;
        DateTime candleIntervalStartDebug = CandleTools.GetUnixDate(candleIntervalStart);
        DateTime candleIntervalEndDebug = CandleTools.GetUnixDate(candleIntervalEnd);
#endif

        // Add to the ZigZag indicator
        long loop = candleIntervalStart;
        while (loop <= candleIntervalEnd)
        {
#if DEBUG
            count++;
            DateTime loopDebug = CandleTools.GetUnixDate(loop);
#endif

            if (candleList.TryGetValue(loop, out CryptoCandle? candle))
            {
                cache.Indicator.Calculate(candle);
                cache.LastCandleAdded = loop;
            }
            loop += accountSymbolIntervalData.Interval.Duration;
        }


        // Maak van de gevonden punten een bruikbare ZigZag lijst
        List<ZigZagResult> zigZagList = PickupZigZagValues(cache.Indicator, log);

        CryptoTrendIndicator trendIndicator = InterpretZigZagValues(zigZagList, log);
        accountSymbolIntervalData.TrendIndicator = trendIndicator;
        accountSymbolIntervalData.TrendInfoUnix = candleIntervalEnd;
        accountSymbolIntervalData.TrendInfoDate = CandleTools.GetUnixDate(candleIntervalEnd);

#if DEBUG
        ScannerLog.Logger.Trace($"{debugText} z.cnt={cache.Indicator.Candles.Count} {accountSymbolIntervalData.TrendInfoDate} {accountSymbolIntervalData.TrendIndicator} added={count} {candleIntervalStartDebug}..{candleIntervalEndDebug} (executed)");
#endif
        return;
    }

}
