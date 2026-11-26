namespace CryptoScanner.Analyzers.ChochFvg;

/// <summary>One candle as the rules see it: only the prices.</summary>
public readonly record struct ChochFvgBar(decimal Open, decimal High, decimal Low, decimal Close);


/// <summary>
/// The entry the rules found: a limit on the middle of a fair value gap, a stop under (above for a
/// short) the candle that made the gap, and a target at RiskReward times the risk.
/// Indexes point into the bar list that was handed over.
/// </summary>
public sealed record ChochFvgSetup(
    decimal Entry,
    decimal Stop,
    decimal Target,
    decimal LegStart,
    decimal LegEnd,
    decimal BrokenSwing,
    int LegStartIndex,
    int SwingIndex,
    int GapIndex)
{
    /// <summary>The distance from the entry to the stop as a percentage of the entry (0..100).</summary>
    public decimal StopPercentage => 100m * Math.Abs(Entry - Stop) / Entry;

    /// <summary>The distance from the entry to the target as a percentage of the entry (0..100).</summary>
    public decimal TargetPercentage => 100m * Math.Abs(Target - Entry) / Entry;
}


/// <summary>
/// The rules of the change-of-character entry with a fair value gap, free of the scanner so they can
/// be tested on a plain list of candles. Long, on the last candle of the list:
/// <list type="number">
/// <item>Downtrend: the lowest low of the lookback window is a lower low, and the swing high before
/// it is a lower high than the swing high before that one.</item>
/// <item>Change of character: the last candle is the FIRST close above that last lower high since
/// the low.</item>
/// <item>The leg runs from that low to the highest high since. The entry zone lies between the
/// FibZoneStart and FibZoneEnd retracement of the leg (50 and 61.8 by default).</item>
/// <item>A bullish fair value gap inside the leg (the high of candle 1 under the low of candle 3)
/// whose middle lies in the zone and has not been traded yet. With several, the highest one, the
/// one the price reaches first.</item>
/// <item>Entry on the middle of the gap, stop under the low of the middle candle of the gap, target
/// RiskReward times the risk above the entry.</item>
/// <item>A stop closer to the entry than MinimumStopPercentage drops the setup: on such a stop the
/// fees eat most of the risk.</item>
/// </list>
/// A short is the mirror image; it is computed by mirroring the prices and running the long rules.
/// </summary>
public static class ChochFvgRules
{
    public static bool TryFindSetup(IReadOnlyList<ChochFvgBar> bars, bool isLong, int swingStrength, int lookbackCandles,
        decimal fibZoneStart, decimal fibZoneEnd, decimal riskReward, out ChochFvgSetup? setup, out string reason,
        decimal minimumStopPercentage = 0m)
    {
        if (!TryFindSide(bars, isLong, swingStrength, lookbackCandles, fibZoneStart, fibZoneEnd, riskReward, out setup, out reason))
            return false;

        if (setup!.StopPercentage < minimumStopPercentage)
        {
            reason = $"stop {setup.StopPercentage:N3}% under the minimum of {minimumStopPercentage:N3}%";
            setup = null;
            return false;
        }

        return true;
    }


    private static bool TryFindSide(IReadOnlyList<ChochFvgBar> bars, bool isLong, int swingStrength, int lookbackCandles,
        decimal fibZoneStart, decimal fibZoneEnd, decimal riskReward, out ChochFvgSetup? setup, out string reason)
    {
        if (isLong)
            return TryFindLong(bars, swingStrength, lookbackCandles, fibZoneStart, fibZoneEnd, riskReward, out setup, out reason);

        // Mirror the prices: a high becomes a low and the other way around
        ChochFvgBar[] mirrored = new ChochFvgBar[bars.Count];
        for (int i = 0; i < bars.Count; i++)
        {
            ChochFvgBar bar = bars[i];
            mirrored[i] = new ChochFvgBar(-bar.Open, -bar.Low, -bar.High, -bar.Close);
        }

        if (!TryFindLong(mirrored, swingStrength, lookbackCandles, fibZoneStart, fibZoneEnd, riskReward, out ChochFvgSetup? found, out reason))
        {
            setup = null;
            return false;
        }

        setup = found! with
        {
            Entry = -found!.Entry,
            Stop = -found.Stop,
            Target = -found.Target,
            LegStart = -found.LegStart,
            LegEnd = -found.LegEnd,
            BrokenSwing = -found.BrokenSwing,
        };
        return true;
    }


