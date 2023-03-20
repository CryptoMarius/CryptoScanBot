using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De SBM methode die Marco hanteerd

public class SignalSbm2Oversold : SignalSbmBase
{
    public SignalSbm2Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.strategySbm2Oversold;
    }


    public bool IsInLowerPartOfBollingerBands(int candleCount = 10, decimal percentage = 99.0m)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Dave bb.PercentB begint bij 0% op de onderste bb, de bovenste bb is 100%
            // Dat is eigenlijk precies andersom dan wat we in gedachten hebben
            // Onderstaande berekening doet het andersom, bovenste is 0% en onderste is 100%
            // Dan loopt ie gelijk met die van Marco wat makkelijker te vergelijken is
            decimal value = 100m * (decimal)last.CandleData.BollingerBands.LowerBand / last.Close;
            if (value >= percentage)
                return true;

            if (!Candles.TryGetValue(last.OpenTime - 1 * Interval.Duration, out last))
            {
                ExtraText = "geen prev candle! " + last.DateLocal.ToString();
                return false;
            }
            if (!IndicatorsOkay(last))
                return false;

            candleCount--;
        }

        return false;
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

        if (!IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm2LowerPartOfBbPercentage))
        {
            ExtraText = "geen lage prijs in de laatste x candles";
            return false;
        }
            }

        if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.Sbm2CandlesForMacdRecovery))
            return false;

        if (CheckMaCrossings())
            return false;

        return true;
    }




    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh))
        {
            ExtraText = "beneden de bb";
            return false;
        }


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
        {
            ExtraText = "Candle above SMA20";
            return true;
        }

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
