using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiMultiShort : SignalSbmBaseShort
{
    public SignalStoRsiMultiShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.StoRsiMulti;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // disable sbm conditions
        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width too small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }
        long unixDate = CandleLast.OpenTime;

        // Is it a signal valid over 4 intervals (multistorsi)
        int okay = 4;
        ExtraText = "";
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(intervalPeriod);
            long candleOpenTime = IntervalTools.StartOfIntervalCandle2(unixDate, Interval.Duration, higherInterval.Interval.Duration);
            if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? candle))
                return false;

            if (candle.CandleData == null)
            {
                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
                if (history == null)
                    return false;
                CandleIndicatorData.CalculateIndicators(history);
            }

            if (IndicatorsOkay(candle!) && candle.IsStochOverbought(GlobalData.Settings.Signal.StoRsi.AddAmount) && candle.IsRsiOverbought(GlobalData.Settings.Signal.StoRsi.AddAmount))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                    return true;
            }
            else
            {
                // first interval needs to be a signal
                if (count == 6)
                    return false;
            }

            //if (okay < count) return false;

            if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                return false;
            intervalPeriod++;
        }


        //// close date shouw be in the lower part of the bb
        //if (!IsInLowerPartOfBollingerBands(1, 10.0m))
        //    return false;

        ExtraText = "";
        return false;
    }

    //public override bool AllowStepIn(CryptoSignal signal)
    //{
    //    // Deze routine is een beetje back to the basics, gewoon een nette SBM, vervolgens
    //    // 2 MACD herstel candles, wat rsi en stoch condities om glijbanen te voorkomen

    //    if (!GetPrevCandle(CandleLast!, out CryptoCandle? candlePrev))
    //        return false;


    //    // ********************************************************************
    //    // MACD
    //    if (GlobalData.Settings.Trading.CheckIncreasingMacd)
    //    {
    //        long unixDate = CandleLast.OpenTime;

    //        ExtraText = "";
    //        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
    //        for (int count = 3; count > 0; count--)
    //        {
    //            CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(intervalPeriod);
    //            long candleOpenTime = IntervalTools.StartOfIntervalCandle2(unixDate, Interval.Duration, higherInterval.Interval.Duration);
    //            if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? lastCandle))
    //            {
    //                ExtraText = $"no curr candle {higherInterval.Interval.Name}";
    //                return false;
    //            }
    //            if (!higherInterval.CandleList.TryGetValue(candleOpenTime - Interval.Duration, out CryptoCandle? prevCandle))
    //            {
    //                ExtraText = $"no prev candle {higherInterval.Interval.Name}";
    //                return false;
    //            }

    //            if (lastCandle.CandleData == null)
    //            {
    //                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
    //                if (history == null)
    //                    return false;
    //                CandleIndicatorData.CalculateIndicators(history);
    //            }

    //            if (!IndicatorsOkay(lastCandle!) || !IndicatorsOkay(prevCandle!))
    //            {
    //                ExtraText = $"no candledata {higherInterval.Interval.Name}";
    //                return false;
    //            }

    //            // to positive?
    //            if (lastCandle.CandleData!.MacdHistogram >= 0 || prevCandle.CandleData!.MacdHistogram >= 0)
    //            {
    //                ExtraText = $"macd already positive {higherInterval.Interval.Name}";
    //                return false;
    //            }

    //            // need pink macd candle
    //            if (lastCandle.CandleData!.MacdHistogram <= prevCandle.CandleData!.MacdHistogram)
    //            {
    //                ExtraText = $"no pink macd-candle on {higherInterval.Interval.Name}";
    //                return false;
    //            }


    //            if (intervalPeriod == CryptoIntervalPeriod.interval1d)
    //                return false;
    //            intervalPeriod++;
    //        }
    //        //if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
    //        //{
    //        //    // ExtraText is al ingevuld
    //        //    return false;
    //        //}
    //    }


    //    // ********************************************************************
    //    // RSI
    //    if (GlobalData.Settings.Trading.CheckIncreasingRsi)
    //    {
    //        // Is there any RSI recovery visible (a bit weak)
    //        if (CandleLast?.CandleData?.Rsi < GlobalData.Settings.General.RsiValueOversold)
    //        {
    //            ExtraText = $"RSI {CandleLast.CandleData.Rsi:N8} niet boven de {GlobalData.Settings.General.RsiValueOversold}";
    //            return false;
    //        }

    //        // 2023-04-28 15:11 Afgesterd, hierdoor stappen we te laat in?
    //        // 2023-04-29 12:15 Weer geactiveerd: Het vermijden van glijbanen.
    //        // Dus we stappen nu later in, maar met een beetje meer zekerheid?
    //        if (!IsRsiIncreasingInTheLast(3, 1))
    //        {
    //            ExtraText = string.Format("RSI niet oplopend in de laatste 3,1");
    //            return false;
    //        }
    //    }

    //    // ********************************************************************
    //    // PSAR
    //    //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Close)
    //    //{
    //    //    ExtraText = string.Format("De PSAR staat niet onder de prijs {0:N8}", CandleLast.CandleData.PSar);
    //    //    return false;
    //    //}


    //    // ********************************************************************
    //    // STOCH
    //    // Stochastic: Omdat ik ze door elkaar haal
    //    // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
    //    // Blauw %K = Oscilator berekend over een lookback periode van 14 candles
    //    if (GlobalData.Settings.Trading.CheckIncreasingStoch)
    //    {
    //        // Stochastic: Omdat ik ze door elkaar haal
    //        // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
    //        // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

    //        // Afgesterd - 27-04-2023 10:12
    //        // Met name de %K moet herstellen
    //        if (candlePrev?.CandleData?.StochOscillator >= CandleLast?.CandleData?.StochOscillator)
    //        {
    //            ExtraText = string.Format("Stoch.K {0:N8} hersteld niet < {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
    //            return false;
    //        }

    //        // Afgesterd - 27-04-2023 10:12
    //        //double? minimumStoch = 25;
    //        //if (CandleLast.CandleData.StochOscillator < minimumStoch)
    //        //{
    //        //    ExtraText = string.Format("Stoch.K {0:N8} niet boven de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
    //        //    return false;
    //        //}

    //        // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
    //        if (CandleLast?.CandleData?.StochSignal > CandleLast?.CandleData?.StochOscillator)
    //        {
    //            ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev?.CandleData?.StochSignal, candlePrev?.CandleData?.StochOscillator);
    //            return false;
    //        }
    //    }


    //    // ********************************************************************
    //    // Extra?

    //    // Profiteren van een nog lagere prijs?
    //    if (GlobalData.Settings.Trading.CheckFurtherPriceMove)
    //    {
    //        if (Symbol.LastPrice < signal.LastPrice)
    //        {
    //            if (Symbol.LastPrice != signal.LastPrice)
    //            {
    //                ExtraText = string.Format("Symbol.LastPrice {0:N8} gaat verder naar beneden {1:N8}", Symbol.LastPrice, signal.LastPrice);
    //            }
    //            return false;
    //        }
    //    }
    //    signal.LastPrice = Symbol.LastPrice;

    //    // Koop als de close vlak bij de bb.lower is (c.q. niet te ver naar boven zit)
    //    // Werkt goed!!! (toch even experimenteren) - maar negeert hierdoor ook veel signalen die wel bruikbaar waren
    //    //double? value = CandleLast.CandleData.BollingerBandsUpperBand - 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
    //    //if (Symbol.LastPrice < (decimal)value)
    //    //{
    //    //    ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.Upper + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
    //    //    signal.LastPrice = Symbol.LastPrice;
    //    //    return false;
    //    //}

    //    return true;
    //}

}
