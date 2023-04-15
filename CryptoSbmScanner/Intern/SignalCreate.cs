using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;
using Microsoft.Office.Interop.Excel;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XWPF.UserModel;
using Renci.SshNet;
using Skender.Stock.Indicators;
using ICell = NPOI.SS.UserModel.ICell;

namespace CryptoSbmScanner.Intern;


public delegate void AnalyseAlgoritmAlarmEvent(CryptoSignal signal);


public class SignalCreate
{
    private bool BackTest { get; set; }
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private AnalyseAlgoritmAlarmEvent AnalyseAlgoritmAlarmEvent { get; set; }

    private CryptoCandle Candle { get; set; }
    private List<CryptoCandle> history = null;

    public SignalCreate(CryptoSymbol symbol, CryptoInterval interval, bool backTest, AnalyseAlgoritmAlarmEvent analyseAlgoritmAlarmEvent)
    {
        Symbol = symbol;
        Interval = interval;
        BackTest = backTest;
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
            if (CandleLast.CandleData != null && CandleLast.CandleData.BollingerBandsLowerBand != null)
            {
                // Hoe vaak komt de prijs boven de BB (de middelste en de bovenste)
                if (prevCandle != null && prevCandle.CandleData != null && prevCandle.CandleData.BollingerBandsLowerBand != null)
                {
                    decimal prevMax = Math.Max(prevCandle.Open, prevCandle.Close);
                    decimal lastMax = Math.Max(CandleLast.Open, CandleLast.Close);

                    // Minpuntje voor beide: als we direct boven de sma of upper zitten dan wordt dat niet geregistreerd

                    // Registreer de wisseling van onder naar boven de sma/upper
                    if (lastMax >= (decimal)CandleLast.CandleData.Sma20 && prevMax < (decimal)prevCandle.CandleData.Sma20)
                        aboveBollingerBandsSma++;
                    if (lastMax >= (decimal)CandleLast.CandleData.BollingerBandsUpperBand && prevMax < (decimal)prevCandle.CandleData.BollingerBandsUpperBand)
                        aboveBollingerBandsUpper++;
                }
            }
            else // een belachelijke value zodat het eruit valt
            {
                GlobalData.AddTextToLogTab(string.Format("Analyse {0} {1} {2:N8} heeft geen candledata of geen BB?", CandleLast.Symbol.Name, CandleLast.DateLocal.ToString(), CandleLast.Close));
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
            TrendIndicator trendIndicatorClass = new(Symbol, interval);

            CryptoTrendIndicator trendIndicator;
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


        float trendPercentage = 100 * (float)percentageSum / (float)maxPercentageSum;
        signal.TrendPercentage = trendPercentage;
        Symbol.TrendPercentage = trendPercentage;

    }


    private double CalculateLastPeriodsInInterval(long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(dit om de berekeningen allemaal wat sneller te maken)

        CryptoCandle candle = Symbol.CandleList.Values.Last();
        long openTime = CandleTools.GetUnixTime(candle.Date, 60);
        if (!Symbol.CandleList.TryGetValue(openTime - interval, out CryptoCandle candlePrev))
            candlePrev = Symbol.CandleList.Values.First(); // niet helemaal okay maar beter dan 0

        double closeLast = (double)candle.Close;
        double closePrev = (double)candlePrev.Close;
        double diff = closeLast - closePrev;

        if (!closePrev.Equals(0))
            return 100.0 * (diff / closePrev);
        else return 0;
    }


    private double CalculateMaxMovementInInterval(CryptoIntervalPeriod intervalPeriod, long candleCount)
    {
        // Op een iets hoger interval gaan we x candles naar achteren en meten de echte beweging
        // (de 24% change is wat effectief overblijft, maar dat is duidelijk niet de echte beweging)
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(intervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;


        double min = double.MaxValue;
        double max = double.MinValue;

        CryptoCandle candle = candles.Values.Last();
        while (candleCount-- > 0)
        {
            if ((double)candle.Low < min)
                min = (double)candle.Low;

            if ((double)candle.High > max)
                max = (double)candle.High;

            if (!candles.TryGetValue(candle.OpenTime - symbolInterval.Interval.Duration, out candle))
                break; // we hebben maximaal 260 candles, minder dan de gewenste 2 dagen
        }

        double diff = max - min;
        if (!min.Equals(0))
            return 100.0 * (diff / min);
        else
            return 0;
    }


    private async void SendSignal(SignalBase algorithm, CryptoSignal signal, string eventText)
    {
        // Hebben we deze al eerder gemeld?
        if (!signal.BackTest)
        {
            AnalyseNotificationClearOutOld();

            string notification = signal.Symbol.Name + "-" + signal.Interval.Name + "-" + signal.Strategy.ToString() + "-" + signal.Candle.Date.ToLocalTime();
            //GlobalData.AddTextToLogTab("debug "+ notification);

            Monitor.Enter(GlobalData.AnalyseNotification);
            try
            {
                if (GlobalData.AnalyseNotification.ContainsKey(notification))
                    return;

                long openTime = CandleTools.GetUnixTime(signal.Candle.Date, 60);
                GlobalData.AnalyseNotification.Add(notification, openTime);
            }
            finally
            {
                Monitor.Exit(GlobalData.AnalyseNotification);
            }
        }


        CalculateTrendStuff(signal);
        signal.EventText = eventText.Trim();

        if (!signal.BackTest)
        {
            try
            {
                // We gebruiken (nog) geen exit signalen, echter dat is best realistisch voor de toekomst
                if (signal.Mode == SignalMode.modeLong) //|| (Alarm.Mode == SignalMode.modeSell) 
                {
                    if (GlobalData.Settings.Bot.TradeOnInterval[(int)signal.Interval.IntervalPeriod])
                    {
                        if (GlobalData.Settings.Bot.TradeOnStrategy[(int)signal.Strategy])
                        {
                            await signal.Symbol.PositionListSemaphore.WaitAsync();
                            try
                            {
                                // Bied het aan het monitorings systeem (indien aangevinkt)
                                // Intern willen we een nieuwer SBM signaal niet direct vervangen
                                // (anders krijgen we continue nieuwe signalen en is de instap weg)
                                // Bij STOBB wil je echt alleen het laatste signaal gebruiken..

                                CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
                                if (symbolInterval.Signal == null || symbolInterval.Signal?.EventTime != signal.EventTime)
                                {
                                    if (symbolInterval.Signal == null || algorithm.ReplaceSignal)
                                        symbolInterval.Signal = signal;
                                }
                            }
                            finally
                            {
                                signal.Symbol.PositionListSemaphore.Release();
                            }
                        }
                    }
                }


                //if (!signal.BackTest)
                //{
                //    // Altijd het event wegschrijven (dat kan nu inderdaad een duplicaat zijn)
                //    using (SqlConnection databaseThread = new SqlConnection(GlobalData.ConnectionString))
                //    {
                //        databaseThread.Close();
                //        databaseThread.Open();
                //        using (var transaction = databaseThread.BeginTransaction())
                //        {
                //            databaseThread.Insert<CryptoSignal>(signal, transaction);
                //            transaction.Commit();
                //        }
                //    }
                //}
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab(error.ToString(), true);
            }
        }

        AnalyseAlgoritmAlarmEvent(signal);
        return;
    }


    //private bool CheckAdditionalSignalProperties(CryptoSignal signal)
    //{
    //    // --------------------------------------------------------------------------------
    //    // Van de laatste 60 candles mogen er maximaal 16 geen volume hebben.
    //    // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
    //    // --------------------------------------------------------------------------------
    //    if ((GlobalData.Settings.Signal.CandlesWithZeroVolume > 0) && (signal.CandlesWithZeroVolume > GlobalData.Settings.Signal.CandlesWithZeroVolume))
    //    {
    //        if (GlobalData.Settings.Signal.LogCandlesWithZeroVolume)
    //        {
    //            string text = string.Format("Analyse {0} {1} teveel candles zonder volume ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.CandlesWithZeroVolume);
    //            GlobalData.AddTextToLogTab(text);
    //        }
    //        return false;
    //    }

    //    // --------------------------------------------------------------------------------
    //    // Van de laatste 60 candles mogen er slechts 18 plat zijn
    //    // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
    //    // --------------------------------------------------------------------------------
    //    if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && signal.CandlesWithFlatPrice > GlobalData.Settings.Signal.CandlesWithFlatPrice)
    //    {
    //        if (GlobalData.Settings.Signal.LogCandlesWithFlatPrice)
    //        {
    //            string text = string.Format("Analyse {0} {1} teveel platte candles ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.CandlesWithFlatPrice);
    //            GlobalData.AddTextToLogTab(text);
    //        }
    //        return false;
    //    }


    //    // Er moet een beetje beweging in de BB zitten (niet enkel op de onderste bb ofzo)
    //    if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
    //    {
    //        if (GlobalData.Settings.Signal.LogAboveBollingerBandsSma)
    //        {
    //            string text = string.Format("Analyse {0} {1} te weinig candles die boven de BB.Sma uitsteken ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.AboveBollingerBandsSma);
    //            GlobalData.AddTextToLogTab(text);
    //        }
    //        return false;
    //    }


    //    // Vervolg op voorgaande wens op beweging in de BB (met het liefst een aantal uitschieters)
    //    if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
    //    {
    //        if (GlobalData.Settings.Signal.LogAboveBollingerBandsUpper)
    //        {
    //            string text = string.Format("Analyse {0} {1} te weinig candles die boven de BB.Upper uitsteken ({2} van 60 candles)", Symbol.Name, Interval.Name, signal.AboveBollingerBandsUpper);
    //            GlobalData.AddTextToLogTab(text);
    //        }
    //        return false;
    //    }


    //    return true;
    //}


    /// <summary>
    /// Dit is gebaseerd op de "RSI Multi Length [LuxAlgo]"
    /// We gebruiken de oversell of overbuy indicator als extra tekst in de melding
    /// </summary>
    /// <param name="overSell">Retourneer de oversell of de overbuy tellertje</param>
    /// <returns></returns>
    private int GetFluxIndcator(bool overSell)
    {
        SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).CandleList;

        // Dat array van 10 (nu globaal)
        decimal[] num = new decimal[10];
        decimal[] den = new decimal[10];
        for (int j = 0; j < 10; j++)
        {
            num[j] = 0m;
            den[j] = 0m;
        }

        // Gefixeerde getallen
        int min = 10;
        int max = 20;
        int oversold = 30;
        int overbought = 70;
        //decimal N = max - min + 1;

        int overbuy = 0; // gebruiken we dit keer niet, maar laat maar staan
        int oversell = 0;
        CryptoCandle candlePrev;
        CryptoCandle candleLast = null;

        for (int j = candles.Count - 30; j < candles.Count; j++)
        {
            if (j < 1)
                continue;
            candlePrev = candleLast;
            candleLast = candles.Values[j];
            if (candlePrev == null)
                continue;

            int k = 0;
            decimal avg = 0m;
            overbuy = 0;
            oversell = 0;
            decimal diff = candleLast.Close - candlePrev.Close;

            for (int i = min; i < max; i++)
            {
                decimal alpha = 1 / (decimal)i;

                decimal num_rma = alpha * diff + (1m - alpha) * num[k];
                decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

                decimal rsi;
                if (den_rma == 0)
                    rsi = 50m;
                else
                    rsi = 50m * num_rma / den_rma + 50m;

                avg += rsi;

                if (rsi > overbought)
                    overbuy++;
                if (rsi < oversold)
                    oversell++;


                num[k] = num_rma;
                den[k] = den_rma;
                k++;

            }
        }

        if (overSell)
            return 10 * oversell;
        else
            return 10 * overbuy;
    }

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
    //    //Ter debug van een hadnekig probleem met het tonen van de signal
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


    public bool PrepareAndSendSignal(SignalBase algorithm)
    {
        // Dit signaal mag niet meerdere keren gerapporteerd worden.
        // Anders dubbele signalen met verschillende type algoritmes!
        // Die isSbmSignal is dus echt noodzakelijk
        CryptoSignal signal = CreateSignal(Candle);
        signal.Mode = algorithm.SignalMode;
        signal.Strategy = algorithm.SignalStrategy;
        signal.LastPrice = Symbol.LastPrice;


        // Momenteel niet zichtbaar in de user interface, daarom uitgezet!
        // Extra attributen erbij halen
        //CalculateAdditionalSignalProperties(signal, history, 60);
        //if (!CheckAdditionalSignalProperties(signal))
        //{
        //    eventText += " invalid attribs";
        //    signal.Mode = SignalMode.modeInfo2;
        //    
        //}

        string eventText = algorithm.ExtraText;


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien) - via de info flag
        if (!algorithm.AdditionalChecks(Candle, out string response))
        {
            eventText += " " + response;
            signal.Mode = SignalMode.modeInfo2;
        }


        // de 24 change moet in dit interval zitten
        // Vraag: Kan deze niet beter naar het begin van de controles?
        signal.Last24HoursChange = CalculateLastPeriodsInInterval(24 * 60 * 60);
        signal.Last24HoursEffective = CalculateMaxMovementInInterval(CryptoIntervalPeriod.interval15m, 1 * 96);

        // Question: We hebben slechts 260 candles, dus dit geeft niet exact het gewenste getal!
        //signal.Last48HoursChange = CalculateLastPeriodsInInterval(48 * 60 * 60);
        //signal.Last48HoursEffective = CalculateMaxMovementInInterval(CryptoIntervalPeriod.interval15m, 2 * 96);

        if (!signal.Last24HoursChange.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
            {
                string text = string.Format("Analyse {0} 24h change {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last24HoursChange.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText += " 24h verandering% te hoog";
            signal.Mode = SignalMode.modeInfo2;
        }

        //if (!signal.Last24HoursEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        //{
        //    if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
        //    {
        //        string text = string.Format("Analyse {0} 24h change (effectief) {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last24HoursEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString());
        //        GlobalData.AddTextToLogTab(text);
        //    }
        //    eventText += " 24h effectief% te hoog";
        //    signal.Mode = SignalMode.modeInfo2;
        //}


        // New coins have a lot of price changes
        // Are there x day's of candles avaliable on the day interval
        var candles1Day = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).CandleList;
        if (candles1Day.Count < GlobalData.Settings.Signal.SymbolMustExistsDays)
        {
            if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
            {
                // TODO: dan het aantal dagen vermelden dat het bestaat
                string text = "";
                if (candles1Day.Count > 0)
                {
                    CryptoCandle first = candles1Day.Values.First();
                    text = CandleTools.GetUnixDate(first.OpenTime).Day.ToString();
                }
                GlobalData.AddTextToLogTab(string.Format("Analyse {0} te nieuw geen {1} dagen {2}", Symbol.Name, GlobalData.Settings.Signal.SymbolMustExistsDays, text));
            }
            eventText += " coin te nieuw";
            signal.Mode = SignalMode.modeInfo2;
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
            // Log iets, maar dat wordt wel veel
            if (GlobalData.Settings.Signal.LogBarometerToLow)
                GlobalData.AddTextToLogTab("Analyse Barometer te laag");
            eventText += " Barometer te laag";
            signal.Mode = SignalMode.modeInfo2;
        }


        if (!BackTest)
        {
            decimal barcodePercentage = 100 * (Symbol.PriceTickSize) / (decimal)Symbol.LastPrice.Value;
            if ((barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage))
            {
                // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
                if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
                    GlobalData.AddTextToLogTab(string.Format("Analyse {0} De tick size percentage is te hoog {1:N3}", Symbol.Name, barcodePercentage));
                eventText += " tick perc to high";
                signal.Mode = SignalMode.modeInfo2;
            }
        }


        // Zoveel voegt dit ook weer niet toe
        if (GlobalData.Settings.General.ShowFluxIndicator5m)
        {
            int oversell = GetFluxIndcator(true);
            //if (oversell > 0)
            signal.FluxIndicator5m = oversell;
            //eventText += " flux 5m=" + oversell.ToString();
        }

        // Voor de SignalSlopesTurning* een variabele zetten of 
        // clearen zodat dit signaal niet om de x candles binnenkomt.
        CryptoSymbolInterval SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        switch (signal.Strategy)
        {
            case SignalStrategy.stobbOversold:
                if (signal.Mode == SignalMode.modeLong)
                    SymbolInterval.LastStobbOrdSbmDate = DateTime.UtcNow;
                break;
            case SignalStrategy.stobbOverbought:
                if (signal.Mode == SignalMode.modeShort)
                    SymbolInterval.LastStobbOrdSbmDate = DateTime.UtcNow;
                break;

            case SignalStrategy.sbm1Oversold:
            case SignalStrategy.sbm2Oversold:
            case SignalStrategy.sbm3Oversold:
            case SignalStrategy.sbm4Oversold:
                if (signal.Mode == SignalMode.modeLong)
                    SymbolInterval.LastStobbOrdSbmDate = DateTime.UtcNow;
                break;

            case SignalStrategy.sbm1Overbought:
            case SignalStrategy.sbm2Overbought:
            case SignalStrategy.sbm3Overbought:
            case SignalStrategy.sbm4Overbought:
                if (signal.Mode == SignalMode.modeShort)
                    SymbolInterval.LastStobbOrdSbmDate = DateTime.UtcNow;
                break;

            case SignalStrategy.slopeSma50TurningNegative:
            case SignalStrategy.slopeSma50TurningPositive:
                SymbolInterval.LastStobbOrdSbmDate = null;
                break;
        }


        if ((!GlobalData.Settings.Signal.ShowInvalidSignals) && (signal.Mode == SignalMode.modeInfo2))
            return false;

        SendSignal(algorithm, signal, eventText);
        return true;
    }


    private CryptoSignal CreateSignal(CryptoCandle candle)
    {
        CryptoSignal signal = new()
        {
            Exchange = Symbol.Exchange,
            Symbol = Symbol,
            Interval = Interval,
            Candle = candle,
            BackTest = BackTest,
            Price = candle.Close,
            Volume = Symbol.Volume,
            EventTime = candle.OpenTime,
            OpenDate = CandleTools.GetUnixDate(candle.OpenTime),
            Strategy = SignalStrategy.candlesJumpUp,  // gets modified later
            Mode = SignalMode.modeInfo // gets modified later
        };

        signal.CloseDate = signal.OpenDate.AddSeconds(Interval.Duration);
        //signal.ExpirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.Signal.RemoveSignalAfterxCandles * Interval.Duration); // 15 candles further (display)

        // Copy indicators values
        signal.BollingerBandsDeviation = candle.CandleData.BollingerBandsDeviation;
        signal.BollingerBandsPercentage = candle.CandleData.BollingerBandsPercentage; // Dit is degene die Marco gebruikt

        signal.Rsi = candle.CandleData.Rsi;
        signal.SlopeRsi = candle.CandleData.SlopeRsi;

        signal.PSar = candle.CandleData.PSar;
        signal.StochSignal = candle.CandleData.StochSignal;
        signal.StochOscillator = candle.CandleData.StochOscillator;
        //#if DEBUG
        //signal.PSarDave = candle.CandleData.PSarDave;
        //signal.PSarJason = candle.CandleData.PSarJason;
        //signal.PSarTulip = candle.CandleData.PSarTulip;
        //#endif
        //signal.Ema8 = candle.CandleData.Ema8;
        signal.Ema20 = candle.CandleData.Ema20;
        signal.Ema50 = candle.CandleData.Ema50;
        //signal.Ema100 = candle.CandleData.Ema100;
        signal.Ema200 = candle.CandleData.Ema200;
        signal.SlopeEma20 = candle.CandleData.SlopeEma20;
        signal.SlopeEma50 = candle.CandleData.SlopeEma50;

        //signal.Tema = candle.CandleData.Tema;

        //signal.Sma8 = candle.CandleData.Sma8;
        signal.Sma20 = candle.CandleData.Sma20;
        signal.Sma50 = candle.CandleData.Sma50;
        //signal.Sma100 = candle.CandleData.Sma100;
        signal.Sma200 = candle.CandleData.Sma200;
        signal.SlopeSma20 = candle.CandleData.SlopeSma20;
        signal.SlopeSma50 = candle.CandleData.SlopeSma50;

        return signal;
    }


    private void AnalyseSymbolOversold()
    {
        // Geen nieuwe signalen ivm blacklist
        // Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        // Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (!BackTest && GlobalData.Settings.UseBlackListOversold && GlobalData.SymbolBlackListOversold.ContainsKey(Symbol.Name))
            return;

        // Geen nieuwe signalen ivm whitelist
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (!BackTest && (GlobalData.Settings.UseWhiteListOversold && !GlobalData.SymbolWhiteListOversold.ContainsKey(Symbol.Name)))
            return;


        // Oversold (SBM is an advanced version of STOBB)
        bool isSbmSignal = false;

        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisShowSbmOversold)
        {
            // De officiele SBM methode van Maurice
            SignalBase algorithm = new SignalSbm1Oversold(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm2Oversold)
        {
            SignalBase algorithm = new SignalSbm2Oversold(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm3Oversold)
        {
            // De SBM methode - BB.Width is > xx% veranderd
            SignalBase algorithm = new SignalSbm3Oversold(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm);
        }
        //if (!isSbmSignal && GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm4Oversold])
        //{
        //    SignalBase algorithm = new SignalSbm4Oversold(Symbol, Interval, candle);
        //    if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
        //        isSbmSignal = PrepareAndSendSignal(algorithm);
        //}
        //if (!isSbmSignal && GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm5Oversold])
        //{
        //    SignalBase algorithm = new SignalSbm5Oversold(Symbol, Interval, Candle);
        //    if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
        //        isSbmSignal = PrepareAndSendSignal(algorithm);
        //}

        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisShowStobbOversold)
        {
            // STOBB (is eigenlijk een "zwakke" variant van SBM)
            SignalBase algorithm = new SignalStobbOversold(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
    }


    private void AnalyseSymbolOverbought()
    {
        //----------------------------------------------------------------
        // Overbought (SBM is an advanced version of STOBB)
        // TODO: De overbought varianten implementeren sbm2 & 3!
        // Geen nieuwe signalen ivm blacklist
        // Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        // Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (!BackTest && GlobalData.Settings.UseBlackListOverbought && GlobalData.SymbolBlackListOversold.ContainsKey(Symbol.Name))
            return;

        // Geen nieuwe signalen ivm whitelist
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (!BackTest && GlobalData.Settings.UseWhiteListOverbought && !GlobalData.SymbolWhiteListOversold.ContainsKey(Symbol.Name))
            return;

        bool isSbmSignal = false;
        if (GlobalData.Settings.Signal.AnalysisShowSbmOverbought)
        {
            SignalBase algorithm = new SignalSbm1Overbought(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                isSbmSignal = PrepareAndSendSignal(algorithm);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm2Overbought)
        {
            SignalBase algorithm = new SignalSbm2Overbought(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm3Overbought)
        {
            SignalBase algorithm = new SignalSbm3Overbought(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        //if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisSbm4Overbought)
        //{
        //    SignalBase algorithm = new SignalSbm4Overbought(Symbol, Interval, Candle);
        //    if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
        //        PrepareAndSendSignal(algorithm);
        //}

        if (!isSbmSignal && GlobalData.Settings.Signal.AnalysisShowStobbOverbought)
        {
            SignalBase algorithm = new SignalStobbOverbought(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
    }


    public void AnalyseSymbolJumps()
    {
        // Hmm, hier is geen Black en white list meer beschikbaar (nu enkel voor oversold en overbought)
        // Nu is er weer behoefte aan een soort van globale black en white list (bah, lijkt op Zignally)

        if (GlobalData.Settings.Signal.AnalysisShowCandleJumpUp)
        {
            SignalBase algorithm = new SignalCandleJumpUp(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        if (GlobalData.Settings.Signal.AnalysisShowCandleJumpDown)
        {
            SignalBase algorithm = new SignalCandleJumpDown(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
    }

#if TRADEBOT
    public void AnalyseSymbolExperimental()
    {
        //----------------------------------------------------------------
        // Experimentele zaken..
        //if (GlobalData.Settings.Signal.BullishEngulfing)
        //{
        //    SignalBase algorithm = new SignalBullishEngulfing(Symbol, Interval, candle);
        //    if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
        //        PrepareAndSendSignal(algorithm);
        //}


        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.priceCrossedSma20])
        {
            SignalBase algorithm = new SignalPriceCrossedSma20(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }

        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.priceCrossedSma50])
        {
            SignalBase algorithm = new SignalPriceCrossedSma50(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }


        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.priceCrossedEma20])
        {
            SignalBase algorithm = new SignalPriceCrossedEma20(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }

        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.priceCrossedEma50])
        {
            SignalBase algorithm = new SignalPriceCrossedEma50(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }


        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeEma50TurningNegative])
        {
            SignalBase algorithm = new SignalSlopeEma50TurningNegative(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeSma50TurningNegative])
        {
            SignalBase algorithm = new SignalSlopeSma50TurningNegative(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }

        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeEma50TurningPositive])
        {
            SignalBase algorithm = new SignalSlopeEma50TurningPositive(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeSma50TurningPositive])
        {
            SignalBase algorithm = new SignalSlopeSma50TurningPositive(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }


        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeEma20TurningPositive])
        {
            SignalBase algorithm = new SignalSlopeEma20TurningPositive(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
        if (GlobalData.Settings.Signal.AnalyseStrategy[(int)SignalStrategy.slopeSma20TurningPositive])
        {
            SignalBase algorithm = new SignalSlopeSma20TurningPositive(Symbol, Interval, Candle);
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                PrepareAndSendSignal(algorithm);
        }
    }
#endif

    public bool Prepare(long candleOpenTime)
    {
        Candle = null;
        string response = "";

        if (!Symbol.LastPrice.HasValue)
        {
            GlobalData.AddTextToLogTab(string.Format("Analyse {0} Er is geen lastprice aanwezig", Symbol.Name));
            return false;
        }

        // Is het volume binnen de gestelde grenzen          
        if (!BackTest && !Symbol.CheckValidMinimalVolume(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalVolume)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }

        // Is de prijs binnen de gestelde grenzen
        if (!BackTest && !Symbol.CheckValidMinimalPrice(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalPrice)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }


        // Build a list of candles
        if (history == null || BackTest)
            history = CandleIndicatorData.CalculateCandles(Symbol, Interval, candleOpenTime, out response);
        if (history == null)
        {
#if DEBUG
            //if (GlobalData.Settings.Signal.LogNotEnoughCandles)
            GlobalData.AddTextToLogTab("Analyse " + response);
#endif
            return false;
        }

        // Eenmalig de indicators klaarzetten
        Candle = history[^1];
        if (Candle.CandleData == null || BackTest)
            CandleIndicatorData.CalculateIndicators(history, BackTest);

        return true;
    }



    // Debug version
    public SignalBase AnalyseSymbolUsingStrategy(long candleOpenTime, SignalStrategy strategy)
    {
        SignalBase algorithm = null;
        if (Prepare(candleOpenTime))
        {
            algorithm = SignalHelper.GetSignalAlgorithm(strategy, Symbol, Interval, Candle);
            if (algorithm != null)
            {
                if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                    PrepareAndSendSignal(algorithm);
            }
        }
        return algorithm;
    }


    public void AnalyseSymbol(long candleOpenTime)
    {
        // We kunnen nog openstaande posities hebben en die vertrouwen erop dat de lastCandle.CandleData wordt berekend
        // Anders wordt er NIET bijgekocht met DCA en dergelijke (dat zit eigenlijk niet zo goed in elkaar, workaround!)
        //string response = "";

        if (Prepare(candleOpenTime))
        {

            // Eenmalig de indicators klaarzetten
            //CryptoCandle candle = history[^1];
            //if (candle.CandleData == null || BackTest)
            //    CandleIndicatorData.CalculateIndicators(history);

            //----------------------------------------------------------------

            // Oversold - STOBB en SBM (SBM is de advanced version van STOBB)
            AnalyseSymbolOversold();

            // Overbought - STOBB + SBM (SBM is de advanced version van STOBB)
            AnalyseSymbolOverbought();

            // Candle jumps
            AnalyseSymbolJumps();


#if TRADEBOT
            // Experimenteel, kan wellicht weg
            AnalyseSymbolExperimental();
#endif

            // TODO! Nog eens uitzoeken of het hierdoor makkelijker wordt
            // prepare the overbought and oversold black and white list
            // crap, de sbm1..x en de stob zijn overlappend, je wil ze niet alle x zien
            // crap, we hebben een verschil in overbought en oversold i.c.m. met de white en blacklist
            //foreach (SignalStrategy strategy in Enum.GetValues(typeof(SignalStrategy)))
            //{
            // PROBLEEM, deze zitten in aparte properties en moeten gemigreerd worden
            //    if (GlobalData.Settings.Signal.AnalyseStrategy[(int)strategy])
            //    {
            //        SignalBase algorithm = SignalHelper.GetSignalAlgorithm(strategy, Symbol, Interval, candle);
            //        if (algorithm != null)
            //        {
            //            if (algorithm.SignalMode == SignalMode.modeLong)
            //            {
            //                // check black and white list oversold
            //            }
            //            if (algorithm.SignalMode == SignalMode.modeShort)
            //            {
            //                // check black and white list overbought
            //            }
            //            if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
            //                PrepareAndSendSignal(algorithm);

            //        }

            //    }
            //}
        }
    }


    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, double value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, DateTime value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteStyle(ISheet sheet, int columnIndex, int rowIndex, ICellStyle style)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        ICell cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.CellStyle = style;
        return cell;
    }

    private static int ExcellHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;
        // Columns...
        WriteCell(sheet, column++, row, "DateLocal");
        WriteCell(sheet, column++, row, "Open");
        WriteCell(sheet, column++, row, "High");
        WriteCell(sheet, column++, row, "Low");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Volume");

        WriteCell(sheet, column++, row, "bb.lower");
        WriteCell(sheet, column++, row, "bb.upper");
        WriteCell(sheet, column++, row, "bb.perc");

        WriteCell(sheet, column++, row, "Sma200");
        WriteCell(sheet, column++, row, "Sma50");
        WriteCell(sheet, column++, row, "Sma20");
        WriteCell(sheet, column++, row, "PSar");

        WriteCell(sheet, column++, row, "macd.value");
        WriteCell(sheet, column++, row, "macd.signal");
        WriteCell(sheet, column++, row, "macd.hist");
        WriteCell(sheet, column++, row, "SBM conditions");
        WriteCell(sheet, column++, row, "200/20");
        WriteCell(sheet, column++, row, "200/50");
        WriteCell(sheet, column++, row, "50/20");
        WriteCell(sheet, column++, row, "recovery text");

        WriteCell(sheet, column++, row, "Rsi");
        WriteCell(sheet, column++, row, "stoch.ocillator");
        WriteCell(sheet, column++, row, "stoch.signal");

        // wat kun je hiermee?     
        WriteCell(sheet, column++, row, "Ema20");
        WriteCell(sheet, column++, row, "Ema50");
        WriteCell(sheet, column++, row, "Ema200");
        return column;
    }

    public void ExportToExcell(SignalStrategy strategy)
    {
        // HSSF => Microsoft Excel(excel 97-2003)
        // XSSF => Office Open XML Workbook(excel 2007)
        HSSFWorkbook book = new();

        //create a entry of DocumentSummaryInformation
        DocumentSummaryInformation documentSummaryInformation = PropertySetFactory.CreateDocumentSummaryInformation();
        documentSummaryInformation.Company = "Crypto Scanner";
        book.DocumentSummaryInformation = documentSummaryInformation;

        //create a entry of SummaryInformation
        SummaryInformation summaryInformation = PropertySetFactory.CreateSummaryInformation();
        summaryInformation.Subject = Symbol.Name;
        book.SummaryInformation = summaryInformation;

        IDataFormat format = book.CreateDataFormat();

        HSSFSheet sheet = (HSSFSheet)book.CreateSheet("Sheet1");


        ICellStyle cellStyleDate = book.CreateCellStyle();
        cellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        cellStyleDate.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

        ICellStyle cellStyleStringGreen = book.CreateCellStyle();
        cellStyleStringGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleStringGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        ICellStyle cellStyleDecimalNormal = book.CreateCellStyle();
        cellStyleDecimalNormal.DataFormat = format.GetFormat("0.00000000");

        ICellStyle cellStyleDecimalGreen = book.CreateCellStyle();
        cellStyleDecimalGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleDecimalGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleDecimalGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        ICellStyle cellStyleDecimalRed = book.CreateCellStyle();
        cellStyleDecimalRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleDecimalRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        cellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
        cellStyleDecimalRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


        ICellStyle cellStylePercentageNormal = book.CreateCellStyle();
        cellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

        ICellStyle cellStylePercentageGreen = book.CreateCellStyle();
        cellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        cellStylePercentageGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
        cellStylePercentageGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


        // macd.red
        ICellStyle cellStyleMacdRed = book.CreateCellStyle();
        cellStyleMacdRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        cellStyleMacdRed.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Lime.Index;

        // macd.roze
        ICellStyle cellStyleMacdLightRed = book.CreateCellStyle();
        cellStyleMacdLightRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdLightRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdLightRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;
        cellStyleMacdLightRed.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdLightRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;

        // macd.green
        ICellStyle cellStyleMacdGreen = book.CreateCellStyle();
        cellStyleMacdGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;
        cellStyleMacdGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;

        // macd.ligh green
        ICellStyle cellStyleMacdLightGreen = book.CreateCellStyle();
        cellStyleMacdLightGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdLightGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdLightGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleMacdLightGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdLightGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


        int columns = ExcellHeaders(sheet, 0);

        int row = 0;
        int column;
        CryptoCandle prev = null;
        for (int i = 0; i < history.Count; i++)
        {
            row++;
            column = 0;
            CryptoCandle candle = history[i];

            ICell cell;
            cell = WriteCell(sheet, column++, row, candle.DateLocal);
            cell.CellStyle = cellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)candle.Open);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.High);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Low);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Close);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Volume);
            cell.CellStyle = cellStyleDecimalNormal;


            if (candle.CandleData != null)
            {
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsLowerBand);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsUpperBand);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsPercentage);
                if (candle.CandleData.BollingerBandsPercentage >= 1.5)
                    cell.CellStyle = cellStylePercentageGreen;
                else
                    cell.CellStyle = cellStylePercentageNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma200);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma50);
                if (candle.CandleData.Sma200 >= candle.CandleData.Sma50)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma20);
                if (candle.CandleData.Sma50 >= candle.CandleData.Sma20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.PSar);
                if (candle.CandleData.Sma20 > candle.CandleData.PSar)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdValue);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdSignal);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdHistogram);
                if (candle.CandleData.MacdHistogram >= 0)
                {
                    // above zero line = green
                    if (prev == null || prev.CandleData == null)
                        cell.CellStyle = cellStyleDecimalNormal;
                    else
                    {
                        if (candle.CandleData.MacdHistogram >= prev.CandleData.MacdHistogram)
                            cell.CellStyle = cellStyleDecimalGreen;
                        else
                            cell.CellStyle = cellStyleMacdLightGreen;
                    }
                }
                else
                {
                    // below zero line = red
                    if (prev == null || prev.CandleData == null)
                        cell.CellStyle = cellStyleDecimalNormal;
                    else
                    {
                        if (candle.CandleData.MacdHistogram <= prev.CandleData.MacdHistogram)
                            cell.CellStyle = cellStyleMacdRed;
                        else
                            cell.CellStyle = cellStyleMacdLightRed;
                    }
                }


                if (candle.IsSbmConditionsOversold(true))
                {
                    WriteCell(sheet, column++, row, "yes");
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out string _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                WriteCell(sheet, column++, row, candle.ExtraText);


                // overbodig?

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Rsi);
                if (candle.CandleData.Rsi > 30)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.StochOscillator);
                if (candle.CandleData.StochOscillator > 20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.StochSignal);
                if (candle.CandleData.StochSignal > 20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;

                // wat kun je hiermee?
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema20);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema50);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema200);
                cell.CellStyle = cellStyleDecimalNormal;
            }
            prev = candle;
        }

        // wel makkelijk als ze ook onderin staan
        ExcellHeaders(sheet, row + 1);

        for (int i = 0; i < columns; i++)
        {
            sheet.AutoSizeColumn(i);
            int width = sheet.GetColumnWidth(i);
            sheet.SetColumnWidth(i, (int)(1.1 * width));
        }


        string text = SignalHelper.GetSignalAlgorithmText(strategy);
        GlobalData.AddTextToLogTab(string.Format("Backtest {0} {1} ready", Symbol.Name, text));

        string folder = GlobalData.GetBaseDir() + @"\BackTest\";
        Directory.CreateDirectory(folder);
        using var fs = new FileStream(folder + Symbol.Name + "-" + text + ".xls", FileMode.Create);

        book.Write(fs);
    }
}
