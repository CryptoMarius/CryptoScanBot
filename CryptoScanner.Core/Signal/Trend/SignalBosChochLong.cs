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
/// The signal is tied to the swing-point candle at which the break occurred
/// (LastStructureEventTime / LastStructureEventPrice), not to the candle on which the
/// trend calculation happens to run. This keeps SignalPrice aligned with the break
/// that is visible on the chart. LastFiredStructureEventTime prevents re-firing on the
/// same event across consecutive calculations.
/// </summary>
public class SignalBosChochLong : SignalCreateBase
{
    // Maximum number of candles to wait for pullback + resumption before giving up
    private const int GiveUpCandles = 10;

    // Startup safety: only fire if the break happened within this many intervals of the
    // current candle. Prevents signalling historical CHoCHs that the engine first sees
    // after a restart.
    // Must absorb the ZigZag pivot-confirmation delay (5 candles lookback in
    // ZigZagLanceBeggs.CheckNewHigh/CheckNewLow) plus a few bars of slack, otherwise every
    // freshly confirmed CHoCH would already be "too old" by the time this signal sees it.
    private const int MaxEventAgeCandles = 10;


    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendBos;

        // A bullish CHoCH event must be present on the swing sequence.
        if (data.LastStructureEvent != CryptoStructureEvent.ChoCh ||
            data.LastStructureEventTime == null ||
            data.LastStructureEventPrice == null ||
            data.Trend != CryptoTrendIndicator.Bullish)
        {
            ExtraText = "no CHoCH";
            return false;
        }

        // Don't fire twice on the same event.
        if (data.LastFiredStructureEventTime != null &&
            data.LastFiredStructureEventTime >= data.LastStructureEventTime)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        // Reject stale events (e.g. when the bot has just started and the last CHoCH
        // is already many candles old).
        CandleTime cutoff = CandleLast.Candle.OpenTime - MaxEventAgeCandles * Interval.Duration;
        if (data.LastStructureEventTime < cutoff)
        {
            ExtraText = "CHoCH too old";
            return false;
        }

        ExtraText = $"CHoCH Long @ {data.LastStructureEventPrice}";
        data.LastFiredStructureEventTime = data.LastStructureEventTime;
        data.LastTrend = data.Trend;
        return true;
    }


    // Report the break-candle price (HH that broke the prior structure) so the signal
    // row matches what is visible on the chart.
    public override decimal? OverrideSignalPrice => SymbolInterval.TrendBos.LastStructureEventPrice;


    /// <summary>
    /// Allow step-in once a pullback pivot (ZigZag Low) has formed after the signal
    /// and the current candle closes above that pivot — confirming the resumption upward.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Recalculate so LastPivot reflects the current bar
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData trend = SymbolInterval.TrendBos;
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
    /// Give up when the BOS/CHoCH structure has reverted to Bearish, or when GiveUpCandles
    /// have passed without a valid pullback + resumption entry.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Structure has already broken back down — setup is invalidated
        if (SymbolInterval.TrendBos.Trend == CryptoTrendIndicator.Bearish)
        {
            ExtraText = "BOS/CHoCH structure reverted to bearish";
            return true;
        }

        // Time limit exceeded
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + GiveUpCandles * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = $"give up after {GiveUpCandles} candles";
            return true;
        }

        return false;
    }
}
