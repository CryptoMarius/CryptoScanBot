using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

public class SignalTrendShort : SignalCreateBase
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
            data.PrevTrend == CryptoTrendIndicator.Bullish && data.Trend == CryptoTrendIndicator.Bearish)
        {
            // Prevent duplicate signals: only fire once per trend change.
            // LastTrend is reset to a different value when the opposite signal fires (SignalTrendLong).
            if (data.LastTrend != CryptoTrendIndicator.Bearish)
            {
                // Note: data.Trend == Unknown is unreachable here (outer check already requires Bearish).
                ExtraText = "Going bearish";
                data.LastTrend = data.Trend;
                return true;
            }
        }

        ExtraText = "no trend change";
        return false;
    }


    /// <summary>
    /// Allow step-in once a pullback pivot (ZigZag High) has formed after the signal
    /// and the current candle closes below that pivot — confirming the resumption downward.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Run the shared trader gates first — see SignalTrendLong for the rationale.
        if (!base.AllowStepIn(signal))
            return false;

        // Recalculate so LastPivot reflects the current bar
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData trend = SymbolInterval.TrendPrimary;
        CandleTime signalTime = CandleTime.FromDateTime(signal.CloseDate);

        // Wait for a ZigZag High to form after the signal (= the pullback pivot)
        if (trend.LastPivotType != 'H' || trend.LastPivotTime <= signalTime)
        {
            ExtraText = "waiting for pullback pivot (ZigZag High)";
            return false;
        }

        // Current candle must close below the pullback pivot (resuming downward)
        if (CandleLast.Candle.Close >= trend.LastPivotValue)
        {
            ExtraText = $"price {CandleLast.Candle.Close:N8} not below pivot high {trend.LastPivotValue:N8}";
            return false;
        }

        // Current candle must be bearish (close < open)
        if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
        {
            ExtraText = "no bearish candle";
            return false;
        }

        return true;
    }


    /// <summary>
    /// Give up when the primary trend has reverted to Bullish, or when the pullback pivot has
    /// formed but resumption below it still hasn't happened GiveUpCandles candles later.
    /// While we're still waiting for the pullback pivot itself to form, there is no time limit —
    /// see SignalTrendLong for the rationale (ZigZag look-right confirmation lag makes a
    /// fixed budget counted from the signal candle expire before AllowStepIn gets a real chance).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Trend has already flipped back — setup is invalidated
        if (SymbolInterval.TrendPrimary.Trend == CryptoTrendIndicator.Bullish)
        {
            ExtraText = "primary trend reverted to bullish";
            return true;
        }

        CryptoTrendData trend = SymbolInterval.TrendPrimary;
        CandleTime signalTime = CandleTime.FromDateTime(signal.CloseDate);

        // Still waiting for the pullback pivot (ZigZag High) to form after the signal — no
        // time limit here, only the trend-revert check above can cancel the setup.
        if (trend.LastPivotType != 'H' || trend.LastPivotTime <= signalTime)
            return false;

        // Pivot has formed — now give up if resumption below it hasn't happened within
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
