using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Intern;
using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Intern;

public class CandleIndicatorData
{
    //public double? Tema { get; set; }

    // Simple Moving Average
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? Sma100 { get; set; }
    public double? Sma200 { get; set; }
#if EXTRASTRATEGIESSLOPESMA
    public double? SlopeSma20 { get; set; }
    public double? SlopeSma50 { get; set; }
#endif

    // Exponential Moving Average
    //public double? Ema8 { get; set; }
#if EXTRASTRATEGIES
    public double? Ema9 { get; set; }
    public double? Ema26 { get; set; }

    public double? Ema20 { get; set; }
    public double? Ema50 { get; set; }
    //public double? Ema100 { get; set; }
    //public double? Ema200 { get; set; }
#endif
#if  EXTRASTRATEGIESSLOPEEMA
    public double? SlopeEma20 { get; set; }
    public double? SlopeEma50 { get; set; }
#endif

    public double? Rsi { get; set; }
    //public double? SlopeRsi { get; set; }

    public double? MacdValue { get; set; }
    public double? MacdSignal { get; set; }
    public double? MacdHistogram { get; set; } // kan ook calculated worden (signal - value of andersom)
#if EXTRASTRATEGIES
    public double? MacdHistogram2 { get { return MacdSignal - MacdValue; } }

    //public double? MacdLtValue { get; set; }
    //public double? MacdLtSignal { get; set; }
    public double? MacdTestHistogram { get; set; } // kan ook calculated worden (signal - value of andersom)
    //public double? MacdLtHistogram2 { get { return MacdLtSignal - MacdLtValue; } }
#endif

    /// <summary>
    /// Stoch Oscillator %K (blauw)
    /// </summary>
    public double? StochOscillator { get; set; } // K
    /// <summary>
    /// Stoch Signal %D (rood)
    /// </summary>
    public double? StochSignal { get; set; } // D

    public double? PSar { get; set; }

    //#if DEBUG
    //public double? PSarDave { get; set; }
    //public double? PSarJason { get; set; }
    //public double? PSarTulip { get; set; }
    //#endif
    public double? BollingerBandsUpperBand { get { return Sma20 + BollingerBandsDeviation; } }
    public double? BollingerBandsLowerBand { get { return Sma20 - BollingerBandsDeviation; } }
    public double? BollingerBandsPercentage { get; set; }
    public double? BollingerBandsDeviation { get; set; }

    public double? KeltnerUpperBand { get; set; }
    public double? KeltnerLowerBand { get; set; }
    public double? KeltnerCenterLine { get; set; }
    public double? KeltnerCenterLineSlope { get; set; }

    // Voor de SMA lookback willen we 60 sma200's erin, dus 200 + 60
    private const int maxCandles = 260;


    /// <summary>
    /// Make a list of candles up to firstCandleOpenTime with at least 260 candles.
    /// (target: ma200 for the last 60 minutes, but also the other indicators)
    /// </summary>
    static public List<CryptoCandle> CalculateCandles(CryptoSymbol symbol, CryptoInterval interval, long firstCandleOpenTime, out string errorstr)
    {
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> intervalCandles = symbolPeriod.CandleList;
        if (intervalCandles.Count < maxCandles)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={intervalCandles.Count} requested={maxCandles}";
            return null;
        }


        // https://trendspider.com/learning-center/linear-regression-slope-a-comprehensive-guide-for-traders/
        //Calculation and Interpretation of the LRS
        //The Linear Regression Slope is calculated using the following formula:
        //Slope = (N * Σ(xy) - Σx * Σy) / (N * Σ(x ^ 2) - (Σx) ^ 2)
        //Where x represents the periods, y represents the asset’s closing prices, and N is the number of periods in the linear regression analysis.


        // Een kleine selectie van candles overnemen voor het uitrekenen van de indicators
        // De firstCandleOpenTime is de eerste candle die we in de selectie moeten zetten.
        List<CryptoCandle> candlesForHistory = [];


        //Monitor.Enter(symbol.CandleList);
        //try
        //{
        // Geen verandering als het goed is (is reeds afgerond als het goed is)
        long candleEndTime = firstCandleOpenTime - firstCandleOpenTime % interval.Duration;
        long candleStartTime = candleEndTime - (maxCandles - 1) * interval.Duration;

