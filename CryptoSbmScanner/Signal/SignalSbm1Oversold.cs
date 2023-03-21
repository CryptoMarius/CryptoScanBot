using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De officiele SBM methode van Maurice Orsel

public class SignalSbm1Oversold : SignalSbmBase
{
    public SignalSbm1Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.strategySbm1Oversold;
    }


    public override string DisplayText()
    {
        decimal value = -999m;

        return string.Format("ma200={0:N8} ma50={1:N8} ma20={2:N8} psar={3:N8} macd.h={4:N8} bm={5:N2} bb.w={6:N2}",
            CandleLast.CandleData.Sma200.Sma.Value,
            CandleLast.CandleData.Sma50.Sma.Value,
            CandleLast.CandleData.BollingerBands.Sma.Value,
            CandleLast.CandleData.PSar,
            CandleLast.CandleData.Macd.Histogram.Value,
            value,
            CandleLast.CandleData.BollingerBands.Width * 100
        //CandleLast.CandleData.Slope20
        );
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


        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh))
        {
            ExtraText = "niet beneden de bb";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOversold())
        {
            ExtraText = "stoch niet oversold";
            return false;
        }

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
            return true;

        // Als de psar bovenin komt te staan
        //if (((decimal)CandleLast.CandleData.PSar > Math.Max(CandleLast.Open, CandleLast.Close)))
        //    return true;


        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if (CandleLast.OpenTime - signal.EventTime > 10 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        // Duh, die is er zeker nog niet!
        //if (!IsMacdRecoveryOversold())
        //    return true;

        if ((decimal)CandleLast.CandleData.PSar < CandleLast.Low)
        {
            ExtraText = string.Format("De PSAR staat onder de low {0:N8}", CandleLast.CandleData.PSar);
            return true;
        }

        ExtraText = "";
        return false;
    }


}
