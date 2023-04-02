using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using MathNet.Numerics.LinearAlgebra.Solvers;
using System.Collections.Generic;

namespace CryptoSbmScanner.Signal;


public static class SignalSbmBaseOversoldHelper
{
    public static bool IsSbmConditionsOversold(this CryptoCandle candle, bool includePsarCheck)
    {
        // Optimalisatie, zou naar de SignalSbmBaseOversold kunnen, maar de stobb gebruikt deze routine ook

        // Oversold (denk groen-geel-rood) - long
        // 200 (red)
        // 50 (orange)
        // 20 (green)
        // psar

        // Staan de 3 ma-lijnen (200, 50, 20) en psar in de juiste volgorde
        if (candle.CandleData.Sma50 >= candle.CandleData.Sma200)
            return false;
        if (candle.CandleData.Sma20 >= candle.CandleData.Sma200)
            return false;
        if (candle.CandleData.Sma20 >= candle.CandleData.Sma50)
            return false;

        if (includePsarCheck)
        {
            if (candle.CandleData.PSar > candle.CandleData.Sma20)
                return false;

            // Dan is de psar omgeslagen (hoort hier niet)
            if ((decimal)candle.CandleData.PSar <= candle.Close)
                return false;
        }
        return true;
    }

    public static bool IsSma200AndSma50OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200 - (decimal)candle.CandleData.Sma50;
        decimal value2 = ((decimal)candle.CandleData.Sma200 + (decimal)candle.CandleData.Sma50) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma50 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma50AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        decimal value = (decimal)candle.CandleData.Sma50 - (decimal)candle.CandleData.Sma20;
        decimal value2 = ((decimal)candle.CandleData.Sma50 + (decimal)candle.CandleData.Sma20) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma50 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }


    public static bool IsSma200AndSma20OkayOversold(this CryptoCandle candle, decimal percentage, out string response)
    {
        // En aanvullend, de ma lijnen moeten afwijken (bij benadering, dat hoeft niet geheel exact)
        decimal value = (decimal)candle.CandleData.Sma200 - (decimal)candle.CandleData.Sma20;
        decimal value2 = ((decimal)candle.CandleData.Sma200 + (decimal)candle.CandleData.Sma20) / 2;
        decimal perc = 100 * value / value2;
        if (perc < percentage)
        {
            response = string.Format("percentage sma200 and sma20 ({0:N2} < {1:N2})", perc, percentage);
            return false;
        }

        response = "";
        return true;
    }
}