        CryptoCandle candleLast = null;
        long candleLoop = candleStartTime;
        while (candleLoop <= candleEndTime)
        {
#if DEBUG
            DateTime candleLoopDate = CandleTools.GetUnixDate(candleLoop);
#endif
            if (intervalCandles.TryGetValue(candleLoop, out CryptoCandle candle))
            {
                //if (!candlesForHistory.ContainsKey(candle.OpenTime))
                candlesForHistory.Add(candle);
                //else
                //    // Hoe kun je hier een duplicate key op krijgen? Dan zou de inhoud van de candles lijst corrupt moeten zijn?
                //    // dwz, de candle.opentime en de key[x] zijn dan niet synchroon (kan dat? uberhaupt)
                //    GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Duplicate candle information " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
            }
            else
            {
                //// De laatste candle is niet altijd aanwezig (wellicht een kwestie van timing, maar ik ben hier onzeker over...)
                //if (firstCandleOpenTime != candleLoop) // && candleLoop != candleEndTime
                //{
                //    // In de hoop dat dit het automatisch zou kunnen fixen?
                //    //symbolPeriod.IsChanged = true;
                //    //if (symbolPeriod.LastCandleSynchronized > candleLoop - interval.Duration)
                //    //    symbolPeriod.LastCandleSynchronized = candleLoop - interval.Duration;
                //    GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information " + CandleTools.GetUnixDate(candleLoop).ToLocalTime());
                //    //ScannerSession.ConnectionWasRestored(""); // A quick fix (dont like it)?
                //}

                // Genereer dan maar een dummy candle
                if (candleLast != null)
                {
                    candle = new()
                    {
                        OpenTime = candleLoop,
                        Open = candleLast.Close,
                        Low = candleLast.Close,
                        High = candleLast.Close,
                        Close = candleLast.Close,
                        Volume = 0,
                        IsDuplicated = true
                    };

                    candlesForHistory.Add(candle);
                }
                //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information (recreated) " + CandleTools.GetUnixDate(candleLoop).ToLocalTime());
            }

            candleLoop += interval.Duration;
            candleLast = candle;
        }


        // Mhh, blijkbaar waren er gewoon niet goed candles
        // Kan ook omdat de exchange geen volume had voor die candle
        // (nu wellicht overbodig doordat we hieboven dummy candles in de history erbij zetten)


        if (candlesForHistory.Count < maxCandles)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={candlesForHistory.Count} requested={maxCandles}";
            if (candlesForHistory.Count != 0)
            {
                CryptoCandle x = candlesForHistory.Last();
                errorstr += " last in history = " + x.DateLocal.ToString();

                x = intervalCandles.Values.Last();
                errorstr += " last in candlelist = " + x.DateLocal.ToString();
            }

