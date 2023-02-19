using System;

namespace CryptoSbmScanner
{

    // De SBM methode die Marco hanteerd (de candle jump variant)

    public class SignalSbm3Overbought : SignalSbmBase
    {
        public SignalSbm3Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeShort;
            SignalStrategy = SignalStrategy.strategySbm3Overbought;
        }


        public override bool IndicatorsOkay()
        {
            if (!IndicatorCandleOkay(CandleLast))
                return false;
            return true;
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

            decimal maxValue = decimal.MinValue;
            decimal minValue = decimal.MaxValue;
            CryptoCandle last = CandleLast;
            while (candleCount > 0)
            {
                decimal value;
                //decimal value = last.Open;
                //if (value > maxValue)
                //    maxValue = value;
                //if (value < minValue)
                //    minValue = value;
                //value = last.Close;
                //if (value > maxValue)
                //    maxValue = value;
                //if (value < minValue)
                //    minValue = value;
                value = (decimal)last.CandleData.BollingerBandsPercentage;
                if (value > maxValue)
                    maxValue = value;
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
            if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.AnalysisBBMinPercentage, 100))
            {
                ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage.ToString("N2");
                return false;
            }

            if (!CandleLast.IsSbmConditionsOverbought(true))
            {
                ExtraText = "geen sbm condities";
                return false;
            }
            
            if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
                return false;

            if (!HasPriceDecreased(GlobalData.Settings.Signal.Sbm3CandlesBbRecovery, GlobalData.Settings.Signal.Sbm3CandlesBbRecoveryPercentage))
                return false;

            if (CheckMaCrossings())
                return false;

            return true;
        }


    }
}