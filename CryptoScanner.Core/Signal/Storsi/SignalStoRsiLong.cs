using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Storsi;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiLong : SignalStoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        if (GlobalData.Settings.Signal.StoRsi.OnlyIfLux5m)
        {
            if (CandleLast.CandleData!.Lux5mValue > -50)
            {
                response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%)";
                return false;
            }
        }


        // Check above/below STOBB BB bands
        if (GlobalData.Settings.Signal.StoRsi.CheckBollingerBandsCondition)
        {
            //if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseHighLow))
            if (!InLowerPartOfBollingerBands(3, 5.0m))
            {
                response = "not in lower part of bb";
                return false;
            }
        }

        if (GlobalData.Settings.Signal.StoRsi.SkipFirstSignal)
        {
            if (HadStorsiInThelastXCandles(SignalSide, 1, 3) == null)
            {
                response = "skip first storsi";
                return false;
            }
        }

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        if (!CandleLast.RsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
        {
            ExtraText = "rsi not oversold";
            return false;
        }

        if (GlobalData.Settings.Signal.StoRsi.CheckTrendDirection)
        {
            _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;
            var period = Interval.IntervalPeriod;
            if (period < CryptoIntervalPeriod.interval5m)
                period = CryptoIntervalPeriod.interval5m;
            var primary = Symbol.GetSymbolInterval(period).TrendPrimary.Trend;
            if (primary != CryptoTrendIndicator.Bullish)
            {
                ExtraText = $"TrendPrimary {primary}, need Bullish";
                return false;
            }
        }

        ExtraText = "";
        return true;
    }

}
