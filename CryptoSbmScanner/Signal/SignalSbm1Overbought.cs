using System;

namespace CryptoSbmScanner
{

    // De officiele SBM methode van Maurice Orsel

    public class SignalSbm1Overbought : SignalSbmBase
    {
        public SignalSbm1Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeShort;
            SignalStrategy = SignalStrategy.strategySbm1Overbought;
        }


        public override bool IndicatorsOkay()
        {
            if (!IndicatorCandleOkay(CandleLast))
                return false;
            return true;
        }


        public override bool IsSignal()
        {
            ExtraText = "";

            // De breedte van de bb is ten minste 1.5%
            if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.AnalysisBBMinPercentage, 100))
            {
                ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage.ToString("N2");
                return false;
            }

            if (!CandleLast.IsSbmConditionsOverbought())
            {
                ExtraText = "geen sbm condities";
                return false;
            }


            // Er een candle onder de bb opent of sluit
            if (!CandleLast.IsAboveBollingerBands())
            {
                ExtraText = "niet beneden de bb";
                return false;
            }

            // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
            if (!CandleLast.IsStochOverbought())
            {
                ExtraText = "stoch niet oversold";
                return false;
            }

            if (CheckMaCrossings())
                return false;

            return true;
        }


    }
}
