using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;


public class SignalSbm3Short : SignalSbmBaseShort
{
    public SignalSbm3Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Sbm3;
    }


    public bool HasBollingerBandsIncreased(int candleCount = 5, decimal percentage = 1.5m)
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
        CryptoCandle? last = CandleLast;
        while (candleCount > 0)
        {
            decimal value;
            value = (decimal)last!.CandleData.BollingerBandsPercentage;
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
            ExtraText = string.Format("Niet genoeg gestegen {0:N8} {1:N8}", bbDiffPerc, percentage);
            return false;
        }

        ExtraText = bbDiffPerc.ToString("N2") + "%";
        return true;
    }



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!HasBollingerBandsIncreased(GlobalData.Settings.Signal.Sbm.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage))
            return false;

        return true;
    }


}
