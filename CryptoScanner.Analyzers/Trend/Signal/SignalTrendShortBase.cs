using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Analyzers.Trend.Signal;

/// <summary>
/// Short signal on a trend flip of the ZigZag-derived trend, followed by a pullback pivot and a
/// resumption through it. Subclasses pick the trend slot (Primary / Secondary) that is read:
/// the primary ZigZag only accepts a pivot once the previous box is broken (rough trend), the
/// secondary accepts every new high/low as a pivot (fine trend), so it flips more often and its
/// pullback pivots form sooner.
/// </summary>
public abstract class SignalTrendShortBase : SignalCreateBase
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
    /// The trend state the signal is armed on: bearish normally, bullish when
    /// <see cref="TrendSettings.InvertDirection"/> is on.
    /// </summary>
    private static CryptoTrendIndicator ArmedOn => TrendPlugin.Settings.InvertDirection
        ? CryptoTrendIndicator.Bullish
        : CryptoTrendIndicator.Bearish;

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
    /// Out once the trend this signal entered on stands bullish again. Deliberately "stands
    /// bullish" and not "flipped on this candle": after a flip it stays bullish until the next one,
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

        _ = SymbolTrend.CalculateSymbolTrendAsync(Symbol, TrendSettings).Result;

        // Which flip arms the short. Normally the flip TO bearish (enter with the new trend); with
        // TrendSettings.InvertDirection the flip TO bullish, so the short sells the pullback instead.
        CryptoTrendIndicator armOn = ArmedOn;
        CryptoTrendIndicator armFrom = ArmedAgainst(armOn);

        CryptoTrendData data = GetTrend();
        if (data.PrevTime != null && data.PrevTime > 0 &&
            data.PrevTime + Interval.Duration == data.Time &&
            data.PrevTrend == armFrom && data.Trend == armOn)
        {
            // Prevent duplicate signals: only fire once per trend change.
            // LastTrend is reset to a different value when the opposite signal fires (SignalTrendLongBase).
            if (data.LastTrend != armOn)
            {
                // Note: data.Trend == Unknown is unreachable here (outer check already requires armOn).
                ExtraText = armOn == CryptoTrendIndicator.Bearish ? "Going bearish" : "Going bullish (inverted)";
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
        // Run the shared trader gates first — see SignalTrendLongBase for the rationale.
        if (!base.AllowStepIn(signal))
            return false;

        // Recalculate so LastPivot reflects the current bar
        _ = SymbolTrend.CalculateSymbolTrendAsync(Symbol, TrendSettings).Result;

        CryptoTrendData trend = GetTrend();
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
    /// Give up when the trend has reverted to Bullish, or when the pullback pivot has
    /// formed but resumption below it still hasn't happened GiveUpCandles candles later.
    /// While we're still waiting for the pullback pivot itself to form, there is no time limit —
    /// see SignalTrendLongBase for the rationale (ZigZag look-right confirmation lag makes a
    /// fixed budget counted from the signal candle expire before AllowStepIn gets a real chance).
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
