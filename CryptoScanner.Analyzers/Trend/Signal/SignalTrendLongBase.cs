using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Analyzers.Trend.Signal;

/// <summary>
/// Long signal on a trend flip of the ZigZag-derived trend, followed by a pullback pivot and a
/// resumption through it. Subclasses pick the trend slot (Primary / Secondary) that is read:
/// the primary ZigZag only accepts a pivot once the previous box is broken (rough trend), the
/// secondary accepts every new high/low as a pivot (fine trend), so it flips more often and its
/// pullback pivots form sooner.
/// </summary>
public abstract class SignalTrendLongBase : SignalCreateBase
{
    // Maximum number of candles to wait for pullback + resumption before giving up
    private const int GiveUpCandles = 5;

    /// <summary>Selects which trend slot (Primary vs Secondary) this signal reads from.</summary>
    protected abstract TrendType TrendType { get; }

    private SettingsZigZag TrendSettings => TrendType == TrendType.Primary
        ? GlobalData.Settings.Trend.Primary
        : GlobalData.Settings.Trend.Secondary;

    private CryptoTrendData GetTrend() => TrendType == TrendType.Primary
        ? SymbolInterval.TrendPrimary
        : SymbolInterval.TrendSecondary;

    private string TrendLabel => TrendType == TrendType.Primary ? "primary" : "secondary";

    /// <summary>
    /// The trend state the signal is armed on: bullish normally, bearish when
    /// <see cref="TrendSettings.InvertDirection"/> is on.
    /// </summary>
    private static CryptoTrendIndicator ArmedOn => TrendPlugin.Settings.InvertDirection
        ? CryptoTrendIndicator.Bearish
        : CryptoTrendIndicator.Bullish;

    /// <summary>The opposite of <paramref name="trend"/> - the state that invalidates the setup and,
    /// once the position is open, ends it.</summary>
    private static CryptoTrendIndicator ArmedAgainst(CryptoTrendIndicator trend) =>
        trend == CryptoTrendIndicator.Bullish ? CryptoTrendIndicator.Bearish : CryptoTrendIndicator.Bullish;


    /// <summary>
    /// The strategy leaves on its own only when the setting asks for it; off, the position lives
    /// entirely on the global stop loss and take profit, the behaviour up to and including run 1036.
    /// </summary>
    public override bool HasExitSignal => TrendPlugin.Settings.ExitOnTrendRevert;


    /// <summary>
    /// Out once the trend this signal entered on stands bearish again. Deliberately "stands
    /// bearish" and not "flipped on this candle": after a flip it stays bearish until the next one,
    /// so a candle the monitor did not get to see is not a lost exit.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        if (!TrendPlugin.Settings.ExitOnTrendRevert)
            return false;

        CryptoTrendIndicator against = ArmedAgainst(ArmedOn);
        if (GetTrend().Trend != against)
        {
            ExtraText = $"{TrendLabel} trend not against the position";
            return false;
        }

        ExtraText = $"{TrendLabel} trend stands {against.ToString().ToLowerInvariant()}";
        return true;
    }


    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, TrendSettings).Result;

        // Which flip arms the long. Normally the flip TO bullish (enter with the new trend); with
        // TrendSettings.InvertDirection the flip TO bearish, so the long buys the bounce instead.
        CryptoTrendIndicator armOn = ArmedOn;
        CryptoTrendIndicator armFrom = ArmedAgainst(armOn);

        CryptoTrendData data = GetTrend();
        if (data.PrevTime != null && data.PrevTime > 0 &&
            data.PrevTime + Interval.Duration == data.Time &&
            data.PrevTrend == armFrom && data.Trend == armOn)
        {
            // Prevent duplicate signals: only fire once per trend change.
            // LastTrend is reset to a different value when the opposite signal fires (SignalTrendShortBase).
            if (data.LastTrend != armOn)
            {
                // Note: data.Trend == Unknown is unreachable here (outer check already requires armOn).
                ExtraText = armOn == CryptoTrendIndicator.Bullish ? "Going bullish" : "Going bearish (inverted)";
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
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, TrendSettings).Result;

        CryptoTrendData trend = GetTrend();
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
    /// Give up when the trend has reverted to Bearish, or when the pullback pivot has
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
        CryptoTrendIndicator against = ArmedAgainst(ArmedOn);
        if (GetTrend().Trend == against)
        {
            ExtraText = $"{TrendLabel} trend reverted to {against.ToString().ToLowerInvariant()}";
            return true;
        }

        CryptoTrendData trend = GetTrend();
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
