using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Signal.Momentum;

public class SignalSbm4Short : SignalSbmBaseShort
{
    public SignalSbm4Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Sbm4;
    }

    private bool HadStobbInThelastXCandles(int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Er een candle onder de bb opent of sluit & een oversold situatie (beide moeten onder de 20 zitten)
            if (last.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last.IsStochOverbought())
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    private bool HasBollingerBandsIncreased(int candleCount, decimal percentage)
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

            if (!GetPrevCandle(last, out last))
                return false;
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
        if (!base.IsSignal())
            return false;


        if (HadStobbInThelastXCandles(GlobalData.Settings.Signal.Sbm.Sbm1CandlesLookbackCount)
            || IsInUpperPartOfBollingerBands(GlobalData.Settings.Signal.Sbm.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm2BbPercentage)
            || HasBollingerBandsIncreased(GlobalData.Settings.Signal.Sbm.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage))
        {
            SignalCreate.GetFluxIndcator(Symbol, out int fluxOverSold, out int _);
            if (fluxOverSold == 100)
            {
                // Er recovery is via de macd
                if (IsMacdRecoveryOverbought(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
                    return true;
            }
        }

        return false;
    }

}