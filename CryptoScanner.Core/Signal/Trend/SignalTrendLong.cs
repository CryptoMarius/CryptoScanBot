using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

public class SignalTrendLong : SignalCreateBase
{


    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendPrimary;
        if (data.PrevTime != null && data.PrevTime > 0 &&
            data.PrevTime + Interval.Duration == data.Time &&
            data.PrevTrend == CryptoTrendIndicator.Bearish && data.Trend == CryptoTrendIndicator.Bullish)
        {
            // Prevent duplicate signals: only fire once per trend change.
            // LastTrend is reset to a different value when the opposite signal fires (SignalTrendShort).
            if (data.LastTrend != CryptoTrendIndicator.Bullish)
            {
                // Note: data.Trend == Unknown is unreachable here (outer check already requires Bullish).
                ExtraText = "Going bullish";
                data.LastTrend = data.Trend;
                return true;
            }
        }

        ExtraText = "no trend change";
        return false;

    }

}
