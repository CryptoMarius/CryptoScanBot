using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Momentum;

namespace CryptoScanner.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelopePull: SignalCreateBase
{
    public SignalLuxNadarayaWatsonEnvelopePull(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
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
        //// We noticed weak turn's
        //int count = 15;
        //decimal diff = 0;
        //decimal value = nwe[max - 1];
        //for (int i = max - 2; i > 0; i--)
        //{
        //    var o2 = nwe[i];
        //    decimal d = Math.Abs(o2 - value);
        //    if (d > diff)
        //        diff = d;

        //    count--;
        //    if (count == 0)
        //        break;
        //}

        //// less than x% change is not enough
        //perc = 100 * diff / value;
        //if (perc < 0.25m)
        //    return true; // false
        perc = 0;
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
                    !candle.Sma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.Sma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.Sma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(out response))
                    return false;
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !candle.IsSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !candle.IsSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !candle.IsSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
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

    private (List<decimal> nwe, decimal sae, int max) Calculate()
    {
        // configuration:
        decimal bandWidth = GlobalData.Settings.Signal.Nwe.BandWidth;

        // Iterate the last 500 candles (huge!)
        int maxlen = 500;
        int max = Math.Min(maxlen, SymbolInterval.CandleList.Count - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in
        // de chart context (=de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = CandleLast.OpenTime; // - max * Interval.Duration;

        decimal sae = 0;
        List<decimal> nwe = [];

        // Compute and set NWE points
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean
            decimal sumWeighted = 0;
            decimal sumSimple = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (double)(bandWidth * bandWidth * 2));
                if (SymbolInterval.CandleList.TryGetValue(offsett - j * Interval.Duration, out CryptoCandle? candlej))
                    sumWeighted += candlej.Close * w;
                sumSimple += w;
            }
            decimal y2 = sumWeighted / sumSimple;

            long openTime = offsett - i * Interval.Duration;
            if (SymbolInterval.CandleList.TryGetValue(openTime, out CryptoCandle? candlei))
            {
                sae += Math.Abs(candlei.Close - y2);
            }
            nwe.Add(y2);
        }
        nwe.Reverse();

        decimal multplier = GlobalData.Settings.Signal.Nwe.Multiplication;
        sae = sae / max * multplier;
        return (nwe, sae, max);
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // First, check ik %K goes in the right direction
        bool hasK = false;
        if (GetPrevCandle(CandleLast, out CryptoCandle? candlePrev))
        {
            if (SignalSide == CryptoTradeSide.Long)
                hasK = candlePrev!.CandleData!.StochOscillator < 25 && CandleLast.CandleData!.StochOscillator > 25;
            else
                hasK = candlePrev!.CandleData!.StochOscillator > 75 && CandleLast.CandleData!.StochOscillator < 75;
        }
        if (!hasK)
            return false;


        // Then check is there was a valid NEW signal before
        var (nwe, sae, max) = Calculate();

        int candles = 6;
        bool hadNwe = false;
        CryptoCandle candleLast = CandleLast;
        for (int i = max; i > 0; i--)
        {
            if (candles-- < 0)
                break;
            decimal nwePrev = nwe[i - 2];
            decimal nweLast = nwe[i - 1];

            // buy alert
            if (SignalSide == CryptoTradeSide.Long)
            {
                decimal lowerband = nweLast - sae;
                // Candle outside the band
                if (CandleLast!.Open <= lowerband && candleLast!.Close <= lowerband && EnoughMomentum(nwe, i, out decimal _))
                {
                    hadNwe = true;
                    break;
                }
                // Candle sticking pearsing trough the band
                if (nwePrev! > lowerband && candleLast!.Close <= lowerband && EnoughMomentum(nwe, i, out decimal _))
                {
                    hadNwe = true;
                    break;
                }
            }

            // sell alert
            if (SignalSide == CryptoTradeSide.Short)
            {
                decimal upperband = nweLast + sae;
                // Candle outside the band
                if (CandleLast!.Open >= upperband && candleLast.Close >= upperband && EnoughMomentum(nwe, i, out decimal _))
                {
                    hadNwe = true;
                    break;
                }
                // Candle sticking pearsing trough the band
                if (nwePrev! < upperband && candleLast.Close >= upperband && EnoughMomentum(nwe, i, out decimal _))
                {
                    hadNwe = true;
                    break;
                }
            }

            if (!GetPrevCandle(candleLast, out candleLast))
                return false;
        }
        return hadNwe;
    }

}
