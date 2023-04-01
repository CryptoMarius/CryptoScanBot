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
    public double? MacdHistogram { get; set; }

    public double? StochOscillator { get; set; } // K
    public double? StochSignal { get; set; } // D

    public double? PSar { get; set; }

#if DEBUG
    //public double? PSarDave { get; set; }
    //public double? PSarJason { get; set; }
    //public double? PSarTulip { get; set; }
#endif
    public double? BollingerBandsUpperBand { get { return Sma20 + BollingerBandsDeviation; } }
    public double? BollingerBandsLowerBand { get { return Sma20 - BollingerBandsDeviation; } }
    public double? BollingerBandsPercentage { get; set; }
    public double? BollingerBandsDeviation { get; set; }

    //public KeltnerResult Keltner { get; set; }

    // 200 voor SMA200, //135 voor de MACD 114; // 64; //30 is genoeg voor de laatste waarde in de StochRSI (28) en wat voor wat extra's 
    // (Dave heeft het verhoogd naar 64, later voor de RSI nog eens naar de 114, en voor de Macd naar 135, SMA naar 200, balen!!)
    // Voor de SMA lookback willen we 60 sma200's erin, dus 200 + 60 !!
    private const int maxCandles = 260;


    /// <summary>
    /// Make a list of candles up to nextCandleOpenTime with at least 260 candles.
    /// (target: ma200 for the last 60 minutes, but also the other indicators)
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="interval"></param>
    /// <param name="nextCandleOpenTime"></param>
    /// <param name="errorstr"></param>
    /// <returns></returns>
    static public List<CryptoCandle> CalculateCandles(CryptoSymbol symbol, CryptoInterval interval, long nextCandleOpenTime, out string errorstr)
    {
        SortedList<long, CryptoCandle> candlesForHistory = new();

        Monitor.Enter(symbol.CandleList);
        try
        {
            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> intervalCandles = symbolPeriod.CandleList;
            if (intervalCandles.Count < maxCandles)
            {
                errorstr = string.Format("{0} {1} Not enough candles available count={2} requested={3}", symbol.Name, interval.Name, intervalCandles.Count, maxCandles);
                return null;
            }

            CryptoCandle firstCandle = intervalCandles.Values.First();
            long firstTime = firstCandle.OpenTime;

            bool first = true;

            // Een kleine selectie van candles overnemen.
            // Mijn vermoeden is we de 1e keer geen candle hebben (omdat we de tijd van de volgende candle krijgen)
            // MAAR, vanuit een backtest is deze candle er wel, dus hier moet ik waarschijnlijk iets aan doen....
            // Probleem, als er niet genoeg candles zijn krijgen we een endless loop! ;-)
            long candleLoop = nextCandleOpenTime - (nextCandleOpenTime % interval.Duration); //Geen verandering als het goed is
                                                                                             //DateTime candleLoopDebug = CandleTools.GetUnixDate(candleLoop); //debug
            while (candlesForHistory.Count < maxCandles)
            {
                if (intervalCandles.TryGetValue(candleLoop, out CryptoCandle candle))
                {
                    if (!candlesForHistory.ContainsKey(candle.OpenTime))
                        candlesForHistory.Add(candle.OpenTime, candle);
                    else
                        // Hoe kun je hier een duplicate key op krijgen? Dan zou de inhoud van de candles lijst corrupt moeten zijn?
                        // dwz, de candle.opentime en de key[x] zijn dan niet synchroon (kan dat? uberhaupt)
                        GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Duplicate candle information " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                }
                else
                {
                    // De laatste candle is niet altijd aanwezig (een kwestie van timing?)
                    if ((nextCandleOpenTime != candleLoop) && !first)
                    {
                        // In de hoop dat dit het automatisch zou kunnen fixen?
                        if (symbolPeriod.LastCandleSynchronized.Value > candleLoop - interval.Duration)
                            symbolPeriod.LastCandleSynchronized = candleLoop - interval.Duration;
                        GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information " + CandleTools.GetUnixDate(candleLoop).ToLocalTime());
                        GlobalData.ConnectionWasRestored(""); // A quick fix (dont like it)?
                    }
                }
                candleLoop -= interval.Duration;
                //candleLoopDebug = CandleTools.GetUnixDate(candleLoop);

                if (candleLoop < firstTime)
                    break;
                first = false;
            }

            //Mhh, blijkbaar waren er gewoon niet goed candles
            if (candlesForHistory.Count < maxCandles)
            {
                errorstr = string.Format("{0} {1} Not enough candles available count={2} requested={3}", symbol.Name, interval.Name, candlesForHistory.Count, maxCandles);
                return null;
            }

            // Fill in missing candles (repeating the last candle.close) up to nextCandleOpenTime
            // We assume nothing has happened in that period (flat candles with no orders)
            CandleTools.AddMissingSticks(candlesForHistory, nextCandleOpenTime, interval);
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }

        // Convert the list to a input kind the stupid indicators are using
        errorstr = "";
        List<CryptoCandle> history = candlesForHistory.Values.ToList();
        return history;
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
    public static void CalculateIndicators(List<CryptoCandle> history, bool always = false)
    {
        // Overslaan indien het gevuld is (meerdere aanroepen)
        CryptoCandle candle = history.Last();
        if (!always && candle.CandleData != null)
            return;

        //List<TemaResult> temaList = (List<TemaResult>)Indicator.GetTema(history, 5);

        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(5);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(5);

        //List<SmaResult> smaList8 = (List<SmaResult>)Indicator.GetSma(history, 8);
        List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        //List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);
        List<SlopeResult> slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(5);
        List<SlopeResult> slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(5);

        //List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(history, 20, 1);

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(history);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi();
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();

        List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSma(10).GetSlope(3);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        List<StochResult> stochList = (List<StochResult>)history.GetStoch(14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...

        //List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(candleList, 20, 1);

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
        int outNbElement; // aantal elementen in de array vanaf 0
        TicTacTec.TA.Library.Core.RetCode retCode;

        double[] psarValues = new double[history.Count];
        retCode = TicTacTec.TA.Library.Core.Sar(startIdx, endIdx, inHigh, inLow, 0.02, 0.2,
            out int outBegIdxPSar, out outNbElement, psarValues);

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
                //candleData.Keltner = keltnerList[index];
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
    private static long InitialCandleCountFetch = 24 * 60 * 60 + 9 * 60 * 60;

    public static void SetInitialCandleCountFetch(long value)
    {
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
                // de 0 was eerst een 10 en bedoeld om meldingen met terugwerkende kracht te berekenen bij de start
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - (0 + maxCandles) * interval.Duration;
            startFetchUnix -= startFetchUnix % interval.Duration;

            // Lets extend that with 1 extra candle just in case...
            startFetchUnix -= interval.Duration;
        }
        //DateTime symbolfetchCandleDebug = CandleTools.GetUnixDate(startFetchUnix);  //debug
        return startFetchUnix;

    }

}
