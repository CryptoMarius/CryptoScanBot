using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


public static class SignalSbmBaseOverboughtHelper
{
    public static bool IsSbmConditionsOverbought(this CryptoCandle candle, bool includePsarCheck = true)
    {
        // Optimalisatie, zou naar de SignalSbmBaseOverbought kunnen (maar laten staan vanwege oversold)

        // Overbought (denk groen-geel-rood) - short
        // psar
        // 20 (green)
        // 50 (orange)
        // 200 (red)

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData.Sma200 >= candle.CandleData.Sma50)
            return false;
        if (candle.CandleData.Sma200 >= candle.CandleData.Sma20)
            return false;
        if (candle.CandleData.Sma50 >= candle.CandleData.Sma20)
            return false;


        if (includePsarCheck)
        {
            if (candle.CandleData.PSar < candle.CandleData.Sma20)
                return false;

            // Dan is de psar omgeslagen (hoort hier niet?)
            if ((decimal)candle.CandleData.PSar >= candle.Close)
                return false;
        }

        return true;
    }

    public static bool IsSma200AndSma50OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma50 - (decimal)candle.CandleData.Sma200;
        decimal value2 = ((decimal)candle.CandleData.Sma50 + (decimal)candle.CandleData.Sma200) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma50AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        decimal value = (decimal)candle.CandleData.Sma20 - (decimal)candle.CandleData.Sma50;
        decimal value2 = ((decimal)candle.CandleData.Sma20 + (decimal)candle.CandleData.Sma50) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma200AndSma20OkayOverbought(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma20 - (decimal)candle.CandleData.Sma200;
        decimal value2 = ((decimal)candle.CandleData.Sma20 + (decimal)candle.CandleData.Sma200) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsStochOverbought(this CryptoCandle candle)
    {
        if (candle.CandleData.StochSignal < 80)
            return false;
        if (candle.CandleData.StochOscillator < 80)
            return false;
        return true;
    }


    public static bool IsRsiOverbought(this CryptoCandle candle)
    {
        if (candle.CandleData.Rsi < 70)
            return false;
        return true;
    }
}


