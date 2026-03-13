using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Helpers;

public static class SignalCreateBaseHelper
{
    public static bool IsMacdRecoveryOversold(this SignalCreateBase myBase, int candleCount)
    {
        // Is there "recovery" (a lighter macd bar)
        MyData last = myBase.CandleLast!;
        while (candleCount-- > 0)
        {
            if (!myBase.GetPrevCandle(last, out MyData? prev))
                return false;

            if (last.CandleData?.MacdHistogram <= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }


    public static bool IsMacdRecoveryOverbought(this SignalCreateBase myBase, int candleCount)
    {
        // Is there "recovery" (a lighter macd bar)
        MyData? last = myBase.CandleLast;

        while (candleCount-- > 0)
        {
            if (!myBase.GetPrevCandle(last, out MyData? prev))
                return false;

            if (last.CandleData?.MacdHistogram >= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }


    public static bool HadStobbInThelastXCandlesOversold(this SignalCreateBase myBase, int candleCount)
    {
        MyData? last = myBase.CandleLast;
        while (candleCount > 0)
        {
            // Closes or opens below the bb & stochastic oversold situation 
            if (last!.IsBelowBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last!.StochOversold())
                return true;

            if (!myBase.GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    public static bool IsStobbInThelastXCandlesOverbought(this SignalCreateBase myBase, int candleCount)
    {
        MyData? last = myBase.CandleLast;
        while (candleCount > 0)
        {
            if (last == null)
                return false;
            // Closes or opens above the bb & stochastic overbought situation 
            if (last!.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last.StochOverbought())
                return true;

            if (!myBase.GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    public static bool IsBollingerBandsIncreased(this SignalCreateBase myBase, int candleCount = 5, decimal percentage = 1.5m)
    {
        // Een waarde die plotseling ~2% hoger of lager ligt dan de vorige candle kan interressant 
        // zijn, ook als dat binnen de bollinger bands plaats vindt (dit is dus aanvullend 
        // ten opzichte van een koers drop ten opzichte van de lower of upper bollinger bands)
        // Ook hier wil je waarschijnlijk meer van de vorige candles meenemen, mijn voorstel is om de 
        // laatste x candles te bekijken en als de totale val meer dan x% is deze melden. Dat lijkt 
        // te werken, maar is het wel interressant genoeg?
        if (candleCount <= 0)
            return false;

        MyData? last = myBase.CandleLast;
        decimal minValue = (decimal)last.CandleData!.BollingerBandsPercentage!;
        while (candleCount > 0)
        {
            decimal value;
            value = (decimal)last!.CandleData!.BollingerBandsPercentage!;
            if (value < minValue)
                minValue = value;

            if (!myBase.GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }
        if (minValue == 0)
            return false;

        // NB: Ik denk dat we alleen de laatste value willen hebben (zodat het niet van max naar min gaat)
        // Daar komt waarschijnlijk ook de verwarring weg met de voorgaande oplossing

        decimal maxValue = (decimal)myBase.CandleLast.CandleData!.BollingerBandsPercentage!;
        decimal bbDiffPerc = 100 * maxValue / minValue;

        if (bbDiffPerc < percentage)
        {
            myBase.ExtraText = string.Format("Niet genoeg gestegen {0:N8} {1:N8}", bbDiffPerc, percentage);
            return false;
        }

        myBase.ExtraText = bbDiffPerc.ToString("N2") + "%";
        return true;
    }
}