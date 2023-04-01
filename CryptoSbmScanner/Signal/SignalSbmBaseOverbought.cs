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


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsAboveBollingerBands(false))
        {
            ExtraText = "above bb.upper";
            return false;
        }

        if (!IsMacdRecoveryOverbought())
            return false;

        //if (CandleLast.CandleData.Tema > CandleLast.CandleData.Sma8)
        //{
        //    ExtraText = string.Format("De tema zit nog boven de sma8 {0:N8} {1:N8}", CandleLast.CandleData.Tema, CandleLast.CandleData.Sma8);
        //    return false;
        //}

        // Wacht tot de PSAR lager is dan de SMA (enkel voor bot)
        if ((decimal)CandleLast.CandleData.PSar < CandleLast.Close)
        {
            double? value = CandleLast.CandleData.Sma20 + 0.125 * CandleLast.CandleData.BollingerBandsDeviation;
            if (CandleLast.CandleData.PSar < value)
            {
                ExtraText = string.Format("PSAR not above sma + 0.125*StdDev {0:N8} {1:N8}", CandleLast.CandleData.Sma20, value);
                return false;
            }
        }

        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;

        // Is there any RSI recovery visible (a bit weak)
        if ((CandleLast.CandleData.Rsi > candlePrev.CandleData.Rsi))
        {
            ExtraText = string.Format("RSI not recovering {0:N8} {1:N8}", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }

        double valueRsi = 80;
        if (CandleLast.CandleData.Rsi > valueRsi)
        {
            ExtraText = string.Format("Stoch.K not above limit {0:N8} < {1:N8}", CandleLast.CandleData.Rsi, valueRsi);
            return false;
        }


        //// Is there any STOCH.K recovery visible (a bit weak)
        ////if ((CandleLast.CandleData.StochOscillator > 75))
        //if ((CandleLast.CandleData.StochOscillator > candlePrev.CandleData.StochOscillator))
        //{
        //    ExtraText = string.Format("Stoch.K not recovering {0:N8} {1:N8}", candlePrev.CandleData.StochOscillator, CandleLast.CandleData.StochOscillator);
        //    return false;
        //}
        //// Is there any STOCH.D recovery visible (a bit weak)
        ////if ((CandleLast.CandleData.StochSignal > 75))
        //if ((CandleLast.CandleData.StochSignal > candlePrev.CandleData.StochSignal))
        //{
        //    ExtraText = string.Format("Stoch.D not recovering {0:N8} {1:N8}", candlePrev.CandleData.StochSignal, CandleLast.CandleData.StochSignal);
        //    return false;
        //}

        //ExtraText = "Alles lijkt goed";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        //ExtraText = "";

        // Als de prijs alweer boven de sma zit ophouden
        //if (CandleLast.CandleData.Tema <= CandleLast.CandleData.Sma20)
        //{
        //    ExtraText = "Candle above SMA20";
        //    return true;
        //}

        // Als de psar bovenin komt te staan
        // (maar dan stap je nooit in, duh)
        //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Close)
        //    return true;


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if ((CandleLast.OpenTime - signal.EventTime) > 10 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        //if (!IsMacdRecoveryOverbougt())
        //    return true;

        //if ((decimal)CandleLast.CandleData.PSar > CandleLast.Low)
        //{
        //    ExtraText = string.Format("De PSAR staat onder de low {0:N8}", CandleLast.CandleData.PSar);
        //    return true;
        //}

        if (CandleLast.CandleData.PSar < CandleLast.CandleData.Sma20)
        {
            ExtraText = string.Format("De PSAR staat onder de sma20 {0:N8}", CandleLast.CandleData.PSar);
            return true;
        }

        ExtraText = "";
        return false;
    }


}
