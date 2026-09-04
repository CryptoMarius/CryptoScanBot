using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.FailedBreakout.Signal;

/// <summary>
/// The break that did not hold. A short fires when price pushed above the highest high of the
/// lookback window and then closed back under it; a long when it dropped under the lowest low and
/// closed back above.
/// <para>
/// Reads nothing but the candles - no indicators - and walks back
/// <c>BreakWithinCandles + LookbackCandles</c> bars, the same order of work as the helpers already
/// in production (HadStorsiInThelastXCandles walks 25).
/// </para>
/// </summary>
public class FailedBreakoutBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
        => data != null && data.Candle.OpenTime != 0;


    public override bool IsSignal()
    {
        ExtraText = "";
        FailedBreakoutSettings settings = FailedBreakoutPlugin.Settings;
        if (settings.LookbackCandles < 2 || settings.BreakWithinCandles < 1)
        {
            ExtraText = "lookback or break window not configured";
            return false;
        }

        // Before the candles, because it is by far the most selective of the two: most candles are
        // nowhere near a zone, and the level window below costs BreakWithinCandles + LookbackCandles
        // lookups. The zone is not the level this strategy builds itself - see RequireZone.
        if (!this.InsideARequiredZone(settings.RequireZone, settings.ZoneTolerancePercentage,
                out string zoneText))
        {
            ExtraText = zoneText;
            return false;
        }

        // Everything from the candle being evaluated back through the level window, oldest last.
        // Collected once: walking the same candles twice is the sort of thing that turns into the
        // dominant cost when it runs for every symbol on every candle.
        int needed = settings.BreakWithinCandles + settings.LookbackCandles;
        List<CryptoCandle> candles = new(needed);
        MyData? walk = CandleLast;
        while (candles.Count < needed && walk != null)
        {
            candles.Add(walk.Candle);
            if (!GetPrevCandle(walk, out walk))
                break;
        }
        if (candles.Count < needed)
        {
            ExtraText = $"not enough candles ({candles.Count} of {needed})";
            return false;
        }

        // The level comes from the candles BEFORE the break window, so the break itself never sets
        // the level it is supposed to have broken.
        decimal level = candles[settings.BreakWithinCandles].High;
        decimal levelLow = candles[settings.BreakWithinCandles].Low;
        for (int i = settings.BreakWithinCandles + 1; i < needed; i++)
        {
            if (candles[i].High > level)
                level = candles[i].High;
            if (candles[i].Low < levelLow)
                levelLow = candles[i].Low;
        }

        decimal range = level - levelLow;
        bool signal = SignalSide == CryptoTradeSide.Short
            ? BrokeAndCameBack(candles, settings, level, range, above: true)
            : BrokeAndCameBack(candles, settings, levelLow, range, above: false);

        // The zone only gets named on a signal. On a rejection ExtraText already says which of the
        // two tests said no, and that is the more useful half.
        if (signal && zoneText.Length > 0)
            ExtraText = $"{ExtraText}, {zoneText}";
        return signal;
    }


    /// <summary>
    /// Whether any candle in the break window pushed past the level far enough, and the candle being
    /// evaluated closed back on the original side of it - and close enough to it, measured as a
    /// share of <paramref name="range"/> (lookback high minus lookback low).
    /// </summary>
    private bool BrokeAndCameBack(List<CryptoCandle> candles, FailedBreakoutSettings settings,
        decimal level, decimal range, bool above)
    {
        if (level <= 0)
            return false;

        decimal margin = level * settings.MinimumBreakPercentage / 100m;
        decimal target = above ? level + margin : level - margin;

        bool broke = false;
        for (int i = 0; i < settings.BreakWithinCandles; i++)
        {
            if (above ? candles[i].High > target : candles[i].Low < target)
            {
                broke = true;
                break;
            }
        }
        if (!broke)
        {
            ExtraText = above ? "no break above the level" : "no break below the level";
            return false;
        }

        decimal close = candles[0].Close;
        if (above ? close >= level : close <= level)
        {
            ExtraText = "the break is still holding";
            return false;
        }

        // Where the close sits between the two levels, as seen from the one that was broken: 0 is
        // right on it, 100 is all the way at the other level. A break that is followed by a close
        // at the far side of the range is not a failed break any more, it is the move that came
        // after it - and the other side of this strategy is the one looking at that.
        decimal position = range > 0 ? 100m * Math.Abs(close - level) / range : 0m;
        if (settings.CloseWithinRangePercentage < 100m && position >= settings.CloseWithinRangePercentage)
        {
            ExtraText = $"close too far from the broken level ({position:N0}% of the range)";
            return false;
        }

        decimal beyond = 100m * Math.Abs(close - level) / level;
        ExtraText = $"failed break of {level}, back inside by {beyond:N2}% ({position:N0}% of the range)";
        return true;
    }
}


public class FailedBreakoutLong : FailedBreakoutBase
{
}


public class FailedBreakoutShort : FailedBreakoutBase
{
}
