﻿using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Signal;

public delegate void AnalyseEvent(CryptoSignal signal);

public class SignalCreate(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side, long lastCandle1mCloseTime)
{
    private CryptoAccount TradeAccount { get; set; } = tradeAccount;
    private CryptoSymbol Symbol { get; set; } = symbol;
    private CryptoInterval Interval { get; set; } = interval;
    private CryptoTradeSide Side { get; set; } = side;
    private long LastCandle1mCloseTime { get; set; } = lastCandle1mCloseTime;

    private CryptoCandle? Candle { get; set; }
    public List<CryptoCandle>? history = null;

    //public bool CreatedSignal = false;
    public List<CryptoSignal> SignalList { get; set; } = [];

#if TRADEBOT
    bool hasOpenPosistion = false;
    bool hasOpenPosistionCalculated = false;
#endif

    private bool HasOpenPosition()
    {
        // Signalen blijven maken als er een positie openstaat (en het volume e.d. sterk afgenomen is)
#if TRADEBOT
        if (!hasOpenPosistionCalculated)
        {
            hasOpenPosistionCalculated = true;
            hasOpenPosistion = GlobalData.Settings.Trading.Active && PositionTools.HasPosition(Symbol) && GlobalData.Settings.Trading.DcaStrategy == CryptoEntryOrDcaStrategy.AfterNextSignal;
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


    private double CalculateLastPeriodsInInterval(CryptoSignal signal, long interval)
    {
        //Dit moet via de standaard 1m candles omdat de lijst niet alle candles bevat
        //(dit om de berekeningen allemaal wat sneller te maken)

        // Vanwege backtest altijd redeneren vanaf het signaal (en niet de laatste candle)
        CryptoCandle candle = signal.Candle; // Symbol.CandleList.Values.Last();
        long openTime = CandleTools.GetUnixTime(candle.Date, 60);
        if (!Symbol.CandleList.TryGetValue(openTime - interval, out CryptoCandle? candlePrev))
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
            if (candles.TryGetValue(unix, out CryptoCandle? candle))
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



    private bool PrepareAndSendSignal(SignalCreateBase algorithm)
    {
        CryptoSignal signal = CreateSignal(Candle!);
        signal.Side = algorithm.SignalSide;
        signal.Strategy = algorithm.SignalStrategy;
        signal.LastPrice = Symbol.LastPrice;

        string response;
        List<string> eventText = [];
        if (algorithm.ExtraText != "")
            eventText.Add(algorithm.ExtraText);


        // Extra attributen erbij halen (dat lukt niet bij een backtest vanwege het ontbreken van een "history list")
        CalculateAdditionalSignalProperties(signal, history!, 60);
        if (!HasOpenPosition() && !CheckAdditionalAlarmProperties(signal, out response))
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
        if (!HasOpenPosition() && TradingConfig.Signals[signal.Side].InBlackList(Symbol.Name) == MatchBlackAndWhiteList.Present)
        {
            // Als de muntpaar op de black lijst staat dan dit signaal overslagen
            eventText.Add("blacklisted");
            signal.IsInvalid = true;
        }

        // Extra controles, staat de symbol op de whitelist?
        if (!HasOpenPosition() && TradingConfig.Signals[signal.Side].InWhiteList(Symbol.Name) == MatchBlackAndWhiteList.NotPresent)
        {
            // Als de muntpaar niet in de white lijst staat dan dit signaal overslagen
            eventText.Add("not whitelisted");
            signal.IsInvalid = true;
        }


        // de 24 change moet in een bepaald interval zitten
        signal.Last24HoursChange = CalculateLastPeriodsInInterval(signal, 24 * 60 * 60);
        if (!HasOpenPosition() && !signal.Last24HoursChange.IsBetween(GlobalData.Settings.Signal.AnalysisMinChangePercentage, GlobalData.Settings.Signal.AnalysisMaxChangePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxChangePercentage)
            {
                string text = string.Format("Analyse {0} 1d change {1} not between {2} .. {3}", Symbol.Name, signal.Last24HoursChange.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinChangePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxChangePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("1d change% to high");
            signal.IsInvalid = true;
        }

        // de 1 * 1d effectief moet in een bepaald interval zitten
        signal.Last24HoursEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval15m, 1 * 96); // 1 * 24 / 15 = 96
        if (!HasOpenPosition() && !signal.Last24HoursEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinEffectivePercentage, GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxEffectivePercentage)
            {
                string text = string.Format("Analyse {0} 1d change effective {1} not between {2} .. {3}", Symbol.Name, signal.Last24HoursEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinEffectivePercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxEffectivePercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("1d effective% to high");
            signal.IsInvalid = true;
        }

        // de 10 * 1d effectief moet in een bepaald interval zitten
        signal.Last10DaysEffective = CalculateMaxMovementInInterval(signal.EventTime, CryptoIntervalPeriod.interval6h, 1 * 40); // 10 * 24 / 6 = 40
        if (!HasOpenPosition() && !signal.Last10DaysEffective.IsBetween(GlobalData.Settings.Signal.AnalysisMinEffective10DaysPercentage, GlobalData.Settings.Signal.AnalysisMaxEffective10DaysPercentage))
        {
            if (GlobalData.Settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage)
            {
                string text = string.Format("Analyse {0} 10d change effective {1} not between {2} .. {3}", Symbol.Name, signal.Last10DaysEffective.ToString("N2"), GlobalData.Settings.Signal.AnalysisMinEffective10DaysPercentage.ToString(), GlobalData.Settings.Signal.AnalysisMaxEffective10DaysPercentage.ToString());
                GlobalData.AddTextToLogTab(text);
            }
            eventText.Add("10d effective% to high");
            signal.IsInvalid = true;
        }


        // Check "Barcode" charts
        if (!HasOpenPosition())
        {
            decimal barcodePercentage = 100 * Symbol.PriceTickSize / Symbol.LastPrice ?? 0;
            if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            {
                // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
                if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
                    GlobalData.AddTextToLogTab($"Analyse {Symbol.Name} De tick size percentage is te hoog {barcodePercentage:N3}");
                eventText.Add("tick perc to high");
                signal.IsInvalid = true;
            }
        }

        if (!GlobalData.Settings.General.ShowInvalidSignals && signal.IsInvalid)
            return false;



        // Iets wat ik wel eens gebruikt als ik trade
        LuxIndicator.Calculate(Symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod.interval5m, Candle!.OpenTime + Interval.Duration);
        if (signal.Side == CryptoTradeSide.Long)
            signal.LuxIndicator5m = luxOverSold;
        else
            signal.LuxIndicator5m = luxOverBought;



        // Calculate MarketTrend and the individual interval trends (reasonably CPU heavy and thatswhy it is on the end of the routine)
        MarketTrend.Calculate(TradeAccount, signal, LastCandle1mCloseTime);



        // Extra controles toepassen en het signaal "afkeuren" (maar toch laten zien)
        if (!HasOpenPosition())
        {
            // Filter op bepaalde intervallen waarvan je wil dat die bullisch of bearisch zijn

            if (!PositionTools.ValidTrendConditions(TradeAccount, signal.Symbol.Name, TradingConfig.Signals[signal.Side].Trend, out string reaction))
            {
                eventText.Add(reaction);
                signal.IsInvalid = true;
            }


            // Filter op de markettrend waarvan je wil dat die qua percentage bullisch of bearisch zijn
            if (!PositionTools.ValidMarketTrendConditions(TradeAccount, signal.Symbol, TradingConfig.Signals[signal.Side].MarketTrend, out reaction))
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
                                //CreatedSignal = true;
                                SignalList.Add(signal);
                            }
                        }
                    }
                }
            }

            // Signal naar database wegschrijven (niet echt noodzakelijk, we doen er later niets mee)
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            using var transaction = databaseThread.BeginTransaction();
            {
                databaseThread.Connection.Insert(signal, transaction);
                transaction.Commit();
            }

            GlobalData.AnalyzeSignalCreated?.Invoke(signal);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
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
            PriceMin = candle.Close, // statistics
            PriceMax = candle.Close, // statistics
            PriceMinPerc = 0, // statistics
            PriceMaxPerc = 0, // statistics
            Volume = Symbol.Volume,
            EventTime = candle.OpenTime,
            OpenDate = CandleTools.GetUnixDate(candle.OpenTime),
            Side = CryptoTradeSide.Long,  // gets modified later
            Strategy = CryptoSignalStrategy.Jump,  // gets modified later
        };

        signal.CloseDate = signal.OpenDate.AddSeconds(Interval.Duration);
        signal.ExpirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * Interval.Duration);