    private static bool IsSwingHigh(IReadOnlyList<ChochFvgBar> bars, int index, int strength, int last)
    {
        if (index - strength < 0 || index + strength > last)
            return false;
        decimal high = bars[index].High;
        for (int j = index - strength; j <= index + strength; j++)
        {
            if (j == index)
                continue;
            if (bars[j].High >= high)
                return false;
        }
        return true;
    }


    private static int PreviousSwingHigh(IReadOnlyList<ChochFvgBar> bars, int before, int strength, int last)
    {
        for (int i = before - 1; i >= 0; i--)
        {
            if (IsSwingHigh(bars, i, strength, last))
                return i;
        }
        return -1;
    }


    private static bool TryFindLong(IReadOnlyList<ChochFvgBar> bars, int swingStrength, int lookbackCandles,
        decimal fibZoneStart, decimal fibZoneEnd, decimal riskReward, out ChochFvgSetup? setup, out string reason)
    {
        setup = null;
        int last = bars.Count - 1;
        if (last < lookbackCandles || last < 2 * swingStrength + 3)
        {
            reason = "not enough candles yet";
            return false;
        }

        // 1. The lowest low of the window, not on the last candle
        int windowStart = last - lookbackCandles;
        int low = windowStart;
        for (int i = windowStart + 1; i < last; i++)
        {
            if (bars[i].Low < bars[low].Low)
                low = i;
        }

        // The last swing high before that low, and the one before it
        int swing = PreviousSwingHigh(bars, low, swingStrength, last);
        if (swing < 0)
        {
            reason = "no swing high before the low";
            return false;
        }
        int swingBefore = PreviousSwingHigh(bars, swing, swingStrength, last);
        if (swingBefore < 0)
        {
            reason = "no second swing high before the low";
            return false;
        }

        decimal swingHigh = bars[swing].High;
        if (swingHigh >= bars[swingBefore].High)
        {
            reason = "no lower high, no downtrend";
            return false;
        }

        decimal lowBetween = decimal.MaxValue;
        for (int i = swingBefore; i <= swing; i++)
            lowBetween = Math.Min(lowBetween, bars[i].Low);
        if (bars[low].Low >= lowBetween)
        {
            reason = "no lower low, no downtrend";
            return false;
        }

        // 2. The last candle has to be the first close above the lower high since the low
        if (bars[last].Close <= swingHigh)
        {
            reason = "no close above the last lower high";
            return false;
        }
        for (int i = low + 1; i < last; i++)
        {
            if (bars[i].Close > swingHigh)
            {
                reason = "the change of character came earlier";
                return false;
            }
        }

        // 3. The leg and the entry zone
        decimal legStart = bars[low].Low;
        decimal legEnd = bars[low].High;
        for (int i = low + 1; i <= last; i++)
            legEnd = Math.Max(legEnd, bars[i].High);
        decimal range = legEnd - legStart;
        if (range <= 0)
        {
            reason = "leg without height";
            return false;
        }
        decimal zoneTop = legEnd - range * Math.Min(fibZoneStart, fibZoneEnd) / 100m;
        decimal zoneBottom = legEnd - range * Math.Max(fibZoneStart, fibZoneEnd) / 100m;

        // 4. The highest untraded bullish gap of the leg with its middle in the zone
        int gap = -1;
        decimal entry = 0;
        bool anyGap = false;
        for (int i = low + 2; i <= last; i++)
        {
            decimal gapBottom = bars[i - 2].High;
            decimal gapTop = bars[i].Low;
            if (gapBottom >= gapTop)
                continue;
            anyGap = true;

            decimal middle = (gapBottom + gapTop) / 2m;
            if (middle < zoneBottom || middle > zoneTop)
                continue;

            bool traded = false;
            for (int j = i + 1; j <= last; j++)
            {
                if (bars[j].Low <= middle)
                {
                    traded = true;
                    break;
                }
            }
            if (traded)
                continue;

            if (gap < 0 || middle > entry)
            {
                gap = i;
                entry = middle;
            }
        }

        if (gap < 0)
        {
            reason = anyGap ? "no untraded gap with its middle in the fib zone" : "no fair value gap in the leg";
            return false;
        }

        if (bars[last].Close <= entry)
        {
            reason = "the price is already under the entry";
            return false;
        }

        // 5. Stop under the candle that made the gap, target at RiskReward times the risk
        decimal stop = bars[gap - 1].Low;
        if (stop >= entry)
        {
            reason = "stop not under the entry";
            return false;
        }
        decimal target = entry + riskReward * (entry - stop);

        setup = new ChochFvgSetup(entry, stop, target, legStart, legEnd, swingHigh, low, swing, gap);
        reason = "";
        return true;
    }
}
