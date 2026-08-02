using CryptoScanner.Analyzers.Sbm;
using CryptoScanner.Analyzers.Stobb;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Nwe.Signal;

public class SignalNweBase : SignalCreateBase
{
    internal bool SmoothRepainting;

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

    private bool HasVolumeClimax(out string response)
    {
        int lookback = NwePlugin.Settings.VolumeClimaxLookback;
        decimal multiplier = NwePlugin.Settings.VolumeClimaxMultiplier;

        decimal sum = 0;
        int count = 0;
        for (int i = 1; i <= lookback; i++)
        {
            CandleTime t = CandleLast.Candle.OpenTime - i * Interval.Duration;
            if (!SymbolInterval.CandleList.TryGetValue(t, out CryptoCandle prev))
                break;
            sum += prev.Volume;
            count++;
        }

        if (count == 0)
        {
            response = "no prev volume data";
            return false;
        }

        decimal avg = sum / count;
        decimal ratio = avg > 0 ? CandleLast.Candle.Volume / avg : 0;
        if (CandleLast.Candle.Volume < multiplier * avg)
        {
            response = $"no volume climax (x{ratio:N2} < x{multiplier:N2})";
            return false;
        }

        response = "";
        return true;
    }

    public override bool AdditionalChecks(MyData data, out string response)
    {
        // Controle op de ma-lijnen
        if (NwePlugin.Settings.IncludeSoftSbm && SignalSide == CryptoTradeSide.Long)
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
        if (NwePlugin.Settings.IncludeSbmPercAndCrossing)
        {
            var sbm = SbmPlugin.Settings;
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (sbm.CheckMa200AndMa50Percentage &&
                    !data.IsPercentageSma200AndSma50OkayOversold(sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (sbm.CheckMa200AndMa20Percentage &&
                    !data.IsPercentageSma200AndSma20OkayOversold(sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (sbm.CheckMa50AndMa20Percentage &&
                    !data.IsPercentageSma50AndSma20OkayOversold(sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(
                    sbm.Ma200AndMa20Crossing, sbm.Ma200AndMa20Lookback,
                    sbm.Ma200AndMa50Crossing, sbm.Ma200AndMa50Lookback,
                    sbm.Ma50AndMa20Crossing, sbm.Ma50AndMa20Lookback,
                    out response))
                    return false;
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (sbm.CheckMa200AndMa50Percentage &&
                    !data.IsPercentageSma200AndSma50OkayOverbought(sbm.Ma200AndMa50Percentage, out response))
                    return false;
                if (sbm.CheckMa200AndMa20Percentage &&
                    !data.IsPercentageSma200AndSma20OkayOverbought(sbm.Ma200AndMa20Percentage, out response))
                    return false;
                if (sbm.CheckMa50AndMa20Percentage &&
                    !data.IsPercentageSma50AndSma20OkayOverbought(sbm.Ma50AndMa20Percentage, out response))
                    return false;

                if (!CheckMaCrossings(
                    sbm.Ma200AndMa20Crossing, sbm.Ma200AndMa20Lookback,
                    sbm.Ma200AndMa50Crossing, sbm.Ma200AndMa50Lookback,
                    sbm.Ma50AndMa20Crossing, sbm.Ma50AndMa20Lookback,
                    out response))
                    return false;
            }
        }

        // Controle op de RSI
        if (NwePlugin.Settings.IncludeRsi)
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

        if (HadStorsiInThelastXCandles(SignalSide, 0, 10, 4) == null && HadStobbInThelastXCandles(SignalSide, 0, 10, StobbPlugin.Settings.UseLowHigh) == null)
        {
            response = "no previous storsi/stobb found";
            return false;
        }

        if (NwePlugin.Settings.RequireVolumeClimax && !HasVolumeClimax(out response))
            return false;

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(StobbPlugin.Settings.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        ExtraText = "";

        var nwe = NweResultCache.GetOrCalculate(
            SymbolInterval.CandleList,
            NwePlugin.Settings.BandWidth,
            NwePlugin.Settings.Multiplication,
            SmoothRepainting);
        var nweLast = nwe[^1];

        // buy alert
        if (SignalSide == CryptoTradeSide.Long && nweLast.Lower != null)
        {
            // Candle outside the band
            decimal? lowerband = nweLast.Lower;
            if (CandleLast!.Candle.Close < lowerband && CandleLast!.Candle.Open < lowerband
                && CandleLast.Candle.Close > CandleLast!.Candle.Open)
            {
                ExtraText = $"{nweLast.OpenTime.ToLocalTime():HH:mm} c={CandleLast!.Candle.Close.ToString(Symbol.PriceDisplayFormat)} o={CandleLast!.Candle.Open.ToString(Symbol.PriceDisplayFormat)} b={lowerband?.ToString(Symbol.PriceDisplayFormat)}";
                return true;
            }
        }

        // sell alert
        if (SignalSide == CryptoTradeSide.Short && nweLast.Upper != null)
        {
            // Candle outside the band
            decimal? upperband = nweLast.Upper;
            if (CandleLast!.Candle.Close > upperband && CandleLast!.Candle.Open > upperband
                && CandleLast.Candle.Close < CandleLast!.Candle.Open)
            {
                ExtraText = $"{nweLast.OpenTime.ToLocalTime():HH:mm} c={CandleLast!.Candle.Close.ToString(Symbol.PriceDisplayFormat)} o={CandleLast!.Candle.Open.ToString(Symbol.PriceDisplayFormat)} b={upperband?.ToString(Symbol.PriceDisplayFormat)}";
                return true;
            }
        }

        return false;
    }

}