using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using System.Text;

namespace CryptoScanBot.Core.Trend;


public class TrendInterval
{
    private static List<ZigZagResult> PickupZigZagValues(ZigZagIndicator zigZagTest, StringBuilder log)
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
    public static CryptoTrendIndicator InterpretZigZagValues(List<ZigZagResult> zigZagList, StringBuilder log)
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


    // Move to candletools, or interval tools, whatever, not here..
    public static long GetStartOfIntervalCandle(CryptoInterval interval, long someUnixDate)
    {
        long diff = someUnixDate % interval!.Duration;
        long lastCandleIntervalOpenTime = someUnixDate - diff;
        // ?Why? remark: debug and explain..
        if (diff != 0)
            lastCandleIntervalOpenTime -= interval.Duration;
        return lastCandleIntervalOpenTime;
    }


    public static void Calculate(CryptoSymbolInterval symbolInterval, long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
    {
        // TODO - de parameter candleIntervalStart controleren! (staat nu nog op twee plekken op 0)
        // Want voor een backtest heb je een eindpunt nodig en dan alleen de candles daarvoor gebruiken, niet allemaal!

        // Methode 1 via een cAlgo ZigZag

        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("ZigZagTest cAlgo#1");
        //GlobalData.AddTextToLogTab("");

        //GlobalData.Logger.Trace($"CalculateTrend.Start {Symbol.Name} {Interval.Name}");
        // Trend overnemen indien het reeds berekend is (scheelt aardig wat cpu)
        // Elk interval moet na het arriveren van een nieuwe candle opnieuw berekend worden.
        // De trend kan dan hergebruikt worden totdat er een nieuwe candle komt



        // We cache the ZigZag indicator, this way we do not have to add all the candles.
        // (hope this makes the scanner a more less cpu hungry)
        // Question: problem: when is it ssave to clear the zigzag? to avoid memory overflow
        // Anwer: We save and load the candles every 24 hours, so problem solved..
        symbolInterval.ZigZagCache ??= new();
        var cache = symbolInterval.ZigZagCache;


        // Parameter start time (possible via a 1m candle datetime)
        if (candleIntervalStart == 0)
        {
            var candle = symbolInterval.CandleList.Values.First();
            candleIntervalStart = candle.OpenTime;
        }
        candleIntervalStart = GetStartOfIntervalCandle(symbolInterval.Interval, candleIntervalStart);
        // correct the start with what we previously added
        if (cache.LastCandleAdded.HasValue && cache.LastCandleAdded.Value >= candleIntervalStart)
            candleIntervalStart = cache.LastCandleAdded.Value;


        // It is already calculated
        if (symbolInterval.TrendInfoUnix.HasValue && candleIntervalStart == symbolInterval.TrendInfoUnix)
        {
            //GlobalData.Logger.Trace($"SignalCreate.CalculateTrendStuff.Reused {Symbol.Name} {Interval.Name} {Side} {intervalPeriod} {symbolInterval.TrendInfoDate} {trendIndicator}");
            return; // symbolInterval.TrendIndicator;
        }


        // Unable to calculate - Note: in fact we need at least ~24 candles because of the zigzag parameters to identify H/L
        if (symbolInterval.CandleList.Count == 0)
        {
            // Hele discussies, maar als we niet genoeg candles hebben om een trend te berekenen
            // gebruiken we toch de sideway's om aan te geven dat het niet berekend kon worden.
            // Bij nieuwe munten, Flatliners (ethusdt) en andere gedrochten is het dus sideway's!
            //Signal.Reaction = string.Format("not enough quotes for {0} trend", interval.Name);
            symbolInterval.TrendInfoUnix = null;
            symbolInterval.TrendInfoDate = null;
            symbolInterval.TrendIndicator = CryptoTrendIndicator.Sideways;
            return;
        }



        // Parameter end time 
        if (candleIntervalEnd == 0)
        {
            var candle = symbolInterval.CandleList.Values.Last();
            candleIntervalEnd = candle.OpenTime;
        }
        candleIntervalEnd = GetStartOfIntervalCandle(symbolInterval.Interval, candleIntervalEnd);


        // Add to the ZigZag indicator
        long loop = candleIntervalStart;
        while (loop < candleIntervalEnd)
        {
            if (symbolInterval.CandleList.TryGetValue(loop, out var candle))
            {
                cache.Indicator.Calculate(candle, true);
                cache.LastCandleAdded = loop;
            }
            loop += symbolInterval.Interval.Duration;
        }
        //GlobalData.Logger.Trace($"CalculateTrend.Pickup {Symbol.Name} {Interval.Name} {Candles.Values.Count}");


        // Maak van de gevonden punten een bruikbare ZigZag lijst
        List<ZigZagResult> zigZagList = PickupZigZagValues(cache.Indicator, log);

        //GlobalData.Logger.Trace($"CalculateTrend.Interpret {Symbol.Name} {Interval.Name} {Candles.Values.Count} {zigZagList.Count}");

        CryptoTrendIndicator trendIndicator = InterpretZigZagValues(zigZagList, log);

        //GlobalData.Logger.Trace($"CalculateTrend.Done {Symbol.Name} {Interval.Name} {Candles.Values.Count} {trendIndicator}");

        //GlobalData.Logger.Trace($"SignalCreate.CalculateTrendStuff.Start {Symbol.Name} {Interval.Name} {Side} {intervalPeriod} {symbolInterval.TrendInfoDate} candles={symbolInterval.CandleList.Count}");
        //TrendInterval trendInterval = new();
        //trendInterval.Calculate(symbolInterval, candleIntervalStart);

        symbolInterval.TrendIndicator = trendIndicator;
        symbolInterval.TrendInfoUnix = candleIntervalStart;
        symbolInterval.TrendInfoDate = CandleTools.GetUnixDate(candleIntervalStart);
        //GlobalData.Logger.Trace($"SignalCreate.CalculateTrendStuff.Done {Symbol.Name} {Interval.Name} {Side} {intervalPeriod} {symbolInterval.TrendInfoDate} {trendIndicator}");
    }

}
