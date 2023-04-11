using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalSbm2Oversold : SignalSbmBaseOversold
{
    public SignalSbm2Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.sbm2Oversold;
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



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm2LowerPartOfBbPercentage))
        {
            ExtraText = "geen lage prijs in de laatste x candles";
            return false;
        }

        return true;
    }


}