public class SignalSbmBaseOversold : SignalSbmBase
{
    public SignalSbmBaseOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out response))
            return false;
        if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out response))
            return false;
        if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out response))
            return false;

        if (!CheckMaCrossings(out response))
            return false;

        return true;
    }

    public bool IsMacdRecoveryOversold(int candleCount)
    {
        CryptoCandle last = CandleLast;
        if (last.CandleData.MacdHistogram > 0)
        {
            ExtraText = string.Format("De MACD.Hist is groen {0:N8}", last.CandleData.MacdHistogram);
            return false;
        }

        // Is er "herstel" ten opzichte van de vorige macd histogram candle?
        int iterator = 0;
        while (candleCount > 0)
        {
            if (!GetPrevCandle(last, out CryptoCandle prev))
                return false;


            if (last.CandleData.MacdHistogram <= prev.CandleData.MacdHistogram || last.CandleData.MacdHistogram >= 0)
            {
                // Vermeld ik de juiste kleur? 
                if (last.CandleData.MacdHistogram >= 0)
                    ExtraText = string.Format("De MACD[{0:N0}].Hist is groen {1:N8} {2:N8}", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                else
                    ExtraText = string.Format("De MACD[{0:N0}].Hist is niet roze {1:N8} {2:N8}", iterator, prev.CandleData.MacdHistogram, last.CandleData.MacdHistogram);
                return false;
            }

            iterator--;
            last = prev;
            candleCount--;
        }

        return true;
    }



    protected bool HadIncreasingVolume(out string reaction)
    {
        // Het gemiddelde volume uitrekenen, als de vorige candle ~4* die avg-volume heeft en 
        // de volgende candle heeft een flink lagere volume dan is er grote kans op een correctie

        // Het probleem is alleen hoe we te lage volumes detecteren?

        reaction = "";
        decimal sumVolume = 0;

        int count = 20;
        for (int i = Candles.Count - 1; i > 0; i--)
        {
            CryptoCandle candle = Candles.Values[i];
            sumVolume += candle.Volume;
            count--;
            if (count < 0)
                break;
        }
        decimal avgVolume = sumVolume / 20;

        // De volume van die laatste x candles moet boven de 1% volume van de munt zitten
        if (sumVolume < 0.01m * Symbol.Volume)
        {
            reaction = string.Format("Geen volume {0:N8} volume(20)={0:N8} < 0.01m*Symbol.Volume {1:N8}", sumVolume, 0.01m * Symbol.Volume);
            return false;
        }


        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
        {
            reaction = ExtraText;
            return false;
        }

        if (candlePrev.Volume > 3 * avgVolume && CandleLast.Volume < avgVolume && CandleLast.Volume < candlePrev.Volume) 
        {
            reaction = string.Format("Volume spike prev={0:N8} last={1:N8}", candlePrev.Volume, CandleLast.Volume);
            return true;
        }
        

        return false;
    }


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
        if (!CandleLast.IsSbmConditionsOversold(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        // Er recovery is via de macd
        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
            return false;

        return true;
    }



    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsBelowBollingerBands(false))
        {
            ExtraText = "beneden de bb.lower";
            return false;
        }

        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
            return false;

        //if (CandleLast.CandleData.Tema < CandleLast.CandleData.Sma8)
        //{
        //    ExtraText = string.Format("Tema onder de sma8 {0:N8} {1:N8}", CandleLast.CandleData.Tema, CandleLast.CandleData.Sma8);
        //    return false;
        //}

        // Wacht tot de PSAR lager is dan de SMA - x(enkel voor bot)
        //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Close)
        //{
        //    double? value = CandleLast.CandleData.Sma20 - 0.125 * CandleLast.CandleData.BollingerBandsDeviation;
        //    if (CandleLast.CandleData.PSar > value)
        //    {
        //        ExtraText = string.Format("PSAR not below sma20 - 0.125*StdDev {0:N8} {1:N8}", CandleLast.CandleData.Sma20, value);
        //        return false;
        //    }
        //}

        //if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
        //    return false;

        //// Is there any RSI recovery visible (a bit weak)
        //if (CandleLast.CandleData.Rsi < candlePrev.CandleData.Rsi)
        //{
        //    ExtraText = string.Format("RSI not recovering {0:N8} {1:N8}", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
        //    return false;
        //}


        //double valueRsi = 20;
        //if (CandleLast.CandleData.Rsi < valueRsi)
        //{
        //    ExtraText = string.Format("RSI not above limit {0:N8} < {1:N8}", CandleLast.CandleData.Rsi, valueRsi);
        //    return false;
        //}

        // Koop als de close vlak bij de bb.lower is (c.q. niet te ver naar boven zit)
        double? value = CandleLast.CandleData.BollingerBandsLowerBand + 0.25 * CandleLast.CandleData.BollingerBandsDeviation;
        if (Symbol.LastPrice > (decimal)value)
        {
            ExtraText = string.Format("Symbol.Lastprice {0:N8} > BB.lower + 0.25 * StdDev {1:N8}", Symbol.LastPrice, value);
            return false;
        }

        // profiteren van een nog lagere prijs...?
        if (Symbol.LastPrice < signal.LastPrice)
        {
            if (Symbol.LastPrice != signal.LastPrice)
            {
                ExtraText = string.Format("Symbol.LastPrice gaat nog verder naar beneden (ff wachten) {0:N8} {1:N8}", Symbol.LastPrice, signal.LastPrice);
                return false;
            }
            signal.LastPrice = Symbol.LastPrice;
        }

        //if (CandleLast.CandleData.SlopeRsi <= 0)
        //{
        //    ExtraText = string.Format("Slope RSI <= 0 {0:N8}", CandleLast.CandleData.SlopeRsi);
        //    return false;
        //}

        // Deze is wel redelijk streng
        //if (!HadIncreasingVolume(out ExtraText))
        //  return false;


        //// Is there any STOCH.K recovery visible (a bit weak)
        //if (CandleLast.CandleData.StochOscillator < candlePrev.CandleData.StochOscillator)
        //{
        //    ExtraText = string.Format("Stoch.K not recovering {0:N8} < {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
        //    return false;
        //}
        //// Is there any STOCH.D recovery visible (a bit weak)
        //if (CandleLast.CandleData.StochSignal < candlePrev.CandleData.StochSignal)
        //{
        //    ExtraText = string.Format("Stoch.D not recovering {0:N8} < {1:N8}", candlePrev.CandleData.StochSignal, CandleLast.CandleData.StochSignal);
        //    return false;
        //}


        //double valueStoch = 20;
        //if (CandleLast.CandleData.StochOscillator < valueStoch)
        //{
        //    ExtraText = string.Format("Stoch.K not above limit {0:N8} < {1:N8}", candlePrev.CandleData.StochOscillator, valueStoch);
        //    return false;
        //}

        //if (CandleLast.CandleData.StochSignal < valueStoch)
        ////if ((CandleLast.CandleData.StochSignal < candlePrev.CandleData.StochSignal))
        //{
        //    ExtraText = string.Format("Stoch.D above limit {0:N8} < {1:N8}", candlePrev.CandleData.StochSignal, valueStoch);
        //    return false;
        //}

        //ExtraText = "Alles lijkt goed";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        //ExtraText = "";

        // Als de prijs alweer boven de sma zit ophouden
        //if (CandleLast.CandleData.Tema >= CandleLast.CandleData.Sma20)
        //{
        //    ExtraText = "Candle above SMA20";
        //    return true;
        //}

        // Als de psar bovenin komt te staan
        // (maar dan stap je nooit in, duh)
        //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Close)
        //    return true;


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if ((CandleLast.OpenTime - signal.EventTime) > 5 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        //if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
        //    return true;

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

        if (CandleLast.CandleData.MacdHistogram >= 0)
        {
            ExtraText = string.Format("De MACD.Hist is ondertussen groen {0:N8}", CandleLast.CandleData.MacdHistogram);
            return true;
        }

        ExtraText = "";
        return false;
    }

}

