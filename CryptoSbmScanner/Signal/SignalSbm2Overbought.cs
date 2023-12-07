using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalSbm2Overbought : SignalSbmBaseOverbought
{
    public SignalSbm2Overbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Sbm2;
    }



    public bool IsInLowerPartOfBollingerBands()
    {
        int candleCount = GlobalData.Settings.Signal.Sbm2CandlesLookbackCount;

        // Is de prijs onlangs dicht bij de onderste bb geweest?
        // In deze aanpak is de afstand van sma tot de upperband 100%

        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            decimal value = (decimal)last.CandleData.BollingerBandsUpperBand;
            value -= (decimal)last.CandleData.BollingerBandsDeviation * GlobalData.Settings.Signal.Sbm2BbPercentage / 100m;

            if (GlobalData.Settings.Signal.Sbm2UseLowHigh)
            {
                if (last.High >= value)
                    return true;
            }
            else
            {
                if (last.Open >= value || last.Close >= value)
                    return true;
            }

            // Dave bb.PercentB begint bij 0% op de onderste bb, de bovenste bb is 100%
            // Dat is eigenlijk precies andersom dan wat we in gedachten hebben
            // Onderstaande berekening doet het andersom, bovenste is 0% en onderste is 100%
            //decimal value = 100m * (decimal)last.CandleData.BollingerBandsUpperBand / (decimal)last.Close;
            //if (value <= percentage)
            //  return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!IsInLowerPartOfBollingerBands())
        {
            ExtraText = "geen hoge prijs in de laatste x candles";
            return false;
        }

        return true;
    }


}
