using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal;

public delegate void AnalyseEvent(CryptoSignal signal);

public class SignalCreate
{
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoTradeSide Side { get; set; }

    // The last candle (in the requested interval)
    public required CryptoCandle Candle { get; set; }
    public required CryptoData CandleData { get; set; }

    // Prepared indicator data
    public required CryptoIndicatorData IndicatorData { get; set; }
    public required CryptoIndicatorDataList IndicatorDataList { get; set; }

    // output
    //public List<CryptoSignal> SignalList { get; set; } = [];


    private void CalculateAdditionalSignalProperties(CryptoSignal signal, int candleCount)
    {
        var symbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);

        double AvgBB = 0;
        short AvgBBCount = 0;
        short countBollingerBand = 0;
        short candlesWithFlatPrice = 0;
        short candlesWithZeroVolume = 0;
        short countBollingerBandSma = 0;

        CandleTime loopFrom = CandleTime.FromDateTime(signal.CloseDate);

        MyData? prevCandle = null;
        while (candleCount-- > 0)
        {
            if (IndicatorData.TryGetCandle(loopFrom, out MyData? candleLast))
            {
                // This was for the backtest.. need to reinstate is somewhere down the line
                //if (unixFrom > 0 && candleLast.OpenTime > unixFrom)
                //    continue;
                if (candleLast == null || candleLast.CandleData?.BollingerBandsPercentage == null)
                    continue;

                AvgBBCount++;
                AvgBB += (double)candleLast.CandleData?.BollingerBandsPercentage!;

                // Aantal candles die vlak zijn (geen beweging)
                if (candleLast.Candle.Close == candleLast.Candle.Open && candleLast.Candle.Close == candleLast.Candle.High
                    && candleLast.Candle.Close == candleLast.Candle.Low)
                    candlesWithFlatPrice++;

                // Aantal candles zonder volume (geen enkele trades)
                if (candleLast.Candle.Volume <= 0)
                    candlesWithZeroVolume++;

                // Hievoor moet dus wel de laatste x candlesdata gevuld zijn (dat is niet het geval!!!!)
                if (candleLast.CandleData.BollingerBandsDeviation != null)
                {
                    // Hoe vaak komt de prijs boven/onder de BB
                    if (prevCandle != null && prevCandle.CandleData.BollingerBandsDeviation != null)
                    {
                        // Minpuntje voor beide: als we direct boven de sma of upper zitten dan wordt dat niet geregistreerd
                        // Registreer de wisseling van onder naar boven de sma/upper of lower
                        // (dit is geen briljante berekening, we tellen het aantal crossings)
                        // Dat zou het aantal keer boven de sma moeten zijn ()
                        if (signal.Side == CryptoTradeSide.Long)
                        {
                            decimal prevMax = Math.Max(prevCandle.Candle.Open, prevCandle.Candle.Close);
                            decimal lastMax = Math.Max(candleLast.Candle.Open, candleLast.Candle.Close);
                            if (lastMax >= (decimal?)candleLast.CandleData.Sma20 && prevMax < (decimal?)prevCandle.CandleData.Sma20)
                                countBollingerBandSma++;
                            if (lastMax >= (decimal?)candleLast.CandleData.BollingerBandsUpperBand && prevMax < (decimal?)prevCandle.CandleData.BollingerBandsUpperBand)
                                countBollingerBand++;
                        }
                        else
                        {
                            decimal prevMin = Math.Min(prevCandle.Candle.Open, prevCandle.Candle.Close);
                            decimal lastMin = Math.Min(candleLast.Candle.Open, candleLast.Candle.Close);
                            if (lastMin <= (decimal?)candleLast.CandleData.Sma20 && prevMin > (decimal?)prevCandle.CandleData.Sma20)
                                countBollingerBandSma++;
                            if (lastMin <= (decimal?)candleLast.CandleData.BollingerBandsLowerBand && prevMin > (decimal?)prevCandle.CandleData.BollingerBandsLowerBand)
                                countBollingerBand++;
                        }
                    }
                }
                //else
                //{
                // Toch maar even melden, want dit is niet normaal..
                //GlobalData.AddTextToLogTab($"Analyse {signal.Symbol.Name} {candleLast.DateLocal} {candleLast.Close:N8} iteration={iterations} heeft geen candledata of geen BB?");
                //}
                prevCandle = candleLast;
            }
            loopFrom -= Interval.Duration;
        }

