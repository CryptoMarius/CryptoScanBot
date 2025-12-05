using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Momentum;

namespace CryptoScanner.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelope: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelope(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    private static bool EnoughMomentum(List<decimal> nwe, int max, out decimal perc)
    {
        // We noticed weak turn's
        int count = 15;
        decimal diff = 0;
        decimal value = nwe[max - 1];
        for (int i = max - 2; i > 0; i--)
        {
            var o2 = nwe[i];
            decimal d = Math.Abs(o2 - value);
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


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (GlobalData.Settings.Signal.Nwe.OnlyIfLux5m && SignalSide == CryptoTradeSide.Long)
        {
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (CandleLast.CandleData!.Lux5mValue > -50)
                {
                    response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%)";
                    return false;
                }
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (CandleLast.CandleData!.Lux5mValue < 50)
                {
                    response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%)";
                    return false;
                }
            }
        }

        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Nwe.IncludeSoftSbm && SignalSide == CryptoTradeSide.Long)
        {
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (!CandleLast!.SbmConditionsOversold(false))
                {
                    response = "no sbm conditions";
                    return false;
                }
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (!CandleLast.IsSbmConditionsOverbought(false))
                {
                    response = "no sbm conditions";
                    return false;
                }
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Nwe.IncludeSbmPercAndCrossing)
        {
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(out response))
                    return false;
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !candle.IsPercentageSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.IsPercentageSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.IsPercentageSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(out response))
                    return false;
            }
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Nwe.IncludeRsi)
        {
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (!CandleLast.RsiOversold())
                {
                    response = "rsi not oversold";
                    return false;
                }
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (!CandleLast.RsiOverbought())
                {
                    {
                        response = "rsi not overbought";
                        return false;
                    }
                }
            }
        }

        if (HadStorsiInThelastXCandles(SignalSide, 0, 10, 4) == null && HadStobbInThelastXCandles(SignalSide, 0, 10) == null)
        {
            response = "no previous storsi/stobb found";
            return false;
        }

        response = "";
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
        decimal bandWidth = GlobalData.Settings.Signal.Nwe.BandWidth;
        decimal multplier = GlobalData.Settings.Signal.Nwe.Multiplication;

        // Iterate the last 500 candles
        int maxlen = 500;
        int max = Math.Min(maxlen, SymbolInterval.CandleList.Count - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        decimal sae = 0;
        List<decimal> nwe = [];

        // Compute and set NWE points
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean
            decimal sum = 0;
            decimal sumw = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (double)(bandWidth * bandWidth * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sum += candlej.Close * w;
                sumw += w;
            }
            decimal y2 = sum / sumw;

            long openTime = offsett - i * Interval.Duration;
            if (SymbolInterval.CandleList.TryGetValue(openTime, out CryptoCandle? candlei))
            {
                sae += Math.Abs(candlei.Close - y2);
            }
            nwe.Add(y2);
        }
        sae = sae / max * multplier;


        ExtraText = "";
        nwe.Reverse();
        var candlePrev = nwe[max - 2];
        decimal nweValue = nwe[max - 1];

        // buy alert
        if (SignalSide == CryptoTradeSide.Long)
        {
            decimal lowerband = nweValue - sae;
            // Candle outside the band
            if (CandleLast!.Open <= lowerband && CandleLast.Close <= lowerband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc1:N2}%";
                return true;
            }
            // Candle sticking pearsing trough the band
            if (candlePrev! > lowerband && CandleLast.Close <= lowerband && EnoughMomentum(nwe, max, out decimal _))
            {
               //ExtraText = $"{angle_degrees2:N2}°, {perc2:N2}%";
                return true;
            }
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short)
        {
            decimal upperband = nweValue + sae;
            // Candle outside the band
            if (CandleLast!.Open >= upperband && CandleLast.Close >= upperband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc3:N2}%";
                return true;
            }
            // Candle sticking pearsing trough the band
            if (candlePrev! < upperband && CandleLast.Close >= upperband && EnoughMomentum(nwe, max, out decimal _))
            {
                //ExtraText = $"{angle_degrees2:N2}°, {perc4:N2}%";
                return true;
            }
        }

        return false;
    }

}
