using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.Trader;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public delegate void AnalyseEvent(CryptoSignal signal);


public class SignalCreate
{
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private CryptoTradeSide Side { get; set; }

    private CryptoCandle Candle { get; set; }
    public List<CryptoCandle> history = null;

    public bool CreatedSignal = false;

#if TRADEBOT
    bool hasOpenPosistion = false;
    bool hasOpenPosistionCalculated = false;
#endif

    // To avoid duplicate signals
    //static private DateTime? AnalyseNotificationClean { get; set;  } = null;
    // Lijkt overbodig te zijn tegenwoordig?
    //static public Dictionary<string, long> AnalyseNotificationList { get; } = new();

    public SignalCreate(CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side)
    {
        Symbol = symbol;
        Interval = interval;
        Side = side;
    }

    private bool HasOpenPosition()
    {
        // Signalen blijven maken als er een positie openstaat (en het volume e.d. sterk afgenomen is)
#if TRADEBOT
        if (!hasOpenPosistionCalculated)
        {
            hasOpenPosistionCalculated = true;
            hasOpenPosistion = GlobalData.Settings.Trading.Active && PositionTools.HasPosition(Symbol);
        }
        return hasOpenPosistion;
#else
        return false;
#endif
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


    private static bool CheckAdditionalAlarmProperties(CryptoSignal signal, out string reaction)
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


    //private static void AnalyseNotificationClearOutOld()
    //{
    //    // 1x in de 15 minuten de notificatie lijst cleanen is wel genoeg
    //    if (AnalyseNotificationClean == null || AnalyseNotificationClean < DateTime.UtcNow)
    //    {
    //        // Next clean date
    //        AnalyseNotificationClean = DateTime.UtcNow.AddMinutes(15);

    //        Monitor.Enter(AnalyseNotificationList);
    //        try
    //        {
    //            // De lijst kleiner maken
    //            long someTimeAgo = CandleTools.GetUnixTime(DateTime.UtcNow.AddHours(-2), 60);
    //            for (int i = AnalyseNotificationList.Count - 1; i >= 0; i--)
    //            {
    //                KeyValuePair<string, long> item2 = AnalyseNotificationList.ElementAt(i);
    //                if (item2.Value < someTimeAgo)
    //                    AnalyseNotificationList.Remove(item2.Key);
    //            }
    //        }
    //        finally
    //        {
    //            Monitor.Exit(AnalyseNotificationList);
    //        }
    //    }
    //}


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
                if (trendIndicator == CryptoTrendIndicator.Bullish)
                    percentageSum += interval.Duration;
                else if (trendIndicator == CryptoTrendIndicator.Bearish)
                    percentageSum -= interval.Duration;

                if (intervalPeriod == signal.Interval.IntervalPeriod)
                    signal.TrendIndicator = trendIndicator;

                // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
                symbolInterval.TrendIndicator = trendIndicator;
                symbolInterval.TrendInfoDate = signal.OpenDate;

                maxPercentageSum += interval.Duration;

                // Doorzetten naar het signal (op verzoek)
                switch (intervalPeriod)
                {
                    case CryptoIntervalPeriod.interval15m:
                        signal.Trend15m = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval30m:
                        signal.Trend30m = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval1h:
                        signal.Trend1h = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval4h:
                        signal.Trend4h = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval12h:
                        signal.Trend12h = trendIndicator;
                        break;
                }

            }


