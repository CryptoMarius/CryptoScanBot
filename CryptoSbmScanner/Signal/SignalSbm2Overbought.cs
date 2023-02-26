using System;

namespace CryptoSbmScanner
{

    // De SBM methode die Marco hanteerd

    public class SignalSbm2Overbought : SignalSbmBase
    {
        public SignalSbm2Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeShort;
            SignalStrategy = SignalStrategy.strategySbm2Overbought;
        }


        public override bool IndicatorsOkay()
        {
            if (!IndicatorCandleOkay(CandleLast))
                return false;
            return true;
        }


        public bool IsInLowerPartOfBollingerBands(int candleCount = 10, decimal percentage = 1.0m)
        {
            // Is de prijs onlangs dicht bij de onderste bb geweest?
            CryptoCandle last = CandleLast;
            while (candleCount > 0)
            {
                // Dave bb.PercentB begint bij 0% op de onderste bb, de bovenste bb is 100%
                // Dat is eigenlijk precies andersom dan wat we in gedachten hebben
                // Onderstaande berekening doet het andersom, bovenste is 0% en onderste is 100%
                // Dan loopt ie gelijk met die van Marco wat makkelijker te vergelijken is
                decimal value = 100m * (decimal)last.CandleData.BollingerBands.UpperBand / (decimal)last.Close;
                if (value <= percentage) 
                    return true;

                if (!Candles.TryGetValue(last.OpenTime - 1 * Interval.Duration, out last))
                {
                    ExtraText = "geen prev candle! " + last.DateLocal.ToString();
                    return false;
                }
                if (!IndicatorCandleOkay(last))
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

            // SBM condities MA200 > MA50 > MA20 >= PSAR
            if (!CandleLast.IsSbmConditionsOverbought(true))
            {
                ExtraText = "geen sbm condities";
                return false;
            }

            if (!IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm2UpperPartOfBbPercentage))
                return false;

            if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm2CandlesForMacdRecovery))
                return false;

            if (CheckMaCrossings())
                return false;

            return true;
        }


    }
}
