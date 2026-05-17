using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

// Combined directional bias of one or more Higher Timeframes (HTF).
// Used by strategies that want to filter / align entries with the larger trend
// (Triple Screen / Dow tide). Built on top of the already-calculated TrendPrimary
// per symbol-interval — does not recalculate anything itself.
public enum HtfBias
{
    StrongBullish, // All checked HTFs bullish
    Bullish,       // At least one bullish, rest unknown (and no bears)
    Unknown,       // No HTF data available — TrendPrimary not yet calculated, or no pivots,
                   // or trading on the highest configured interval. Blocks both Long and Short
                   // when the filter is enabled (user asked to verify HTF alignment; if we
                   // cannot verify, we refuse rather than silently pass).
    Conflict,      // Some bullish, some bearish — non-blocking (let other filters decide)
    Bearish,       // At least one bearish, rest unknown (and no bulls)
    StrongBearish, // All checked HTFs bearish
}


public static class TrendBiasTools
{
    /// <summary>
    /// Resolve the combined Primary trend bias of the next <paramref name="levels"/> higher
    /// timeframes above <paramref name="currentPeriod"/>.
    ///
    /// Assumes MarketTrend.CalculateMarketTrendAsync has been invoked recently — it populates
    /// TrendPrimary on every interval. The caller usually runs that calculation themselves.
    /// However, the calculation can short-circuit (missing candles on a higher interval) or
    /// return CryptoTrendIndicator.Unknown (insufficient ZigZag pivots — common on new coins).
    /// In those cases the affected HTFs are skipped here and the verdict downgrades; if no
    /// HTF can be evaluated at all, the result is Unknown.
    /// </summary>
    public static HtfBias GetHtfBias(CryptoSymbol symbol, CryptoIntervalPeriod currentPeriod, int levels = 1)
        => GetHtfBias(symbol, currentPeriod, levels, out _);


    /// <summary>
    /// Overload that also returns a human-readable explanation listing the considered
    /// HTFs and their Primary trends. Useful for debug logging.
    /// </summary>
    public static HtfBias GetHtfBias(CryptoSymbol symbol, CryptoIntervalPeriod currentPeriod, int levels, out string explanation)
    {
        if (levels < 1)
            levels = 1;

        var higher = FindHigherIntervals(currentPeriod, levels);
        if (higher.Count == 0)
        {
            explanation = "no higher intervals available";
            return HtfBias.Unknown;
        }

        var details = new System.Text.StringBuilder();
        int bull = 0;
        int bear = 0;
        int evaluated = 0;
        foreach (var period in higher)
        {
            var symbolInterval = symbol.GetSymbolInterval(period);
            var trendData = symbolInterval.TrendPrimary;

            string state;
            // Time == null means CalculateAsync was never run for this interval (e.g. MarketTrend
            // returned early because an earlier interval was missing its LastCandle).
            // Trend == Unknown means it was run but produced no verdict (< 2 ZigZag pivots).
            if (trendData.Time == null)
                state = "not-calculated";
            else if (trendData.Trend == CryptoTrendIndicator.Unknown)
                state = "Unknown(no-pivots)";
            else
            {
                evaluated++;
                if (trendData.Trend == CryptoTrendIndicator.Bullish)
                    bull++;
                else if (trendData.Trend == CryptoTrendIndicator.Bearish)
                    bear++;
                state = trendData.Trend.ToString();
            }

            if (details.Length > 0)
                details.Append(", ");
            details.Append($"{period}={state}");
        }

        HtfBias result;
        if (evaluated == 0)
            result = HtfBias.Unknown;
        else if (bull == evaluated)
            result = evaluated >= 2 ? HtfBias.StrongBullish : HtfBias.Bullish;
        else if (bear == evaluated)
            result = evaluated >= 2 ? HtfBias.StrongBearish : HtfBias.Bearish;
        else
            result = HtfBias.Conflict;

        explanation = $"[{details}] -> {result}";
        return result;
    }


    /// <summary>
    /// True when bias agrees with going Long. Unknown blocks: the user enabled the filter,
    /// so we refuse to fire when we cannot verify alignment. Conflict is allowed through
    /// because both directions have HTF support — let other filters decide.
    /// </summary>
    public static bool AllowsLong(HtfBias bias)
        => bias == HtfBias.StrongBullish
        || bias == HtfBias.Bullish
        || bias == HtfBias.Conflict;


    /// <summary>
    /// True when bias agrees with going Short.
    /// </summary>
    public static bool AllowsShort(HtfBias bias)
        => bias == HtfBias.StrongBearish
        || bias == HtfBias.Bearish
        || bias == HtfBias.Conflict;


    private static List<CryptoIntervalPeriod> FindHigherIntervals(CryptoIntervalPeriod currentPeriod, int count)
    {
        // Walk the configured interval list (exchange-dependent) for the next intervals
        // whose enum value is strictly greater than currentPeriod. 1w is skipped to match
        // MarketTrend.CalculateMarketTrendAsync, which excludes it as well.
        var result = new List<CryptoIntervalPeriod>();
        foreach (var interval in GlobalData.IntervalList)
        {
            if (interval.IntervalPeriod <= currentPeriod)
                continue;
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                continue;
            result.Add(interval.IntervalPeriod);
            if (result.Count >= count)
                break;
        }
        return result;
    }
}
