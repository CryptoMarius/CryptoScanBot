using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Intern;
using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Intern;

public class CandleIndicatorData: CryptoData
{
    private const int SlopeCount = 2;

    // Test
    public double? Ema9 { get; set; }
    public double? Wma30 { get; set; }
    public double? Tema { get; set; }

#if EXTRASTRATEGIES
    public double? MacdHistogram2 { get { return MacdSignal - MacdValue; } }

    //public double? MacdLtValue { get; set; }
    //public double? MacdLtSignal { get; set; }
    public double? MacdTestHistogram { get; set; } // kan ook calculated worden (signal - value of andersom)
    //public double? MacdLtHistogram2 { get { return MacdLtSignal - MacdLtValue; } }
#endif


    // Voor de SMA lookback willen we 60 sma200's erin, dus 200 + 60
    private const int maxCandles = 260;
    //private const int maxCandles = 310 * 2; // extended to 620 because of markettrend calculation


    /// <summary>
    /// Make a list of candles up to firstCandleOpenTime with at least 260 candles.
    /// (target: ma200 for the last 60 minutes, but also the other indicators)
    /// </summary>
    static public List<CryptoCandle>? CalculateCandles(CryptoSymbol symbol, CryptoInterval interval, long firstCandleOpenTime, out string errorstr)
    {
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList intervalCandles = symbolPeriod.CandleList;
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


        //Monitor.Enter(symbol.CandleList);await symbol.CandleLock.WaitAsync();
        //try
        //{
        // Geen verandering als het goed is (is reeds afgerond als het goed is)
        long candleEndTime = firstCandleOpenTime - firstCandleOpenTime % interval.Duration;
        long candleStartTime = candleEndTime - (maxCandles - 1) * interval.Duration;

        CryptoCandle? candleLast = null;
        long candleLoop = candleStartTime;
        while (candleLoop <= candleEndTime)
        {
#if DEBUG
            DateTime candleLoopDate = CandleTools.GetUnixDate(candleLoop);
#endif
            if (intervalCandles.TryGetValue(candleLoop, out CryptoCandle? candle))
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
#if SUPPORTBASEVOLUME
                        BaseVolume = 0,
#endif
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


    /// <summary>
    /// Calculate all the indicators we want to have an fill in the last 60 candles
    /// </summary>
    public static void CalculateIndicators(List<CryptoCandle> history, int fillMax = 61)
    {
        // Overslaan indien het gevuld is (meerdere aanroepen)
        CryptoCandle candle = history.Last();
        if (candle.CandleData != null)
            return;

        List<TemaResult> temaList = (List<TemaResult>)Indicator.GetTema(history, 5);

        List<EmaResult> emaList9 = (List<EmaResult>)history.GetEma(9);
#if EXTRASTRATEGIES
        List<EmaResult> emaList5 = (List<EmaResult>)history.GetEma(5);
        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
        List<EmaResult> emaList26 = (List<EmaResult>)history.GetEma(26);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        //List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
#endif
#if EXTRASTRATEGIESSLOPEEMA
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(SlopeCount);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(SlopeCount);
#endif

        //List<SmaResult> smaList8 = (List<SmaResult>)Indicator.GetSma(history, 8);
        List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);

        // GetSlope looks buggy? (specially with sma(200) and count <> 200)
        List<SlopeResult>? slopeSma20List = null;
        List<SlopeResult>? slopeSma50List = null;
        List<SlopeResult>? slopeSma100List = null;
        List<SlopeResult>? slopeSma200List = null;
        try
        {
            slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(SlopeCount);
            slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(SlopeCount);
            slopeSma100List = (List<SlopeResult>)smaList100.GetSlope(SlopeCount);
            slopeSma200List = (List<SlopeResult>)smaList200.GetSlope(SlopeCount);
        }
        catch (Exception)
        {
            //ignore
        }


        List<WmaResult> wmaList30 = (List<WmaResult>)history.GetWma(30);

        // Berekend vanuit de EMA 20 en de upper en lowerband ontstaat uit 2x de ATR
        //List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(history, 20, 1);

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(history);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi();
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();
        List<SlopeResult> slopeMacdList = (List<SlopeResult>)macdList.GetSlope(SlopeCount);
        //List<VwapResult> vwapList = (List<VwapResult>)history.GetVwap();
#if EXTRASTRATEGIES
        List<MacdResult> macdLtList = (List<MacdResult>)history.GetMacd(34, 144);
#endif

        List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSlope(SlopeCount);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        List<StochResult> stochList = (List<StochResult>)history.GetStoch(14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...
        List<SlopeResult> slopeStochList = (List<SlopeResult>)stochList.GetSlope(SlopeCount);

        List<ParabolicSarResult> psarList = (List<ParabolicSarResult>)history.GetParabolicSar();

        // dan kan nu ook met de stdDev * setting.... Maar komt het wel overeen?
        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)history.GetBollingerBands(
            standardDeviations: GlobalData.Settings.General.BbStdDeviation);

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
                // EMA's
#if EXTRASTRATEGIES
                ////candleData.Ema8 = emaList8[index].Ema;
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


                // SMA's
                //candleData.Sma8 = smaList8[index].Sma;
                candleData.Sma20 = bollingerBandsList[index].Sma;
                candleData.Sma50 = smaList50[index].Sma;
                candleData.Sma100 = smaList100[index].Sma;
                candleData.Sma200 = smaList200[index].Sma;

                if (slopeSma20List != null && index < slopeSma20List.Count)
                    candleData.SlopeSma20 = slopeSma20List[index].Slope;
                if (slopeSma50List != null && index < slopeSma50List.Count)
                    candleData.SlopeSma50 = slopeSma50List[index].Slope;
                if (slopeSma100List != null && index < slopeSma100List.Count)
                    candleData.SlopeSma100 = slopeSma100List[index].Slope;
                if (slopeSma200List != null && index < slopeSma200List.Count) 
                    candleData.SlopeSma200 = slopeSma200List[index].Slope;

                candleData.Rsi = rsiList[index].Rsi;
                candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;
                candleData.SlopeMacd = slopeMacdList[index].Slope;

                // Test
                candleData.Ema9 = emaList9[index].Ema;
                candleData.Tema = temaList[index].Tema;
                candleData.Wma30 = wmaList30[index].Wma;

                //candleData.Vwap = vwapList[index].Vwap;

#if EXTRASTRATEGIES
                //candleData.MacdLtValue = macdLtList[index].Macd;
                //candleData.MacdLtSignal = macdLtList[index].Signal;
                candleData.MacdTestHistogram = macdLtList[index].Histogram;
#endif

                candleData.StochSignal = stochList[index].Signal;
                candleData.StochOscillator = stochList[index].Oscillator;
                candleData.SlopeStoch = slopeStochList[index].Slope;
                

                double? BollingerBandsLowerBand = bollingerBandsList[index].LowerBand;
                double? BollingerBandsUpperBand = bollingerBandsList[index].UpperBand;
                candleData.BollingerBandsDeviation = 0.5 * (BollingerBandsUpperBand - BollingerBandsLowerBand);
                candleData.BollingerBandsPercentage = 100 * ((BollingerBandsUpperBand / BollingerBandsLowerBand) - 1);

                if (psarList[index].Sar != null)
                    candleData.PSar = psarList[index].Sar;
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab("error indicators");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
                //GlobalData.AddTextToLogTab(history.ToString());
                throw;
            }

        }
    }

    // We need 1 day + X hours because of the barometr calculation (we show ~5 hours in the display)
    // As soon as the barometer has been calculated it will be lowered to 1 day + 10 candles..
    private const int barometerGraphHours = 5;
    private static long InitialCandleCountFetch = (24 + barometerGraphHours) * 60;


    public static void SetInitialCandleCountFetch(long value)
    {
        if (InitialCandleCountFetch != value)
        {
            //GlobalData.AddTextToLogTab($"SetInitialCandleCountFetch from {InitialCandleCountFetch} to {value}");
            InitialCandleCountFetch = value;
        }
    }


    public static long GetCandleFetchStart(CryptoSymbol symbol, CryptoInterval interval, DateTime utcNow)
    {
        long startFetchUnix;
        // Since the market climate is also a coin we must make an exception, it needs more candles because of the 24h bm calculation
        if (symbol.IsBarometerSymbol())
            startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - barometerGraphHours * 60 * GlobalData.IntervalList[0].Duration;
        else
        {
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                // For the 1m we need *initially* ~1 day plus the data needed for the barometer graph
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - InitialCandleCountFetch * interval.Duration;
            else
                // 260 would be enough for calculating the standard indicator data.
                // But we extended that amount because of the markettrend calculation.
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - 500 * interval.Duration;
            startFetchUnix -= startFetchUnix % interval.Duration;
        }
        return startFetchUnix;
    }


    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, 
        CryptoCandle candle, out string reaction, int fillMax = 61)
    {
        if (candle.CandleData == null)
        {
            // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
            List<CryptoCandle>? History = CalculateCandles(symbol, symbolInterval.Interval, candle.OpenTime, out reaction);
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


//public static class KeltnerHelper
//{
//    // parameter validation
//    private static void ValidateSlope(int lookbackPeriods)
//    {
//        // check parameter arguments
//        if (lookbackPeriods <= 1)
//        {
//            throw new ArgumentOutOfRangeException(nameof(lookbackPeriods), lookbackPeriods,
//                "Lookback periods must be greater than 1 for Slope/Linear Regression.");
//        }
//    }

//    // calculate series
//    internal static List<SlopeResult> GetSlope(this List<KeltnerResult> tpList, int lookbackPeriods)
//    {
//        // check parameter arguments
//        ValidateSlope(lookbackPeriods);

//        // initialize
//        int length = tpList.Count;
//        List<SlopeResult> results = new(length);

//        // roll through quotes
//        for (int i = 0; i < length; i++)
//        {
//            KeltnerResult x = tpList[i];

//            SlopeResult r = new(x.Date);
//            results.Add(r);

//            // skip initialization period
//            if (i + 1 < lookbackPeriods)
//            {
//                continue;
//            }

//            // get averages for period
//            double sumX = 0;
//            double sumY = 0;

//            for (int p = i - lookbackPeriods + 1; p <= i; p++)
//            {
//                KeltnerResult x2 = tpList[p];

//                sumX += p + 1d;
//                if (x2.Centerline.HasValue)
//                    sumY += (double)x2.Centerline;
//            }

//            double avgX = sumX / lookbackPeriods;
//            double avgY = sumY / lookbackPeriods;

//            // least squares method
//            double sumSqX = 0;
//            double sumSqY = 0;
//            double sumSqXY = 0;

//            for (int p = i - lookbackPeriods + 1; p <= i; p++)
//            {
//                KeltnerResult x3 = tpList[p];

//                double devX = p + 1d - avgX;
//                double devY = 0;
//                if (x3.Centerline.HasValue)
//                    devY = (double)x3.Centerline - avgY;

//                sumSqX += devX * devX;
//                sumSqY += devY * devY;
//                sumSqXY += devX * devY;
//            }

//            r.Slope = (sumSqXY / sumSqX).NaN2Null();
//            r.Intercept = (avgY - r.Slope * avgX).NaN2Null();

//            // calculate Standard Deviation and R-Squared
//            double stdDevX = Math.Sqrt(sumSqX / lookbackPeriods);
//            double stdDevY = Math.Sqrt(sumSqY / lookbackPeriods);
//            r.StdDev = stdDevY.NaN2Null();

//            if (stdDevX * stdDevY != 0)
//            {
//                double arrr = sumSqXY / (stdDevX * stdDevY) / lookbackPeriods;
//                r.RSquared = (arrr * arrr).NaN2Null();
//            }
//        }

//        // add last Line (y = mx + b)
//        if (length >= lookbackPeriods)
//        {
//            SlopeResult last = results.LastOrDefault();
//            for (int p = length - lookbackPeriods; p < length; p++)
//            {
//                SlopeResult d = results[p];
//                d.Line = (decimal?)(last?.Slope * (p + 1) + last?.Intercept).NaN2Null();
//            }
//        }

//        return results;
//    }
//}
