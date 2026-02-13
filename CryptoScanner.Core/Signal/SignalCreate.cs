using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicator;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal;

public delegate void AnalyseEvent(CryptoSignal signal);

public class SignalCreate
{
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private CryptoTradeSide Side { get; set; }

    public CryptoCandle? Candle { get; set; }
    public List<CryptoCandle>? History { get; set; }
    public List<CryptoSignal> SignalList { get; set; } = [];

    public SignalCreate(CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side)
    {
        Symbol = symbol;
        Interval = interval;
        Side = side;
    }

    private static void CalculateAdditionalSignalProperties(CryptoSignal signal, 
        List<CryptoCandle> history, int candleCount)
    {
        CandleTime unixFrom = CandleTime.MinValue;

        // dit zou ook bij het verzamelen van de History lijst kunnen (scheelt een iteratie)
        double AvgBB = 0;
        int AvgBBCount = 0;
        int candlesWithFlatPrice = 0;
        int candlesWithZeroVolume = 0;
        int countBollingerBandSma = 0;
        int countBollingerBand = 0;


        int iterations = 0;
        CryptoCandle? prevCandle, CandleLast = null;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            prevCandle = CandleLast;
            CandleLast = history[i];

            // Voor de backtest, pas tellen vanaf het moment dat het nodig is
            if (unixFrom > 0 && CandleLast.OpenTime > unixFrom)
                continue;
            if (CandleLast.CandleData == null || CandleLast.CandleData?.BollingerBandsPercentage == null)
                continue;

            AvgBBCount++;
            AvgBB += (double)CandleLast.CandleData?.BollingerBandsPercentage!;

            // Aantal candles die vlak zijn (geen beweging)
            if (CandleLast.Close == CandleLast.Open && CandleLast.Close == CandleLast.High && CandleLast.Close == CandleLast.Low)
                candlesWithFlatPrice++;

            // Aantal candles zonder volume (geen enkele trades)
            if (CandleLast.Volume <= 0)
                candlesWithZeroVolume++;

            // Hievoor moet dus wel de laatste x candlesdata gevuld zijn (dat is niet het geval!!!!)
            if (CandleLast.CandleData != null && CandleLast.CandleData.BollingerBandsDeviation != null)
            {
                // Hoe vaak komt de prijs boven/onder de BB
                if (prevCandle != null && prevCandle.CandleData != null && prevCandle.CandleData.BollingerBandsDeviation != null)
                {
                    // Minpuntje voor beide: als we direct boven de sma of upper zitten dan wordt dat niet geregistreerd
                    // Registreer de wisseling van onder naar boven de sma/upper of lower
                    // (dit is geen briljante berekening, we tellen het aantal crossings)
                    // Dat zou het aantal keer boven de sma moeten zijn ()
                    if (signal.Side == CryptoTradeSide.Long)
                    {
                        decimal prevMax = Math.Max(prevCandle.Open, prevCandle.Close);
                        decimal lastMax = Math.Max(CandleLast.Open, CandleLast.Close);
                        if (lastMax >= (decimal?)CandleLast.CandleData.Sma20 && prevMax < (decimal?)prevCandle.CandleData.Sma20)
                            countBollingerBandSma++;
                        if (lastMax >= (decimal?)CandleLast.CandleData.BollingerBandsUpperBand && prevMax < (decimal?)prevCandle.CandleData.BollingerBandsUpperBand)
                            countBollingerBand++;
                    }
                    else
                    {
                        decimal prevMin = Math.Min(prevCandle.Open, prevCandle.Close);
                        decimal lastMin = Math.Min(CandleLast.Open, CandleLast.Close);
                        if (lastMin <= (decimal?)CandleLast.CandleData.Sma20 && prevMin > (decimal?)prevCandle.CandleData.Sma20)
                            countBollingerBandSma++;
                        if (lastMin <= (decimal?)CandleLast.CandleData.BollingerBandsLowerBand && prevMin > (decimal?)prevCandle.CandleData.BollingerBandsLowerBand)
                            countBollingerBand++;
                    }
                }
            }
            else
            {
                // Toch maar even melden, want dit is niet normaal..
                GlobalData.AddTextToLogTab($"Analyse {signal.Symbol.Name} {CandleLast.DateLocal} {CandleLast.Close:N8} iteration={iterations} heeft geen candledata of geen BB?");
            }

            iterations++;
            if (iterations >= candleCount)
                break;
        }

