using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Fires a Long signal on a bullish CHoCH (Change of Character): a Higher High that breaks
/// the previous bearish structure, switching the BOS/CHoCH trend to Bullish.
///
/// Uses TrendBos which reacts faster than Dow Theory (single structural break is sufficient).
///
/// Startup safety: the PrevTime + Duration == Time check ensures the transition was detected
/// on consecutive candles, preventing signals from firing on historical data at startup.
/// </summary>
public class SignalBosChochLong : SignalCreateBase
{
    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendBos;
        if (data.PrevTime != null && data.PrevTime > 0 &&
            data.PrevTime + Interval.Duration == data.Time &&
            data.PrevTrend == CryptoTrendIndicator.Bearish && data.Trend == CryptoTrendIndicator.Bullish)
        {
            // Prevent duplicate signals: only fire once per trend change.
            // LastTrend is reset when the opposite signal fires (SignalBosChochShort).
            if (data.LastTrend != CryptoTrendIndicator.Bullish)
            {
                ExtraText = "CHoCH Long";
                data.LastTrend = data.Trend;
                return true;
            }
        }

        ExtraText = "no CHoCH";
        return false;
    }
}
