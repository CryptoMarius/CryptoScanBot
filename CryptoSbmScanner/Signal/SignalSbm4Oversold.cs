using System.Text;

using CryptoExchange.Net.CommonObjects;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;



public class SignalSbm4Oversold : SignalSbmBaseOversold
{
    public SignalSbm4Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = CryptoTradeDirection.Long;
        SignalStrategy = SignalStrategy.Sbm4;
    }

    public bool HadStobbInThelastXCandles(int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Er een candle onder de bb opent of sluit & een oversold situatie (beide moeten onder de 20 zitten)
            if ((last.IsBelowBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh)) && (last.IsStochOversold()))
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }

    public bool IsInLowerPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Dave bb.PercentB begint bij 0% op de onderste bb, de bovenste bb is 100%
            // Dat is eigenlijk precies andersom dan wat we in gedachten hebben
            // Onderstaande berekening doet het andersom, bovenste is 0% en onderste is 100%
            decimal value = 100m * (decimal)last.CandleData.BollingerBandsLowerBand / (decimal)last.Close;
            if (value >= percentage)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    public bool HasBollingerBandsIncreased(int candleCount, decimal percentage)
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


        //ExtraText = "";

        //// De breedte van de bb is ten minste 1.5%
        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.SbmBBMinPercentage, GlobalData.Settings.Signal.SbmBBMaxPercentage))
        //{
        //    ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
        //    return false;
        //}

        //// De ma lijnen en psar goed staan
        //if (!CandleLast.IsSbmConditionsOversold(false))
        //{
        //    ExtraText = "geen sbm condities";
        //    return false;
        //}

        //// Er recovery is via de macd
        //if (!IsMacdRecoveryOversold(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
        //    return false;


        if (HadStobbInThelastXCandles(GlobalData.Settings.Signal.Sbm1CandlesLookbackCount) 
            || IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm2BbPercentage)
            || HasBollingerBandsIncreased(GlobalData.Settings.Signal.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm3CandlesBbRecoveryPercentage))
            {
            SignalCreate.GetFluxIndcator(Symbol, out int fluxOverSold, out int _);
            if (fluxOverSold == 100)
                return true;
        }

        return false;
    }

}