        if (AvgBBCount > 0)
            signal.AvgBB = AvgBB / AvgBBCount;
        else
            signal.AvgBB = 0;
        signal.CandlesWithFlatPrice = candlesWithFlatPrice;
        signal.CandlesWithZeroVolume = candlesWithZeroVolume;
        signal.AboveBollingerBandsSma = countBollingerBandSma;
        signal.AboveBollingerBandsUpper = countBollingerBand;
    }


    private static bool CheckAdditionalAlarmProperties(CryptoSignal signal, out string reaction)
    {
        // --------------------------------------------------------------------------------
        // Van de laatste 60 candles mogen er maximaal 16 geen volume hebben.
        // (dit op aanranden van zowel Roelf als Helga). Er moet wat te "beleven" zijn
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.Signal.CandlesWithZeroVolumeCheck)
        {
            if (GlobalData.Settings.Signal.CandlesWithZeroVolume > 0 && signal.CandlesWithZeroVolume > GlobalData.Settings.Signal.CandlesWithZeroVolume)
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
            if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && signal.CandlesWithFlatPrice > GlobalData.Settings.Signal.CandlesWithFlatPrice)
            {
                reaction = string.Format("teveel platte candles ({0} van 60 candles)", signal.CandlesWithFlatPrice);
                return false;
            }
        }


        // Er moet een beetje beweging in de BB zitten (niet enkel op de onderste bb ofzo)
        if (GlobalData.Settings.Signal.AboveBollingerBandsSmaCheck)
        {
            if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
            {
                reaction = string.Format("te weinig candles die boven de BB.Sma uitsteken ({0} van 60 candles)", signal.AboveBollingerBandsSma);
                return false;
            }
        }


        // Vervolg op voorgaande wens op beweging in de BB (met het liefst een aantal uitschieters)
        if (GlobalData.Settings.Signal.AboveBollingerBandsUpperCheck)
        {
            if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
            {
                reaction = string.Format("te weinig candles die boven de BB.Upper uitsteken ({0} van 60 candles)", signal.AboveBollingerBandsUpper);
                return false;
            }
        }


        reaction = "";
        return true;
    }


    private double CalculateLastPeriodsInInterval(long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(om de berekeningen allemaal wat sneller te maken)
        // CandleList contains normally about 1 day of candles

        CandleTime openTime = Candle!.OpenTime; // Note: backtest, alway's take the signal candle 
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (!symbolInterval.CandleList.TryGetValue(openTime - interval, out CryptoCandle? candlePrev))
            candlePrev = symbolInterval.CandleList.Values.First(); // better than zero of null (approx)

        double closeLast = (double)Candle!.Close;
        double closePrev = (double)candlePrev.Close;
        double diff = closeLast - closePrev;

        if (!closePrev.Equals(0))
            return 100.0 * (diff / closePrev);
        else return 0;
    }


    private double CalculateMaxMovementInInterval(decimal? lastPrice, CandleTime startTime, 
        CryptoIntervalPeriod intervalPeriod, long candleCount)
    {
        if (lastPrice == null)
            return 0;

        decimal min = lastPrice.Value;
        decimal max = lastPrice.Value;

        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(intervalPeriod);
        CandleTime unix = startTime.AlignToIntervalMinutes(symbolInterval.Interval.Duration);

        while (candleCount-- > 0)
        {
            if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
            {
                if (candle.Low < min)
                    min = candle.Low;

                if (candle.High > max)
                    max = candle.High;
            }

            unix -= symbolInterval.Interval.Duration;
        }

        decimal diff = max - min;
        if (!max.Equals(0))
            return (double)(100.0m * (diff / max));
        else
            return 0;
    }



    private async Task<bool> PrepareAndSendSignalAsync(SignalCreateBase algorithm)
    {
        CryptoSignal signal = CreateSignal(Candle!);
        signal.Side = algorithm.SignalSide;
        signal.Strategy = algorithm.SignalStrategy;

        List<string> eventText = [];
        if (algorithm.ExtraText != "")
            eventText.Add(algorithm.ExtraText);


        // Extra attributen erbij halen (dat lukt niet bij een backtest vanwege het ontbreken van een "History list")
        CalculateAdditionalSignalProperties(signal, History!, 60);
        if (!CheckAdditionalAlarmProperties(signal, out string response))
        {
            eventText.Add(response);
            signal.IsInvalid = true;
        }


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        if (!algorithm.AdditionalChecks(Candle!, out response))
        {
            eventText.Add(response);
            signal.IsInvalid = true;
        }

        // Extra controles, staat de symbol op de blacklist?
        if (TradingConfig.Signals[signal.Side].InBlackList(Symbol.Name) == MatchBlackAndWhiteList.Present)
        {
            // Als de muntpaar op de black lijst staat dan dit signaal overslagen
            eventText.Add("blacklisted");
            signal.IsInvalid = true;
        }

        // Extra controles, staat de symbol op de whitelist?
        if (TradingConfig.Signals[signal.Side].InWhiteList(Symbol.Name) == MatchBlackAndWhiteList.NotPresent)
        {
            // Als de muntpaar niet in de white lijst staat dan dit signaal overslagen
            eventText.Add("not whitelisted");
            signal.IsInvalid = true;
        }

        // Barometers
        CryptoBarometerData barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval15m);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer15m = barometerData.PriceBarometer.Value;
        else
            signal.Barometer15m = null;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval30m);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer30m = barometerData.PriceBarometer.Value;
        else
            signal.Barometer30m = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval1h);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer1h = barometerData.PriceBarometer.Value;
        else
            signal.Barometer1h = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval4h);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer4h = barometerData.PriceBarometer.Value;
        else
            signal.Barometer4h = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval1d);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer1d = barometerData.PriceBarometer.Value;
        else
            signal.Barometer1d = 0;


        // de 24 change moet in een bepaald interval zitten
        signal.Last24HoursChange = CalculateLastPeriodsInInterval(24 * 60);
        if (!signal.Last24HoursChange.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
            {
                string text = string.Format("Analyse {0} 1d change {1} not between {2} .. {3}", Symbol.Name, signal.Last24HoursChange.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("1d change% to high");
            signal.IsInvalid = true;
        }


        // Check the % effective over multiple day's
        int countInInterval4H = GlobalData.Settings.Signal.AnalysisEffectiveDays * 6;
        signal.LastXDaysEffective = CalculateMaxMovementInInterval(Symbol.LastPrice, CandleTime.AlignFromDateTime(signal.CloseDate, 1), CryptoIntervalPeriod.interval4h, countInInterval4H);
        if (!signal.LastXDaysEffective.IsBetween(0, GlobalData.Settings.Signal.AnalysisEffectivePercentage))
        {
            if (GlobalData.Settings.Signal.AnalysisMaxEffectiveLog)
            {
                string text = $"Analyse {Symbol.Name} {GlobalData.Settings.Signal.AnalysisEffectiveDays}d change effective {signal.LastXDaysEffective:N2} not between 0 .. {GlobalData.Settings.Signal.AnalysisEffectivePercentage:N2}";
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add($"{GlobalData.Settings.Signal.AnalysisEffectiveDays}d effective% to high");
            signal.IsInvalid = true;
        }


        // Check "Barcode" charts
        decimal barcodePercentage = 100 * Symbol.PriceTickSize / Symbol.LastPrice ?? 0;
        if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
        {
            // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
            if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
                GlobalData.AddTextToLogTab($"Analyse {Symbol.Name} De tick size percentage is te hoog {barcodePercentage:N3}");
            eventText.Add("tick perc to high");
            signal.IsInvalid = true;
        }

        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;



        // Iets wat ik wel eens gebruikt als ik trade
        LuxIndicator.Calculate(Symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod.interval5m, Candle!.OpenTime + Interval.Duration);
        if (signal.Side == CryptoTradeSide.Long)
            signal.LuxIndicator5m = luxOverSold;
        else
            signal.LuxIndicator5m = luxOverBought;



        // Calculate MarketTrend and the individual interval trends (reasonably CPU heavy and that is why it is on the end of the routine)
        _ = await MarketTrend.CalculateMarketTrendAsync(signal.Symbol, GlobalData.Settings.Trend.Primary);
        if (signal.Symbol.Data.TrendPrimary.Percentage.HasValue)
        {
            signal.TrendPercentagePrimary = (float)signal.Symbol.Data.TrendPrimary.Percentage!;
            signal.TrendInterval = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod).TrendPrimary.Trend;
            signal.Trend15m = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval15m).TrendPrimary.Trend;
            signal.Trend30m = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval30m).TrendPrimary.Trend;
            signal.Trend1h = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).TrendPrimary.Trend;
            signal.Trend4h = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval4h).TrendPrimary.Trend;
            signal.Trend1d = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).TrendPrimary.Trend;
        }

        // This is for comparison only
        _ = await MarketTrend.CalculateMarketTrendAsync(signal.Symbol, GlobalData.Settings.Trend.Secondary);
        if (signal.Symbol.Data.TrendSecondary.Percentage.HasValue)
            signal.TrendPercentageSecondary = (float)signal.Symbol.Data.TrendSecondary.Percentage!;


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        // Filter op bepaalde intervallen waarvan je wil dat die bullisch of bearisch zijn
        if (!PositionTools.ValidTrendConditions(signal.Symbol, TrendType.Primary, TradingConfig.Signals[signal.Side].Trend, out string reaction))
        {
            eventText.Add(reaction);
            signal.IsInvalid = true;
        }


        // Filter op de markettrend waarvan je wil dat die qua percentage bullisch of bearisch zijn
        if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TrendType.Primary, TradingConfig.Signals[signal.Side].MarketTrend, out reaction))
        {
            eventText.Add(reaction);
            signal.IsInvalid = true;
        }


        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;


        signal.EventText = string.Join(", ", eventText);
        try
        {
            // Add it to the monitorings system (if active) 
            // (lagere intervallen hebben hogere prioriteit - via EventTime, klopt dat?)
            // We gebruiken (nog) geen exit signalen, echter dat zou best realistisch zijn voor de toekomst
            if (!signal.IsInvalid && GlobalData.Settings.Trading.Active)
            {
                if (TradingConfig.Trading[signal.Side].IntervalPeriod.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    if (TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
                    {
                        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
                        {
                            SignalList.Add(signal);
                            symbolInterval.SignalList.Add(signal);
                        }
                    }
                }
            }


            GlobalData.ThreadSaveObjects!.AddToQueue(signal);
            GlobalData.AnalyzeSignalCreated?.Invoke(signal);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
            return false;
        }

        if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            GlobalData.AddTextToLogTab($"Debug Signal created {Symbol.Name} {Interval.Name} {signal.StrategyText} {signal.Side}");

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
            SignalPrice = candle.Close,
            PriceMin = candle.Close, // statistics
            PriceMax = candle.Close, // statistics
            PriceMinPerc = 0, // statistics
            PriceMaxPerc = 0, // statistics
            SignalStatus = CryptoSignalStatus.Run,
            SignalVolume = Symbol.Volume,
            Side = CryptoTradeSide.Long,  // gets modified later
            Strategy = CryptoSignalStrategy.Jump,  // gets modified later
            OpenDate = candle.OpenTime.ToDateTime(),
            CloseDate = candle.OpenTime.ToDateTime().AddMinutes(Interval.Duration),
            EventTime = 12345, //(candle.OpenTime + Interval.Duration).ToUnixSeconds(), // close of the candle
        };

        //signal.CloseDate = signal.OpenDate.AddMinutes(Interval.Duration);
        signal.ExpirationDate = signal.GetExpirationDate(Interval);

        // Copy common indicator values
        signal.AssignValues(candle.CandleData!);
        return signal;
    }


    private void AddToLiveData()
    {
        if (!GlobalData.LiveDataQueueAdded.TryGetValue((Symbol.Name, Interval.IntervalPeriod), out CryptoLiveData? liveData))
        {
            if (Monitor.TryEnter(GlobalData.LiveDataQueue))
            {
                try
                {
                    liveData = new()
                    {
                        Symbol = Symbol,
                        Interval = Interval,
                        Candle = Candle!,
                    };
                    GlobalData.LiveDataQueue.Enqueue(liveData);
                    GlobalData.LiveDataQueueAdded.TryAdd((Symbol.Name, Interval.IntervalPeriod), liveData);
                }
                finally
                {
                    Monitor.Exit(GlobalData.LiveDataQueue);
                }
            }
        }
        else
        {
            liveData.Candle = Candle!;
        }
    }

    public async Task<bool> ExecuteAlgorithmAsync(AlgorithmDefinition strategyDefinition)
    {
        SignalCreateBase? algorithm = RegisterAlgorithms.GetAlgorithm(Side, strategyDefinition.Strategy, Symbol, Interval, Candle!);
        if (algorithm != null)
        {
            AddToLiveData();

            if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                GlobalData.AddTextToLogTab($"Debug Signal create {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            //GlobalData.AddTextToLogTab($"SignalCreate.Done {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            if (algorithm.IndicatorsOkay(Candle!) && algorithm.IsSignal())
                return await PrepareAndSendSignalAsync(algorithm);
        }
        return false;
    }


    //private bool CheckSymbol(long candleOpenTime, bool zones)
    //{
    //    if (!Symbol.LastPrice.HasValue)
    //    {
    //        // LastPrice is filled in the price tickers, but can be delayed..
    //        GlobalData.AddTextToLogTab($"Analyse {Symbol.Name} No last price available");
    //        return false;
    //    }


    //    // Is the volume within a certain minimal limit
    //    if (!Symbol.CheckValidMinimalVolume(zones, candleOpenTime, Interval.Duration, out string response))
    //    {
    //        if (GlobalData.Settings.Signal.LogMinimalVolume)
    //            GlobalData.AddTextToLogTab("Analyse " + response);
    //        if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //            GlobalData.AddTextToLogTab("Analyse " + response);

    //        return false;
    //    }

    //    // Is the price within a certain minimal limit
    //    if (!Symbol.CheckValidMinimalPrice(out response))
    //    {
    //        if (GlobalData.Settings.Signal.LogMinimalPrice)
    //            GlobalData.AddTextToLogTab("Analyse " + response);
    //        if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //            GlobalData.AddTextToLogTab("Analyse " + response);
    //        return false;
    //    }
    //    return true;
    //}


    //    /// <summary>
    //    /// Zet de de laatste x candles op een rijtje en bereken de indicators
    //    /// </summary>
    //    /// <param name="candleOpenTime"></param>
    //    /// <returns></returns>
    //    private bool PrepareIndicators(long candleOpenTime)
    //    {
    //        //GlobalData.Logger.Trace($"SignalCreate.PrepareIndicators.Start {Symbol.Name} {Interval.Name} {Side}");

    //        Candle = null;
    //        string response = "";

    //        // TODO: Avoid the CollectCandles if we can by checking the last candle.CandleData
    //        //CryptoSymbolInterval symbolPeriod = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
    //        //CryptoCandleList intervalCandles = symbolPeriod.CandleList;
    //        //long candleEndTime = candleOpenTime - candleOpenTime % Interval.Duration;


    //        // Build a list of candles
    //        History ??= CandleIndicatorData.CollectCandles(Symbol, Interval, candleOpenTime, out response);
    //        if (History == null)
    //        {
    //#if DEBUG
    //            //if (GlobalData.Settings.Signal.LogNotEnoughCandles)
    //            GlobalData.AddTextToLogTab($"Analyse {response}");
    //#endif
    //            return false;
    //        }

    //        // Eenmalig de indicators klaarzetten
    //        Candle = History[^1];
    //        if (Candle.CandleData == null)
    //            CandleIndicatorData.CalculateIndicators(Symbol, Interval, History);

    //        //GlobalData.Logger.Trace($"SignalCreate.PrepareIndicators.Stop {Symbol.Name} {Interval.Name} {Side}");
    //        return true;
    //    }



    //public async Task<bool> AnalyzeAsync(long candleIntervalOpenTime)
    //{
    //    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //        GlobalData.AddTextToLogTab($"Debug Signal create {Symbol.Name} {Interval.Name} {Side}");
    //    //ScannerLog.Logger.Trace($"SignalCreate.Start {Symbol.Name} {Interval.Name}");
    //    //GlobalData.AddTextToLogTab($"SignalCreate.Start {Symbol.Name} {Interval.Name} {Side}");

    //    if (CheckSymbol(candleIntervalOpenTime, false)) // && PrepareIndicators(candleIntervalOpenTime))
    //    {
    //        if (!GlobalData.LiveDataQueueAdded.TryGetValue((Symbol.Name, Interval.IntervalPeriod), out CryptoLiveData? liveData))
    //        {
    //            if (Monitor.TryEnter(GlobalData.LiveDataQueue))
    //            {
    //                try
    //                {
    //                    liveData = new()
    //                    {
    //                        Symbol = this.Symbol,
    //                        Interval = this.Interval,
    //                        Candle = Candle!,
    //                    };
    //                    GlobalData.LiveDataQueue.Enqueue(liveData);
    //                    GlobalData.LiveDataQueueAdded.TryAdd((Symbol.Name, Interval.IntervalPeriod), liveData);
    //                }
    //                finally
    //                {
    //                    Monitor.Exit(GlobalData.LiveDataQueue);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            liveData.Candle = Candle!;
    //        }


    //        foreach (CryptoSignalStrategy strategy in TradingConfig.Signals[Side].Strategy.Keys.ToList())
    //        {
    //            if (RegisterAlgorithms.GetAlgorithm(strategy, out AlgorithmDefinition? strategyDefinition))
    //            {
    //                if (await ExecuteAlgorithmAsync(strategyDefinition!))
    //                    break;
    //            }
    //        }



    //    }
    //    //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name}");
    //    return SignalList.Count > 0;
    //}



    //public async Task<bool> AnalyzeZonesAsync(long candleIntervalOpenTime)
    //{
    //    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //        GlobalData.AddTextToLogTab($"Debug Signal create {Symbol.Name} {Interval.Name} {Side} dlz zones");
    //    //ScannerLog.Logger.Trace($"SignalCreate.Start {Symbol.Name} {Interval.Name} zones");
    //    //GlobalData.AddTextToLogTab($"SignalCreate.Start {Symbol.Name} {Interval.Name} {Side} zones");

    //    if (CheckSymbol(candleIntervalOpenTime, true) && PrepareIndicators(candleIntervalOpenTime))
    //    {
    //        if (RegisterAlgorithms.AlgorithmDefinitionList.TryGetValue(CryptoSignalStrategy.DominantLevel, out AlgorithmDefinition? algorithmDefinition))
    //            await ExecuteAlgorithmAsync(algorithmDefinition!);

    //        if (RegisterAlgorithms.AlgorithmDefinitionList.TryGetValue(CryptoSignalStrategy.DominantLevelNear, out AlgorithmDefinition? algorithmDefinitionNear))
    //            await ExecuteAlgorithmAsync(algorithmDefinitionNear!);
    //    }
    //    //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name} zones");
    //    return SignalList.Count > 0;
    //}


    //public async Task<bool> AnalyzeFairValueGapAsync(long candleIntervalOpenTime)
    //{
    //    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //        GlobalData.AddTextToLogTab($"Debug Signal create {Symbol.Name} {Interval.Name} {Side} fvg zones");
    //    //ScannerLog.Logger.Trace($"SignalCreate.Start {Symbol.Name} {Interval.Name} zones");
    //    //GlobalData.AddTextToLogTab($"SignalCreate.Start {Symbol.Name} {Interval.Name} {Side} zones");

    //    if (CheckSymbol(candleIntervalOpenTime, true) && PrepareIndicators(candleIntervalOpenTime))
    //    {
    //        if (RegisterAlgorithms.AlgorithmDefinitionList.TryGetValue(CryptoSignalStrategy.FairValueGap, out AlgorithmDefinition? algorithmDefinition))
    //        {
    //            await ExecuteAlgorithmAsync(algorithmDefinition!);
    //            //await MarketTrend.CalculateMarketTrendAsync(GlobalData.ActiveAccount!, symbol, 0, 0);
    //        }
    //    }
    //    //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name} zones");
    //    return SignalList.Count > 0;
    //}
}