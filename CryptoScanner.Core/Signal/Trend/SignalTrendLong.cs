using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

public class SignalTrendLong : SignalCreateBase
{
    // Maximum number of candles to wait for pullback + resumption before giving up
    private const int GiveUpCandles = 5;


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


    /// <summary>
    /// Allow step-in once a pullback pivot (ZigZag Low) has formed after the signal
    /// and the current candle closes above that pivot — confirming the resumption upward.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Recalculate so LastPivot reflects the current bar
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData trend = SymbolInterval.TrendPrimary;
        CandleTime signalTime = CandleTime.FromDateTime(signal.CloseDate);

        // Wait for a ZigZag Low to form after the signal (= the pullback pivot)
        if (trend.LastPivotType != 'L' || trend.LastPivotTime <= signalTime)
        {
            ExtraText = "waiting for pullback pivot (ZigZag Low)";
            return false;
        }

        // Current candle must close above the pullback pivot (resuming upward)
        if (CandleLast.Candle.Close <= trend.LastPivotValue)
        {
            ExtraText = $"price {CandleLast.Candle.Close:N8} not above pivot low {trend.LastPivotValue:N8}";
            return false;
        }

        // Current candle must be bullish (close > open)
        if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
        {
            ExtraText = "no bullish candle";
            return false;
        }

        return true;
    }


    /// <summary>
    /// Give up when the primary trend has reverted to Bearish, or when GiveUpCandles have passed
    /// without a valid pullback + resumption entry.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Trend has already flipped back — setup is invalidated
        if (SymbolInterval.TrendPrimary.Trend == CryptoTrendIndicator.Bearish)
        {
            ExtraText = "primary trend reverted to bearish";
            return true;
        }

        // Time limit exceeded (same fix as SignalCreateBase.GiveUp — count from signal OPEN
        // and use >= so the signal is removed when GiveUpCandles full candles have elapsed,
        // not GiveUpCandles+2 like the old condition did).
        long expiryOpenMinutes = CandleTime.FromDateTime(signal.OpenDate).Minutes + GiveUpCandles * Interval.Duration;
        if (CandleLast.Candle.OpenTime.Minutes >= expiryOpenMinutes)
        {
            ExtraText = $"give up after {GiveUpCandles} candles";
            return true;
        }

        return false;
    }
}
