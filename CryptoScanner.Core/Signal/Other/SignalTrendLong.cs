using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Other;

public class SignalTrendLong : SignalCreateBase
{


    public override bool IsSignal()
    {
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendPrimary;
        if (data.PrevTime != null && data.PrevTime > 0 &&
            data.PrevTime + Interval.Duration == data.Time &&
            data.PrevTrend == CryptoTrendIndicator.Bearish && data.Trend == CryptoTrendIndicator.Bullish)
        {
            if (data.LastTrend != CryptoTrendIndicator.Bullish)
            {
                if (data.Trend == CryptoTrendIndicator.Unknown)
                {
                    data.LastTrend = data.Trend;
                    return false;
                }
                else
                {
                    data.LastTrend = data.Trend;
                    return true;
                }
            }
        }

        ExtraText = "no trend change";
        return false;

    }

}
