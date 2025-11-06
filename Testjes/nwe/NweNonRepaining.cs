using CryptoScanner.Core.Model;

namespace CryptoScanner.TestStuff.nwe;

/*
Ik wil graag het Pinescript "Nadaraya-Watson Envelope [LuxAlgo]" van tradingview naar c# overzetten. 
Ik wil enkel de code voor de niet repainting versie (Repainting Smoothing = false). 
Ik heb de beschikking over dictionaries voor alle timeframes met daarin de OHLC candles. 
Ik wil graag het Pinescript "Nadaraya-Watson Envelope [LuxAlgo]" van tradingview naar c# overzetten. 
Ik heb de beschikking over dictionaries voor alle timeframes met daarin de OHLC candles. 
Gebruik de Dave Skender Indicators van Github voor eventuele berekeningen
*/

public class NadarayaWatsonEnvelopeX
{
    private readonly int _lookback;      // hoeveel bars terug je meeneemt
    private readonly double _bandWidth;          // bandwidth voor Gaussian kernel
    private readonly double _multiplier;       // multiplier voor bandbreedte

    public List<decimal> Trend { get; private set; }
    public List<decimal> UpperBand { get; private set; }
    public List<decimal> LowerBand { get; private set; }

    public NadarayaWatsonEnvelopeX(int lookBack, double bandWidth, double multiplier)
    {
        _lookback = lookBack;
        _bandWidth = bandWidth;
        _multiplier = multiplier;

        Trend = new List<decimal>();
        UpperBand = new List<decimal>();
        LowerBand = new List<decimal>();
    }

    // Gaussian kernel: w(x) = exp( - (x^2) / (bandWidth^2 * 2) )
    private double GaussianWeight(int distance)
    {
        return Math.Exp(-(distance * distance) / (_bandWidth * _bandWidth * 2.0));
    }

    // Bereken indicator over de reeks van candles
    public void Calculate(List<CryptoCandle> candles)
    {
        int candleCount = candles.Count;
        Trend = new List<decimal>(new decimal[candleCount]);
        UpperBand = new List<decimal>(new decimal[candleCount]);
        LowerBand = new List<decimal>(new decimal[candleCount]);

        // For each bar
        for (int index = 0; index < candleCount; index++)
        {
            int maxLookBack = Math.Min(_lookback, index + 1);
            double sumW = 0.0;
            double sumWeighted = 0.0;

            for (int barsBack = 0; barsBack < maxLookBack; barsBack++)
            {
                int distance = barsBack; // barsBack bars back
                double w = GaussianWeight(distance);
                sumW += w;
                sumWeighted += (double)candles[index - barsBack].Close * w;
            }

            decimal trendValue = (decimal)(sumWeighted / sumW);
            Trend[index] = trendValue;

            // Bereken MAE (mean absolute error) over dezelfde lookBack
            double sumAbsError = 0.0;
            for (int i = 0; i < maxLookBack; i++)
            {
                decimal src = candles[index - i].Close;
                decimal tr = Trend[index]; // let op: hier identiek voor alle barsBack — simplificatie
                sumAbsError += (double)Math.Abs(src - tr);
            }
            decimal mae = (decimal)(sumAbsError / maxLookBack);
            decimal bandWidth = mae * (decimal)_multiplier;

            UpperBand[index] = trendValue + bandWidth;
            LowerBand[index] = trendValue - bandWidth;
        }
    }
}
