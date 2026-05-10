using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicator;

namespace CryptoScanner.Core.Signal.Other;

public class SignalLuxNadarayaWatsonEnvelopeNp : SignalCreateBase
{

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
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
        //    return false

        perc = 0;
        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
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
                if (!CandleLast!.IsSbmConditionsOversold())
                {
                    response = "no sbm conditions";
                    return false;
                }
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (!CandleLast.IsSbmConditionsOverbought())
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
                    !data.IsPercentageSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !data.IsPercentageSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !data.IsPercentageSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(out response))
                    return false;
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                    !data.IsPercentageSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                    !data.IsPercentageSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                    !data.IsPercentageSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
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

        ExtraText = "";
        if (!GetPrevCandle(CandleLast, out MyData? candlePrev))
            return false;


        NweIndicator indicator = new(
            bandwidth: (double)GlobalData.Settings.Signal.Nwe.BandWidth,
            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
            smoothRepainting: false
           );
        var candles = SymbolInterval.CandleList;
        var nwe = indicator.Calculate(candles);
        var nweLast = nwe[^1];

        // buy alert
        if (SignalSide == CryptoTradeSide.Long && nweLast.Lower != null)
        {
            // Candle outside the band // && EnoughMomentum(nwe, max, out decimal _)
            decimal? lowerband = nweLast.Lower;
            //if (CandleLast!.Candle.Open <= lowerband && CandleLast.Candle.Close <= lowerband )
            //if (candlePrev!.Candle.Close > lowerband && CandleLast.Candle.Close <= lowerband)
            if (CandleLast!.Candle.Close < lowerband && CandleLast!.Candle.Open < lowerband
                && CandleLast.Candle.Close > CandleLast!.Candle.Open)
            {
                //ExtraText = $"{angle_degrees2:N2}�, {perc1:N2}%";
                return true;
            }
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short && nweLast.Upper != null)
        {
            // Candle outside the band //&& EnoughMomentum(nwe, max, out decimal _)
            decimal? upperband = nweLast.Upper;
            if (CandleLast!.Candle.Close > upperband && CandleLast!.Candle.Open > upperband
                && CandleLast.Candle.Close < CandleLast!.Candle.Open)
            {
                //ExtraText = $"{angle_degrees2:N2}�, {perc3:N2}%";
                return true;
            }
        }

        return false;
    }

}
