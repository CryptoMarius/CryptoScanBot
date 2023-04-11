using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalStobbOversold : SignalSbmBaseOversold // inherit from sbm because of 1 call there
{
    public SignalStobbOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        //ReplaceSignal = true;
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.stobbOversold;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.StochSignal == null)
           || (candle.CandleData.StochOscillator == null)
           || (candle.CandleData.BollingerBandsDeviation == null)
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast.CandleData.StochOscillator,
            CandleLast.CandleData.StochSignal
        );
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (GlobalData.Settings.Signal.StobIncludeSoftSbm)
        {
            if (!CandleLast.IsSbmConditionsOversold(false))
            {
                response = "maar geen sbm condities";
                return false;
            }

            // TODO: Een instelling hiervoor maken!!
            if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out response))
                return false;
            if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out response))
                return false;
            if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        if (GlobalData.Settings.Signal.StobIncludeRsi && !CandleLast.IsRsiOversold())
        {
            ExtraText = "rsi niet oversold";
            return false;
        }


        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.StobbUseLowHigh))
        {
            ExtraText = "niet beneden de bb.lower";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOversold())
        {
            ExtraText = "stoch niet oversold";
            return false;
        }


        // TODO: Wellicht toch weer activeren?
        // Platte candles of candles zonder volume
        // EXTRA (anders krijg je toch vreemde momenten met platte candles)
        //CryptoSignal Signal = new CryptoSignal();
        //SignalCreate.CalculateAdditionalAlarmProperties(Signal, Candles.Values.ToList(), 30, CandleLast.OpenTime);
        //if (GlobalData.Settings.Signal.CandlesWithZeroVolume > 0 && Signal.CandlesWithZeroVolume >= GlobalData.Settings.Signal.CandlesWithZeroVolume)
        //    return false;
        //if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && Signal.CandlesWithFlatPrice >= GlobalData.Settings.Signal.CandlesWithFlatPrice)
        //    return false;
        //if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && Signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
        //    return false;
        //if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && Signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
        //    return false;


        return true;
    }


    ///*
    // * De oude routine werkte zo slecht nog niet
    // * even bewaren?
    //public override bool AllowStepIn(CryptoSignal signal)
    //{

    //    // Er een candle onder de bb opent of sluit
    //    if (CandleLast.IsBelowBollingerBands(false))
    //    {
    //        ExtraText = "Close beneden de bb.lower";
    //        signal.LastPrice = Symbol.LastPrice;
    //        return false;
    //    }



    //    if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
    //        return false;

    //    // Is there any RSI recovery visible (a bit weak)
    //    if (CandleLast.CandleData.Rsi < candlePrev.CandleData.Rsi)
    //    {
    //        ExtraText = string.Format("RSI {0:N8} hersteld niet {1:N8}", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
    //        return false;
    //    }


    //    // Deze is te streng
    //    //if (!HadIncreasingVolume(out ExtraText))
    //    //  return false;

    //    //double valueRsi = 20;
    //    //if (CandleLast.CandleData.Rsi < valueRsi)
    //    //{
    //    //    ExtraText = string.Format("RSI not above limit {0:N8} < {1:N8}", CandleLast.CandleData.Rsi, valueRsi);
    //    //    return false;
    //    //}

    //    // Koop als de close vlak bij de bb.lower is (c.q. niet te ver naar boven zit)
    //    // Werkt goed!!! (toch even experimenteren) - maar negeert hierdoor ook veel signalen die wel bruikbaar waren
    //    //double? value = CandleLast.CandleData.BollingerBandsLowerBand + 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
    //    //if (Symbol.LastPrice > (decimal)value)
    //    //{
    //    //    ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.lower + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
    //    //    signal.LastPrice = Symbol.LastPrice;
    //    //    return false;
    //    //}

    //    //// Is there any RSI recovery visible (a bit stronger)
    //    //if (CandleLast.CandleData.SlopeRsi < 0)
    //    //{
    //    //    ExtraText = string.Format("Slope RSI < 0 {0:N8}", CandleLast.CandleData.SlopeRsi);
    //    //    signal.LastPrice = Symbol.LastPrice;
    //    //    return false;
    //    //}

    //    // Profiteren van een nog lagere prijs?
    //    // Maar nu schiet ie door naar de 1e de beste groene macd candle?
    //    if (Symbol.LastPrice < signal.LastPrice)
    //    {
    //        if (Symbol.LastPrice != signal.LastPrice)
    //        {
    //            ExtraText = string.Format("Symbol.LastPrice {0:N8} gaat verder naar beneden {1:N8}", Symbol.LastPrice, signal.LastPrice);
    //        }
    //        return false;
    //    }
    //    signal.LastPrice = Symbol.LastPrice;

    //    // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
    //    if (CandleLast.IsStochOversold())
    //    {
    //        ExtraText = "stoch nog steeds oversold";
    //        return false;
    //    }



    //    if (CandleLast.CandleData.StochOscillator < candlePrev.CandleData.StochOscillator)
    //    {
    //        ExtraText = string.Format("Stoch.K {0:N8} hersteld niet < {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
    //        return false;
    //    }
    //    if (CandleLast.CandleData.StochSignal < candlePrev.CandleData.StochSignal)
    //    {
    //        ExtraText = string.Format("Stoch.D {0:N8} hersteld niet < {1:N8}", candlePrev.CandleData.StochSignal, CandleLast.CandleData.StochSignal);
    //        return false;
    //    }


    //    double? minimumStoch = 20;
    //    if (CandleLast.CandleData.StochSignal < minimumStoch)
    //    {
    //        ExtraText = string.Format("Stoch.D {0:N8} niet boven de {1:N0}", candlePrev.CandleData.StochSignal, minimumStoch);
    //        return false;
    //    }
    //    if (CandleLast.CandleData.StochOscillator < minimumStoch)
    //    {
    //        ExtraText = string.Format("Stoch.K {0:N8} niet boven de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
    //        return false;
    //    }

    //    // Stochastic: Omdat ik ze wel eens door elkaar haal:
    //    // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
    //    // Blauw %K = Oscilator berekend over een lookback periode van 14 candles
    //    // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
    //    if (CandleLast.CandleData.StochSignal > CandleLast.CandleData.StochOscillator)
    //    {
    //        ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev.CandleData.StochSignal, candlePrev.CandleData.StochOscillator);
    //        return false;
    //    }

    //    // %K Oscillator (geel, de "snelle"), was 14, maar ik stap dan te vroeg in?
    //    //decimal value = GlobalData.Settings.Bot.StepInStochValue;
    //    //if ((decimal)CandleLast.CandleData.StochOscillator < value)
    //    //{
    //    //    string text = "Monitor " + Symbol.Name + " " + signal.Interval.Name + " signal from=" + signal.OpenDate.ToLocalTime() + " price=" + Symbol.LastPrice;
    //    //    ExtraText = string.Format("{0}  stoch to low ({1} < {2})", text, CandleLast.CandleData.StochOscillator.ToString("N5"), value);
    //    //    return false;
    //    //}

    //    return true;
    //}


    //*/

    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;

        //// Er een candle onder de bb opent of sluit (overbodig icm macd)
        //if (CandleLast.IsBelowBollingerBands(false))
        //{
        //    ExtraText = "Close beneden de bb.lower";
        //    return false;
        //}


        // ********************************************************************
        // MACD
        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
        {
            return false;
        }


        // ********************************************************************
        // RSI

        // Is there any RSI recovery visible (a bit weak)
        if (CandleLast.CandleData.Rsi < candlePrev.CandleData.Rsi)
        {
            ExtraText = string.Format("RSI {0:N8} hersteld niet {1:N8}", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }


        // ********************************************************************
        // STOCH
        // Stochastic: Omdat ik ze door elkaar haal
        // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
        // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

        // Met name de %K moet herstellen
        if (CandleLast.CandleData.StochOscillator < candlePrev.CandleData.StochOscillator)
        {
            ExtraText = string.Format("Stoch.K {0:N8} hersteld niet < {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
            return false;
        }

        double? minimumStoch = 16;
        if (CandleLast.CandleData.StochOscillator < minimumStoch)
        {
            ExtraText = string.Format("Stoch.K {0:N8} niet boven de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
            return false;
        }

        // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
        if (CandleLast.CandleData.StochSignal > CandleLast.CandleData.StochOscillator)
        {
            ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev.CandleData.StochSignal, candlePrev.CandleData.StochOscillator);
            return false;
        }


        // ********************************************************************
        // Extra?

        // Profiteren van een nog lagere prijs?
        // Maar nu schiet ie door naar de 1e de beste groene macd candle?
        if (Symbol.LastPrice < signal.LastPrice)
        {
            if (Symbol.LastPrice != signal.LastPrice)
            {
                ExtraText = string.Format("Symbol.LastPrice {0:N8} gaat verder naar beneden {1:N8}", Symbol.LastPrice, signal.LastPrice);
            }
            return false;
        }
        signal.LastPrice = Symbol.LastPrice;

        // Koop als de close vlak bij de bb.lower is (c.q. niet te ver naar boven zit)
        // Dit werkt - maar hierdoor negeren we ook veel signalen die wel bruikbaar waren..
        //double? value = CandleLast.CandleData.BollingerBandsLowerBand + 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
        //if (Symbol.LastPrice > (decimal)value)
        //{
        //    ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.lower + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
        //    signal.LastPrice = Symbol.LastPrice;
        //    return false;
        //}

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return true;
        }


        if (Math.Min(CandleLast.Open, CandleLast.Close) >= (decimal)CandleLast.CandleData.Sma20)
        {
            //reason = string.Format("{0} give up (pricewise.body > bb) {1}", text, dcaPrice.ToString0());
            ExtraText = "give up (pricewise.body > bb)";
            return true;
        }


        if (CandleLast.CandleData.StochOscillator >= 80)
        {
            ExtraText = "give up(stoch.osc > 80)";
            //AppendLine(string.Format("{0} give up (stoch.osc > 80) {1}", text, dcaPrice.ToString0());
            return true;
        }


        // Langer dan x candles willen we niet wachten
        if ((CandleLast.OpenTime - signal.EventTime) > GlobalData.Settings.Bot.GlobalBuyRemoveTime * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        if (CandleLast.CandleData.PSar > CandleLast.CandleData.Sma20)
        {
            ExtraText = string.Format("De PSAR staat boven de sma20 {0:N8}", CandleLast.CandleData.PSar);
            return true;
        }

        if (Symbol.LastPrice > (decimal)CandleLast.CandleData.Sma20)
        {
            ExtraText = string.Format("De prijs staat boven de sma20 {0:N8}", Symbol.LastPrice);
            return true;
        }


        // Als de barometer alsnog daalt dan stoppen
        BarometerData barometerData = Symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
        if (barometerData.PriceBarometer <= GlobalData.Settings.Bot.Barometer01hBotMinimal)
        {
            ExtraText = string.Format("Barometer 1h {0} below {1}", barometerData.PriceBarometer?.ToString0("N2"), GlobalData.Settings.Bot.Barometer01hBotMinimal.ToString0("N2"));
            return true;
        }

        ExtraText = "";
        return false;
    }

}

