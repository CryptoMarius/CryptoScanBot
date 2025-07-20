using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelope: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelope(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing, implements both long and short
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
        double h = 8f;
        double mult = 3.0f;

        // Iterate the last 500 candles
        int maxlen = 500;
        int n = SymbolInterval.CandleList.Count;
        int max = Math.Min(maxlen, n - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        List<decimal> nwe = [];
        decimal sae = 0;

        // Compute and set NWE points 
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean 
            decimal sum = 0;
            decimal sumw = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (h * h * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sum += candlej.Close * w;
                sumw += w;
            }
            decimal y2 = sum / sumw;
            nwe.Add(y2);

            if (SymbolInterval.CandleList.TryGetValue(offsett - i * Interval.Duration, out CryptoCandle? candlei))
                sae += Math.Abs(candlei.Close - y2);
        }
        sae = sae / max * (decimal)mult;


        if (!GetPrevCandle(CandleLast!, out CryptoCandle? candlePrev))
            return false;


        decimal nwevalue = nwe[0];
        decimal upperband = nwevalue + sae;
        decimal lowerband = nwevalue - sae;

        // buy alert
        if (SignalSide == CryptoTradeSide.Long && candlePrev!.Close > lowerband && CandleLast.Close <= lowerband)
        {
            ExtraText = "";
            return true;
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short && candlePrev!.Close < upperband && CandleLast.Close >= upperband)
        {
            ExtraText = "";
            return true;
        }

        ExtraText = "";
        return false;

    }

}
