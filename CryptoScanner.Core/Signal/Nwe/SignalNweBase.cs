using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Core.Signal.Nwe;

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
        int lookback = GlobalData.Settings.Signal.Nwe.VolumeClimaxLookback;
        decimal multiplier = GlobalData.Settings.Signal.Nwe.VolumeClimaxMultiplier;

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
        // BUGFIX: outer guard previously was `OnlyIfLux5m && SignalSide == Long`, which made
        // the Short branch below unreachable — NWE-Short skipped the Lux gate entirely while
        // StoRsi and Stobb did filter both sides. Aligned with the other two strategies.
        if (GlobalData.Settings.Signal.Nwe.OnlyIfLux5m)
        {
            int needed = GlobalData.Settings.Signal.Nwe.Lux5mPercentage;
            if (SignalSide == CryptoTradeSide.Long)
            {
                if (CandleLast.CandleData!.Lux5mValue > -needed)
                {
                    response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%, need <= -{needed}%)";
                    return false;
                }
            }
            else if (SignalSide == CryptoTradeSide.Short)
            {
                if (CandleLast.CandleData!.Lux5mValue < needed)
                {
                    response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%, need >= {needed}%)";
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

        if (GlobalData.Settings.Signal.Nwe.RequireVolumeClimax && !HasVolumeClimax(out response))
            return false;

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

        NweIndicator indicator = new(
            bandwidth: (double)GlobalData.Settings.Signal.Nwe.BandWidth,
            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
            smoothRepainting: SmoothRepainting
           );
        var candles = SymbolInterval.CandleList;
        var nwe = indicator.Calculate(candles);
        var nweLast = nwe[^1];

        // buy alert
        if (SignalSide == CryptoTradeSide.Long && nweLast.Lower != null)
        {
            // Candle outside the band
            decimal? lowerband = nweLast.Lower;
            if (CandleLast!.Candle.Close < lowerband && CandleLast!.Candle.Open < lowerband
                && CandleLast.Candle.Close > CandleLast!.Candle.Open)
            {
                ExtraText = $"{nweLast.OpenTime.ToLocalTime():ddd yyyy-MM-dd HH:mm} c={CandleLast!.Candle.Close.ToString(Symbol.PriceDisplayFormat)} o={CandleLast!.Candle.Open.ToString(Symbol.PriceDisplayFormat)} b={lowerband?.ToString(Symbol.PriceDisplayFormat)}";
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
                ExtraText = $"{nweLast.OpenTime.ToLocalTime():ddd yyyy-MM-dd HH:mm} c={CandleLast!.Candle.Close.ToString(Symbol.PriceDisplayFormat)} o={CandleLast!.Candle.Open.ToString(Symbol.PriceDisplayFormat)} b={upperband?.ToString(Symbol.PriceDisplayFormat)}";
                return true;
            }
        }

        return false;
    }

}