        // Copy indicators values
        signal.BollingerBandsDeviation = candle.CandleData?.BollingerBandsDeviation;
        signal.BollingerBandsPercentage = candle.CandleData?.BollingerBandsPercentage; // Dit is degene die Marco gebruikt

        signal.Rsi = candle.CandleData?.Rsi;
        //signal.SlopeRsi = candle.CandleData.SlopeRsi;

        signal.PSar = candle.CandleData?.PSar;
#if DEBUG
        signal.PSarDave = candle.CandleData?.PSarDave;
        signal.PSarJason = candle.CandleData?.PSarJason;
        signal.PSarTulip = candle.CandleData?.PSarTulip;
#endif
        signal.StochSignal = candle.CandleData?.StochSignal;
        signal.StochOscillator = candle.CandleData?.StochOscillator;
        //signal.Ema8 = candle.CandleData.Ema8;
        //signal.Ema20 = candle.CandleData.Ema20;
        //signal.Ema50 = candle.CandleData.Ema50;
        //signal.Ema100 = candle.CandleData.Ema100;
        //signal.Ema200 = candle.CandleData.Ema200;
        //signal.SlopeEma20 = candle.CandleData.SlopeEma20;
        //signal.SlopeEma50 = candle.CandleData.SlopeEma50;

        //signal.Tema = candle.CandleData.Tema;