        if (AvgBBCount > 0)
            signal.AvgBB = (float)(AvgBB / AvgBBCount);
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


    private float CalculateLastPeriodsInInterval(long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(om de berekeningen allemaal wat sneller te maken)
        // CandleList contains normally about 1 day of candles

        CandleTime openTime = Candle.OpenTime; // Note: backtest, alway's take the signal candle
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (!symbolInterval.CandleList.TryGetValue(openTime - interval, out CryptoCandle candlePrev))
            symbolInterval.CandleList.TryGetFirstCandle(out candlePrev); // better than zero or null (approx)

        double closeLast = (double)Candle.Close;
        double closePrev = (double)candlePrev!.Close;
        double diff = closeLast - closePrev;

        if (!closePrev.Equals(0))
            return (float)(100.0 * (diff / closePrev));
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
            if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle candle))
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
        CryptoSignal signal = CreateSignal(Candle);
        signal.Side = algorithm.SignalSide;
        signal.Strategy = algorithm.SignalStrategy;
        // Might be different?
        signal.Interval = algorithm.Interval;
        signal.IntervalId = algorithm.Interval.Id;

        // Algorithms that detect events on an earlier candle (e.g. BOS/CHoCH swing break)
        // can report the actual event price here so SignalPrice reflects the break, not
        // the close of the candle on which the check happened to run.
        if (algorithm.OverrideSignalPrice is decimal overridePrice)
            signal.SignalPrice = overridePrice;

        // Strategies that anchor SL/TP on structural levels (swing high/low, BB band, RRR target)
        // report their proposed prices here. PositionTools.AddSignalProperties copies them onto
        // the resulting position, where PositionMonitor.CalculateTpPrices picks them up.
        signal.SlPrice = algorithm.OverrideSlPrice;
        signal.TpPrice = algorithm.OverrideTpPrice;

        List<string> eventText = [];
        if (algorithm.ExtraText != "")
            eventText.Add(algorithm.ExtraText);


        // Extra attributen erbij halen (dat lukt niet bij een backtest vanwege het ontbreken van een "History list")
        CalculateAdditionalSignalProperties(signal, 60);
        if (!CheckAdditionalAlarmProperties(signal, out string response))
        {
            eventText.Add(response);
            signal.IsInvalid = true;
        }


        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        MyData myData = new() { Candle = this.Candle, CandleData = this.CandleData };
        if (!algorithm.AdditionalChecks(myData, out response))
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
            signal.Barometer15m = (float)barometerData.PriceBarometer.Value;
        else
            signal.Barometer15m = null;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval30m);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer30m = (float)barometerData.PriceBarometer.Value;
        else
            signal.Barometer30m = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval1h);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer1h = (float)barometerData.PriceBarometer.Value;
        else
            signal.Barometer1h = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval4h);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer4h = (float)barometerData.PriceBarometer.Value;
        else
            signal.Barometer4h = 0;

        barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(Symbol.Quote, CryptoIntervalPeriod.interval1d);
        if (barometerData.PriceBarometer.HasValue)
            signal.Barometer1d = (float)barometerData.PriceBarometer.Value;
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
        signal.LastXDaysEffective = (float)CalculateMaxMovementInInterval(Symbol.LastPrice, CandleTime.AlignFromDateTime(signal.CloseDate, 1), CryptoIntervalPeriod.interval4h, countInInterval4H);
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
        decimal barcodePercentage = Symbol.LastPrice is > 0
            ? 100 * Symbol.PriceTickSize / Symbol.LastPrice.Value
            : 0;
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
        if (signal.LuxIndicator5m == null)
        {
            LuxIndicator.Calculate(Symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod.interval5m, Candle!.OpenTime + 5);
            if (signal.Side == CryptoTradeSide.Long)
                signal.LuxIndicator5m = luxOverSold;
            else
                signal.LuxIndicator5m = luxOverBought;
        }



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
        if (!PositionTools.ValidTrendConditions(signal.Symbol, signal.Interval, TrendType.Primary, TradingConfig.Signals[signal.Side].Trend, out string reaction))
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

        // Additional INTERSECT filter on the secondary market trend (lower-timeframe scope).
        // Allows catching divergences such as Primary +100 / Secondary -63 where the lower
        // timeframe has already rolled over.
        if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TrendType.Secondary, TradingConfig.Signals[signal.Side].MarketTrendSecondary, out reaction))
        {
            eventText.Add(reaction);
            signal.IsInvalid = true;
        }


        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;


        signal.EventText = string.Join(", ", eventText);
        try
        {
            // Pass it into the monitorings system (if trading)
            // (lower intervals have higher priority - via EventTime?)
            // We dont use (nog) any exit signals, but that can be done as wll (somewhere in the future)
            if (!signal.IsInvalid && GlobalData.Settings.Trading.Active)
            {
                if (TradingConfig.Trading[signal.Side].IntervalPeriod.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    if (TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
                    {
                        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
                        symbolInterval.SignalList.Add(signal);
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
            ScannerLog.Logger.Info($"Debug Signal created {Symbol.Name} {Interval.Name} {signal.StrategyText} {signal.Side}");

        return true;
    }


    private CryptoSignal CreateSignal(CryptoCandle candle)
    {
        CryptoSignal signal = new()
        {
            Exchange = Symbol.Exchange,
            ExchangeId = Symbol.ExchangeId,
            Symbol = Symbol,
            SymbolId = Symbol.Id,
            Interval = Interval,
            IntervalId = Interval.Id,
            Candle = candle,
            BackTest = GlobalData.IsEmulatorMode,
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
        };

        //signal.CloseDate = signal.OpenDate.AddMinutes(Interval.Duration);
        signal.ExpirationDate = signal.GetExpirationDate(Interval);

        // Copy common indicator values
        signal.AssignValues(CandleData);
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
                    if (IndicatorData.Data.TryGetValue(Candle.OpenTime, out CryptoData? candleData))
                    {
                        //public CandleIndicatorData? candleLastData { get; set; }

                        liveData = new()
                        {
                            Symbol = Symbol,
                            Interval = Interval,
                            Candle = Candle,
                            CandleData = candleData,
                        };
                        GlobalData.LiveDataQueue.Enqueue(liveData);
                        GlobalData.LiveDataQueueAdded.TryAdd((Symbol.Name, Interval.IntervalPeriod), liveData);
                    }
                    //else
                    //{
                    //    if (liveData != null)
                    //    {
                    //        GlobalData.LiveDataQueueAdded.Remove((Symbol.Name, Interval.IntervalPeriod));
                    //    }
                    //}
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
        SignalCreateBase? algorithm = RegisterAlgorithms.GetAlgorithm(Side, strategyDefinition.Strategy);
        if (algorithm != null)
        {
            MyData myData = new() { Candle = IndicatorData.LastCandle, CandleData = IndicatorData.LastCandleData };
            algorithm.Symbol = Symbol;
            algorithm.Interval = Interval;
            algorithm.SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
            algorithm.CandleLast = myData;
            algorithm.IndicatorData = IndicatorData;
            algorithm.IndicatorDataList = IndicatorDataList;

            AddToLiveData();

            if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                ScannerLog.Logger.Info($"Debug Signal create {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            //GlobalData.AddTextToLogTab($"SignalCreate.Done {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            if (algorithm.IndicatorsOkay(myData!) && algorithm.IsSignal())
                return await PrepareAndSendSignalAsync(algorithm);
        }
        return false;
    }

}