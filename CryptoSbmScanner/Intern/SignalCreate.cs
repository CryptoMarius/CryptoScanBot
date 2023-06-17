using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public delegate void AnalyseEvent(CryptoSignal signal);


public class SignalCreate
{
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }

    private CryptoCandle Candle { get; set; }
    public List<CryptoCandle> history = null;

    // To avoid duplicate signals
    static private DateTime? AnalyseNotificationClean { get; set;  } = null;
    static public Dictionary<string, long> AnalyseNotificationList { get; } = new();

    public SignalCreate(CryptoSymbol symbol, CryptoInterval interval)
    {
        Symbol = symbol;
        Interval = interval;
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
                GlobalData.AddTextToLogTab(string.Format("Analyse {0} {1} {2:N8} heeft geen candledata of geen BB?", signal.Symbol.Name, CandleLast.DateLocal.ToString(), CandleLast.Close));
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


    private bool CheckAdditionalAlarmProperties(CryptoSignal signal, out string reaction)
    {
        // --------------------------------------------------------------------------------
        // Van de laatste 60 candles mogen er maximaal 16 geen volume hebben.
        // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.Signal.CandlesWithZeroVolumeCheck)
        {
            if ((GlobalData.Settings.Signal.CandlesWithZeroVolume > 0) && (signal.CandlesWithZeroVolume > GlobalData.Settings.Signal.CandlesWithZeroVolume))
            {
                reaction = string.Format("teveel candles zonder volume ({0} van 60 candles)", signal.CandlesWithZeroVolume);
                return false;
            }
        }

        // --------------------------------------------------------------------------------
        // Van de laatste 60 candles mogen er slechts 18 plat zijn
        // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.Signal.CandlesWithFlatPriceCheck)
        {
            if ((GlobalData.Settings.Signal.CandlesWithFlatPrice > 0) && (signal.CandlesWithFlatPrice > GlobalData.Settings.Signal.CandlesWithFlatPrice))
            {
                reaction = string.Format("teveel platte candles ({0} van 60 candles)", signal.CandlesWithFlatPrice);
                return false;
            }
        }


        // Er moet een beetje beweging in de BB zitten (niet enkel op de onderste bb ofzo)
        if (GlobalData.Settings.Signal.AboveBollingerBandsSmaCheck)
        {
            if ((GlobalData.Settings.Signal.AboveBollingerBandsSma > 0) && (signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma))
            {
                reaction = string.Format("te weinig candles die boven de BB.Sma uitsteken ({0} van 60 candles)", signal.AboveBollingerBandsSma);
                return false;
            }
        }


        // Vervolg op voorgaande wens op beweging in de BB (met het liefst een aantal uitschieters)
        if (GlobalData.Settings.Signal.AboveBollingerBandsUpperCheck)
        {
            if ((GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0) && (signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper))
            {
                reaction = string.Format("te weinig candles die boven de BB.Upper uitsteken ({0} van 60 candles)", signal.AboveBollingerBandsUpper);
                return false;
            }
        }


        reaction = "";
        return true;
    }


    private static void AnalyseNotificationClearOutOld()
    {
        // 1x in de 15 minuten de notificatie lijst cleanen is wel genoeg
        if (AnalyseNotificationClean == null || AnalyseNotificationClean < DateTime.UtcNow)
        {
            // Next clean date
            AnalyseNotificationClean = DateTime.UtcNow.AddMinutes(15);

            Monitor.Enter(AnalyseNotificationList);
            try
            {
                // De lijst kleiner maken
                long someTimeAgo = CandleTools.GetUnixTime(DateTime.UtcNow.AddHours(-2), 60);
                for (int i = AnalyseNotificationList.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<string, long> item2 = AnalyseNotificationList.ElementAt(i);
                    if (item2.Value < someTimeAgo)
                        AnalyseNotificationList.Remove(item2.Key);
                }
            }
            finally
            {
                Monitor.Exit(AnalyseNotificationList);
            }
        }
    }


    void CalculateTrendStuff(CryptoSignal signal)
    {
        //int iterator = 0;
        long percentageSum = 0;
        long maxPercentageSum = 0;
        try
        {

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
                symbolInterval.TrendInfoDate = signal.OpenDate;

                maxPercentageSum += interval.Duration;
            }


            float trendPercentage = 100 * (float)percentageSum / (float)maxPercentageSum;
            signal.TrendPercentage = trendPercentage;
            Symbol.TrendPercentage = trendPercentage;
            Symbol.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);

            signal.TrendPercentage = -100;
            Symbol.TrendPercentage = -100;
            Symbol.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
    }


    private double CalculateLastPeriodsInInterval(CryptoSignal signal, long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(dit om de berekeningen allemaal wat sneller te maken)

        // Vanwege backtest altijd redeneren vanaf het signaal (en niet de laatste candle)
        CryptoCandle candle = signal.Candle; // Symbol.CandleList.Values.Last();
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


    private double CalculateMaxMovementInInterval(long startTime, CryptoIntervalPeriod intervalPeriod, long candleCount)
    {
        // Op een iets hoger interval gaan we x candles naar achteren en meten de echte beweging
        // (de 24% change is wat effectief overblijft, maar dat is duidelijk niet de echte beweging)
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(intervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;


        double min = double.MaxValue;
        double max = double.MinValue;

        // Vanwege backtest altijd redeneren vanaf het signaal (en niet de laatste candle)
        long unix = CandleTools.GetUnixTime(startTime, symbolInterval.Interval.Duration);

        while (candleCount-- > 0)
        {
            if (candles.TryGetValue(unix, out CryptoCandle candle))
            {
                //break; // we hebben maximaal 260 candles, minder dan de gewenste 2 dagen

                if ((double)candle.Low < min)
                    min = (double)candle.Low;

                if ((double)candle.High > max)
                    max = (double)candle.High;

                //if (!candles.TryGetValue(candle.OpenTime - symbolInterval.Interval.Duration, out candle))
                //    break; // we hebben maximaal 260 candles, minder dan de gewenste 2 dagen
            }

            unix -= symbolInterval.Interval.Duration;
        }

        double diff = max - min;
        if (!min.Equals(0))
            return 100.0 * (diff / min);
        else
            return 0;
    }


    private void SendSignal(SignalCreateBase algorithm, CryptoSignal signal, string eventText)
    {
        // Hebben we deze al eerder gemeld?
        if (!signal.BackTest)
        {
            AnalyseNotificationClearOutOld();

            string notification =
                signal.Symbol.Name + "-" +
                signal.Interval.Name + "-" +
                signal.Strategy.ToString() + "-" +
                signal.Mode.ToString() + "-" +
                signal.Candle.Date.ToLocalTime();

            Monitor.Enter(AnalyseNotificationList);
            try
            {
                if (AnalyseNotificationList.ContainsKey(notification))
                    return;

                AnalyseNotificationList.Add(notification, signal.EventTime);
            }
            finally
            {
                Monitor.Exit(AnalyseNotificationList);
            }
        }


        CalculateTrendStuff(signal); // CPU heavy
        signal.EventText = eventText.Trim();

        // die willen we nu juist  (zal wel weer andere problemen geven)
        //if (!signal.BackTest)
        //{
        try
        {
            // We gebruiken (nog) geen exit signalen, echter dat zou best realistisch zijn voor de toekomst
            if (!signal.IsInvalid && GlobalData.Settings.Trading.Active && signal.Mode == CryptoTradeDirection.Long) //|| (Alarm.Mode == SignalMode.modeSell) 
            {
                if (TradingConfig.MonitorInterval.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    if (TradingConfig.Config[signal.Mode].MonitorStrategy.ContainsKey(signal.Strategy))
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
                }
            }

            // Signal naar database wegschrijven (niet noodzakelijk, we doen er achteraf niets mee)
            if (!signal.BackTest)
            {
                using (CryptoDatabase databaseThread = new())
                {
                    databaseThread.Close();
                    databaseThread.Open();
                    using (var transaction = databaseThread.BeginTransaction())
                    {
                        databaseThread.Connection.Insert<CryptoSignal>(signal, transaction);
                        transaction.Commit();
                    }
                }
            }

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);
        }
        //}

        GlobalData.SignalEvent?.Invoke(signal);
        return;
    }


    /// <summary>
    /// Dit is gebaseerd op de "RSI Multi Length [LuxAlgo]"
    /// We gebruiken de oversell of overbuy indicator als extra tekst in de melding
    /// </summary>
    /// <param name="overSell">Retourneer de oversell of de overbuy tellertje</param>
    /// <returns></returns>
    public static void GetFluxIndcator(CryptoSymbol symbol, out int fluxOverSold, out int fluxOverBought)
    {
        SortedList<long, CryptoCandle> candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).CandleList;

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

        int overbuy = 0;
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

        fluxOverSold = 10 * oversell;
        fluxOverBought = 10 * overbuy;
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


    public bool PrepareAndSendSignal(SignalCreateBase algorithm)
    {
        CryptoSignal signal = CreateSignal(Candle);
        signal.Mode = algorithm.SignalMode;
        signal.Strategy = algorithm.SignalStrategy;
        signal.LastPrice = (decimal)Symbol.LastPrice;

        string response;
        string eventText = algorithm.ExtraText;

        // Extra attributen erbij halen (dat lukt niet bij een backtest vanwege het ontbreken van een "history list")
        if (!GlobalData.BackTest)
        {
            CalculateAdditionalSignalProperties(signal, history, 60);
            if (!CheckAdditionalAlarmProperties(signal, out response))
            {
                eventText += " " + response;
                signal.IsInvalid = true;
            }
        }


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien) - via de info flag
        if (!algorithm.AdditionalChecks(Candle, out response))
        {
            eventText += " " + response;
            signal.IsInvalid = true;
        }

        // Weer een extra controle, staat de symbol op de black of whitelist?
        if (!signal.BackTest && TradingConfig.Config[signal.Mode].InBlackList(Symbol.Name))
        {
            // Als de muntpaar op de black lijst staat dit signaal overslagen
            eventText += " " + "staat op blacklist";
            signal.IsInvalid = true;
        }
        else if (!signal.BackTest && !TradingConfig.Config[signal.Mode].InWhiteList(Symbol.Name))
        {
            // Als de muntpaar niet op de white lijst staat dit signaal overslagen
            eventText += " " + "niet in whitelist";
            signal.IsInvalid = true;
        }


        // de 24 change moet in dit interval zitten
        // Vraag: Kan deze niet beter naar het begin van de controles?
        signal.Last24HoursChange = CalculateLastPeriodsInInterval(signal, 24 * 60 * 60);
        signal.Last24HoursEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval15m, 1 * 96);

        // Question: We hebben slechts 260 candles, dit geeft dus het gewenste getal voor 48 uur (afgesterd)!
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
            signal.IsInvalid = true;
        }

        if (!signal.Last24HoursEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinEffectivePercentage, GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxEffectivePercentage)
            {
                string text = string.Format("Analyse {0} 24h change (effectief) {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last24HoursEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinEffectivePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText += " 24h effectief% te hoog";
            signal.IsInvalid = true;
        }


        // New coins have a lot of price changes
        // Are there x day's of candles avaliable on the day interval
        // Bij het backtesten wordt slechts een gelimiteerd aantal candles ingelezen, dus daarom uitgeschakeld)
        if (!GlobalData.BackTest)
        {
            var candles1Day = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).CandleList;
            if (candles1Day.Count < GlobalData.Settings.Signal.SymbolMustExistsDays)
            {
                if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
                {
                    // Het aantal dagen vermelden dat het bestaat
                    string text = "";
                    if (candles1Day.Count > 0)
                    {
                        CryptoCandle first = candles1Day.Values.First();
                        text = CandleTools.GetUnixDate(first.OpenTime).Day.ToString();
                    }
                    GlobalData.AddTextToLogTab(string.Format("Analyse {0} te nieuw geen {1} dagen {2}", Symbol.Name, GlobalData.Settings.Signal.SymbolMustExistsDays, text));
                }
                eventText += " coin te nieuw";
                signal.IsInvalid = true;
            }
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
            signal.IsInvalid = true;
        }


        if (!signal.BackTest)
        {
            decimal barcodePercentage = 100 * Symbol.PriceTickSize / (decimal)Symbol.LastPrice;
            if ((barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage))
            {
                // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
                if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
                    GlobalData.AddTextToLogTab(string.Format("Analyse {0} De tick size percentage is te hoog {1:N3}", Symbol.Name, barcodePercentage));
                eventText += " tick perc to high";
                signal.IsInvalid = true;
            }
        }


        // Zoveel voegt dit ook weer niet toe
        if (GlobalData.Settings.General.ShowFluxIndicator5m)
        {
            GetFluxIndcator(Symbol, out int fluxOverSold, out int _);
            signal.FluxIndicator5m = fluxOverSold;
        }


        // Voor de SignalSlopesTurning* een variabele zetten of 
        // clearen zodat dit signaal niet om de x candles binnenkomt.
        // mhja, nog eens bedenken of we deze strategien willen publiseren....
        CryptoSymbolInterval SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        switch (signal.Strategy)
        {
            case SignalStrategy.Sbm1:
            case SignalStrategy.Sbm2:
            case SignalStrategy.Sbm3:
            case SignalStrategy.Sbm4:
            case SignalStrategy.Stobb:
                if (signal.Mode == CryptoTradeDirection.Long)
                    SymbolInterval.LastStobbOrdSbmDate = signal.OpenDate;
                break;

            case SignalStrategy.SlopeSma20:
            case SignalStrategy.SlopeEma20:
            case SignalStrategy.SlopeSma50:
            case SignalStrategy.SlopeEma50:
                SymbolInterval.LastStobbOrdSbmDate = null;
                break;
        }


        if (!GlobalData.Settings.Signal.ShowInvalidSignals && signal.IsInvalid)
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
            ExchangeId = Symbol.ExchangeId,
            SymbolId = Symbol.Id,
            IntervalId = Interval.Id,
            BackTest = GlobalData.BackTest,
            Price = candle.Close,
            Volume = Symbol.Volume,
            EventTime = candle.OpenTime,
            OpenDate = CandleTools.GetUnixDate(candle.OpenTime),
            Mode = CryptoTradeDirection.Long,  // gets modified later
            Strategy = SignalStrategy.Jump,  // gets modified later
        };

        signal.CloseDate = signal.OpenDate.AddSeconds(Interval.Duration);
        signal.ExpirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * Interval.Duration);

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


    private bool ExecuteAlgorithm(AlgorithmDefinition strategyDefinition)
    {
        SignalCreateBase algorithm = strategyDefinition.InstantiateAnalyzeLong(Symbol, Interval, Candle);
        if (algorithm != null)
        {
            if (algorithm.IndicatorsOkay(Candle) && algorithm.IsSignal())
                return PrepareAndSendSignal(algorithm);
        }
        return false;
    }


    /// <summary>
    /// Zet de de laatste x candles op een rijtje en bereken de indicators
    /// </summary>
    /// <param name="candleOpenTime"></param>
    /// <returns></returns>
    public bool Prepare(long candleOpenTime)
    {
        Candle = null;
        string response = "";

        if (!Symbol.LastPrice.HasValue)
        {
            // Die wordt ingevuld in de BinanceStream1mCandles en BinanceStreamPriceTicker, dus zelden leeg
            GlobalData.AddTextToLogTab(string.Format("Analyse {0} Er is geen lastprice aanwezig", Symbol.Name));
            return false;
        }

        // Is het volume binnen de gestelde grenzen          
        if (!GlobalData.BackTest && !Symbol.CheckValidMinimalVolume(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalVolume)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }

        // Is de prijs binnen de gestelde grenzen
        if (!GlobalData.BackTest && !Symbol.CheckValidMinimalPrice(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalPrice)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }


        if (GlobalData.BackTest)
        {
            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle candle))
                Candle = candle;
        }
        else
        {
            // Build a list of candles
            if (history == null)
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
            if (Candle.CandleData == null)
                CandleIndicatorData.CalculateIndicators(history);
        }
        return true;
    }



    public void AnalyzeSymbol(long candleOpenTime)
    {
        // Eenmalig de indicators klaarzetten
        if (Prepare(candleOpenTime))
        {
            foreach (CryptoTradeDirection tradeDirection in Enum.GetValues(typeof(CryptoTradeDirection)))
            {
                // Indien we ongeldige signalen laten zien moeten we deze controle overslagen.
                // (verderop in het process wordt alsnog hierop gecontroleerd)

                //if (!GlobalData.Settings.Signal.ShowInvalidSignals && !BackTest)
                //{
                //    // Dan kunnen we direct die handel hier afkappen..
                //    // Weer een extra controle, staat de symbol op de black of whitelist?
                //    if (TradingConfig.Config[tradeDirection].InBlackList(Symbol.Name))
                //    {
                //        // Als de muntpaar op de black lijst staat dit signaal overslagen
                //        continue;
                //    }
                //    else if (!TradingConfig.Config[tradeDirection].InWhiteList(Symbol.Name))
                //    {
                //        // Als de muntpaar niet op de white lijst staat dit signaal overslagen
                //        continue;
                //    }
                //}

                // SBM en stobb zijn afhankelijk van elkaar, vandaar de break
                // Ze staan alfabetisch, sbm1, sbm2, sbm3, stobb dat gaat per ongeluk goed
                foreach (AlgorithmDefinition strategyDefinition in TradingConfig.Config[tradeDirection].StrategySbmStob.Values)
                {
                    if (ExecuteAlgorithm(strategyDefinition))
                        break;
                }

                // En de overige waaronder de jump
                foreach (AlgorithmDefinition strategyDefinition in TradingConfig.Config[tradeDirection].StrategyOthers.Values)
                    ExecuteAlgorithm(strategyDefinition);
            }

        }
    }
}