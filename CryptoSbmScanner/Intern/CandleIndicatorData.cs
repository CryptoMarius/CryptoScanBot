using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Intern;

public class CandleIndicatorData
{
    //public double? Tema { get; set; }

    // Simple Moving Average
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? Sma100 { get; set; }
    public double? Sma200 { get; set; }
    public double? SlopeSma20 { get; set; }
    public double? SlopeSma50 { get; set; }

    // Exponential Moving Average
    //public double? Ema8 { get; set; }
    public double? Ema20 { get; set; }
    public double? Ema50 { get; set; }
    //public double? Ema100 { get; set; }
    public double? Ema200 { get; set; }
    public double? SlopeEma20 { get; set; }
    public double? SlopeEma50 { get; set; }

    public double? Rsi { get; set; }
    public double? SlopeRsi { get; set; }

    public double? MacdValue { get; set; }
    public double? MacdSignal { get; set; }
    public double? MacdHistogram { get; set; } // kan ook calculated worden (signal - value of andersom)
    public double? MacdHistogram2 { get { return MacdSignal - MacdValue; } }

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


        // Een kleine selectie van candles overnemen voor het uitrekenen van de indicators
        // De firstCandleOpenTime is de eerste candle die we in de selectie moeten zetten.
        List<CryptoCandle> candlesForHistory = new();


        //Monitor.Enter(symbol.CandleList);
        //try
        //{
        // Geen verandering als het goed is (is reeds afgerond als het goed is)
        long candleEndTime = firstCandleOpenTime - (firstCandleOpenTime % interval.Duration);
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
                    candle = new();
                    candle.OpenTime = candleLoop;
                    candle.Open = candleLast.Close;
                    candle.Low = candleLast.Close;
                    candle.High = candleLast.Close;
                    candle.Close = candleLast.Close;
                    candle.Volume = 0;
                    candle.IsDuplicated = true;

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
            if (candlesForHistory.Any())
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
    //        GlobalData.Logger.Error(error);
    //    }
    //}


    /// <summary>
    /// Calculate all the indicators we want to have an fill in the last 60 candles
    /// </summary>
    public static void CalculateIndicators(List<CryptoCandle> history)
    {
        // Overslaan indien het gevuld is (meerdere aanroepen)
        CryptoCandle candle = history.Last();
        if (!GlobalData.BackTest && candle.CandleData != null)
            return;

        //List<TemaResult> temaList = (List<TemaResult>)Indicator.GetTema(history, 5);

        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(3);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(3);

        //List<SmaResult> smaList8 = (List<SmaResult>)Indicator.GetSma(history, 8);
        List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        //List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);
        List<SlopeResult> slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(3);
        List<SlopeResult> slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(3);

        // Berekend vanuit de EMA 20 en de upper en lowerband ontstaat uit 2x de ATR
        List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(history, 20, 1);

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(history);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi();
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();

        List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSma(25).GetSlope(3);

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
            if (iteration > 61)
                break;


            candle = history[index];

            CandleIndicatorData candleData = new();
            candle.CandleData = candleData;
            try
            {
                // EMA's
                //candleData.Ema8 = emaList8[index].Ema;
                candleData.Ema20 = emaList20[index].Ema;
                candleData.Ema50 = emaList50[index].Ema;
                //candleData.Ema100 = emaList100[index].Ema;
                candleData.Ema200 = emaList200[index].Ema;
                candleData.SlopeEma20 = slopeEma20List[index].Slope;
                candleData.SlopeEma50 = slopeEma50List[index].Slope;

                //candleData.Tema = temaList[index].Tema;

                // SMA's
                //candleData.Sma8 = smaList8[index].Sma;
                candleData.Sma20 = bollingerBandsList[index].Sma;
                candleData.Sma50 = smaList50[index].Sma;
                //candleData.Sma100 = smaList100[index].Sma;
                candleData.Sma200 = smaList200[index].Sma;
                candleData.SlopeSma20 = slopeSma20List[index].Slope;
                candleData.SlopeSma50 = slopeSma50List[index].Slope;

                candleData.Rsi = rsiList[index].Rsi;
                candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;
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
}
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.Logger.Error(error);
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
    private static long InitialCandleCountFetch = (24 + 7) * 60 * 60;

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
            startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - 1440 * interval.Duration;
        }
        else
        {
            // Bepaal de start datum (we willen ook niet "teveel" data uit de database of bestanden ophalen)
            // Voor de 1m tenminste 2 dagen en anders tenminste 215 candles zodat de indicators het doen
            // (die 2 dagen is nodig voor de (volume) berekening van de 24h barometer, de 215 vanwege de indicators)
            // Als we de volume berekening achterwege laten dan zijn het er 24*60=1440
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - InitialCandleCountFetch;
            else
                // de 0 was eerst een 10 (en later 49) en bedoeld om meldingen met terugwerkende kracht te berekenen bij de start
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - (49 + maxCandles) * interval.Duration;
            startFetchUnix -= startFetchUnix % interval.Duration;

            // Lets extend that with 1 extra candle just in case...
            startFetchUnix -= interval.Duration;
        }
        //DateTime symbolfetchCandleDebug = CandleTools.GetUnixDate(startFetchUnix);  //debug
        return startFetchUnix;
    }


    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, CryptoCandle candle, out string reaction)
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

            CalculateIndicators(History);
        }

        reaction = "";
        return true;
    }

}
