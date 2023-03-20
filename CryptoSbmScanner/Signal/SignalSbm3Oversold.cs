using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De SBM methode die Marco hanteerd (de candle jump variant)

public class SignalSbm3Oversold : SignalSbmBase
{
    public SignalSbm3Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.strategySbm3Oversold;
    }


    public bool HasPriceDecreased(int candleCount = 5, decimal percentage = 1.5m)
    {
        // Een waarde die plotseling ~2% hoger of lager ligt dan de vorige candle kan interressant 
        // zijn, ook als dat binnen de bollinger bands plaats vindt (dit is dus aanvullend 
        // ten opzichte van een koers drop ten opzichte van de lower of upper bollinger bands)
        // Ook hier wil je waarschijnlijk meer van de vorige candles meenemen, mijn voorstel is om de 
        // laatste x candles te bekijken en als de totale val meer dan x% is deze melden. Dat lijkt 
        // te werken, maar is het wel interressant genoeg?
        if (candleCount <= 0)
            return false;

        decimal minValue = (decimal)CandleLast.CandleData.BollingerBandsPercentage;
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            decimal value;
            value = (decimal)last.CandleData.BollingerBandsPercentage;
            if (value < minValue)
                minValue = value;

            if (!Candles.TryGetValue(last.OpenTime - 1 * Interval.Duration, out last))
            {
                ExtraText = "geen prev candle! " + last.DateLocal.ToString();
                return false;
            }

            candleCount--;
        }

        // NB: Ik denk dat we alleen de laatste value willen hebben (zodat het niet van max naar min gaat)
        // Daar komt waarschijnlijk ook de verwarring weg met de voorgaande oplossing

        decimal maxValue = (decimal)CandleLast.CandleData.BollingerBandsPercentage;
        decimal bbDiffPerc = 100 * maxValue / minValue;

        if (bbDiffPerc < percentage)
        {
            ExtraText = string.Format("Niet genoeg gevallen {0:N8} {1:N8}", bbDiffPerc, percentage);
            return false;
        }

        ExtraText = bbDiffPerc.ToString("N2") + "%";
        return true;
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage.ToString("N2");
            return false;
        }

        if (!CandleLast.IsSbmConditionsOversold(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.Sbm3CandlesForMacdRecovery))
            return false;

        if (!HasPriceDecreased(GlobalData.Settings.Signal.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm3CandlesBbRecoveryPercentage))
            return false;


        if (CheckMaCrossings())
            return false;

        return true;
    }




    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.


        if (!IsMacdRecoveryOversold())
            return false;


        if (!Candles.TryGetValue(CandleLast.OpenTime - 1 * Interval.Duration, out CryptoCandle CandlePrev1))
        {
            ExtraText = "No prev1";
            return false;
        }

        // Herstel? Verbeterd de RSI
        if (CandleLast.CandleData.Rsi.Rsi.Value < CandlePrev1.CandleData.Rsi.Rsi.Value)
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last)", CandlePrev1.CandleData.Rsi.Rsi.Value, CandleLast.CandleData.Rsi.Rsi.Value);
            return false;
        }

        ExtraText = "Alles lijkt goed";
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";

        // Als de prijs alweer boven de sma zit ophouden
        if (Math.Max(CandleLast.Open, CandleLast.Close) >= (decimal)CandleLast.CandleData.BollingerBands.Sma.Value)
            return true;

        // Als de psar bovenin komt te staan
        //if (((decimal)CandleLast.CandleData.PSar > Math.Max(CandleLast.Open, CandleLast.Close)))
        //    return true;


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if (CandleLast.OpenTime - signal.EventTime > 3 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        if (!IsMacdRecoveryOversold())
            return true;

        if ((decimal)CandleLast.CandleData.PSar < CandleLast.Low)
        {
            ExtraText = string.Format("De PSAR staat onder de low {0:N8}", CandleLast.CandleData.PSar);
            return true;
        }

        ExtraText = "";
        return false;
    }


}
