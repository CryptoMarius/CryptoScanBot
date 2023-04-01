using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De SBM methode die Marco hanteerd

public class SignalSbm2Overbought : SignalSbmBaseOverbought
{
    public SignalSbm2Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeShort;
        SignalStrategy = SignalStrategy.sbm2Overbought;
    }



    public bool IsInLowerPartOfBollingerBands(int candleCount = 10, decimal percentage = 99.50m)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            // Dave bb.PercentB begint bij 0% op de onderste bb, de bovenste bb is 100%
            // Dat is eigenlijk precies andersom dan wat we in gedachten hebben
            // Onderstaande berekening doet het andersom, bovenste is 0% en onderste is 100%
            // Dan loopt ie gelijk met die van Marco wat makkelijker te vergelijken is
            decimal value = 100m * (decimal)last.CandleData.BollingerBandsUpperBand / (decimal)last.Close;
            if (value <= percentage)
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

        // SBM condities MA200 > MA50 > MA20 >= PSAR
        if (!CandleLast.IsSbmConditionsOverbought(true))
        {
            ExtraText = "geen sbm condities";
            return false;
        }

        if (!IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm2UpperPartOfBbPercentage))
        {
            ExtraText = "geen hoge prijs in de laatste x candles";
            return false;
        }

        //if (!IsMacdRecoveryOverbought(GlobalData.Settings.Signal.SbmCandlesForMacdRecovery))
        //{
        //    ExtraText = "Heen herstel van MACD";
        //    return true;
        //}

        //if (CheckMaCrossings())
        //    return false;

        return true;
    }


}