            return null;
        }

        // Fill in missing candles (repeating the last candle.close) up to nextCandleOpenTime
        // We assume nothing has happened in that period (flat candles with no orders)
        //CandleTools.AddMissingSticks(candlesForHistory, firstCandleOpenTime, interval);
        //}
        //finally
        //{
        //    Monitor.Exit(symbol.CandleList);
        //}

        // Convert the list to a input kind the stupid indicators are using
        errorstr = "";
        //List<CryptoCandle> history = candlesForHistory.Values.ToList();
        return candlesForHistory;
    }



    //private void DumpCandles()
    //{
    //    GlobalData.AddTextToLogTab(Symbol.Name + " error data?");
    //    try
    //    {
    //        foreach (CryptoCandle quotte in Candles.Values)
    //        {
    //            GlobalData.AddTextToLogTab(
    //                Symbol.Name + " " +
    //                quotte.DateLocal.ToString() + ", " +
    //                quotte.Open.ToString() + ", " +
    //                quotte.High.ToString() + ", " +
    //                quotte.Low.ToString() + ", " +
    //                quotte.Close.ToString() + " " +
    //                "sma200=" + quotte.Low.ToString() + ", " +

    //                );
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //    }
    //}


    /// <summary>
    /// Calculate all the indicators we want to have an fill in the last 60 candles
    /// </summary>
    public static void CalculateIndicators(List<CryptoCandle> history, int fillMax = 61)
    {
        // Overslaan indien het gevuld is (meerdere aanroepen)
        CryptoCandle candle = history.Last();
        if (!GlobalData.BackTest && candle.CandleData != null)
            return;

        //List<TemaResult> temaList = (List<TemaResult>)Indicator.GetTema(history, 5);

        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
#if EXTRASTRATEGIES
        List<EmaResult> emaList9 = (List<EmaResult>)history.GetEma(9);
        List<EmaResult> emaList26 = (List<EmaResult>)history.GetEma(26);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        //List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
#endif
#if EXTRASTRATEGIESSLOPEEMA
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(3);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(3);
#endif

        //List<SmaResult> smaList8 = (List<SmaResult>)Indicator.GetSma(history, 8);
        List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        //List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);
#if EXTRASTRATEGIESSLOPESMA
        List<SlopeResult> slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(3);
        List<SlopeResult> slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(3);
#endif

        // Berekend vanuit de EMA 20 en de upper en lowerband ontstaat uit 2x de ATR
        List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(history, 20, 1);
        List<SlopeResult> keltnerSlopeList = keltnerList.GetSlope(3);

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(history);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi();
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();
#if EXTRASTRATEGIES
        List<MacdResult> macdLtList = (List<MacdResult>)history.GetMacd(34, 144);
#endif

        //List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSma(25).GetSlope(3);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        List<StochResult> stochList = (List<StochResult>)history.GetStoch(14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...


#if DEBUG
        //List<ParabolicSarResult> psarListDave = (List<ParabolicSarResult>)history.GetParabolicSar();
        //List<ParabolicSarResult> psarListJason = (List<ParabolicSarResult>)history.CalcParabolicSarJasonLam();
        //List<ParabolicSarResult> psarListTulip = (List<ParabolicSarResult>)history.CalcParabolicSarTulip();
#endif
        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)history.GetBollingerBands();

        // Because Skender.Psar has different results  we use the old ta-lib (I dont like that)
        //var inOpen = history.Select(x => Convert.ToDouble(x.Open)).ToArray();
        var inHigh = history.Select(x => Convert.ToDouble(x.High)).ToArray();
        var inLow = history.Select(x => Convert.ToDouble(x.Low)).ToArray();
        //var inClose = history.Select(x => Convert.ToDouble(x.Close)).ToArray();

        int startIdx = 0;
        int endIdx = history.Count - 1;
        //int outNbElement; // aantal elementen in de array vanaf 0
        TicTacTec.TA.Library.Core.RetCode retCode;

        double[] psarValues = new double[history.Count];
        retCode = TicTacTec.TA.Library.Core.Sar(startIdx, endIdx, inHigh, inLow, 0.02, 0.2,
            out int outBegIdxPSar, out int outNbElement, psarValues);

        //// We might do everything via ta-lib, but its a tricky library
        //// (for now we only use it for the correct psar values)
        //double[] bbUpperBand = new double[history.Count];
        //double[] bbMiddleBand = new double[history.Count];
        //double[] bbLowerBand = new double[history.Count];
        //retCode = TicTacTec.TA.Library.Core.Bbands(startIdx, endIdx, inClose, 20, 2.0, 2.0, TicTacTec.TA.Library.Core.MAType.Sma,
        //    out int outBegIdxBb, out outNbElement, bbUpperBand, bbMiddleBand, bbLowerBand);

        //double[] macdValue = new double[history.Count];
        //double[] macdSignal = new double[history.Count];
        //double[] macdHistogram = new double[history.Count];
        //retCode = TicTacTec.TA.Library.Core.Macd(startIdx, endIdx, inClose, 12, 26, 9, 
        //    out int outBegIdxMacd, out outNbElement, macdValue, macdSignal, macdHistogram);

        // Fill the last 60 candles with the indicator data
        int iteration = 0;
        for (int index = history.Count - 1; index >= 0; index--)
        {
            // Maximaal 60 records aanvullen
            iteration++;
            if (iteration > fillMax)
                break;


            candle = history[index];

            CandleIndicatorData candleData = new();
            candle.CandleData = candleData;
            try
            {
                //// EMA's
                ////candleData.Ema8 = emaList8[index].Ema;
#if EXTRASTRATEGIES
                candleData.Ema9 = emaList9[index].Ema;
                candleData.Ema26 = emaList26[index].Ema;
                candleData.Ema20 = emaList20[index].Ema;
                candleData.Ema50 = emaList50[index].Ema;
                //candleData.Ema100 = emaList100[index].Ema;
                //candleData.Ema200 = emaList200[index].Ema;
#endif
#if EXTRASTRATEGIESSLOPEEMA
                candleData.SlopeEma20 = slopeEma20List[index].Slope;
                candleData.SlopeEma50 = slopeEma50List[index].Slope;
#endif

                //candleData.Tema = temaList[index].Tema;

                // SMA's
                //candleData.Sma8 = smaList8[index].Sma;
                candleData.Sma20 = bollingerBandsList[index].Sma;
                candleData.Sma50 = smaList50[index].Sma;
                //candleData.Sma100 = smaList100[index].Sma;
                candleData.Sma200 = smaList200[index].Sma;
#if EXTRASTRATEGIESSLOPESMA
                candleData.SlopeSma20 = slopeSma20List[index].Slope;
                candleData.SlopeSma50 = slopeSma50List[index].Slope;
#endif

                candleData.Rsi = rsiList[index].Rsi;
                //candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;

#if EXTRASTRATEGIES
                //candleData.MacdLtValue = macdLtList[index].Macd;
                //candleData.MacdLtSignal = macdLtList[index].Signal;
                candleData.MacdTestHistogram = macdLtList[index].Histogram;
#endif

                candleData.StochSignal = stochList[index].Signal;
                candleData.StochOscillator = stochList[index].Oscillator;

                double? BollingerBandsLowerBand = bollingerBandsList[index].LowerBand;
                double? BollingerBandsUpperBand = bollingerBandsList[index].UpperBand;
                candleData.BollingerBandsDeviation = 0.5 * (BollingerBandsUpperBand - BollingerBandsLowerBand);
                candleData.BollingerBandsPercentage = 100 * ((BollingerBandsUpperBand / BollingerBandsLowerBand) - 1);

                if (index >= outBegIdxPSar)
                    candleData.PSar = psarValues[index - outBegIdxPSar];
#if DEBUG
                //if (psarListDave[index].Sar != null)
                //    candleData.PSarDave = psarListDave[index].Sar;
                //candleData.PSarJason = psarListJason[index].Sar;
                //candleData.PSarTulip = psarListTulip[index].Sar;
#endif
                candleData.KeltnerUpperBand = keltnerList[index].UpperBand;
                candleData.KeltnerLowerBand = keltnerList[index].LowerBand;
                candleData.KeltnerCenterLine = keltnerList[index].Centerline;
                candleData.KeltnerCenterLineSlope = keltnerSlopeList[index].Slope;
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab("error indicators");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab(history.ToString());
                throw;
            }

        }
    }

    // Extended with 1 day + 9 hours because of the 24 hour market climate (or barometer).  (we show ~6 hours of that in the display)
    private static long InitialCandleCountFetch = ((24 + 7) * 60 * 60) * 2;

    public static void SetInitialCandleCountFetch(long value)
    {
        // Ter debug uitgezet
        InitialCandleCountFetch = value;
    }

    public static long GetCandleFetchStart(CryptoSymbol symbol, CryptoInterval interval, DateTime utcNow)
    {
        long startFetchUnix;

        // Since the market climate is also a coin we must make an exception, it needs more candles because of the 24h bm calculation
        if (symbol.IsBarometerSymbol())
        {
            startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - 1440 * 60; // interval.Duration;
        }
        else
        {
            // Bepaal de start datum (we willen niet "teveel" data uit de database of bestanden ophalen)
            // Voor de 1m tenminste 2 dagen (die zijn nodig voor de (prijs & volume) berekening van de 24h barometer
            // --> Als we de volume berekening achterwege laten dan zijn het er 24*60=1440 (volume doen we vanwege hoge cpu niet meer)
            // En anders tenminste 215 candles voor de indicators (215 vanwege de 200 sma of macd indicator)

            // NB: Achteraf hebben we een markttrend geintroduceerd en deze heeft meer candles nodig dan we in gedachten hadden..
            // Achteraf beredeneerd wordt de markttrend dus niet correct berekend vanwege het (minimaal) aantal aanwezige candles..
            // Met deze nieuwe kennis: Zou het een idee zijn om oude zigzag waarden (per interval) te bewaren?
            // (zodat we niet een volledige hoeveelheid candles hoeven in te laden?)
            // Maar dan heb je dus wel de voorgeschiedenis nodig, beetje kip/ei
            // Voorlopig heb ik het aantal candles verdubbeld, we zien het wel.....

            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - InitialCandleCountFetch;
            else
                // de 0 was eerst een 10 (en later 49) en bedoeld om meldingen met terugwerkende kracht te berekenen bij de start
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - ((49 + maxCandles) * interval.Duration) * 2;
            startFetchUnix -= startFetchUnix % interval.Duration;

            // Lets extend that with 1 extra candle just in case...
            startFetchUnix -= interval.Duration;
        }
        //DateTime symbolfetchCandleDebug = CandleTools.GetUnixDate(startFetchUnix);  //debug
        return startFetchUnix;
    }


    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, 
        CryptoCandle candle, out string reaction, int fillMax = 61)
    {
        if (candle.CandleData == null)
        {
            // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
            List<CryptoCandle> History = CalculateCandles(symbol, symbolInterval.Interval, candle.OpenTime, out reaction);
            if (History == null)
            {
                //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                //symbolInterval.Signal = null;
                return false;
            }

            if (History.Count == 0)
            {
                reaction = "Geen candles";
                //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                //symbolInterval.Signal = null;
                //reaction = 
                return false;
            }

            CalculateIndicators(History, fillMax);
        }

        reaction = "";
        return true;
    }
}


