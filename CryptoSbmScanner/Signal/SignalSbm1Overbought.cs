using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De officiele SBM methode van Maurice Orsel

public class SignalSbm1Overbought : SignalSbmBaseOverbought
{
    public SignalSbm1Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeShort;
        SignalStrategy = SignalStrategy.sbm1Overbought;
    }


    public bool HadStobbInThelastXCandles(int candleCount = 10)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Er een candle onder de bb opent of sluit & een oversold situatie (beide moeten onder de 20 zitten)
            if ((last.IsAboveBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh)) && (last.IsStochOverbought()))
                return true;

            if (!GetPrevCandle(last, out last))
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
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (!CandleLast.IsSbmConditionsOverbought())
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        if (!HadStobbInThelastXCandles(GlobalData.Settings.Signal.Sbm1CandlesLookbackCount))
        {
            ExtraText = "geen stob in de laatste x candles";
            return false;
        }

        if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
            return false;

        //// Er een candle onder de bb opent of sluit
        //if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh))
        //{
        //    ExtraText = "niet beneden de bb";
        //    return false;
        //}

        //// Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        //if (!CandleLast.IsStochOverbought())
        //{
        //    ExtraText = "stoch niet oversold";
        //    return false;
        //}

        //if (CheckMaCrossings())
        //    return false;

        return true;
    }


}
