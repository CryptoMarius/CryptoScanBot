using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Analyzers.Trend.Signal;

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
        // Run the shared trader gates first (WaitForStochRecovery, WaitForRsiRecovery, CheckFurtherPriceMove,
        // CheckIncreasingRsi/Stoch/Macd, CheckTrendPrimaryDirection, …). Without this call
        // a Trend-strategy entry would silently bypass every Settings.Trading.Check* flag
        // the user enabled in the trader UI.
        if (!base.AllowStepIn(signal))
            return false;

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
    /// Give up when the primary trend has reverted to Bearish, or when the pullback pivot has
    /// formed but resumption above it still hasn't happened GiveUpCandles candles later.
    /// While we're still waiting for the pullback pivot itself to form, there is no time limit —
    /// pivot formation timing is unpredictable (and itself lags a few candles behind the actual
    /// swing due to the ZigZag look-right confirmation), so a fixed budget counted from the
    /// signal candle was expiring before AllowStepIn ever got a real chance. The trend-revert
    /// check above remains the safety net for that waiting phase.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Trend has already flipped back — setup is invalidated
        if (SymbolInterval.TrendPrimary.Trend == CryptoTrendIndicator.Bearish)
        {
            ExtraText = "primary trend reverted to bearish";
            return true;
        }

        CryptoTrendData trend = SymbolInterval.TrendPrimary;
        CandleTime signalTime = CandleTime.FromDateTime(signal.CloseDate);

        // Still waiting for the pullback pivot (ZigZag Low) to form after the signal — no
        // time limit here, only the trend-revert check above can cancel the setup.
        if (trend.LastPivotType != 'L' || trend.LastPivotTime <= signalTime)
            return false;

        // Pivot has formed — now give up if resumption above it hasn't happened within
        // GiveUpCandles candles, counted from the pivot itself, not from the original signal.
        CandleTime expiry = trend.LastPivotTime!.Value + GiveUpCandles * Interval.Duration;
        if (CandleLast.Candle.OpenTime >= expiry)
        {
            ExtraText = $"give up {GiveUpCandles} candles after pullback pivot";
            return true;
        }

        return false;
    }
}