public class SignalSbmBaseOverbought : SignalSbmBase
{
    public SignalSbmBaseOverbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (!candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out response))
            return false;
        if (!candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out response))
            return false;
        if (!candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out response))
            return false;

        if (!CheckMaCrossings(out response))
            return false;

        return true;
    }

    public bool IsMacdRecoveryOverbought(int candleCount = 2)
    {
        CryptoCandle last = CandleLast;
        if (last.CandleData.MacdHistogram < 0)
        {
            ExtraText = string.Format("De MACD.Hist is rood {0:N8}", last.CandleData.MacdHistogram);
            return false;
        }

        // Is er "herstel" ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;


            if (last.CandleData.MacdHistogram >= prev.CandleData.MacdHistogram || last.CandleData.MacdHistogram < 0)
            {
                // vermeld ik de juiste kleur? ;-)
                if (last.CandleData.MacdHistogram <= 0)
                    ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtrood {1:N8} {2:N8} (last)", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                else
                    ExtraText = string.Format("De MACD[{0:N0}].Hist is niet lichtgroen {1:N8} {2:N8} (last)", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                return false;
            }

            iterator--;
            last = prev;
            candleCount--;
        }

        return true;
    }



    //protected bool HadIncreasingVolume(out string reaction)
    //{
    //    // Het gemiddelde volume uitrekenen, als de vorige candle ~4* die avg-volume heeft en 
    //    // de volgende candle heeft een flink lagere volume dan is er grote kans op een correctie

    //    // Het probleem is alleen hoe we te lage volumes detecteren?

    //    reaction = "";
    //    decimal sumVolume = 0;

    //    int count = 20;
    //    for (int i = Candles.Count - 1; i > 0; i--)
    //    {
    //        CryptoCandle candle = Candles.Values[i];
    //        sumVolume += candle.Volume;
    //        count--;
    //        if (count < 0)
    //            break;
    //    }
    //    decimal avgVolume = sumVolume / 20;

    //    // De volume van die laatste x candles moet boven de 1% volume van de munt zitten
    //    if (sumVolume < 0.01m * Symbol.Volume)
    //    {
    //        reaction = string.Format("Geen volume {0:N8} volume(20)={0:N8} < 0.01m*Symbol.Volume {1:N8}", sumVolume, 0.01m * Symbol.Volume);
    //        return false;
    //    }


    //    if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
    //    {
    //        reaction = ExtraText;
    //        return false;
    //    }

    //    if (candlePrev.Volume > 3 * avgVolume && CandleLast.Volume < avgVolume && CandleLast.Volume < candlePrev.Volume) 
    //    {
    //        reaction = string.Format("Volume spike prev={0:N8} last={1:N8}", candlePrev.Volume, CandleLast.Volume);
    //        return true;
    //    }


    //    return false;
    //}


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // De ma lijnen en psar goed staan
        if (!CandleLast.IsSbmConditionsOverbought(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // Er recovery is via de macd
        if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
            return false;

        return true;
    }



    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;

        //// Er een candle onder de bb opent of sluit (overbodig icm macd)
        //if (CandleLast.IsAboveBollingerBands(false))
        //{
        //    ExtraText = "Close boven de bb.lower";
        //    return false;
        //}


        // ********************************************************************
        // MACD
        if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
        {
            return false;
        }


        // ********************************************************************
        // RSI

        // Is there any RSI recovery visible (a bit weak)
        if ((CandleLast.CandleData.Rsi > candlePrev.CandleData.Rsi))
        {
            ExtraText = string.Format("RSI not recovering {0:N8} {1:N8}", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }


        // ********************************************************************
        // STOCH
        // Stochastic: Omdat ik ze door elkaar haal
        // Rood %D = signal, het gemiddelde van de laatste 3 %K waarden
        // Blauw %K = Oscilator berekend over een lookback periode van 14 candles

        // Met name de %K moet herstellen
        if (CandleLast.CandleData.StochOscillator > candlePrev.CandleData.StochOscillator)
        {
            ExtraText = string.Format("Stoch.K {0:N8} hersteld niet > {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
            return false;
        }

        double? minimumStoch = 84;
        if (CandleLast.CandleData.StochOscillator > minimumStoch)
        {
            ExtraText = string.Format("Stoch.K {0:N8} niet onder de {1:N0}", candlePrev.CandleData.StochOscillator, minimumStoch);
            return false;
        }

        // De %D en %K moeten elkaar gekruist hebben. Dus %K(snel/blauw) > %D(traag/rood)
        if (CandleLast.CandleData.StochSignal < CandleLast.CandleData.StochOscillator)
        {
            ExtraText = string.Format("Stoch.%D {0:N8} heeft de %K {1:N8} niet gekruist", candlePrev.CandleData.StochSignal, candlePrev.CandleData.StochOscillator);
            return false;
        }


        // ********************************************************************
        // Extra?

        // Profiteren van een nog lagere prijs?
        // Maar nu schiet ie door naar de 1e de beste groene macd candle?
        if (Symbol.LastPrice > signal.LastPrice)
        {
            if (Symbol.LastPrice != signal.LastPrice)
            {
                ExtraText = string.Format("Symbol.LastPrice {0:N8} gaat verder naar beneden {1:N8}", Symbol.LastPrice, signal.LastPrice);
            }
            return false;
        }
        signal.LastPrice = (decimal)Symbol.LastPrice;

        // Koop als de close vlak bij de bb.upper is (c.q. niet te ver naar boven zit)
        // Werkt goed!!! (toch even experimenteren) - maar negeert hierdoor ook veel signalen die wel bruikbaar waren
        //double? value = CandleLast.CandleData.BollingerBandsUpperBand - 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
        //if (Symbol.LastPrice < (decimal)value)
        //{
        //    ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.Upper + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
        //    signal.LastPrice = Symbol.LastPrice;
        //    return false;
        //}

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return true;
        }

        //ExtraText = "";

        if (Math.Min(CandleLast.Open, CandleLast.Close) <= (decimal)CandleLast.CandleData.Sma20)
        {
            //reason = string.Format("{0} give up (pricewise.body > bb) {1}", text, dcaPrice.ToString0());
            ExtraText = "give up (pricewise.body < bb)";
            return true;
        }


        if (CandleLast.CandleData.StochOscillator <= 20)
        {
            ExtraText = "give up(stoch.osc > 20)";
            //AppendLine(string.Format("{0} give up (stoch.osc > 20) {1}", text, dcaPrice.ToString0());
            return true;
        }


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if ((CandleLast.OpenTime - signal.EventTime) > GlobalData.Settings.Trading.GlobalBuyRemoveTime * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        // Als de barometer alsnog daalt dan stoppen
        // (MAAR - willen we voor short niet een andere barometer check hanteren?????)
        BarometerData barometerData = Symbol.QuoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
        if (barometerData.PriceBarometer <= GlobalData.Settings.Trading.Barometer01hBotMinimal)
        {
            ExtraText = string.Format("Barometer 1h {0} below {1}", barometerData.PriceBarometer?.ToString0("N2"), GlobalData.Settings.Trading.Barometer01hBotMinimal.ToString0("N2"));
            return true;
        }

        ExtraText = "";
        return false;
    }

}