            float trendPercentage = 100 * (float)percentageSum / (float)maxPercentageSum;
            signal.TrendPercentage = trendPercentage;
            Symbol.TrendPercentage = trendPercentage;
            Symbol.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error, "");
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
                if ((double)candle.Low < min)
                    min = (double)candle.Low;

                if ((double)candle.High > max)
                    max = (double)candle.High;
            }

            unix -= symbolInterval.Interval.Duration;
        }

        double diff = max - min;
        if (!max.Equals(0))
            return 100.0 * (diff / max);
        else
            return 0;
        //signal.Last10DaysEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval6h, 1 * 40);
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

    //static public List<CryptoCandle> CalculateHistory(SortedList<long, CryptoCandle> candleSticks, int maxCandles)
    //{
    //    //Transporteer de candles naar de Stock list
    //    //Jammer dat we met tussen-array's moeten werken
    //    List<CryptoCandle> history = new();
    //    Monitor.Enter(candleSticks);
    //    try
    //    {
    //        //Vanwege performance nemen we een gedeelte van de candles
    //        for (int i = candleSticks.Values.Count - 1; i >= 0; i--)
    //        {
    //            CryptoCandle candle = candleSticks.Values[i];

    //            //In omgekeerde volgorde in de lijst zetten
    //            if (history.Count == 0)
    //                history.Add(candle);
    //            else
    //                history.Insert(0, candle);

    //            maxCandles--;
    //            if (maxCandles == 0)
    //                break;
    //        }
    //    }
    //    finally
    //    {
    //        Monitor.Exit(candleSticks);
    //    }
    //    return history;
    //}


    private bool PrepareAndSendSignal(SignalCreateBase algorithm)
    {
        CryptoSignal signal = CreateSignal(Candle);
        signal.Side = algorithm.SignalSide;
        signal.Strategy = algorithm.SignalStrategy;
        signal.LastPrice = (decimal)Symbol.LastPrice;

        string response;
        List<string> eventText = new();
        if (algorithm.ExtraText != "")
            eventText.Add(algorithm.ExtraText);


        // Extra attributen erbij halen (dat lukt niet bij een backtest vanwege het ontbreken van een "history list")
        if (!GlobalData.BackTest)
        {
            CalculateAdditionalSignalProperties(signal, history, 60);
            if (!HasOpenPosition() && !CheckAdditionalAlarmProperties(signal, out response))
            {
                eventText.Add(response);
                signal.IsInvalid = true;
            }
        }


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        if (!algorithm.AdditionalChecks(Candle, out response))
        {
            eventText.Add(response);
            signal.IsInvalid = true;
        }

        // Extra controles, staat de symbol op de blacklist?
        if (!HasOpenPosition() && !signal.BackTest && TradingConfig.Signals[signal.Side].InBlackList(Symbol.Name) == MatchBlackAndWhiteList.Present)
        {
            // Als de muntpaar op de black lijst staat dan dit signaal overslagen
            eventText.Add("staat op blacklist");
            signal.IsInvalid = true;
        }

        // Extra controles, staat de symbol op de whitelist?
        if (!HasOpenPosition() && !signal.BackTest && TradingConfig.Signals[signal.Side].InWhiteList(Symbol.Name) == MatchBlackAndWhiteList.NotPresent)
        {
            // Als de muntpaar niet in de white lijst staat dan dit signaal overslagen
            eventText.Add("niet in whitelist");
            signal.IsInvalid = true;
        }


        // de 24 change moet in een bepaald interval zitten
        signal.Last24HoursChange = CalculateLastPeriodsInInterval(signal, 24 * 60 * 60);
        if (!HasOpenPosition() && !signal.Last24HoursChange.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
            {
                string text = string.Format("Analyse {0} 1d change {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last24HoursChange.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("1d verandering% te hoog");
            signal.IsInvalid = true;
        }

        // de 1 * 1d effectief moet in een bepaald interval zitten
        signal.Last24HoursEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval15m, 1 * 96); // 1 * 24 / 15 = 96
        if (!HasOpenPosition() && !signal.Last24HoursEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinEffectivePercentage, GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxEffectivePercentage)
            {
                string text = string.Format("Analyse {0} 1d change effectief {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last24HoursEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinEffectivePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("1d effectief% te hoog");
            signal.IsInvalid = true;
        }

        // de 10 * 1d effectief moet in een bepaald interval zitten
        signal.Last10DaysEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval6h, 1 * 40); // 10 * 24 / 6 = 40
        if (!HasOpenPosition() && !signal.Last10DaysEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinEffective10DaysPercentage, GlobalData.Settings.Signal.AnalysisMaxEffective10DaysPercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage)
            {
                string text = string.Format("Analyse {0} 10d change effectief {1} niet tussen {2} .. {3}", Symbol.Name, signal.Last10DaysEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinEffective10DaysPercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxEffective10DaysPercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("10d effectief% te hoog");
            signal.IsInvalid = true;
        }




        // New coins have a lot of price changes
        // Are there x day's of candles avaliable on the day interval
        // Bij het backtesten wordt slechts een gelimiteerd aantal candles ingelezen, dus daarom uitgeschakeld)
        if (!HasOpenPosition() && !GlobalData.BackTest)
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
                eventText.Add("coin te nieuw");
                signal.IsInvalid = true;
            }
        }


        // TODO: Willen we dat nu hier of in de aansturing (de invalid signals klopt nu niet)
        //// Check low barometer
        //decimal? Barometer1h = -99m;
        //if (GlobalData.Settings.QuoteCoins.TryGetValue(Symbol.Quote, out CryptoQuoteData quoteData))
        //{
        //    BarometerData barometerData = quoteData.BarometerList[CryptoIntervalPeriod.interval1h];
        //    Barometer1h = barometerData.PriceBarometer;
        //}
        //if (Barometer1h <= GlobalData.Settings.Signal.Barometer1hMinimal)
        //{
        //    // Log iets, maar dat wordt wel veel
        //    if (GlobalData.Settings.Signal.LogBarometerToLow)
        //        GlobalData.AddTextToLogTab("Analyse Barometer te laag");
        //    eventText.Add("barometer te laag");
        //    signal.IsInvalid = true;
        //}


        // Check "Barcode" charts
        if (!HasOpenPosition() && !signal.BackTest)
        {
            decimal barcodePercentage = 100 * Symbol.PriceTickSize / (decimal)Symbol.LastPrice;
            if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            {
                // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
                if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
                    GlobalData.AddTextToLogTab(string.Format("Analyse {0} De tick size percentage is te hoog {1:N3}", Symbol.Name, barcodePercentage));
                eventText.Add("tick perc to high");
                signal.IsInvalid = true;
            }
        }

        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;



        // Iets wat ik wel eens gebruikt als ik trade
        GetFluxIndcator(Symbol, out int fluxOverSold, out int fluxOverBought);
        if (signal.Side == CryptoTradeSide.Long)
            signal.FluxIndicator5m = fluxOverSold;
        else
            signal.FluxIndicator5m = fluxOverBought;


        // Dit lijkt overbodig te zijn tegenwoordig?
        //// Hebben we deze al eerder gemeld?
        //if (!signal.BackTest)
        //{
        //    AnalyseNotificationClearOutOld();

        //    string notification =
        //        signal.Symbol.Name + "-" +
        //        signal.Interval.Name + "-" +
        //        signal.Strategy.ToString() + "-" +
        //        signal.Side.ToString() + "-" +
        //        signal.Candle.Date.ToLocalTime();

        //    Monitor.Enter(AnalyseNotificationList);
        //    try
        //    {
        //        if (AnalyseNotificationList.ContainsKey(notification))
        //        {
        //            // Is deze nog nodig?
        //            GlobalData.AddTextToLogTab("Duplicaat melding settings " + notification);
        //            return false;
        //        }

        //        AnalyseNotificationList.Add(notification, signal.EventTime);
        //    }
        //    finally
        //    {
        //        Monitor.Exit(AnalyseNotificationList);
        //    }
        //}


        // Bereken de trend, dat is tamelijk CPU heavy en daarom staat deze controle op het einde
        CalculateTrendStuff(signal);


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        if (!HasOpenPosition())
        {
            // Filter op bepaalde intervallen waarvan je wil dat die bullisch of bearisch zijn
            if (!PositionTools.ValidTrendConditions(signal.Symbol, TradingConfig.Signals[signal.Side].Trend, out string reaction))
            {
                eventText.Add(reaction);
                signal.IsInvalid = true;
            }


            // Filter op de markettrend waarvan je wil dat die qua percentage bullisch of bearisch zijn
            if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TradingConfig.Signals[signal.Side].MarketTrend, out reaction))
            {
                eventText.Add(reaction);
                signal.IsInvalid = true;
            }


            // En aanvullend specifiek voro de STOBB ook een controle op de markettrend
            if (signal.Strategy == CryptoSignalStrategy.Stobb)
            {
                if (signal.Side == CryptoTradeSide.Long && GlobalData.Settings.Signal.Stobb.TrendLong > -99m && (decimal)signal.TrendPercentage < GlobalData.Settings.Signal.Stobb.TrendLong)
                {
                    eventText.Add("de trend% is te laag");
                    signal.IsInvalid = true;
                }
                // Die -99 begint verwarrend te werken
                if (signal.Side == CryptoTradeSide.Short && GlobalData.Settings.Signal.Stobb.TrendShort > -99m && (decimal)signal.TrendPercentage > GlobalData.Settings.Signal.Stobb.TrendShort)
                {
                    eventText.Add("de trend% is te hoog");
                    signal.IsInvalid = true;
                }
            }
        }

        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;


        signal.EventText = string.Join(", ", eventText);
        try
        {
            // Bied het aan het monitorings systeem (indien aangevinkt) 
            // (lagere intervallen hebben hogere prioriteit - via EventTime, klopt dat?)
            // We gebruiken (nog) geen exit signalen, echter dat zou best realistisch zijn voor de toekomst
            if (!signal.IsInvalid && GlobalData.Settings.Trading.Active)
            {
                if (TradingConfig.Trading[signal.Side].IntervalPeriod.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    if (TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
                    {
                        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
                        if (symbolInterval.Signal == null || symbolInterval.Signal?.EventTime != signal.EventTime)
                        {
                            if (symbolInterval.Signal == null || algorithm.ReplaceSignal)
                            {
                                symbolInterval.Signal = signal;
                                CreatedSignal = true;
                            }
                        }
                    }
                }
            }

            // Signal naar database wegschrijven (niet echt noodzakelijk, we doen er later niets mee)
            if (!signal.BackTest)
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Open();
                using var transaction = databaseThread.BeginTransaction();
                {
                    databaseThread.Connection.Insert<CryptoSignal>(signal, transaction);
                    transaction.Commit();
                }
            }

            GlobalData.AnalyzeSignalCreated?.Invoke(signal);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);
            return false;
        }

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
            Side = CryptoTradeSide.Long,  // gets modified later
            Strategy = CryptoSignalStrategy.Jump,  // gets modified later
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
        SignalCreateBase algorithm;
        if (Side == CryptoTradeSide.Long)
            algorithm = strategyDefinition.InstantiateAnalyzeLong(Symbol, Interval, Candle);
        else
            algorithm = strategyDefinition.InstantiateAnalyzeShort(Symbol, Interval, Candle);

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
        if (!HasOpenPosition() && !GlobalData.BackTest && !Symbol.CheckValidMinimalVolume(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalVolume)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }

        // Is de prijs binnen de gestelde grenzen
        if (!HasOpenPosition() && !GlobalData.BackTest && !Symbol.CheckValidMinimalPrice(out response))
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



    public void Analyze(long candleOpenTime)
    {
        // Eenmalig de indicators klaarzetten
        if (Prepare(candleOpenTime))
        {
            // TODO: opnieuw activeren en controleren of het idee klopt:

            // Indien we ongeldige signalen laten zien moeten we deze controle overslagen.
            // (verderop in het process wordt alsnog hierop gecontroleerd)

            //if (!GlobalData.Settings.General.ShowInvalidSignals && !BackTest)
            //{
            // Dan kunnen we direct de controles hier afkappen (scheelt wat cpu)
            // Weer een extra controle, staat de symbol op de black of whitelist?
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
            foreach (CryptoSignalStrategy strategy in TradingConfig.Trading[Side].StrategySbmStob.ToList())
            {
                if (SignalHelper.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition strategyDefinition))
                {
                    if (ExecuteAlgorithm(strategyDefinition))
                        break;
                }
            }

            // En de overige waaronder de jump
            foreach (CryptoSignalStrategy strategy in TradingConfig.Trading[Side].StrategyOthers.ToList())
            {
                if (SignalHelper.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition strategyDefinition))
                {
                    ExecuteAlgorithm(strategyDefinition);
                }
            }

        }
    }

}