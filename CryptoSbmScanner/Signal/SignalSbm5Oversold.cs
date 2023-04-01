using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// De SBM methode die Marco hanteerd (de candle jump variant)

public class SignalSbm5Oversold : SignalSbmBaseOversold
{
    public SignalSbm5Oversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.sbm5Oversold;
    }


    public bool HasDistanceIncreased(int candleCount, double percentage)
    {
        if (candleCount <= 0)
            return false;

        double beforeValue = double.MaxValue;

        CryptoCandle last = CandleLast;
        while (candleCount > 0)
        {
            double value;
            value = (double)last.CandleData.Sma50 - (double)last.CandleData.Sma20;
            if (value < beforeValue)
                beforeValue = value;

            // crossed, oh dear
            if (last.CandleData.Sma50 <= last.CandleData.Sma20)
                return false;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        // NB: Ik denk dat we alleen de laatste value willen hebben (zodat het niet van max naar min gaat)
        // Daar komt waarschijnlijk ook de verwarring weg met de voorgaande oplossing

        double lastValue = (double)CandleLast.CandleData.Sma50 - (double)CandleLast.CandleData.Sma20;
        double bbDiffPerc = 100 * lastValue / beforeValue;

        if (bbDiffPerc < percentage)
        {
            ExtraText = string.Format("Niet genoeg uit elkaar gegaan {0:N8} {1:N8}", bbDiffPerc, percentage);
            return false;
        }

        ExtraText = bbDiffPerc.ToString("N2") + "%";
        return true;
    }



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!HasDistanceIncreased(30, 1.6))
            return false;

        return true;
    }


}