public static class KeltnerHelper
{
    // parameter validation
    private static void ValidateSlope(int lookbackPeriods)
    {
        // check parameter arguments
        if (lookbackPeriods <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lookbackPeriods), lookbackPeriods,
                "Lookback periods must be greater than 1 for Slope/Linear Regression.");
        }
    }

    // calculate series
    internal static List<SlopeResult> GetSlope(this List<KeltnerResult> tpList, int lookbackPeriods)
    {
        // check parameter arguments
        ValidateSlope(lookbackPeriods);

        // initialize
        int length = tpList.Count;
        List<SlopeResult> results = new(length);

        // roll through quotes
        for (int i = 0; i < length; i++)
        {
            KeltnerResult x = tpList[i];

            SlopeResult r = new(x.Date);
            results.Add(r);

            // skip initialization period
            if (i + 1 < lookbackPeriods)
            {
                continue;
            }

            // get averages for period
            double sumX = 0;
            double sumY = 0;

            for (int p = i - lookbackPeriods + 1; p <= i; p++)
            {
                KeltnerResult x2 = tpList[p];

                sumX += p + 1d;
                if (x2.Centerline.HasValue)
                    sumY += (double)x2.Centerline;
            }

            double avgX = sumX / lookbackPeriods;
            double avgY = sumY / lookbackPeriods;

            // least squares method
            double sumSqX = 0;
            double sumSqY = 0;
            double sumSqXY = 0;

            for (int p = i - lookbackPeriods + 1; p <= i; p++)
            {
                KeltnerResult x3 = tpList[p];

                double devX = p + 1d - avgX;
                double devY = 0;
                if (x3.Centerline.HasValue)
                    devY = (double)x3.Centerline - avgY;

                sumSqX += devX * devX;
                sumSqY += devY * devY;
                sumSqXY += devX * devY;
            }

            r.Slope = (sumSqXY / sumSqX).NaN2Null();
            r.Intercept = (avgY - r.Slope * avgX).NaN2Null();

            // calculate Standard Deviation and R-Squared
            double stdDevX = Math.Sqrt(sumSqX / lookbackPeriods);
            double stdDevY = Math.Sqrt(sumSqY / lookbackPeriods);
            r.StdDev = stdDevY.NaN2Null();

            if (stdDevX * stdDevY != 0)
            {
                double arrr = sumSqXY / (stdDevX * stdDevY) / lookbackPeriods;
                r.RSquared = (arrr * arrr).NaN2Null();
            }
        }

        // add last Line (y = mx + b)
        if (length >= lookbackPeriods)
        {
            SlopeResult last = results.LastOrDefault();
            for (int p = length - lookbackPeriods; p < length; p++)
            {
                SlopeResult d = results[p];
                d.Line = (decimal?)(last?.Slope * (p + 1) + last?.Intercept).NaN2Null();
            }
        }

        return results;
    }
}