        //signal.Sma8 = candle.CandleData.Sma8;
        signal.Sma20 = candle.CandleData?.Sma20;
        signal.Sma50 = candle.CandleData?.Sma50;
        //signal.Sma100 = candle.CandleData.Sma100;
        signal.Sma200 = candle.CandleData?.Sma200;
        //signal.SlopeSma20 = candle.CandleData.SlopeSma20;
        //signal.SlopeSma50 = candle.CandleData.SlopeSma50;

        return signal;
    }


    private bool ExecuteAlgorithm(AlgorithmDefinition strategyDefinition)
    {
        SignalCreateBase? algorithm;
        if (Side == CryptoTradeSide.Long)
            algorithm = strategyDefinition.InstantiateAnalyzeLong(Symbol, Interval, Candle!);
        else
            algorithm = strategyDefinition.InstantiateAnalyzeShort(Symbol, Interval, Candle!);

        if (algorithm != null)
        {
            //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name} {strategyDefinition.Name} {Side}");
            if (algorithm.IndicatorsOkay(Candle!) && algorithm.IsSignal())
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
        //GlobalData.Logger.Trace($"SignalCreate.Prepare.Start {Symbol.Name} {Interval.Name} {Side}");

        Candle = null;
        string response = "";

        if (!Symbol.LastPrice.HasValue)
        {
            // Die wordt ingevuld in de BinanceStream1mCandles en BinanceStreamPriceTicker, dus zelden leeg
            GlobalData.AddTextToLogTab(string.Format("Analyse {0} Er is geen lastprice aanwezig", Symbol.Name));
            return false;
        }


        // Is het volume binnen de gestelde grenzen          
        if (!HasOpenPosition() && !Symbol.CheckValidMinimalVolume(candleOpenTime, Interval.Duration, out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalVolume)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }

        // Is de prijs binnen de gestelde grenzen
        if (!HasOpenPosition() && !Symbol.CheckValidMinimalPrice(out response))
        {
            if (GlobalData.Settings.Signal.LogMinimalPrice)
                GlobalData.AddTextToLogTab("Analyse " + response);
            return false;
        }


        // Build a list of candles
        history ??= CandleIndicatorData.CalculateCandles(Symbol, Interval, candleOpenTime, out response);
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
        
        //GlobalData.Logger.Trace($"SignalCreate.Prepare.Stop {Symbol.Name} {Interval.Name} {Side}");
        return true;
    }



    public bool Analyze(long candleIntervalOpenTime)
    {
        //GlobalData.Logger.Trace($"SignalCreate.Start {Symbol.Name} {Interval.Name}");
        // Eenmalig de indicators klaarzetten
        if (Prepare(candleIntervalOpenTime))
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
            foreach (CryptoSignalStrategy strategy in TradingConfig.Signals[Side].StrategySbmStob.ToList())
            {
                if (SignalHelper.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition? strategyDefinition))
                {
                    if (ExecuteAlgorithm(strategyDefinition))
                        break;
                }
            }

            // En de overige waaronder de jump
            foreach (CryptoSignalStrategy strategy in TradingConfig.Signals[Side].StrategyOthers.ToList())
            {
                if (SignalHelper.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition? strategyDefinition))
                {
                    ExecuteAlgorithm(strategyDefinition);
                }
            }

        }
        return SignalList.Count > 0;
        //GlobalData.Logger.Trace($"SignalCreate.Done {Symbol.Name} {Interval.Name}");
    }

}