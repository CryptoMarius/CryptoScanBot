using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelopeCross: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelopeCross(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing, implements both long and short
    }


    private static bool EnoughMomentum(List<CryptoCandle> nwe, int max, out decimal perc)
    {
        // We noticed weak turn's
        int count = 15;
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

        // less than x% change is not enough
        perc = 100 * diff / value;
        if (perc < 0.25m)
            return true; // false

        return true;
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
        decimal h = GlobalData.Settings.Signal.Nwe.BandWidth;
        decimal mult = GlobalData.Settings.Signal.Nwe.Multiplication;

        // Iterate the last 500 candles
        int maxlen = 500;
        int n = SymbolInterval.CandleList.Count;
        int max = Math.Min(maxlen, n - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        // TODO: apply ratio? so we have the right "angle and degrees"
        double tijdRange = 500 * Interval.Duration / 60;
        double prijsRange = 22000.0 - 20000.0;
        double chartBreedte = 100.0;
        double chartHoogte = 100.0;

        double xPerPixel = tijdRange / chartBreedte;
        double yPerPixel = prijsRange / chartHoogte;
        double verhouding = xPerPixel / yPerPixel;

        List<CryptoCandle> nwe = [];
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
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (double)(h * h * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sum += candlej.Close * w;
                sumw += w;
            }
            decimal y2 = sum / sumw;

            if (SymbolInterval.CandleList.TryGetValue(offsett - i * Interval.Duration, out CryptoCandle? candlei))
                sae += Math.Abs(candlei.Close - y2);

            nwe.Add(new CryptoCandle { 
                Close = y2, 
                Open = y2, 
                High = y2, 
                Low = y2, 
                Volume = 0, 
                OpenTime = candlei!.OpenTime,
                CandleData = candlei?.CandleData,
            });
        }
        sae = sae / max * mult;


        ExtraText = "";
        nwe.Reverse();
        List<SlopeResult> slopeNweList = (List<SlopeResult>)nwe.GetSlope(2);
        var slopeNweLast = slopeNweList[max - 1];
        // Calculate the angle in degrees
        //double angle_radians = Math.Atan(slopeNweLast.Slope ?? 0);
        //double angle_degrees = angle_radians * (180 / Math.PI);
        double angle_degrees2 = (180 / Math.PI) * (slopeNweLast.Slope ?? 0);

        var candleLast = nwe[max - 1];
        var candlePrev = nwe[max - 2];

        if (candleLast.CandleData == null || candlePrev.CandleData == null)
            return false;



        // Buy alert when the nwe.lower crosses the sma20 upwards
        if (SignalSide == CryptoTradeSide.Long 
            && candlePrev.Close - sae < (decimal)candlePrev.CandleData!.BollingerBandsLowerBand!
            && candleLast.Close - sae >= (decimal)candleLast.CandleData!.BollingerBandsLowerBand!
            && EnoughMomentum(nwe, max, out decimal perc1)
            && HadStobbInThelastXCandles(CryptoTradeSide.Long, 0, 15) != null) //&& angle_degrees2 > 0 
        {
            ExtraText = $"nwe.lower crossed bb.lower upwards {angle_degrees2:N2}°, {perc1:N2}%";
            return true;
        }

        // Sell alert when the nwe.upper crosses the bb.upper downwards
        if (SignalSide == CryptoTradeSide.Short
            && candlePrev.Close + sae > (decimal)candlePrev.CandleData!.BollingerBandsUpperBand!
            && candleLast.Close + sae <= (decimal)candleLast.CandleData!.BollingerBandsUpperBand!
            && EnoughMomentum(nwe, max, out decimal perc2)
            && HadStobbInThelastXCandles(CryptoTradeSide.Short, 0, 15) != null) // && angle_degrees2 < 0 
        {
            ExtraText = $"nwe.upper crossed bb.upper downwards {angle_degrees2:N2}°, {perc2:N2}%";
            return true;
        }

        return false;
    }

}
