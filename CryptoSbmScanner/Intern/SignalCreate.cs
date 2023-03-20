using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

namespace CryptoSbmScanner.Intern;


public delegate void AnalyseAlgoritmAlarmEvent(CryptoSignal alarm);


public class SignalCreate
{
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private AnalyseAlgoritmAlarmEvent AnalyseAlgoritmAlarmEvent { get; set; }

    private List<CryptoCandle> history = null;

    public SignalCreate(CryptoSymbol symbol, CryptoInterval interval, AnalyseAlgoritmAlarmEvent analyseAlgoritmAlarmEvent)
    {
        Symbol = symbol;
        Interval = interval;
        AnalyseAlgoritmAlarmEvent = analyseAlgoritmAlarmEvent;
    }


    public static void CalculateAdditionalSignalProperties(CryptoSignal signal, List<CryptoCandle> history, int candleCount, long unixFrom = 0)
    {
        // dit zou ook bij het verzamelen van de history lijst kunnen (scheelt een iteratie)
        int candlesWithFlatPrice = 0;
        int candlesWithZeroVolume = 0;
        int aboveBollingerBandsSma = 0;
        int aboveBollingerBandsUpper = 0;


        int iterations = 0;
        CryptoCandle prevCandle, CandleLast = null;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            prevCandle = CandleLast;
            CandleLast = history[i];

            // voor de backtest, pas tellen vanaf het moment dat het nodig is (verre van ideeal!!!)
            if (unixFrom > 0 && CandleLast.OpenTime > unixFrom)
                continue;

            if (CandleLast.Close == CandleLast.Open && CandleLast.Close == CandleLast.High && CandleLast.Close == CandleLast.Low)
                candlesWithFlatPrice++;
            if (CandleLast.Volume <= 0)
                candlesWithZeroVolume++;

            // Hievoor moet dus wel de laatste x candlesdata gevuld zijn (dat is niet het geval!!!!)
            if (CandleLast.CandleData != null && CandleLast.CandleData.BollingerBands != null && CandleLast.CandleData.BollingerBands.Width.HasValue)
            {
                // Hoe vaak komt de prijs boven de BB (de middelste en de bovenste)
                if (prevCandle != null && prevCandle.CandleData != null && prevCandle.CandleData.BollingerBands != null && prevCandle.CandleData.BollingerBands.Width.HasValue)
                {
                    decimal prevMax = Math.Max(prevCandle.Open, prevCandle.Close);
                    decimal lastMax = Math.Max(CandleLast.Open, CandleLast.Close);

                    // Minpuntje voor beide: als we direct boven de sma of upper zitten dan wordt dat niet geregistreerd

                    // Registreer de wisseling van onder naar boven de sma/upper
                    if (lastMax >= (decimal)CandleLast.CandleData.BollingerBands.Sma && prevMax < (decimal)prevCandle.CandleData.BollingerBands.Sma)
                        aboveBollingerBandsSma++;
                    if (lastMax >= (decimal)CandleLast.CandleData.BollingerBands.UpperBand && prevMax < (decimal)prevCandle.CandleData.BollingerBands.UpperBand)
                        aboveBollingerBandsUpper++;
                }
            }
            else // een belachelijke value zodat het eruit valt
            {
                GlobalData.AddTextToLogTab(string.Format("{0} {1} {2:N8} heeft geen candledata of geen BB?", CandleLast.Symbol.Name, CandleLast.DateLocal.ToString(), CandleLast.Close));
            }

            iterations++;
            if (iterations >= candleCount)
                break;
        }
        signal.CandlesWithFlatPrice = candlesWithFlatPrice;
        signal.CandlesWithZeroVolume = candlesWithZeroVolume;
        signal.AboveBollingerBandsSma = aboveBollingerBandsSma;
        signal.AboveBollingerBandsUpper = aboveBollingerBandsUpper;
    }


    private decimal CalculateLastPeriodsInInterval(long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(dit om de berekeningen allemaal wat sneller te maken)

        CryptoCandle candle = Symbol.CandleList.Values.Last();
        long openTime = CandleTools.GetUnixTime(candle.Date, 60);
        if (Symbol.CandleList.TryGetValue(openTime - interval, out CryptoCandle candlePrev))
        {
            decimal closeLast = candle.Close;
            decimal closePrev = candlePrev.Close;
            decimal diff = closeLast - closePrev;

            if (!closePrev.Equals(0))
                return 100.0m * (diff / closePrev);
            else return 0;

        }
        else return 0;
    }



    private static void AnalyseNotificationClearOutOld()
    {
        Monitor.Enter(GlobalData.AnalyseNotification);
        try
        {
            //De notificaties kleiner maken
            long someTimeAgo = CandleTools.GetUnixTime(DateTime.UtcNow.AddHours(-2), 60);
            for (int i = GlobalData.AnalyseNotification.Count - 1; i >= 0; i--)
            {
                KeyValuePair<string, long> item2 = GlobalData.AnalyseNotification.ElementAt(i);
                if (item2.Value < someTimeAgo)
                    GlobalData.AnalyseNotification.Remove(item2.Key);
            }
        }
        finally
        {
            Monitor.Exit(GlobalData.AnalyseNotification);
        }
    }


    void CalculateTrendStuff(CryptoSignal signal)
    {
        //int iterator = 0;
        long percentageSum = 0;
        long maxPercentageSum = 0;
        for (CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1m; intervalPeriod <= CryptoIntervalPeriod.interval1d; intervalPeriod++)
        {
            //iterator++;
            if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                return;

            // Nu gebaseerd op de SMA's
            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);
            CryptoTrendIndicator trendIndicator;



            //SortedList<long, CryptoCandle> intervalCandles = symbolInterval.CandleList;
            //if (!intervalCandles.Any())
            //{
            //    GlobalData.AddTextToLogTab("No candles available");
            //    continue;
            //}

            //string text;
            //CryptoCandle candle = intervalCandles.Values.Last();
            //if (candle.CandleData == null)
            //{
            //    List<CryptoCandle> history = CandleIndicatorData.CalculateCandles(Symbol, interval, candle.OpenTime, out text);
            //    if (history == null)
            //    {
            //        //GlobalData.AddTextToLogTab("No indicators available " + intervalCandles.Count.ToString());
            //        continue;
            //    }

            //    // Eenmalig de indicators klaarzetten
            //    candle = history[history.Count - 1];
            //    if (candle.CandleData == null)
            //        CandleIndicatorData.CalculateIndicators(history);
            //}

            //if (candle.CandleData.Sma100.Sma.Value > candle.CandleData.Sma200.Sma.Value)
            //    trendIndicator = CryptoTrendIndicator.trendBullish;
            //else if (candle.CandleData.Sma100.Sma.Value < candle.CandleData.Sma200.Sma.Value)
            //    trendIndicator = CryptoTrendIndicator.trendBearish;
            //else
            //    trendIndicator = CryptoTrendIndicator.trendSideways;




            TrendIndicator trendIndicatorClass = new(Symbol, interval);
            trendIndicator = trendIndicatorClass.CalculateTrend();

            if (trendIndicator == CryptoTrendIndicator.trendBullish)
                percentageSum += interval.Duration;
            else if (trendIndicator == CryptoTrendIndicator.trendBearish)
                percentageSum -= interval.Duration;

            if (intervalPeriod == signal.Interval.IntervalPeriod)
                signal.TrendIndicator = trendIndicator;

            // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
            symbolInterval.TrendIndicator = trendIndicator;
            symbolInterval.TrendInfoDate = DateTime.UtcNow;

            maxPercentageSum += interval.Duration;
        }


        float trendPercentage = 100 * (float)percentageSum / maxPercentageSum;
        signal.TrendPercentage = trendPercentage;
        Symbol.TrendPercentage = trendPercentage;

    }


    void SendSignal(CryptoSignal signal, string eventText)
    {
        AnalyseNotificationClearOutOld();


        //Hebben we deze al eerder gemeld?
        string notification = signal.Symbol.Name + "-" + signal.Interval.Name + "-" + signal.Strategy.ToString() + "-" + signal.Candle.Date.ToLocalTime();
        if (!signal.BackTest)
        {
            if (GlobalData.AnalyseNotification.ContainsKey(notification))
                return;

            Monitor.Enter(GlobalData.AnalyseNotification);
            try
            {
                long openTime = CandleTools.GetUnixTime(signal.Candle.Date, 60);
                GlobalData.AnalyseNotification.Add(notification, openTime);
            }
            finally
            {
                Monitor.Exit(GlobalData.AnalyseNotification);
            }
        }



        // de 24 change moet in dit interval zitten
        // Vraag: Kan deze niet beter naar het begin van de controles?
        signal.Last24Hours = CalculateLastPeriodsInInterval(24 * 60 * 60);
        if (!signal.Last24Hours.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
            {
                string text = string.Format("Analyse {0} 24 hour changed {1} niet tussen de {2} .. {3}", Symbol.Name, signal.Last24Hours.ToString0("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString0(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString0());
                GlobalData.AddTextToLogTab(text);
            }
            return;
        }


        CalculateTrendStuff(signal);
        signal.EventText = eventText;

        signal.EventText = eventText.Trim();
        AnalyseAlgoritmAlarmEvent(signal);
    }


    private bool CheckAdditionalAlarmProperties(CryptoSignal signal)
    {
        // --------------------------------------------------------------------------------
        // Van de laatste 60 candles mogen er maximaal 16 geen volume hebben.
        // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.Signal.CandlesWithZeroVolume > 0 && signal.CandlesWithZeroVolume > GlobalData.Settings.Signal.CandlesWithZeroVolume)
        {
            if (GlobalData.Settings.Signal.LogCandlesWithZeroVolume)
            {
                string text = string.Format("Analyse {0} {1} teveel candles zonder volume ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.CandlesWithZeroVolume);
                GlobalData.AddTextToLogTab(text);
            }
            return false;
        }

        // --------------------------------------------------------------------------------
        // Van de laatste 60 candles mogen er slechts 18 plat zijn
        // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && signal.CandlesWithFlatPrice > GlobalData.Settings.Signal.CandlesWithFlatPrice)
        {
            if (GlobalData.Settings.Signal.LogCandlesWithFlatPrice)
            {
                string text = string.Format("Analyse {0} {1} teveel platte candles ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.CandlesWithFlatPrice);
                GlobalData.AddTextToLogTab(text);
            }
            return false;
        }


        // Er moet een beetje beweging in de BB zitten (niet enkel op de onderste bb ofzo)
        if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
        {
            if (GlobalData.Settings.Signal.LogAboveBollingerBandsSma)
            {
                string text = string.Format("Analyse {0} {1} te weinig candles die boven de BB.Sma uitsteken ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.AboveBollingerBandsSma);
                GlobalData.AddTextToLogTab(text);
            }
            return false;
        }


        // Vervolg op voorgaande wens op beweging in de BB (met het liefst een aantal uitschieters)
        if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
        {
            if (GlobalData.Settings.Signal.LogAboveBollingerBandsUpper)
            {
                string text = string.Format("Analyse {0} {1} te weinig candles die boven de BB.Upper uitsteken ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.AboveBollingerBandsUpper);
                GlobalData.AddTextToLogTab(text);
            }
            return false;
        }


        return true;
    }



    ///// <summary>
    ///// Dit is gebaseerd op de "RSI Multi Length [LuxAlgo]"
    ///// We gebruiken de oversell of overbuy indicator als extra tekst in de melding
    ///// </summary>
    ///// <param name="overSell">Retourneer de oversell of de overbuy tellertje</param>
    ///// <returns></returns>
    //private int GetFluxIndcator(bool overSell)
    //{
    //    SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).CandleList;

    //    // Dat array van 10 (nu globaal)
    //    decimal[] num = new decimal[10];
    //    decimal[] den = new decimal[10];
    //    for (int j = 0; j < 10; j++)
    //    {
    //        num[j] = 0m;
    //        den[j] = 0m;
    //    }

    //    // Gefixeerde getallen
    //    int min = 10;
    //    int max = 20;
    //    int oversold = 30;
    //    int overbought = 70;
    //    decimal N = max - min + 1;

    //    int overbuy = 0; // gebruiken we dit keer niet, maar laat maar staan
    //    int oversell = 0;
    //    CryptoCandle candlePrev;
    //    CryptoCandle candleLast = null;

    //    for (int j = candles.Count - 30; j < candles.Count; j++)
    //    {
    //        if (j < 1)
    //            continue;
    //        candlePrev = candleLast;
    //        candleLast = candles.Values[j];
    //        if (candlePrev == null)
    //            continue;

    //        int k = 0;
    //        decimal avg = 0m;
    //        overbuy = 0;
    //        oversell = 0;
    //        decimal diff = candleLast.Close - candlePrev.Close;

    //        for (int i = min; i < max; i++)
    //        {
    //            decimal alpha = 1 / (decimal)i;

    //            decimal num_rma = alpha * diff + (1m - alpha) * num[k];
    //            decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

    //            decimal rsi;
    //            if (den_rma == 0)
    //                rsi = 50m;
    //            else
    //                rsi = 50m * num_rma / den_rma + 50m;

    //            avg += rsi;

    //            if (rsi > overbought)
    //                overbuy++;
    //            if (rsi < oversold)
    //                oversell++;


    //            num[k] = num_rma;
    //            den[k] = den_rma;
    //            k++;

    //        }
    //    }

    //    if (overSell)
    //        return 10 * oversell;
    //    else
    //        return 10 * overbuy;
    //}


    static public List<CryptoCandle> CalculateHistory(SortedList<long, CryptoCandle> candleSticks, int maxCandles)
    {
        //Transporteer de candles naar de Stock list
        //Jammer dat we met tussen-array's moeten werken
        List<CryptoCandle> history = new();
        Monitor.Enter(candleSticks);
        try
        {
            //Vanwege performance nemen we een gedeelte van de candles
            for (int i = candleSticks.Values.Count - 1; i >= 0; i--)
            {
                CryptoCandle candle = candleSticks.Values[i];

                //In omgekeerde volgorde in de lijst zetten
                if (history.Count == 0)
                    history.Add(candle);
                else
                    history.Insert(0, candle);

                maxCandles--;
                if (maxCandles == 0)
                    break;
            }
        }
        finally
        {
            Monitor.Exit(candleSticks);
        }
        return history;
    }


    //public void DumpSymbol()
    //{
    //    //Ter debug van een hadnekig probleem met het tonen van de alarm
    //    var csv = new StringBuilder();
    //    var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", "OpenTime", "IntervalId", "Open", "High", "Low", "Close", "Volume");
    //    csv.AppendLine(newLine);

    //    Monitor.Enter(history);
    //    try
    //    {
    //        for (int i = 0; i < history.Count; i++)
    //        {
    //            CryptoCandle candle = history[i];

    //            newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}",
    //            candle.Date.ToString(),
    //            candle.IntervalId.ToString(),
    //            candle.Open.ToString(),
    //            candle.High.ToString(),
    //            candle.Low.ToString(),
    //            candle.Close.ToString(),
    //            candle.Volume.ToString());

    //            csv.AppendLine(newLine);
    //        }
    //    }
    //    finally
    //    {
    //        Monitor.Exit(history);
    //    }
    //    string filename = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location));
    //    filename = filename + @"\data\" + Symbol.Exchange.Name + @"\Candles\" + Interval.Name + @"\";
    //    System.IO.Directory.CreateDirectory(filename);
    //    System.IO.File.WriteAllText(filename + Symbol.Name + "-" + Interval.Name + ".csv", csv.ToString());
    //}


    private bool PrepareAndSendSignal(SignalBase algorithm, CryptoCandle candle, bool backTest)
    {
        // Dit signaal mag niet meerdere keren gerapporteerd worden.
        // Anders dubbele signalen met verschillende type algoritmes!
        // Die isSbmSignal is dus echt noodzakelijk
        CryptoSignal signal = CreateSignal(candle, backTest);
        signal.Mode = algorithm.SignalMode;
        signal.Strategy = algorithm.SignalStrategy;

        // Extra attributen erbij halen
        CalculateAdditionalSignalProperties(signal, history, 60);
        if (!CheckAdditionalAlarmProperties(signal))
            return false;

        string eventText = algorithm.ExtraText;

        //if (GlobalData.ShowExtraStuff)
        //{
        //    int oversell = GetFluxIndcator(true);
        //    if (oversell > 0)
        //        eventText += " (" + oversell.ToString() + ")";
        //}

        SendSignal(signal, eventText);

        return true;
    }


    private CryptoSignal CreateSignal(CryptoCandle candle, bool backTest)
    {
        CryptoSignal signal = new()
        {
            Exchange = Symbol.Exchange,
            Symbol = Symbol,
            Interval = Interval,
            Candle = candle,
            BackTest = backTest,
            Price = candle.Close,
            Volume = Symbol.Volume,
            EventTime = candle.OpenTime,
            OpenDate = CandleTools.GetUnixDate(candle.OpenTime)
        };
        signal.CloseDate = signal.OpenDate.AddSeconds(Interval.Duration);
        //signal.ExpirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.Signal.RemoveSignalAfterxCandles * Interval.Duration); // 15 candles further (display)
        signal.Strategy = SignalStrategy.strategyCandlesJumpUp;  // gets modified later
        signal.Mode = SignalMode.modeInfo; // gets modified later

        // Waarden van de indicators invulen
        signal.BollingerBandsLowerBand = (float)candle.CandleData.BollingerBands.LowerBand;
        signal.Sma20 = (float)candle.CandleData.BollingerBands.Sma;
        signal.BollingerBandsUpperBand = (float)candle.CandleData.BollingerBands.UpperBand;
        signal.BollingerBandsPercentage = (float)candle.CandleData.BollingerBandsPercentage; // Dit is degene die Marco gebruikt

        signal.Rsi = (float)candle.CandleData.Rsi.Rsi.Value;
        signal.PSar = (float)candle.CandleData.PSar;
        signal.StochSignal = (float)candle.CandleData.Stoch.Signal.Value;
        signal.StochOscillator = (float)candle.CandleData.Stoch.Oscillator.Value;
#if DEBUG
        signal.PSarDave = (float)candle.CandleData.PSarDave;
        signal.PSarJason = (float)candle.CandleData.PSarJason;
        signal.PSarTulip = (float)candle.CandleData.PSarTulip;
        //signal.Slope200 = (float)candle.CandleData.Slope200;
        //signal.Slope50 = (float)candle.CandleData.Slope50;
        //signal.Slope20 = (float)candle.CandleData.Slope20;
#endif

        signal.Sma200 = (float)candle.CandleData.Sma200.Sma.Value;
        signal.Sma50 = (float)candle.CandleData.Sma50.Sma.Value;

        return signal;
    }


    private void AnalyseSymbolOversold(CryptoCandle candle, bool backTest)
    {
        // Geen nieuwe signalen ivm blacklist
        // Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        // Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (!backTest && GlobalData.Settings.UseBlackListOversold && GlobalData.SymbolBlackListOversold.ContainsKey(Symbol.Name))
            return;

        // Geen nieuwe signalen ivm whitelist
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (!backTest && GlobalData.Settings.UseWhiteListOversold && !GlobalData.SymbolWhiteListOversold.ContainsKey(Symbol.Name))
            return;


        // Oversold (SBM is an advanced version of STOBB)
        bool isSbmSignal = false;

        if (GlobalData.Settings.Signal.AnalysisShowSbmOversold)
        {
            // De officiele SBM methode van Maurice
            SignalBase algorithm = new SignalSbm1Oversold(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm2Oversold)
        {
            // De SBM methode - Marco - (min(Candle.Open/Close) > 99.50%
            SignalBase algorithm = new SignalSbm2Oversold(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm3Oversold)
        {
            // De SBM methode - BB.Width is > xx% veranderd
            SignalBase algorithm = new SignalSbm3Oversold(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm, candle, backTest);
        }

        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisShowStobbOversold)
        {
            // STOBB (is eigenlijk een "zwakke" variant van SBM)
            SignalBase algorithm = new SignalStobbOversold(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
    }


    private void AnalyseSymbolOverbought(CryptoCandle candle, bool backTest)
    {
        //----------------------------------------------------------------
        // Overbought (SBM is an advanced version of STOBB)
        // TODO: De overbought varianten implementeren sbm2 & 3!
        // Geen nieuwe signalen ivm blacklist
        // Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        // Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (!backTest && GlobalData.Settings.UseBlackListOverbought && GlobalData.SymbolBlackListOversold.ContainsKey(Symbol.Name))
            return;

        // Geen nieuwe signalen ivm whitelist
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (!backTest && GlobalData.Settings.UseWhiteListOverbought && !GlobalData.SymbolWhiteListOversold.ContainsKey(Symbol.Name))
            return;

        bool isSbmSignal = false;
        if (GlobalData.Settings.Signal.AnalysisShowSbmOverbought)
        {
            SignalBase algorithm = new SignalSbm1Overbought(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm2Overbought)
        {
            SignalBase algorithm = new SignalSbm2Overbought(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm3Overbought)
        {
            SignalBase algorithm = new SignalSbm3Overbought(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisShowStobbOverbought)
        {
            SignalBase algorithm = new SignalStobbOverbought(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
    }


    public void AnalyseSymbolJumps(CryptoCandle candle, bool backTest)
    {
        // Hmm, hier is geen Black en white list meer beschikbaar (nu enkel voor oversold en overbought)
        // Nu is er weer behoefte aan een soort van globale black en white list (bah, lijkt op Zignally)

        if (GlobalData.Settings.Signal.AnalysisShowCandleJumpUp)
        {
            SignalBase algorithm = new SignalCandleJumpUp(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
        if (GlobalData.Settings.Signal.AnalysisShowCandleJumpDown)
        {
            SignalBase algorithm = new SignalCandleJumpDown(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }
    }

    public void AnalyseSymbolExperimental(CryptoCandle candle, bool backTest)
    {
        //----------------------------------------------------------------
        // Experimentele zaken..
        if (GlobalData.Settings.Signal.AnalysisPriceCrossingMa && GlobalData.ShowExtraStuff)
        {
            SignalBase algorithm = new SignalPriceCrossedMa20(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);

            algorithm = new SignalPriceCrossedMa50(Symbol, Interval, candle);
            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm, candle, backTest);
        }


        //// 3Green
        //if (GlobalData.Settings.Signal.AnalysisShow3GreenMinus)
        //{
        //    SignalBase algorithm = new Signal3Green(Symbol, Interval, candle);
        //    algorithm.CandleLast = candle;
        //    if (algorithm.IsSignal())
        //        PrepareAndSendSignal(signal, SignalStrategy.strategy3GreenOversold, SignalMode.modeLong);
        //}
        ////if (GlobalData.Settings.Signal.AnalysisShow3GreenPlus)
        ////    BinanceAnalyseSymbol3GreenOverbought();
    }


    public void AnalyseSymbol(long candleOpenTime, bool backTest)
    {
        // We kunnen nog openstaande posities hebben en die vertrouwen erop dat de lastCandle.CandleData wordt berekend
        // Anders wordt er NIET bijgekocht met DCA en dergelijke (dat zit eigenlijk niet zo goed in elkaar, workaround!)
        string text = "";

        if (!Symbol.LastPrice.HasValue)
        {
            //GlobalData.AddTextToLogTab(string.Format("{0} Er is geen lastprice aanwezig", symbol.Name));
            return;
        }

        // New coins have a lot of price changes
        // Are there x day's of candles avaliable on the day interval
        CryptoSymbolInterval symbolPeriod = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d);
        SortedList<long, CryptoCandle> candles1Day = symbolPeriod.CandleList;
        if (candles1Day.Count < GlobalData.Settings.Signal.SymbolMustExistsDays)
        {
            if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
                GlobalData.AddTextToLogTab(string.Format("{0} The coin is to new, no {1} day's candles available", Symbol.Name, GlobalData.Settings.Signal.SymbolMustExistsDays));
            return;
        }

        // Is het volume binnen de gestelde grenzen          
        if (!backTest && !Symbol.CheckValidMinimalVolume(out text))
        {
            //if (GlobalData.Settings.Signal.LogMinimalVolume)
            //    GlobalData.AddTextToLogTab("Analyse " + text);
            return;
        }

        // Is de prijs binnen de gestelde grenzen
        if (!backTest && !Symbol.CheckValidMinimalPrice(out text))
        {
            //if (GlobalData.Settings.Signal.LogMinimalPrice)
            //    GlobalData.AddTextToLogTab("Analyse " + text);
            return;
        }

        // Laat maar als het handelsklimaat cq barometer niet zo goed is.
        decimal? Barometer1h = -99m;
        if (GlobalData.Settings.QuoteCoins.TryGetValue(Symbol.Quote, out CryptoQuoteData quoteData))
        {
            BarometerData barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
            Barometer1h = barometerData.PriceBarometer;
        }
        if (Barometer1h <= GlobalData.Settings.Signal.Barometer1hMinimal)
        {
            if (GlobalData.Settings.Signal.LogBarometerToLow)
            {
                // Log iets, maar dat wordt wel veel
                GlobalData.AddTextToLogTab("Barometer te laag");
            }
            return;
        }

        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
        //if (Symbol.LastPrice.HasValue) al gecontroleerd
        //{
        //    Symbol.BarcodePercentage = 100 * (Symbol.PriceTickSize) / (decimal)Symbol.LastPrice.Value;

        // Dit gebruikt ik wel in een bot om uit de barcode charts weg te blijven, laat ik nog even staan
        //    Signal.BarcodePercentage = (decimal)Symbol.BarcodePercentage;
        //    if (!backTest)
        //    {
        //        if ((!backTest))
        //        {
        //            if ((Symbol.BarcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage))
        //            {
        //                if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
        //                {
        //                    // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
        //                    GlobalData.AddTextToLogTab(string.Format("{0} De tick size percentage is te hoog {1:N3}", Symbol.Name, Symbol.BarcodePercentage));
        //                }
        //                return;
        //            }
        //        }
        //    }
        //}




        // Build a list of candles
        // The amount of candles in it depends on the used indicators (ma200)
        history ??= CandleIndicatorData.CalculateCandles(Symbol, Interval, candleOpenTime, out text);
        if (history == null)
        {
#if DEBUG
            //if (GlobalData.Settings.Signal.LogNotEnoughCandles)
            GlobalData.AddTextToLogTab("Analyse " + text);
#endif
            return;
        }


        // Eenmalig de indicators klaarzetten
        CryptoCandle candle = history[^1];
        if (candle.CandleData == null)
            CandleIndicatorData.CalculateIndicators(history);

        //----------------------------------------------------------------

        // Oversold - STOBB en SBM (SBM is de advanced version van STOBB)
        AnalyseSymbolOversold(candle, backTest);

        // Overbought - STOBB + SBM (SBM is de advanced version van STOBB)
        AnalyseSymbolOverbought(candle, backTest);

        // Candle jumps
        AnalyseSymbolJumps(candle, backTest);


        // Experimenteel, kan wellicht weg
        AnalyseSymbolExperimental(candle, backTest);
    }
}
