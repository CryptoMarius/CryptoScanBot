using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelopeSlope: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelopeSlope(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // configuration:
        // configuration:
        double h = GlobalData.Settings.Signal.Nwe.BandWidth;
        double mult = GlobalData.Settings.Signal.Nwe.Multiplication;

        // Iterate the last 500 candles
        int maxlen = 500;
        int n = SymbolInterval.CandleList.Count;
        int max = Math.Min(maxlen, n - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) slopeObject in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige slopeObject.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        List<CryptoCandle> nwe = [];
        double sae = 0;

        // Compute and set NWE points 
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean 
            double sum = 0;
            double sumw = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                double w = Math.Exp(-(Math.Pow(i - j, 2)) / (h * h * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sum += (double)candlej.Close * w;
                sumw += w;
            }

            double y2 = (double)(sum / sumw);
            nwe.Add(new CryptoCandle { 
                Close = (decimal)y2, 
                Open = (decimal)y2, 
                High = (decimal)y2, 
                Low = (decimal)y2, 
                Volume = 0, 
                OpenTime = 0 
            });

            if (SymbolInterval.CandleList.TryGetValue(offsett - i * Interval.Duration, out CryptoCandle? candlei))
                sae += Math.Abs((double)candlei.Close - y2);
        }
        sae = sae / max * mult;


        ExtraText = "";
        nwe.Reverse();
        List<SlopeResult> slopeNweList = (List<SlopeResult>)nwe.GetSlope(2);


        // buy alert
        if (SignalSide == CryptoTradeSide.Long)
        {
            var slopeObject = slopeNweList[max-1];
            if (slopeObject.Slope < 0)
                return false;

            slopeObject = slopeNweList[max - 2];
            if (slopeObject.Slope >= 0)
                return false;
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short)
        {
            var slopeObject = slopeNweList[max - 1];
            if (slopeObject.Slope > 0)
                return false;

            slopeObject = slopeNweList[max - 2];
            if (slopeObject.Slope <= 0)
                return false;
        }


        // We noticed weak turn's
        int count = GlobalData.Settings.Signal.Nwe.CandleCountSlope;
        var o = nwe[max - 1];
        decimal value = o.Close;
        decimal diff = 0;
        for (int i = max - 2; i > 0; i--)
        {
            var o2 = nwe[i];
            decimal d = Math.Abs(o2.Close - value);
            if (d > diff)
                diff = d;

            count--;
            if (count == 0)
                break;
        }

        // less than 1% change is not enough
        if (diff / value < (GlobalData.Settings.Signal.Nwe.IgnorePercentage / 100))
            return false;

        return true;
    }

}
